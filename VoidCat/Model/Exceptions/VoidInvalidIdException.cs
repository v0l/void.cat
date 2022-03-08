namespace VoidCat.Model.Exceptions;

/// <summary>
/// Specified id was not in the correct format
/// </summary>
public class VoidInvalidIdException : Exception
{
    public VoidInvalidIdException(string id)
    {
        Id = id;
    }

    /// <summary>
    /// The id in question
    /// </summary>
    public string Id { get; }
}