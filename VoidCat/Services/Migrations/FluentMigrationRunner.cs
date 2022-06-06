using FluentMigrator.Runner;

namespace VoidCat.Services.Migrations;

public class FluentMigrationRunner : IMigration
{
    private readonly IMigrationRunner _runner;

    public FluentMigrationRunner(IMigrationRunner runner)
    {
        _runner = runner;
    }

    public ValueTask Migrate(string[] args)
    {
        _runner.MigrateUp();
        return ValueTask.CompletedTask;
    }

    public bool ExitOnComplete => false;
}