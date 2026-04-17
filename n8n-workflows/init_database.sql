USE SmartBankDB;
GO

-- Create Roles table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Roles]') AND type in (N'U'))
BEGIN
CREATE TABLE Roles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nom NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255)
);
END
GO

-- Create Agencies table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Agencies]') AND type in (N'U'))
BEGIN
CREATE TABLE Agencies (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nom NVARCHAR(100) NOT NULL,
    Adresse NVARCHAR(255),
    Region NVARCHAR(100)
);
END
GO

-- Create Users table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
BEGIN
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nom NVARCHAR(100) NOT NULL,
    Prenom NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    Specialite NVARCHAR(100),
    RoleId INT FOREIGN KEY REFERENCES Roles(Id),
    AgenceId INT FOREIGN KEY REFERENCES Agencies(Id),
    EstActif BIT DEFAULT 1,
    DateCreation DATETIME DEFAULT GETUTCDATE()
);
END
GO

-- Create Clients table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Clients]') AND type in (N'U'))
BEGIN
CREATE TABLE Clients (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nom NVARCHAR(100) NOT NULL,
    Prenom NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255),
    Telephone NVARCHAR(50),
    ReferenceClient NVARCHAR(50) UNIQUE,
    DateCreation DATETIME DEFAULT GETUTCDATE()
);
END
GO

-- Create ComplaintTypes
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ComplaintTypes]') AND type in (N'U'))
BEGIN
CREATE TABLE ComplaintTypes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nom NVARCHAR(100) NOT NULL,
    SlaHeuresDefault INT NOT NULL,
    PrioriteDefault NVARCHAR(50)
);
END
GO

-- Create Complaints
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Complaints]') AND type in (N'U'))
BEGIN
CREATE TABLE Complaints (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RefReclamation NVARCHAR(50) NOT NULL UNIQUE,
    ClientId INT FOREIGN KEY REFERENCES Clients(Id),
    Type NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    Canal NVARCHAR(50),
    AgenceId INT FOREIGN KEY REFERENCES Agencies(Id),
    Priorite NVARCHAR(50) DEFAULT 'Moyenne',
    Statut NVARCHAR(50) DEFAULT 'Nouvelle',
    AgentAssigneId INT FOREIGN KEY REFERENCES Users(Id),
    SlaDueDate DATETIME,
    DateCreation DATETIME DEFAULT GETUTCDATE(),
    DateAssignation DATETIME,
    DateCloture DATETIME,
    DateRejet DATETIME,
    JustificationRejet NVARCHAR(MAX),
    DerniereModification DATETIME DEFAULT GETUTCDATE()
);
END
GO

-- Create ComplaintStatusHistory
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ComplaintStatusHistory]') AND type in (N'U'))
BEGIN
CREATE TABLE ComplaintStatusHistory (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ComplaintId INT FOREIGN KEY REFERENCES Complaints(Id),
    StatutPrecedent NVARCHAR(50),
    NouveauStatut NVARCHAR(50) NOT NULL,
    ChangePar NVARCHAR(100),
    DateChangement DATETIME DEFAULT GETUTCDATE(),
    Commentaire NVARCHAR(MAX),
    Justification NVARCHAR(MAX),
    Notes NVARCHAR(MAX)
);
END
GO

-- Create Assignments
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND type in (N'U'))
BEGIN
CREATE TABLE Assignments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ComplaintId INT FOREIGN KEY REFERENCES Complaints(Id),
    AgentId INT FOREIGN KEY REFERENCES Users(Id),
    DateAssignation DATETIME DEFAULT GETUTCDATE(),
    Notes NVARCHAR(MAX)
);
END
GO

-- Create SLA
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SLA]') AND type in (N'U'))
BEGIN
CREATE TABLE SLA (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ComplaintId INT FOREIGN KEY REFERENCES Complaints(Id),
    SlaHeures INT NOT NULL,
    DateDebut DATETIME NOT NULL,
    DateLimite DATETIME NOT NULL,
    Statut NVARCHAR(50) DEFAULT 'EnCours',
    DateFin DATETIME,
    EstRespectee BIT,
    NiveauEscalade NVARCHAR(50),
    NombreEscalades INT DEFAULT 0,
    DerniereVerification DATETIME
);
END
GO

-- Create Notifications
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND type in (N'U'))
BEGIN
CREATE TABLE Notifications (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT FOREIGN KEY REFERENCES Users(Id),
    Titre NVARCHAR(255) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    Type NVARCHAR(50),
    EntityId INT,
    EstLu BIT DEFAULT 0,
    DateCreation DATETIME DEFAULT GETUTCDATE()
);
END
GO

-- Create AuditLogs
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND type in (N'U'))
BEGIN
CREATE TABLE AuditLogs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Action NVARCHAR(100) NOT NULL,
    TableCiblee NVARCHAR(100),
    EnregistrementId INT,
    UtilisateurId INT,
    DateAction DATETIME DEFAULT GETUTCDATE(),
    Details NVARCHAR(MAX)
);
END
GO

-- Insérer des rôles de base
IF NOT EXISTS (SELECT * FROM Roles WHERE Nom = 'Agent')
BEGIN
    INSERT INTO Roles (Nom, Description) VALUES ('Agent', 'Agent traitant les réclamations');
    INSERT INTO Roles (Nom, Description) VALUES ('ResponsableAgence', 'Responsable de l''agence');
    INSERT INTO Roles (Nom, Description) VALUES ('ResponsableDept', 'Responsable du département régional');
    INSERT INTO Roles (Nom, Description) VALUES ('Directeur', 'Direction Centrale');
    INSERT INTO Roles (Nom, Description) VALUES ('Admin', 'Administrateur Systeme');
END
GO

-- Insérer une agence de test
IF NOT EXISTS (SELECT * FROM Agencies WHERE Nom = 'STB Mahdia Centre')
BEGIN
    INSERT INTO Agencies (Nom, Region, Adresse) VALUES ('STB Mahdia Centre', 'Mahdia', 'Avenue Habib Bourguiba');
END
GO

-- Insérer un agent de test (pour tester l'auto-affectation n8n)
IF NOT EXISTS (SELECT * FROM Users WHERE Email = 'agent1@stb.com.tn')
BEGIN
    DECLARE @RoleId INT = (SELECT Id FROM Roles WHERE Nom = 'Agent');
    DECLARE @AgenceId INT = (SELECT Id FROM Agencies WHERE Nom = 'STB Mahdia Centre');
    
    INSERT INTO Users (Nom, Prenom, Email, PasswordHash, Specialite, RoleId, AgenceId, EstActif)
    VALUES ('Ben Ali', 'Mohamed', 'agent1@stb.com.tn', 'HASHED_PASSWORD', 'Carte,Compte', @RoleId, @AgenceId, 1);
END
GO

-- Insérer un client de test
IF NOT EXISTS (SELECT * FROM Clients WHERE ReferenceClient = 'CL-001')
BEGIN
    INSERT INTO Clients (Nom, Prenom, Email, Telephone, ReferenceClient)
    VALUES ('Trabelsi', 'Sami', 'sami.test@gmail.com', '+216 20 123 456', 'CL-001');
END
GO
