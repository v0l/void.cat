using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using VoidCat.Model;

namespace VoidCat.Services.Migrations;

public class EFMigrationSetup : IMigration
{
    private readonly VoidContext _db;
    private readonly VoidSettings _settings;

    public EFMigrationSetup(VoidContext db, VoidSettings settings)
    {
        _db = db;
        _settings = settings;
    }

    public int Order => -99;

    public async ValueTask<IMigration.MigrationResult> Migrate(string[] args)
    {
        if (!_settings.HasPostgres()) return IMigration.MigrationResult.Skipped;

        var conn = (_db.Database.GetDbConnection() as NpgsqlConnection)!;
        await conn.OpenAsync();
        try
        {
            await using var cmd = new NpgsqlCommand("select max(\"Version\") from \"VersionInfo\"", conn);

            var vMax = await cmd.ExecuteScalarAsync() as long?;
            if (!(vMax > 0)) return IMigration.MigrationResult.Skipped;

            await PrepEfMigration(conn);
        }
        catch (DbException dx) when (dx.SqlState is "42P01")
        {
            //ignored, VersionInfo does not exist
            return IMigration.MigrationResult.Skipped;
        }

        return IMigration.MigrationResult.Completed;
    }

    private static async Task PrepEfMigration(NpgsqlConnection conn)
    {
        await using var tx = await conn.BeginTransactionAsync();

        await new NpgsqlCommand(@"
ALTER TABLE ""Files"" ALTER COLUMN ""Size"" TYPE numeric(20) USING ""Size""::numeric;
ALTER TABLE ""Payment"" RENAME COLUMN ""File"" TO ""FileId"";
ALTER TABLE ""UserFiles"" RENAME COLUMN ""File"" TO ""FileId"";
ALTER TABLE ""UserFiles"" RENAME COLUMN ""User"" TO ""UserId"";
ALTER TABLE ""UserRoles"" RENAME COLUMN ""User"" TO ""UserId"";
ALTER TABLE ""UsersAuthToken"" RENAME COLUMN ""User"" TO ""UserId"";
ALTER TABLE ""UsersAuthToken"" ADD ""IdToken"" text NULL;
ALTER TABLE ""VirusScanResult"" RENAME COLUMN ""File"" TO ""FileId"";
ALTER TABLE ""ApiKey"" RENAME CONSTRAINT ""FK_ApiKey_UserId_Users_Id"" TO ""FK_ApiKey_Users_UserId"";
ALTER TABLE ""UserFiles"" RENAME CONSTRAINT ""FK_UserFiles_File_Files_Id"" TO ""FK_UserFiles_Files_FileId"";
ALTER TABLE ""UserFiles"" RENAME CONSTRAINT ""FK_UserFiles_User_Users_Id"" TO ""FK_UserFiles_Users_UserId"";
ALTER TABLE ""UserRoles"" RENAME CONSTRAINT ""FK_UserRoles_User_Users_Id"" TO ""FK_UserRoles_Users_UserId"";
ALTER TABLE ""UsersAuthToken"" RENAME CONSTRAINT ""FK_UsersAuthToken_User_Users_Id"" TO ""FK_UsersAuthToken_Users_UserId"";
ALTER TABLE ""VirusScanResult"" RENAME CONSTRAINT ""FK_VirusScanResult_File_Files_Id"" TO ""FK_VirusScanResult_Files_FileId"";

DROP TABLE ""EmailVerification"";
DROP TABLE ""PaymentOrderLightning"";
DROP TABLE ""PaymentOrder"";
DROP TABLE ""PaymentStrike"";
DROP TABLE ""Payment"";
DROP TABLE ""VersionInfo"";
", conn, tx).ExecuteNonQueryAsync();

        // manually create init migration entry for EF to skip Init migration
        await new NpgsqlCommand(@"
CREATE TABLE ""__EFMigrationsHistory"" (
    ""MigrationId"" varchar(150) NOT NULL,
    ""ProductVersion"" varchar(32) NOT NULL,
CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
)", conn, tx).ExecuteNonQueryAsync();

        await new NpgsqlCommand(
                "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES('20230503115108_Init', '7.0.5')", conn,
                tx)
            .ExecuteNonQueryAsync();

        await tx.CommitAsync();
    }
}
