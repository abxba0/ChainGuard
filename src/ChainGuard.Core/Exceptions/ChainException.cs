namespace ChainGuard.Core.Exceptions;

/// <summary>
/// Base exception for chain-related errors.
/// </summary>
public class ChainException : Exception
{
    public ChainException() : base()
    {
    }

    public ChainException(string message) : base(message)
    {
    }

    public ChainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
