namespace VoidCat.Services.Migrations;

public interface IMigration
{
    ValueTask Migrate();
}