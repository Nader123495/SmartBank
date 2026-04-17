USE SmartBankDB;
GO

-- 1. Agence
IF NOT EXISTS (SELECT * FROM Agencies WHERE Name = 'STB Mahdia Centre')
BEGIN
    SET IDENTITY_INSERT Agencies ON;
    INSERT INTO Agencies (Id, Name, Code, City, IsActive, CreatedAt)
    VALUES (1, 'STB Mahdia Centre', 'MAH001', 'Mahdia', 1, GETUTCDATE());
    SET IDENTITY_INSERT Agencies OFF;
END
GO

-- 2. Rôle
IF NOT EXISTS (SELECT * FROM Roles WHERE Name = 'Agent')
BEGIN
    INSERT INTO Roles (Name, Description, CreatedAt) 
    VALUES ('Agent', 'Agent traitant les réclamations', GETUTCDATE());
END
GO

-- 3. Agent (Mohamed Ben Ali)
IF NOT EXISTS (SELECT * FROM Users WHERE Email = 'agent1@stb.com.tn')
BEGIN
    DECLARE @RoleId INT = (SELECT TOP 1 Id FROM Roles WHERE Name = 'Agent');
    INSERT INTO Users (FirstName, LastName, Email, PasswordHash, RoleId, AgencyId, IsActive, EmailVerified, CreatedAt)
    VALUES ('Mohamed', 'Ben Ali', 'agent1@stb.com.tn', 'HASHED_PWD', @RoleId, 1, 1, 1, GETUTCDATE());
END
GO

-- 4. Type de réclamation
IF NOT EXISTS (SELECT * FROM ComplaintTypes WHERE Name = 'Carte')
BEGIN
    INSERT INTO ComplaintTypes (Name, Code, DefaultSLAHours, IsActive, CreatedAt)
    VALUES ('Carte', 'CARD', 24, 1, GETUTCDATE());
END
GO
