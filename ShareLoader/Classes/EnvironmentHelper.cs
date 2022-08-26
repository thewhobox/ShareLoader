

namespace ShareLoader.Classes;

public class EnvironmentHelper
{
    public static string GetVariable(string name)
    {
        string key = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
        if(!string.IsNullOrEmpty(key)) return key;
        key = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
        if(!string.IsNullOrEmpty(key)) return key;
        key = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        if(!string.IsNullOrEmpty(key)) return key;
        key = Environment.GetEnvironmentVariable(name);
        return key;
    }
}