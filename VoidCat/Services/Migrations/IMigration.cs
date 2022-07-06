namespace VoidCat.Services.Migrations;

/// <summary>
/// Startup migrations
/// </summary>
public interface IMigration
{
    /// <summary>
    /// Order to run migrations
    /// </summary>
    int Order { get; }
    
    /// <summary>
    /// Run migration
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    ValueTask<MigrationResult> Migrate(string[] args);

    /// <summary>
    /// Results of running migration
    /// </summary>
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