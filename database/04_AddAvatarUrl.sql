-- Ajout de la colonne AvatarUrl pour la photo de profil utilisateur
USE SmartBankDB;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Users') AND name = 'AvatarUrl'
)
BEGIN
    ALTER TABLE Users ADD AvatarUrl NVARCHAR(MAX) NULL;
END
GO
