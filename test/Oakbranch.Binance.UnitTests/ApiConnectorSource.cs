﻿using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Oakbranch.Binance.Abstractions;
using Oakbranch.Binance.Core;
using Oakbranch.Binance.Core.TimeProviders;

namespace Oakbranch.Binance.UnitTests
{
    public static class ApiConnectorSource
    {
        #region Nested types

        private sealed class BuiltInConnectorFactory : IApiConnectorFactory, IDisposable
        {
            private readonly ILoggerFactory _loggerFactory;
            private string _apiKey;
            private string? _secretKey;

            public BuiltInConnectorFactory(string apiKey, string? secretKey)
            {
                _loggerFactory = new ConsoleLoggerFactory(DefaultLogLevel);
                _apiKey = apiKey;
                _secretKey = secretKey;
            }

            public IApiConnector Create()
            {
                if (_apiKey == null)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
                return new ApiConnector(
                    apiKey: _apiKey,
                    secretKey: _secretKey,
                    timeProvider: new SystemTimeProvider(),
                    logger: _loggerFactory.CreateLogger<ApiConnector>());
            }

            public void Dispose()
            {
                _apiKey = null!;
                _secretKey = null;
                _loggerFactory.Dispose();
            }
        }

        #endregion

        #region Constants

        public const string KeyContainerPath = "TestApiKeys.ini";
        private const LogLevel DefaultLogLevel = LogLevel.Debug;

        #endregion

        #region Static methods

        public static IEnumerable<IApiConnectorFactory> CreateAllWithKeyContainer()
        {
            if (!TryReadApiKeysFromContainer(out string apiKey, out string? secretKey))
            {
                throw new Exception("Unable to read API keys from the file container.");
            }

            return CreateAll(apiKey, secretKey);
        }

        public static IEnumerable<IApiConnectorFactory> CreateAll(string apiKey, string? secretKey)
        {
            if (String.IsNullOrEmpty(apiKey))
                throw new ArgumentNullException(nameof(apiKey));

            IApiConnectorFactory builtInConnectorFactory = CreateBuiltIn(apiKey, secretKey);
            return new List<IApiConnectorFactory> { builtInConnectorFactory, };
        }

        public static IApiConnectorFactory CreateBuiltIn(string apiKey, string? secretKey)
        {
            if (String.IsNullOrEmpty(apiKey))
                throw new ArgumentNullException(nameof(apiKey));
            if (String.IsNullOrEmpty(secretKey))
                secretKey = null;

            return new BuiltInConnectorFactory(apiKey, secretKey);
        }

        public static bool TryReadApiKeysFromContainer(out string apiKey, out string? secretKey)
        {
            string containerPath = KeyContainerPath;

            try
            {
                using (FileStream fs = new FileStream(containerPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (CancellationTokenSource cts = new CancellationTokenSource(5000))
                    {
                        (string, string?) keys = ReadApiKeysAsync(fs, cts.Token).Result;
                        apiKey = keys.Item1;
                        secretKey = keys.Item2;
                        return true;
                    }
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc);
                apiKey = null!;
                secretKey = null;
                return false;
            }
        }

        private static async Task<(string, string?)> ReadApiKeysAsync(Stream inputStream, CancellationToken ct)
        {
            using (StreamReader sr = new StreamReader(inputStream, Encoding.ASCII))
            {
                ct.ThrowIfCancellationRequested();
                if (sr.EndOfStream)
                {
                    throw new Exception("The specified stream is empty.");
                }

                string? key = await sr.ReadLineAsync(ct).ConfigureAwait(false);
                if (key == null || key.Length != 64)
                {
                    throw new Exception(
                        $"The API key in the keys container has an invalid format ({key?.Length}). " +
                        $"The valid key is represented by a 64-character ASCII string.");
                }

                string? secret;
                if (!sr.EndOfStream)
                {
                    ct.ThrowIfCancellationRequested();
                    secret = await sr.ReadLineAsync(ct).ConfigureAwait(false);
                    if (secret != null && secret.Length != 64)
                    {
                        throw new Exception(
                            $"The secret key in the keys container has an invalid format ({key?.Length}). " +
                            $"The valid key is represented by a 64-character ASCII string.");
                    }
                }
                else
                {
                    secret = null;
                }

                return new(key, secret);
            }
        }

        #endregion
    }
}
