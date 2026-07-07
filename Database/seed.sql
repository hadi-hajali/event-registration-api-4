-- seed.sql
-- Demonstration data for the Event Registration System
-- Run after migrations 001-004 have been applied.

-- ---------------------------------------------------------------------
-- Categories (4)
-- ---------------------------------------------------------------------
INSERT INTO Categories (Name, Description, IsActive) VALUES
('Technology',        'Talks and workshops about software and IT',        1),
('Business',          'Networking and professional development events',   1),
('Health & Wellness', 'Fitness, nutrition, and mental health sessions',    1),
('Arts & Culture',    'Exhibitions, music, and creative workshops',        1);

-- ---------------------------------------------------------------------
-- Events (8) - CategoryId references the rows inserted above (1..4)
-- ---------------------------------------------------------------------
INSERT INTO Events (CategoryId, Name, Description, Location, StartAt, EndAt, RegistrationDeadline, Capacity, IsActive) VALUES
(1, 'Intro to Web APIs',        'Hands-on session building REST APIs.',        'Nablus Tech Hub, Hall A',   '2026-07-20 09:00:00', '2026-07-20 13:00:00', '2026-07-18 23:59:59', 3, 1),
(1, 'Cloud Fundamentals',       'Overview of cloud computing basics.',         'Nablus Tech Hub, Hall B',   '2026-08-05 10:00:00', '2026-08-05 15:00:00', '2026-08-03 23:59:59', 40, 1),
(2, 'Startup Networking Night', 'Meet local founders and investors.',          'Downtown Business Center',  '2026-07-15 18:00:00', '2026-07-15 21:00:00', '2026-07-14 23:59:59', 2, 1),
(2, 'Leadership Workshop',      'Practical leadership and team management.',   'Downtown Business Center',  '2026-09-01 09:00:00', '2026-09-01 16:00:00', '2026-08-28 23:59:59', 25, 1),
(3, 'Morning Yoga Retreat',     'Full-day guided yoga and mindfulness.',       'Riverside Wellness Park',   '2026-06-20 07:00:00', '2026-06-20 12:00:00', '2026-06-18 23:59:59', 20, 1),
(3, 'Nutrition Basics Seminar', 'Understanding healthy eating habits.',        'Community Health Center',   '2026-07-25 17:00:00', '2026-07-25 19:00:00', '2026-07-23 23:59:59', 30, 1),
(4, 'Local Art Exhibition',     'Showcasing regional painters and sculptors.', 'City Gallery',              '2026-06-10 16:00:00', '2026-06-10 20:00:00', '2026-06-08 23:59:59', 50, 1),
(4, 'Traditional Music Night',  'Live performances of traditional music.',     'Old Town Cultural Center',  '2026-08-15 19:00:00', '2026-08-15 22:00:00', '2026-08-13 23:59:59', 4, 1);

-- ---------------------------------------------------------------------
-- Participants (12)
-- ---------------------------------------------------------------------
INSERT INTO Participants (FullName, Email, Phone, DateOfBirth, IsActive) VALUES
('Ahmad Khalil',    'ahmad.khalil@example.com',    '+970-59-1000001', '1995-03-12', 1),
('Sara Odeh',       'sara.odeh@example.com',       '+970-59-1000002', '1998-07-22', 1),
('Mohammad Nasser', 'mohammad.nasser@example.com', '+970-59-1000003', '1990-11-05', 1),
('Lina Hamdan',     'lina.hamdan@example.com',     '+970-59-1000004', '2000-01-30', 1),
('Yousef Amer',     'yousef.amer@example.com',     '+970-59-1000005', '1992-09-14', 1),
('Rana Suleiman',   'rana.suleiman@example.com',   '+970-59-1000006', '1997-05-18', 1),
('Omar Zaid',       'omar.zaid@example.com',       '+970-59-1000007', '1994-12-02', 1),
('Dana Barakat',    'dana.barakat@example.com',    '+970-59-1000008', '1999-04-09', 1),
('Khaled Fares',    'khaled.fares@example.com',    '+970-59-1000009', '1991-08-27', 1),
('Nour Awad',       'nour.awad@example.com',       '+970-59-1000010', '1996-02-16', 1),
('Hiba Salem',      'hiba.salem@example.com',      '+970-59-1000011', '1993-10-08', 1),
('Tariq Mansour',   'tariq.mansour@example.com',   '+970-59-1000012', '2001-06-25', 1);

-- ---------------------------------------------------------------------
-- Registrations (12 active + 3 cancelled)
-- Event 1 "Intro to Web APIs" has Capacity 3 -> filled to demonstrate a full event.
-- Event 8 "Traditional Music Night" has Capacity 4 -> 3 active + 1 cancelled slot reopened.
-- ---------------------------------------------------------------------

-- Event 1 (Intro to Web APIs, capacity 3) - fully booked
INSERT INTO Registrations (EventId, ParticipantId, Status, Notes, RegisteredAt) VALUES
(1, 1, 1, NULL, '2026-07-01 08:00:00'),
(1, 2, 1, NULL, '2026-07-01 09:15:00'),
(1, 3, 1, NULL, '2026-07-02 10:30:00');

-- Event 2 (Cloud Fundamentals, capacity 40) - some seats taken
INSERT INTO Registrations (EventId, ParticipantId, Status, Notes, RegisteredAt) VALUES
(2, 4, 1, NULL, '2026-07-03 11:00:00'),
(2, 5, 1, NULL, '2026-07-03 12:00:00');

-- Event 3 (Startup Networking Night, capacity 2) - fully booked
INSERT INTO Registrations (EventId, ParticipantId, Status, Notes, RegisteredAt) VALUES
(3, 6, 1, NULL, '2026-07-04 09:00:00'),
(3, 7, 1, NULL, '2026-07-04 09:30:00');

-- Event 4 (Leadership Workshop, capacity 25) - a few seats taken
INSERT INTO Registrations (EventId, ParticipantId, Status, Notes, RegisteredAt) VALUES
(4, 8, 1, NULL, '2026-07-05 14:00:00');

-- Event 6 (Nutrition Basics Seminar, capacity 30) - a couple registrations
INSERT INTO Registrations (EventId, ParticipantId, Status, Notes, RegisteredAt) VALUES
(6, 9, 1, NULL, '2026-07-05 15:00:00'),
(6, 10, 1, NULL, '2026-07-06 08:45:00');

-- Event 8 (Traditional Music Night, capacity 4) - 3 active seats
INSERT INTO Registrations (EventId, ParticipantId, Status, Notes, RegisteredAt) VALUES
(8, 11, 1, NULL, '2026-07-06 16:00:00'),
(8, 12, 1, NULL, '2026-07-06 16:20:00'),
(8, 1,  1, NULL, '2026-07-06 17:00:00');

-- Cancelled registrations (3) - demonstrate cancellation behavior
INSERT INTO Registrations (EventId, ParticipantId, Status, Notes, RegisteredAt, CancelledAt) VALUES
(2, 6,  2, 'Participant could not attend.', '2026-06-25 10:00:00', '2026-07-01 09:00:00'),
(4, 9,  2, 'Schedule conflict.',            '2026-06-28 12:00:00', '2026-07-02 13:00:00'),
(8, 3,  2, 'Cancelled to free a seat.',     '2026-06-30 09:00:00', '2026-07-03 10:00:00');