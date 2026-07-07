-- 004_create_registrations.sql
-- Creates the Registrations table linking Events and Participants

CREATE TABLE Registrations (
    Id            BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    EventId       BIGINT UNSIGNED NOT NULL,
    ParticipantId BIGINT UNSIGNED NOT NULL,
    Status        TINYINT         NOT NULL DEFAULT 1, -- 1 = Active, 2 = Cancelled
    Notes         VARCHAR(500)    NULL,
    RegisteredAt  DATETIME        NOT NULL DEFAULT (UTC_TIMESTAMP()),
    CancelledAt   DATETIME        NULL,
    PRIMARY KEY (Id),
    CONSTRAINT FK_Registrations_Events
        FOREIGN KEY (EventId) REFERENCES Events (Id)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,
    CONSTRAINT FK_Registrations_Participants
        FOREIGN KEY (ParticipantId) REFERENCES Participants (Id)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,
    CONSTRAINT CK_Registrations_Status CHECK (Status IN (1, 2)),
    UNIQUE KEY UQ_Registrations_Event_Participant (EventId, ParticipantId),
    KEY IX_Registrations_EventId (EventId),
    KEY IX_Registrations_ParticipantId (ParticipantId),
    KEY IX_Registrations_Status (Status)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;