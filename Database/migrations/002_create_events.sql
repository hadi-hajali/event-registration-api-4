CREATE TABLE Events (
    Id BIGINT NOT NULL AUTO_INCREMENT,
    CategoryId BIGINT NOT NULL,
    Name VARCHAR(150) NOT NULL,
    Description VARCHAR(1000) NULL,
    Location VARCHAR(200) NOT NULL,
    StartAt DATETIME NOT NULL,
    EndAt DATETIME NOT NULL,
    RegistrationDeadline DATETIME NOT NULL,
    Capacity INT NOT NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT UTC_TIMESTAMP(),
    UpdatedAt DATETIME NULL,

    CONSTRAINT PK_Events PRIMARY KEY (Id),
    CONSTRAINT FK_Events_Categories
        FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    INDEX IX_Events_CategoryId (CategoryId),
    INDEX IX_Events_StartAt (StartAt),
    INDEX IX_Events_IsActive (IsActive)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;