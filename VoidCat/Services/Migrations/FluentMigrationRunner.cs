using FluentMigrator.Runner;

namespace VoidCat.Services.Migrations;

/// <inheritdoc />
public class FluentMigrationRunner : IMigration
{
    private readonly IMigrationRunner _runner;

    public FluentMigrationRunner(IMigrationRunner runner)
    {
        _runner = runner;
    }

    /// <inheritdoc />
    public int Order => -1;

    /// <inheritdoc />
    public ValueTask<IMigration.MigrationResult> Migrate(string[] args)
    {
        _runner.MigrateUp();
        return ValueTask.FromResult(IMigration.MigrationResult.Completed);
    }
}