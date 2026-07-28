#!/usr/bin/env bash
# Licensed to the .NET Core Community under one or more agreements.
# The .NET Core Community licenses this file to you under the MIT license.
#
# End-to-end test runner for Mocha (Tier 1: full docker-compose stack).
#
# Orchestrates the full cross-process chain:
#   1. Initialize the OTLP proto git submodule (required to build the generated protos).
#   2. docker compose up --build -d for the selected backend variant.
#   3. Wait for the OTLP gRPC port (4317) and Query HTTP port (5775) to accept connections.
#   4. Build and run the YAML-driven .NET runner (Mocha.EndToEnd): it discovers e2e/cases/*.yaml,
#      sends each case's OTLP payload and polls the Query API until every assertion passes.
#   5. On failure, dump container logs for diagnosis.
#   6. Tear the stack down (docker compose down -v), unless --keep is given.
#
# Usage:
#   e2e/run-e2e.sh [--backend litedb|mysql-influxdb] [--timeout <seconds>] [--keep] [--no-build-sender]
#
# Options:
#   --backend           Which storage backend to exercise. Default: litedb.
#                       litedb          -> docker/docker-compose.yml
#                       mysql-influxdb  -> docker/docker-compose-mysql-influxdb.yml
#   --timeout           Per-assertion poll timeout in seconds passed to the sender. Default: 30.
#   --keep              Do not tear the stack down after the run (for debugging).
#   --no-build-sender   Assume the sender is already built (skip "dotnet build").
#
# Exit code 0 => stack came up and every assertion passed.

set -euo pipefail

# --- Resolve paths ---------------------------------------------------------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

# --- Defaults --------------------------------------------------------------
BACKEND="litedb"
TIMEOUT_SECONDS="30"
KEEP_STACK="false"
BUILD_SENDER="true"

OTLP_ENDPOINT="http://localhost:4317"
QUERY_BASEURL="http://localhost:5775"
OTLP_PORT="4317"
QUERY_PORT="5775"
READINESS_TIMEOUT_SECONDS="180"

# --- Parse arguments -------------------------------------------------------
while [[ $# -gt 0 ]]; do
  case "$1" in
    --backend)
      BACKEND="${2:-}"
      shift 2
      ;;
    --timeout)
      TIMEOUT_SECONDS="${2:-}"
      shift 2
      ;;
    --keep)
      KEEP_STACK="true"
      shift
      ;;
    --no-build-sender)
      BUILD_SENDER="false"
      shift
      ;;
    -h|--help)
      grep '^#' "$0" | sed 's/^# \{0,1\}//'
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 2
      ;;
  esac
done

case "${BACKEND}" in
  litedb)
    BASE_COMPOSE="${REPO_ROOT}/docker/docker-compose.yml"
    ;;
  mysql-influxdb)
    BASE_COMPOSE="${REPO_ROOT}/docker/docker-compose-mysql-influxdb.yml"
    ;;
  *)
    echo "Unknown backend '${BACKEND}'. Use 'litedb' or 'mysql-influxdb'." >&2
    exit 2
    ;;
esac

OVERRIDE_COMPOSE="${SCRIPT_DIR}/docker-compose.e2e.yml"

# --- Choose docker compose command ----------------------------------------
if docker compose version >/dev/null 2>&1; then
  COMPOSE=(docker compose)
elif command -v docker-compose >/dev/null 2>&1; then
  COMPOSE=(docker-compose)
else
  echo "Neither 'docker compose' nor 'docker-compose' is available." >&2
  exit 3
fi

COMPOSE_FILES=(-f "${BASE_COMPOSE}" -f "${OVERRIDE_COMPOSE}")

echo "=============================================="
echo " Mocha end-to-end run"
echo "   backend       : ${BACKEND}"
echo "   base compose  : ${BASE_COMPOSE}"
echo "   override      : ${OVERRIDE_COMPOSE}"
echo "   assert timeout: ${TIMEOUT_SECONDS}s"
echo "=============================================="

