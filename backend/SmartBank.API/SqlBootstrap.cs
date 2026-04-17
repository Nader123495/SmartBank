using Microsoft.Data.SqlClient;

namespace SmartBank.API;

internal static class SqlBootstrap
{
    internal static string ResolveSmartBankConnectionString(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString("SmartBankDB")
            ?? throw new InvalidOperationException("Connection string 'SmartBankDB' is missing.");
        var envPwd = Environment.GetEnvironmentVariable("MSSQL_SA_PASSWORD");
        if (string.IsNullOrEmpty(envPwd))
            return configured;

        var csb = new SqlConnectionStringBuilder(configured);
        if (!csb.IntegratedSecurity)
            csb.Password = envPwd;
        return csb.ConnectionString;
    }

    internal static void EnsureSqlServerCatalogExists(string connectionString)
    {
        var csb = new SqlConnectionStringBuilder(connectionString);
        var catalog = csb.InitialCatalog;
        if (string.IsNullOrWhiteSpace(catalog))
            return;

        csb.InitialCatalog = "master";
        using var conn = new SqlConnection(csb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        var lit = catalog.Replace("'", "''");
        var bracketed = catalog.Replace("]", "]]");
        cmd.CommandText =
            $"IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'{lit}') CREATE DATABASE [{bracketed}];";
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Applies idempotent schema patches after EF has created/updated the base schema.
    /// Safe to run on every startup — each statement checks for column existence first.
    /// </summary>
    internal static void ApplySchemaPatches(string connectionString)
    {
        using var conn = new SqlConnection(connectionString);
        conn.Open();

        var patches = new[]
        {
            // Users — profile details
            "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'City') " +
            "ALTER TABLE Users ADD City NVARCHAR(150) NULL;",

            "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'Gender') " +
            "ALTER TABLE Users ADD Gender NVARCHAR(20) NULL;",

            "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'PhoneNumber') " +
            "ALTER TABLE Users ADD PhoneNumber NVARCHAR(50) NULL;",

            "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'AccountNumber') " +
            "ALTER TABLE Users ADD AccountNumber NVARCHAR(100) NULL;",

            // Complaints — geolocation at submission time
            "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Complaints') AND name = 'SubmissionCity') " +
            "ALTER TABLE Complaints ADD SubmissionCity NVARCHAR(150) NULL;",

            "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Complaints') AND name = 'Latitude') " +
            "ALTER TABLE Complaints ADD Latitude FLOAT NULL;",

            "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Complaints') AND name = 'Longitude') " +
            "ALTER TABLE Complaints ADD Longitude FLOAT NULL;",

            // Specific fields for dynamic categories
            "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Complaints') AND name = 'CardLastFour') " +
            "ALTER TABLE Complaints ADD CardLastFour NVARCHAR(4) NULL;",

            "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Complaints') AND name = 'IncidentDate') " +
            "ALTER TABLE Complaints ADD IncidentDate DATETIME2 NULL;",

            "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Complaints') AND name = 'AccountType') " +
            "ALTER TABLE Complaints ADD AccountType NVARCHAR(100) NULL;",

            "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Complaints') AND name = 'Amount') " +
            "ALTER TABLE Complaints ADD Amount DECIMAL(18,2) NULL;",

            "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Complaints') AND name = 'VirementReference') " +
            "ALTER TABLE Complaints ADD VirementReference NVARCHAR(100) NULL;",

            "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Complaints') AND name = 'CreditType') " +
            "ALTER TABLE Complaints ADD CreditType NVARCHAR(100) NULL;",

            "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Complaints') AND name = 'DossierNumber') " +
            "ALTER TABLE Complaints ADD DossierNumber NVARCHAR(100) NULL;"
        };

        foreach (var sql in patches)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }
}
