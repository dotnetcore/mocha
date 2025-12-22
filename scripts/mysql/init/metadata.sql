-- Licensed to the .NET Core Community under one or more agreements.
-- The .NET Core Community licenses this file to you under the MIT license.

CREATE DATABASE IF NOT EXISTS mocha DEFAULT CHARACTER SET utf8mb4;

USE mocha;

CREATE TABLE IF NOT EXISTS span_metadata
(
    id           BIGINT       NOT NULL AUTO_INCREMENT PRIMARY KEY,
    service_name VARCHAR(255) NOT NULL,
    operation    VARCHAR(255) NOT NULL,
    INDEX idx_service_operation (service_name, operation)
);

CREATE TABLE IF NOT EXISTS metric_metadata
(
    id           BIGINT       NOT NULL AUTO_INCREMENT PRIMARY KEY,
    metric       VARCHAR(255) NOT NULL,
    service_name VARCHAR(255) NOT NULL,
    type         TINYINT      NOT NULL,
    description  TEXT,
    unit         VARCHAR(255) NOT NULL,
    INDEX idx_metric_service_name (metric, service_name)
);
