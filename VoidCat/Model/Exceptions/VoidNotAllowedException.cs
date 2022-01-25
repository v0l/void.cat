namespace VoidCat.Model.Exceptions;

public class VoidNotAllowedException : Exception
{
    public VoidNotAllowedException(string message) : base(message)
    {
    }
}