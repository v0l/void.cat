namespace VoidCat.Model.Exceptions;

/// <summary>
/// Specified file was not found
/// </summary>
public class VoidFileNotFoundException : Exception
{
    public VoidFileNotFoundException(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; }
}