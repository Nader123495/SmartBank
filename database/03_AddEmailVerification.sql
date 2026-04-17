-- Ajout de la vérification email (exécuter après 01_CreateDatabase.sql et 02_SeedData.sql)
USE SmartBankDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'EmailVerified')
BEGIN
    ALTER TABLE Users ADD EmailVerified BIT NOT NULL DEFAULT 1;
    -- Les utilisateurs existants sont considérés comme vérifiés (comptes seed)
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'EmailVerificationToken')
    ALTER TABLE Users ADD EmailVerificationToken NVARCHAR(20) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'EmailVerificationExpiry')
    ALTER TABLE Users ADD EmailVerificationExpiry DATETIME2 NULL;
GO

-- S'assurer que les comptes seed restent vérifiés
UPDATE Users SET EmailVerified = 1 WHERE EmailVerificationToken IS NULL;
GO
