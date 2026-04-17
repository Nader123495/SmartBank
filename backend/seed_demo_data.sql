USE SmartBankDB;
GO

-- 1. Ensure Roles exist
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Admin')
    INSERT INTO Roles (Name, CreatedAt) VALUES ('Admin', GETUTCDATE());
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Agent')
    INSERT INTO Roles (Name, CreatedAt) VALUES ('Agent', GETUTCDATE());
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Client')
    INSERT INTO Roles (Name, CreatedAt) VALUES ('Client', GETUTCDATE());
GO

-- 2. Create Admin user if missing
DECLARE @AdminRoleId INT = (SELECT Id FROM Roles WHERE Name = 'Admin');
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'admin@stb.tn')
BEGIN
    INSERT INTO Users (FirstName, LastName, Email, PasswordHash, RoleId, IsActive, EmailVerified, CreatedAt)
    VALUES ('Super', 'Admin', 'admin@stb.tn', '$2a$11$S8m5M4.uZ6m5M4.uZ6m5M4.uZ6m5M4.uZ6m5M4.uZ6m5M4.uZ6m5M4.', @AdminRoleId, 1, 1, GETUTCDATE());
END
GO

-- 3. Seed Agencies
IF NOT EXISTS (SELECT 1 FROM Agencies)
BEGIN
    INSERT INTO Agencies (Name, Code, City, Email, Phone, IsActive, CreatedAt)
    VALUES 
    ('STB Siège', 'HQ001', 'Tunis', 'siege@stb.tn', '+216 71 000 000', 1, GETUTCDATE()),
    ('STB Sousse Corniche', 'AG001', 'Sousse', 'sousse.corniche@stb.tn', '+216 73 111 222', 1, GETUTCDATE()),
    ('STB Sfax Centre', 'AG002', 'Sfax', 'sfax.centre@stb.tn', '+216 74 333 444', 1, GETUTCDATE()),
    ('STB Monastir Falaise', 'AG003', 'Monastir', 'monastir.falaise@stb.tn', '+216 73 555 666', 1, GETUTCDATE());
END
GO

-- 4. Seed Complaints
DECLARE @AdminId INT = (SELECT Id FROM Users WHERE Email = 'admin@stb.tn');
DECLARE @TypeCarte INT = (SELECT Id FROM ComplaintTypes WHERE Name = 'Carte Bancaire');
DECLARE @TypeVirement INT = (SELECT Id FROM ComplaintTypes WHERE Name = 'Virement');
DECLARE @TypeCompte INT = (SELECT Id FROM ComplaintTypes WHERE Name = 'Compte Courant');
DECLARE @TypeDigital INT = (SELECT Id FROM ComplaintTypes WHERE Name = 'Digital Banking');
DECLARE @TypeCheque INT = (SELECT Id FROM ComplaintTypes WHERE Name = 'Chèque');
DECLARE @TypeAutre INT = (SELECT Id FROM ComplaintTypes WHERE Name = 'Autre');

DECLARE @AgencyTunis INT = (SELECT Id FROM Agencies WHERE City = 'Tunis');
DECLARE @AgencySousse INT = (SELECT Id FROM Agencies WHERE City = 'Sousse');

IF NOT EXISTS (SELECT 1 FROM Complaints)
BEGIN
    INSERT INTO Complaints (Reference, Title, Description, ComplaintTypeId, Status, Priority, Channel, CreatedAt, CreatedByUserId, SLADeadline, IsEscalated, AgencyId)
    VALUES 
    ('REC-2026-0001', 'Carte avalée par le GAB', 'Ma carte a été capturée par l''automate.', ISNULL(@TypeCarte, 1), 'Nouvelle', 'Haute', 'Agence', DATEADD(HOUR, -2, GETUTCDATE()), @AdminId, DATEADD(DAY, 2, GETUTCDATE()), 0, @AgencySousse),
    ('REC-2026-0002', 'Virement non reçu', 'Un virement effectué il y a 3 jours n''apparaît pas.', ISNULL(@TypeVirement, 5), 'En cours', 'Moyenne', 'Téléphone', DATEADD(DAY, -1, GETUTCDATE()), @AdminId, DATEADD(DAY, 1, GETUTCDATE()), 0, @AgencyTunis),
    ('REC-2026-0003', 'Frais bancaires excessifs', 'Prélèvement de frais non justifiés.', ISNULL(@TypeCompte, 3), 'Nouvelle', 'Faible', 'E-mail', DATEADD(HOUR, -5, GETUTCDATE()), @AdminId, DATEADD(DAY, 3, GETUTCDATE()), 0, NULL),
    ('REC-2026-0004', 'Accès application mobile bloqué', 'Impossible de me connecter à l''application.', ISNULL(@TypeDigital, 4), 'Clôturée', 'Haute', 'Application', DATEADD(DAY, -2, GETUTCDATE()), @AdminId, DATEADD(DAY, -1, GETUTCDATE()), 0, NULL),
    ('REC-2026-0005', 'Chéquier non reçu', 'Demande de chéquier faite il y a 2 semaines.', ISNULL(@TypeCheque, 6), 'Nouvelle', 'Moyenne', 'Agence', DATEADD(DAY, -10, GETUTCDATE()), @AdminId, DATEADD(DAY, -5, GETUTCDATE()), 1, @AgencySousse),
    ('REC-2026-0006', 'Erreur sur taux de change', 'Taux appliqué erroné.', ISNULL(@TypeVirement, 5), 'En cours', 'Haute', 'E-mail', DATEADD(HOUR, -12, GETUTCDATE()), @AdminId, DATEADD(DAY, 1, GETUTCDATE()), 0, @AgencyTunis),
    ('REC-2026-0007', 'Dossier crédit sans réponse', 'Dossier déposé sans retour.', 2, 'Nouvelle', 'Moyenne', 'Agence', DATEADD(DAY, -8, GETUTCDATE()), @AdminId, DATEADD(DAY, -4, GETUTCDATE()), 0, @AgencySousse),
    ('REC-2026-0008', 'Problème SMS OTP', 'Je ne reçois plus les codes OTP.', ISNULL(@TypeDigital, 4), 'Clôturée', 'Haute', 'Application', DATEADD(DAY, -3, GETUTCDATE()), @AdminId, DATEADD(DAY, -2, GETUTCDATE()), 0, NULL);
END
GO
