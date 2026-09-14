namespace UpdCommanderChecker;

internal sealed class ConfigException : Exception
{
    internal ConfigException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
