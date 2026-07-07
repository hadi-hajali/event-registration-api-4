-- 001_create_categories.sql
-- Creates the Categories table

CREATE TABLE Categories (
    Id          BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    Name        VARCHAR(100)    NOT NULL,
    Description VARCHAR(500)    NULL,
    IsActive    TINYINT(1)      NOT NULL DEFAULT 1,
    CreatedAt   DATETIME        NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UpdatedAt   DATETIME        NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY UQ_Categories_Name (Name)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;