/* =============================================================
   RaceDay Database Schema
   Module: PROG6212 - Part 1, Section C
   Target: SQL Server (run in SQL Server Management Studio - SSMS)

   This script:
     1. Creates the RaceDayDB database if it does not already exist.
     2. Drops any existing RaceDay tables (in FK-safe order) so the
        script can be re-run cleanly on the same instance.
     3. Creates all six tables with primary keys, foreign keys, and
        NOT NULL / UNIQUE / DEFAULT constraints, matching the ERD.
        Events additionally store the overall event distance and type
        required by the Part 2 assessment; category distances remain
        available for multi-distance events.
     4. Seeds realistic sample data: 2 Organisers, 2 Participants,
        3 Events, categories for each event, sample enrolments,
        and sample captured results.
   ============================================================= */

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'RaceDayDB')
BEGIN
    CREATE DATABASE RaceDayDB;
END
GO

USE RaceDayDB;
GO

-- Drop tables if they already exist, in FK-safe (child-to-parent) order
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
GO

/* =============================================================
   Table: Roles
   Lookup table for the system's two user roles.
   ============================================================= */
CREATE TABLE dbo.Roles (
    RoleId      INT IDENTITY(1,1) PRIMARY KEY,
    RoleName    VARCHAR(20) NOT NULL UNIQUE
);
GO

/* =============================================================
   Table: Users
   Stores both Organisers and Participants, distinguished by RoleId.
   ============================================================= */
CREATE TABLE dbo.Users (
    UserId          INT IDENTITY(1,1) PRIMARY KEY,
    RoleId          INT NOT NULL,
    FullName        VARCHAR(100) NOT NULL,
    Email           VARCHAR(150) NOT NULL UNIQUE,
    PasswordHash    VARCHAR(255) NOT NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId)
);
GO

/* =============================================================
   Table: Events
   Created and owned by an Organiser (Users).
   ============================================================= */
CREATE TABLE dbo.Events (
    EventId         INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId     INT NOT NULL,
    EventName       VARCHAR(150) NOT NULL,
    Description     VARCHAR(1000) NULL,
    EventDate       DATETIME NOT NULL,
    Location        VARCHAR(200) NOT NULL,
    RouteInfo       VARCHAR(500) NULL,
    EventDistanceKm DECIMAL(6,2) NOT NULL
        CONSTRAINT CK_Events_EventDistanceKm CHECK (EventDistanceKm > 0),
    EventType       VARCHAR(10) NOT NULL
        CONSTRAINT CK_Events_EventType CHECK (EventType IN ('Run', 'Walk', 'Cycle')),
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserId) REFERENCES dbo.Users(UserId)
);
GO

/* =============================================================
   Table: Categories
   Each event can have multiple categories (e.g. 5km, 10km, Half Marathon).
   ============================================================= */
CREATE TABLE dbo.Categories (
    CategoryId      INT IDENTITY(1,1) PRIMARY KEY,
    EventId         INT NOT NULL,
    CategoryName    VARCHAR(50) NOT NULL,
    DistanceKm      DECIMAL(5,2) NOT NULL,
    MaxParticipants INT NOT NULL DEFAULT 100
        CONSTRAINT CK_Categories_MaxParticipants CHECK (MaxParticipants > 0),
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId) REFERENCES dbo.Events(EventId)
);
GO

/* =============================================================
   Table: Enrolments
   Junction table resolving the many-to-many relationship between
   Participants (Users) and Categories.
   ============================================================= */
CREATE TABLE dbo.Enrolments (
    EnrolmentId     INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantId   INT NOT NULL,
    CategoryId      INT NOT NULL,
    EnrolmentDate   DATETIME NOT NULL DEFAULT GETDATE(),
    Status          VARCHAR(20) NOT NULL DEFAULT 'Registered',
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(CategoryId),
    CONSTRAINT UQ_Enrolments_Participant_Category UNIQUE (ParticipantId, CategoryId)
);
GO

/* =============================================================
   Table: Results
   One captured result per enrolment, recorded by an Organiser.
   ============================================================= */
CREATE TABLE dbo.Results (
    ResultId            INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId         INT NOT NULL UNIQUE,
    FinishTime          TIME NOT NULL
        CONSTRAINT CK_Results_FinishTime CHECK (FinishTime > '00:00:00'),
    [Position]          INT NULL
        CONSTRAINT CK_Results_Position CHECK ([Position] IS NULL OR [Position] > 0),
    RecordedByUserId    INT NOT NULL,
    RecordedAt          DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId) REFERENCES dbo.Enrolments(EnrolmentId),
    CONSTRAINT FK_Results_Users FOREIGN KEY (RecordedByUserId) REFERENCES dbo.Users(UserId)
);
GO

/* =============================================================
   Seed Data
   ============================================================= */

-- Roles
INSERT INTO dbo.Roles (RoleName) VALUES ('Organiser'), ('Participant');
GO

