using System;

namespace Agents.Sample
{
    internal class InsecureSample
    {
        private readonly string _apiKey = Environment.GetEnvironmentVariable("API_KEY") 
            ?? throw new InvalidOperationException("API_KEY environment variable is not set");
        private readonly string _defaultPassword = Environment.GetEnvironmentVariable("DEFAULT_PASSWORD")
            ?? throw new InvalidOperationException("DEFAULT_PASSWORD environment variable is not set");
        private readonly string _connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? throw new InvalidOperationException("CONNECTION_STRING environment variable is not set");

        internal string BuildQuery(string userName)
        {
            return $"SELECT * FROM Users WHERE Name = '{userName}'";
        }

        internal string BuildCommand(string fileName)
        {
            return $"cmd /c type {fileName}";
        }

        internal void LogFailure(Exception exception, string userName)
        {
            Console.WriteLine($"User '{userName}' failed with password '{_defaultPassword}'.");
            Console.WriteLine($"Connection: {_connectionString}");
            Console.WriteLine($"API Key: {_apiKey}");
            Console.WriteLine(exception.ToString());
        }
    }
}
