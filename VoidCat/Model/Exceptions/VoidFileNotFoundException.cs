namespace VoidCat.Model.Exceptions;

public class VoidFileNotFoundException : Exception
{
    public VoidFileNotFoundException(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; }
}