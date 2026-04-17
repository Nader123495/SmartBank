USE SmartBankDB;
GO

DECLARE @AgentId INT = (SELECT TOP 1 Id FROM Users WHERE Email = 'agent1@stb.tn');
IF @AgentId IS NULL 
    SET @AgentId = (SELECT TOP 1 Id FROM Users WHERE Email = 'agent1@stb.com.tn');

DECLARE @AgencyId INT = (SELECT TOP 1 Id FROM Agencies WHERE Code = 'MAH001');
IF @AgencyId IS NULL 
    SET @AgencyId = (SELECT TOP 1 Id FROM Agencies);

DECLARE @TypeId INT = (SELECT TOP 1 Id FROM ComplaintTypes WHERE Code = 'CARD');
IF @TypeId IS NULL 
    SET @TypeId = (SELECT TOP 1 Id FROM ComplaintTypes);

INSERT INTO Complaints (
    Reference, Title, Description, ComplaintTypeId, Channel, Priority, 
    Status, AgencyId, AssignedToUserId, SLADeadline, CreatedAt
)
VALUES (
    'REF-TEST-' + FORMAT(GETUTCDATE(), 'HHmmss'), 
    'Alerte SLA Critique Simulée', 
    'Ceci est une réclamation de test insérée automatiquement pour tester le workflow SLA n8n.', 
    @TypeId, 'Agence', 'Haute', 'En cours', 
    @AgencyId, @AgentId, 
    DATEADD(HOUR, -9, GETUTCDATE()), 
    DATEADD(DAY, -2, GETUTCDATE())
);
GO
