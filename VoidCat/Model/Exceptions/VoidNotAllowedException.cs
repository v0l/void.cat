namespace VoidCat.Model.Exceptions;

/// <summary>
/// Operation is not allowed
/// </summary>
public class VoidNotAllowedException : Exception
{
    public VoidNotAllowedException(string message) : base(message)
    {
    }
}