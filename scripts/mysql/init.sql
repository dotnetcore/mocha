-- Licensed to the .NET Core Community under one or more agreements.
-- The .NET Core Community licenses this file to you under the MIT license.

CREATE DATABASE IF NOT EXISTS mocha DEFAULT CHARACTER SET utf8mb4;

USE mocha;

CREATE TABLE IF NOT EXISTS span
(
    id                   BIGINT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    trace_id             VARCHAR(255)    NOT NULL,
    span_id              VARCHAR(255)    NOT NULL,
    span_name            VARCHAR(255)    NOT NULL,
    parent_span_id       VARCHAR(255),
    start_time_unix_nano BIGINT UNSIGNED NOT NULL,
    end_time_unix_nano   BIGINT UNSIGNED NOT NULL,
    duration_nanoseconds BIGINT UNSIGNED NOT NULL,
    status_code          INT,
    status_message       VARCHAR(1024),
    span_kind            int             NOT NULL,
    service_name         VARCHAR(255)    NOT NULL,
    service_instance_id  VARCHAR(255)    NOT NULL,
    trace_flags          INT UNSIGNED    NOT NULL,
    trace_state          VARCHAR(1024),
    INDEX idx_trace_id (trace_id),
    INDEX idx_span_id (span_id)
);

CREATE TABLE IF NOT EXISTS span_attribute
(
    id         BIGINT       NOT NULL AUTO_INCREMENT PRIMARY KEY,
    trace_id   VARCHAR(255) NOT NULL,
    span_id    VARCHAR(255) NOT NULL,
    `key`      VARCHAR(255) NOT NULL,
    value_type int          NOT NULL,
    `value`    VARCHAR(255) NOT NULL,
    INDEX idx_trace_id (trace_id),
    INDEX idx_span_id (span_id),
    INDEX idx_key_value (`key`, `value`)
);

CREATE TABLE IF NOT EXISTS resource_attribute
(
    id         BIGINT       NOT NULL AUTO_INCREMENT PRIMARY KEY,
    trace_id   VARCHAR(255) NOT NULL,
    span_id    VARCHAR(255) NOT NULL,
    `key`      VARCHAR(255) NOT NULL,
    value_type int          NOT NULL,
    `value`    VARCHAR(255) NOT NULL
);

CREATE TABLE IF NOT EXISTS span_event
(
    Id                  BIGINT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    trace_id            VARCHAR(255)    NOT NULL,
    span_id             VARCHAR(255)    NOT NULL,
    `index`             int             NOT NULL,
    name                VARCHAR(255)    NOT NULL,
    timestamp_unix_nano BIGINT UNSIGNED NOT NULL,
    INDEX idx_trace_id (trace_id),
    INDEX idx_span_id (span_id)
);

CREATE TABLE IF NOT EXISTS span_event_attribute
(
    id               BIGINT       NOT NULL AUTO_INCREMENT PRIMARY KEY,
    trace_id         VARCHAR(255) NOT NULL,
    span_event_index int          NOT NULL,
    span_id          VARCHAR(255) NOT NULL,
    `key`            VARCHAR(255) NOT NULL,
    value_type       int          NOT NULL,
    `value`          VARCHAR(255) NOT NULL
);

CREATE TABLE IF NOT EXISTS span_link
(
    id                 BIGINT       NOT NULL AUTO_INCREMENT PRIMARY KEY,
    trace_id           VARCHAR(255) NOT NULL,
    span_id            VARCHAR(255) NOT NULL,
    `index`            int          NOT NULL,
    linked_trace_id    VARCHAR(255) NOT NULL,
    linked_span_id     VARCHAR(255) NOT NULL,
    linked_trace_state VARCHAR(1024),
    linked_trace_flags INT UNSIGNED,
    INDEX idx_trace_id (trace_id),
    INDEX idx_span_id (span_id)
);

CREATE TABLE IF NOT EXISTS span_link_attribute
(
    id              BIGINT       NOT NULL AUTO_INCREMENT PRIMARY KEY,
    trace_id        VARCHAR(255) NOT NULL,
    span_link_index int          NOT NULL,
    span_id         VARCHAR(255) NOT NULL,
    `key`           VARCHAR(255) NOT NULL,
    value_type      int          NOT NULL,
    `value`         VARCHAR(255) NOT NULL
);
