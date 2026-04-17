-- ============================================================
-- SmartBank Complaint Platform - Database Schema
-- STB Bank - SQL Server
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SmartBankDB')
    CREATE DATABASE SmartBankDB;
GO

USE SmartBankDB;
GO

-- ============================================================
-- TABLE: Agencies
-- ============================================================
CREATE TABLE Agencies (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    Code NVARCHAR(20) NOT NULL UNIQUE,
    City NVARCHAR(100),
    Phone NVARCHAR(20),
    Email NVARCHAR(100),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- ============================================================
-- TABLE: Roles
-- ============================================================
CREATE TABLE Roles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL UNIQUE,  -- Admin, Agent, Client
    Description NVARCHAR(200),
    Permissions NVARCHAR(MAX),          -- JSON permissions list
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- ============================================================
-- TABLE: Users
-- ============================================================
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL,
    RoleId INT NOT NULL FOREIGN KEY REFERENCES Roles(Id),
    AgencyId INT FOREIGN KEY REFERENCES Agencies(Id),
    IsActive BIT DEFAULT 1,
    LastLogin DATETIME2,
    RefreshToken NVARCHAR(500),
    RefreshTokenExpiry DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2
);

-- ============================================================
-- TABLE: ComplaintTypes
-- ============================================================
CREATE TABLE ComplaintTypes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,        -- Carte, Crédit, Compte, Digital Banking
    Code NVARCHAR(20) NOT NULL UNIQUE,
    DefaultSLAHours INT DEFAULT 48,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- ============================================================
-- TABLE: Complaints
-- ============================================================
CREATE TABLE Complaints (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Reference NVARCHAR(30) NOT NULL UNIQUE,   -- REF-2025-000001
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    ComplaintTypeId INT NOT NULL FOREIGN KEY REFERENCES ComplaintTypes(Id),
    Channel NVARCHAR(50) NOT NULL,            -- Agence, Téléphone, E-Banking, Email
    Priority NVARCHAR(20) DEFAULT 'Moyenne',  -- Faible, Moyenne, Haute, Critique
    Status NVARCHAR(30) DEFAULT 'Nouvelle',   -- Nouvelle, Assignée, En cours, Validation, Clôturée
    AgencyId INT FOREIGN KEY REFERENCES Agencies(Id),
    ClientName NVARCHAR(150),
    ClientEmail NVARCHAR(150),
    ClientPhone NVARCHAR(20),
    ClientAccountNumber NVARCHAR(30),
    AssignedToUserId INT FOREIGN KEY REFERENCES Users(Id),
    CreatedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    ClosedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    ResolutionNote NVARCHAR(MAX),
    RejectionReason NVARCHAR(MAX),
    SLADeadline DATETIME2,
    IsEscalated BIT DEFAULT 0,
    EscalatedAt DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2,
    ClosedAt DATETIME2
);

-- ============================================================
-- TABLE: ComplaintAttachments
-- ============================================================
CREATE TABLE ComplaintAttachments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ComplaintId INT NOT NULL FOREIGN KEY REFERENCES Complaints(Id) ON DELETE CASCADE,
    FileName NVARCHAR(255) NOT NULL,
    FilePath NVARCHAR(500) NOT NULL,
    FileSize BIGINT,
    FileType NVARCHAR(50),
    UploadedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    UploadedAt DATETIME2 DEFAULT GETDATE()
);

-- ============================================================
-- TABLE: ComplaintStatusHistory
-- ============================================================
CREATE TABLE ComplaintStatusHistory (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ComplaintId INT NOT NULL FOREIGN KEY REFERENCES Complaints(Id) ON DELETE CASCADE,
    OldStatus NVARCHAR(30),
    NewStatus NVARCHAR(30) NOT NULL,
    ChangedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    Comment NVARCHAR(MAX),
    ChangedAt DATETIME2 DEFAULT GETDATE()
);

-- ============================================================
-- TABLE: Assignments
-- ============================================================
CREATE TABLE Assignments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ComplaintId INT NOT NULL FOREIGN KEY REFERENCES Complaints(Id),
    AssignedToUserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    AssignedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    AssignedAt DATETIME2 DEFAULT GETDATE(),
    Notes NVARCHAR(500)
);

-- ============================================================
-- TABLE: Comments
-- ============================================================
CREATE TABLE Comments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ComplaintId INT NOT NULL FOREIGN KEY REFERENCES Complaints(Id) ON DELETE CASCADE,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    Content NVARCHAR(MAX) NOT NULL,
    IsInternal BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- ============================================================
-- TABLE: SLAConfigs
-- ============================================================
CREATE TABLE SLAConfigs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ComplaintTypeId INT FOREIGN KEY REFERENCES ComplaintTypes(Id),
    Priority NVARCHAR(20) NOT NULL,
    MaxHours INT NOT NULL,
    EscalationHours INT NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- ============================================================
-- TABLE: Notifications
-- ============================================================
CREATE TABLE Notifications (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    Type NVARCHAR(50),                -- Info, Warning, Alert, Success
    ComplaintId INT FOREIGN KEY REFERENCES Complaints(Id),
    IsRead BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    ReadAt DATETIME2
);

-- ============================================================
-- TABLE: AuditLogs
-- ============================================================
CREATE TABLE AuditLogs (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId INT FOREIGN KEY REFERENCES Users(Id),
    Action NVARCHAR(100) NOT NULL,
    Entity NVARCHAR(100),
    EntityId INT,
    OldValues NVARCHAR(MAX),
    NewValues NVARCHAR(MAX),
    IPAddress NVARCHAR(50),
    UserAgent NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- ============================================================
-- INDEXES for performance
-- ============================================================
CREATE INDEX IX_Complaints_Status ON Complaints(Status);
CREATE INDEX IX_Complaints_Priority ON Complaints(Priority);
CREATE INDEX IX_Complaints_AssignedToUserId ON Complaints(AssignedToUserId);
CREATE INDEX IX_Complaints_CreatedAt ON Complaints(CreatedAt);
CREATE INDEX IX_Complaints_Reference ON Complaints(Reference);
CREATE INDEX IX_Notifications_UserId_IsRead ON Notifications(UserId, IsRead);
CREATE INDEX IX_AuditLogs_UserId ON AuditLogs(UserId);
CREATE INDEX IX_AuditLogs_CreatedAt ON AuditLogs(CreatedAt);
GO