# --- Teardown handling -----------------------------------------------------
teardown() {
  if [[ "${KEEP_STACK}" == "true" ]]; then
    echo "--- Leaving stack running (--keep). Tear down with:"
    echo "    ${COMPOSE[*]} ${COMPOSE_FILES[*]} down -v"
    return
  fi
  echo "--- Tearing down stack (docker compose down -v)..."
  "${COMPOSE[@]}" "${COMPOSE_FILES[@]}" down -v --remove-orphans || true
  # Remove the host bind-mounted LiteDB data so subsequent runs start clean.
  # The distributor/query containers write these files as root, so a plain host-side
  # "rm" would fail on a non-root workstation; delete them from inside a throwaway
  # container that runs as root, then drop the (now empty) directory.
  local litedb_data="${REPO_ROOT}/docker/litedb_data"
  if [[ -d "${litedb_data}" ]]; then
    docker run --rm -v "${litedb_data}:/data" busybox:latest sh -c 'rm -rf /data/* /data/.* 2>/dev/null || true' \
      >/dev/null 2>&1 || true
    rmdir "${litedb_data}" 2>/dev/null || rm -rf "${litedb_data}" 2>/dev/null || true
  fi
}

dump_logs_on_failure() {
  echo "--- Capturing container logs for diagnosis ---"
  "${COMPOSE[@]}" "${COMPOSE_FILES[@]}" ps || true
  "${COMPOSE[@]}" "${COMPOSE_FILES[@]}" logs --no-color --tail 200 || true
}

# --- Wait for a TCP port to accept connections -----------------------------
wait_for_port() {
  local host="$1"
  local port="$2"
  local label="$3"
  local deadline=$(( SECONDS + READINESS_TIMEOUT_SECONDS ))

  echo "--- Waiting for ${label} (${host}:${port}) ..."
  while (( SECONDS < deadline )); do
    if (exec 3<>"/dev/tcp/${host}/${port}") 2>/dev/null; then
      exec 3>&- 2>/dev/null || true
      echo "    ${label} is accepting connections."
      return 0
    fi
    sleep 2
  done

  echo "    Timed out waiting for ${label} after ${READINESS_TIMEOUT_SECONDS}s." >&2
  return 1
}

# --- 1. Submodule ----------------------------------------------------------
echo "--- Ensuring OTLP proto submodule is initialized..."
git -C "${REPO_ROOT}" submodule update --init --recursive

# --- 2. Build the sender (fail fast before spinning up docker) -------------
if [[ "${BUILD_SENDER}" == "true" ]]; then
  echo "--- Building generated protos and the YAML-driven e2e runner..."
  dotnet build "${REPO_ROOT}/src/Mocha.Protocol.Generated/Mocha.Protocol.Generated.csproj" -c Release
  dotnet build "${REPO_ROOT}/e2e/Mocha.EndToEnd/Mocha.EndToEnd.csproj" -c Release
fi

# --- 3. Bring the stack up -------------------------------------------------
FAILED="false"
trap teardown EXIT

echo "--- Starting stack (docker compose up --build -d)..."
"${COMPOSE[@]}" "${COMPOSE_FILES[@]}" up --build -d

# --- 4. Wait for readiness -------------------------------------------------
if ! wait_for_port "localhost" "${OTLP_PORT}" "OTLP gRPC"; then
  FAILED="true"
fi
if [[ "${FAILED}" == "false" ]] && ! wait_for_port "localhost" "${QUERY_PORT}" "Query HTTP"; then
  FAILED="true"
fi

# --- 5. Run the sender + assertions ----------------------------------------
if [[ "${FAILED}" == "false" ]]; then
  echo "--- Running OTLP sender + Query assertions..."
  set +e
  MOCHA_E2E_OTLP_ENDPOINT="${OTLP_ENDPOINT}" \
  MOCHA_E2E_QUERY_BASEURL="${QUERY_BASEURL}" \
  MOCHA_E2E_TIMEOUT_SECONDS="${TIMEOUT_SECONDS}" \
    dotnet run --project "${REPO_ROOT}/e2e/Mocha.EndToEnd/Mocha.EndToEnd.csproj" -c Release --no-build
  SENDER_EXIT=$?
  set -e
  if [[ "${SENDER_EXIT}" -ne 0 ]]; then
    FAILED="true"
  fi
fi

# --- 6. Report -------------------------------------------------------------
if [[ "${FAILED}" == "true" ]]; then
  dump_logs_on_failure
  echo "=============================================="
  echo " END-TO-END RUN FAILED (backend: ${BACKEND})"
  echo "=============================================="
  exit 1
fi

echo "=============================================="
echo " END-TO-END RUN PASSED (backend: ${BACKEND})"
echo "=============================================="
