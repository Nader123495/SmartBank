-- ============================================================
-- SmartBank Complaint Platform - Seed Data (idempotent)
-- Exécuter après 01_CreateDatabase.sql. Peut être relancé sans erreur.
-- Rôles métier : Admin (pilotage complet), Agent, Client — plus de rôle « Responsable ».
-- ============================================================

USE SmartBankDB;
GO

-- Bases déjà peuplées : fusionner l'ancien rôle Responsable dans Admin
IF EXISTS (SELECT 1 FROM Roles WHERE Name = N'Responsable')
BEGIN
    DECLARE @adminIdM INT = (SELECT TOP (1) Id FROM Roles WHERE Name = N'Admin');
    DECLARE @respIdM INT = (SELECT TOP (1) Id FROM Roles WHERE Name = N'Responsable');
    IF @adminIdM IS NOT NULL AND @respIdM IS NOT NULL
    BEGIN
        UPDATE Users SET RoleId = @adminIdM WHERE RoleId = @respIdM;
        DELETE FROM Roles WHERE Id = @respIdM;
    END
END
GO

-- Rôles (insert seulement si table vide)
IF (SELECT COUNT(*) FROM Roles) = 0
BEGIN
    INSERT INTO Roles (Name, Description, Permissions) VALUES
    (N'Admin', N'Administrateur — pilotage, assignation, utilisateurs, audit', N'["all"]'),
    (N'Agent', N'Agent de traitement des réclamations', N'["view_assigned","create","update","comment"]');
END
GO

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = N'Client')
    INSERT INTO Roles (Name, Description, Permissions) VALUES
    (N'Client', N'Client bancaire — dépôt et suivi des réclamations', N'[]');
GO

-- Agencies (insert only if empty)
IF (SELECT COUNT(*) FROM Agencies) = 0
BEGIN
    INSERT INTO Agencies (Name, Code, City, Phone, Email) VALUES
    ('Agence Tunis Centre', 'TUN-01', 'Tunis', '+216 71 123 456', 'tunis.centre@stb.tn'),
    ('Agence Sfax', 'SFX-01', 'Sfax', '+216 74 123 456', 'sfax@stb.tn'),
    ('Agence Sousse', 'SOU-01', 'Sousse', '+216 73 123 456', 'sousse@stb.tn'),
    ('Agence Bizerte', 'BIZ-01', 'Bizerte', '+216 72 123 456', 'bizerte@stb.tn'),
    ('Agence Nabeul', 'NAB-01', 'Nabeul', '+216 72 987 654', 'nabeul@stb.tn');
END
GO

-- ComplaintTypes (insert only if empty)
IF (SELECT COUNT(*) FROM ComplaintTypes) = 0
BEGIN
    INSERT INTO ComplaintTypes (Name, Code, DefaultSLAHours) VALUES
    ('Carte Bancaire', 'CARTE', 24),
    ('Crédit et Prêts', 'CREDIT', 72),
    ('Compte Courant', 'COMPTE', 48),
    ('Digital Banking', 'DIGITAL', 12),
    ('Virement', 'VIREMENT', 24),
    ('Chèque', 'CHEQUE', 48),
    ('Autre', 'AUTRE', 72);
END
GO

-- SLA Configurations (insert only if empty)
IF (SELECT COUNT(*) FROM SLAConfigs) = 0
BEGIN
    INSERT INTO SLAConfigs (ComplaintTypeId, Priority, MaxHours, EscalationHours) VALUES
    (1, 'Critique', 4, 2),
    (1, 'Haute', 12, 8),
    (1, 'Moyenne', 24, 20),
    (1, 'Faible', 48, 40),
    (2, 'Critique', 8, 4),
    (2, 'Haute', 24, 18),
    (2, 'Moyenne', 72, 60),
    (2, 'Faible', 120, 100),
    (3, 'Critique', 6, 3),
    (3, 'Haute', 24, 18),
    (3, 'Moyenne', 48, 40),
    (3, 'Faible', 96, 80),
    (4, 'Critique', 2, 1),
    (4, 'Haute', 8, 6),
    (4, 'Moyenne', 12, 10),
    (4, 'Faible', 24, 20);
END
GO

-- Utilisateurs de démo (RoleId = Id du rôle en base)
DECLARE @ridAdmin INT = (SELECT TOP (1) Id FROM Roles WHERE Name = N'Admin');
DECLARE @ridAgent INT = (SELECT TOP (1) Id FROM Roles WHERE Name = N'Agent');

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'admin@stb.tn')
    INSERT INTO Users (FirstName, LastName, Email, PasswordHash, RoleId)
    VALUES (N'Super', N'Admin', N'admin@stb.tn', N'$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBpj4UfFVy8Q6e', @ridAdmin);

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'agent1@stb.tn')
    INSERT INTO Users (FirstName, LastName, Email, PasswordHash, RoleId, AgencyId)
    VALUES (N'Mohamed', N'Ben Ali', N'agent1@stb.tn', N'$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBpj4UfFVy8Q6e', @ridAgent, 1);

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'agent2@stb.tn')
    INSERT INTO Users (FirstName, LastName, Email, PasswordHash, RoleId, AgencyId)
    VALUES (N'Fatma', N'Trabelsi', N'agent2@stb.tn', N'$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBpj4UfFVy8Q6e', @ridAgent, 2);

-- Second compte admin (même rôle Admin ; e-mail historique « responsable@ »)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'responsable@stb.tn')
    INSERT INTO Users (FirstName, LastName, Email, PasswordHash, RoleId, AgencyId)
    VALUES (N'Ahmed', N'Mansouri', N'responsable@stb.tn', N'$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBpj4UfFVy8Q6e', @ridAdmin, 1);
GO
