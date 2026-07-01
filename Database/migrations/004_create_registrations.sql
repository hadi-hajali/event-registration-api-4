CREATE TABLE Registrations (
    Id BIGINT NOT NULL AUTO_INCREMENT,
    EventId BIGINT NOT NULL,
    ParticipantId BIGINT NOT NULL,
    Status TINYINT NOT NULL,
    Notes VARCHAR(500) NULL,
    RegisteredAt DATETIME NOT NULL DEFAULT UTC_TIMESTAMP(),
    CancelledAt DATETIME NULL,

    CONSTRAINT PK_Registrations PRIMARY KEY (Id),

    CONSTRAINT FK_Registrations_Events
        FOREIGN KEY (EventId) REFERENCES Events(Id)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    CONSTRAINT FK_Registrations_Participants
        FOREIGN KEY (ParticipantId) REFERENCES Participants(Id)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    CONSTRAINT UQ_Registrations_EventId_ParticipantId
        UNIQUE (EventId, ParticipantId),

    INDEX IX_Registrations_EventId (EventId),
    INDEX IX_Registrations_ParticipantId (ParticipantId),
    INDEX IX_Registrations_Status (Status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;