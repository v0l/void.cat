namespace VoidCat.Services.Migrations;

public interface IMigration
{
    ValueTask<MigrationResult> Migrate(string[] args);

    public enum MigrationResult
    {
        /// <summary>
        /// Migration was not run
        /// </summary>
        Skipped,
        
        /// <summary>
        /// Migration completed successfully, continue to startup
        /// </summary>
        Completed,
        
        /// <summary>
        /// Migration completed Successfully, exit application
        /// </summary>
        ExitCompleted
    }
}