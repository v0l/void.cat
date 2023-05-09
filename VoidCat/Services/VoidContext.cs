using Microsoft.EntityFrameworkCore;
using VoidCat.Database;
using File = VoidCat.Database.File;

namespace VoidCat.Services;

public class VoidContext : DbContext
{
    public VoidContext()
    {
    }

    public VoidContext(DbContextOptions<VoidContext> ctx) : base(ctx)
    {
    }
    
    public DbSet<User> Users => Set<User>();
    public DbSet<File> Files => Set<File>();
    public DbSet<VirusScanResult> VirusScanResults => Set<VirusScanResult>();
    public DbSet<Paywall> Paywalls => Set<Paywall>();
    public DbSet<PaywallOrder> PaywallOrders => Set<PaywallOrder>();
    public DbSet<UserAuthToken> UserAuthTokens => Set<UserAuthToken>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<EmailVerification> EmailVerifications => Set<EmailVerification>();
    public DbSet<UserFile> UserFiles => Set<UserFile>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VoidContext).Assembly);
    }
}
