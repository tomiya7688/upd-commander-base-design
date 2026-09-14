namespace UpdCommanderChecker;

internal sealed class ConfigException : Exception
{
    internal ConfigException(string message)
        : base(message) { }
}
