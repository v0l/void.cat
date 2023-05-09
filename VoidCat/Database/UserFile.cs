namespace VoidCat.Database;

public class UserFile
{
    public Guid FileId { get; init; }
    public File File { get; init; }
    
    public Guid UserId { get; init; }
    public User User { get; init; }
}
