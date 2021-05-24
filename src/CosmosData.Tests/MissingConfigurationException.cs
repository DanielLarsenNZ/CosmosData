using System;

namespace CosmosData.Tests
{
    public class MissingConfigurationException : Exception
    {
        public MissingConfigurationException(string configKey) : base($"App Setting \"{configKey}\" is missing.")
        {
        }
    }
}
