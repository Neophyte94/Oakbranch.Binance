using System;

namespace Oakbranch.Binance.Exceptions
{
    /// <summary>
    /// The exception that is thrown when trying to use an API client that has not been initialized.
    /// </summary>
    public class ClientNotInitializedException : Exception
    {
        public ClientNotInitializedException(ApiClientBase instance) 
            : base(GenerateMessage(instance))
        { }

        private static string GenerateMessage(ApiClientBase instance)
        {
            string className = instance != null ? instance.GetType().Name : "API client";
            string methodName = nameof(ApiClientBase) + "." + nameof(ApiClientBase.InitializeAsync);
            return $"The {className} instance has not been initialized. Ensure to call {methodName}() before using it.";
        }
    }
}