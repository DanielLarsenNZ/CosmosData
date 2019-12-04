using System;

namespace CosmosData
{
    public class MissingConfigurationException : Exception
    {
        public MissingConfigurationException(string configKey) : base($"App Setting \"{configKey}\" is missing.")
        {
        }
    }
}