-- Users: 2 Organisers, 2 Participants
-- These HASHED_PASSWORD_* values are Part 1 placeholders, not valid password
-- hashes. The sample accounts cannot authenticate to the API; register real
-- development accounts through POST /api/auth/register instead.
INSERT INTO dbo.Users (RoleId, FullName, Email, PasswordHash)
VALUES
((SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Organiser'), 'Thandeka Mokoena', 'thandeka@raceday.co.za', 'HASHED_PASSWORD_1'),
((SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Organiser'), 'Johan van der Merwe', 'johan@raceday.co.za', 'HASHED_PASSWORD_2'),
((SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Participant'), 'Lerato Dlamini', 'lerato@example.com', 'HASHED_PASSWORD_3'),
((SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Participant'), 'Sipho Nkosi', 'sipho@example.com', 'HASHED_PASSWORD_4');
GO

-- Events: 3 events, owned by the two Organisers
INSERT INTO dbo.Events (OrganiserId, EventName, Description, EventDate, Location, RouteInfo, EventDistanceKm, EventType)
VALUES
((SELECT UserId FROM dbo.Users WHERE Email = 'thandeka@raceday.co.za'),
 'Cape Town Park Run Challenge', 'A community park run through Green Point.',
 '2026-10-10 07:00:00', 'Green Point, Cape Town', 'Flat 5km loop around the Green Point Urban Park.', 5.00, 'Run'),
((SELECT UserId FROM dbo.Users WHERE Email = 'thandeka@raceday.co.za'),
 'Durban Beachfront Marathon', 'Scenic marathon along the Durban beachfront.',
 '2026-11-15 06:00:00', 'Durban Beachfront', 'Coastal out-and-back route, mostly flat with sea views.', 42.20, 'Run'),
((SELECT UserId FROM dbo.Users WHERE Email = 'johan@raceday.co.za'),
 'Joburg Hills Cycle Tour', 'A challenging cycling tour through the Johannesburg hills.',
 '2026-09-20 06:30:00', 'Sandton, Johannesburg', 'Hilly 40km loop with three major climbs.', 40.00, 'Cycle');
GO

-- Categories: at least one per event
INSERT INTO dbo.Categories (EventId, CategoryName, DistanceKm, MaxParticipants)
VALUES
((SELECT EventId FROM dbo.Events WHERE EventName = 'Cape Town Park Run Challenge'), '5km Fun Run', 5.00, 200),
((SELECT EventId FROM dbo.Events WHERE EventName = 'Cape Town Park Run Challenge'), '10km Challenge', 10.00, 150),
((SELECT EventId FROM dbo.Events WHERE EventName = 'Durban Beachfront Marathon'), 'Half Marathon', 21.10, 500),
((SELECT EventId FROM dbo.Events WHERE EventName = 'Durban Beachfront Marathon'), 'Full Marathon', 42.20, 300),
((SELECT EventId FROM dbo.Events WHERE EventName = 'Joburg Hills Cycle Tour'), '40km Road Cycle', 40.00, 100);
GO

-- Enrolments: participants entering categories across different events
INSERT INTO dbo.Enrolments (ParticipantId, CategoryId, Status)
VALUES
((SELECT UserId FROM dbo.Users WHERE Email = 'lerato@example.com'),
 (SELECT CategoryId FROM dbo.Categories WHERE CategoryName = '5km Fun Run'), 'Registered'),
((SELECT UserId FROM dbo.Users WHERE Email = 'lerato@example.com'),
 (SELECT CategoryId FROM dbo.Categories WHERE CategoryName = 'Half Marathon'), 'Registered'),
((SELECT UserId FROM dbo.Users WHERE Email = 'sipho@example.com'),
 (SELECT CategoryId FROM dbo.Categories WHERE CategoryName = '10km Challenge'), 'Registered'),
((SELECT UserId FROM dbo.Users WHERE Email = 'sipho@example.com'),
 (SELECT CategoryId FROM dbo.Categories WHERE CategoryName = '40km Road Cycle'), 'Registered');
GO

-- Results: sample captured results for two completed enrolments
INSERT INTO dbo.Results (EnrolmentId, FinishTime, [Position], RecordedByUserId)
VALUES
((SELECT EnrolmentId FROM dbo.Enrolments
    WHERE ParticipantId = (SELECT UserId FROM dbo.Users WHERE Email = 'lerato@example.com')
      AND CategoryId = (SELECT CategoryId FROM dbo.Categories WHERE CategoryName = '5km Fun Run')),
 '00:24:35', 3, (SELECT UserId FROM dbo.Users WHERE Email = 'thandeka@raceday.co.za')),
((SELECT EnrolmentId FROM dbo.Enrolments
    WHERE ParticipantId = (SELECT UserId FROM dbo.Users WHERE Email = 'sipho@example.com')
      AND CategoryId = (SELECT CategoryId FROM dbo.Categories WHERE CategoryName = '10km Challenge')),
 '00:52:10', 1, (SELECT UserId FROM dbo.Users WHERE Email = 'thandeka@raceday.co.za'));
GO

/* =============================================================
   Quick verification queries (optional — comment out before
   submission if you only want the schema + seed to run silently)
   ============================================================= */
-- SELECT * FROM dbo.Roles;
-- SELECT * FROM dbo.Users;
-- SELECT * FROM dbo.Events;
-- SELECT * FROM dbo.Categories;
-- SELECT * FROM dbo.Enrolments;
-- SELECT * FROM dbo.Results;
