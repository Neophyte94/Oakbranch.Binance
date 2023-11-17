using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Oakbranch.Common.Logging;
using Oakbranch.Binance.Models.Spot;
using Oakbranch.Binance.Clients;
using Oakbranch.Binance.Core;
using Oakbranch.Binance.Core.RateLimits;
using Oakbranch.Binance.Abstractions;

namespace Oakbranch.Binance.Benchmark
{
    public class Program
    {
        private const string ApiKeysContainerName = "ApiKeys.ini";

        static void Main()
        {
            ApiConnector connector = null;
            CancellationTokenSource cts = null;
            try
            {
                cts = new CancellationTokenSource();
                ILogger logger = new ConsoleLogger();

                (string, string)? keysSet = null;
                while (keysSet == null)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    FileStream keyFileStream = TryOpenApiKeysContainer();
                    if (keyFileStream == null)
                    {
                        Console.WriteLine(
                            "The API keys container was not found. Please create the file \"ApiKeys.ini\" " +
                            "in the runtime directory containing a 64-character API key " +
                            "and (optionally) a 64-character secret key for accessing Binance API.");
                        Console.WriteLine("Once you create the file, press any key to proceed, or type \"exit\" to terminate.");

                        if (WaitForInputLineAsync().Result)
                        {
                            cts.Cancel();
                        }
                        continue;
                    }

                    try
                    {
                        keysSet = ReadApiKeysAsync(keyFileStream, cts.Token).Result;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine(exc);
                        if (WaitForInputLineAsync().Result)
                        {
                            cts.Cancel();
                        }
                    }
                    finally
                    {
                        keyFileStream?.Close();
                        keyFileStream = null;
                    }
                }

                connector = new ApiConnector(keysSet.Value.Item1, keysSet.Value.Item2);
                IRateLimitsRegistry limitsRegistry = new RateLimitsRegistry(limitCapacity: 8);
                
                Task testTask = TestEndpointsAsync(connector, limitsRegistry, logger, cts.Token);
                Task inputTask = WaitForInputKeyAsync();
                Console.WriteLine("The test has been started. Press any key to interrupt it.");
                Console.WriteLine();

                if (Task.WhenAny(testTask, inputTask).Result == inputTask)
                {
                    cts.Cancel();
                    try { testTask.Wait(); }
                    catch { }

                    Console.WriteLine("The test has been interrupted.");
                }
                else
                {
                    if (testTask.IsFaulted)
                    {
                        Console.WriteLine($"The test failed:\r\n{testTask.Exception.InnerException}");
                    }
                    else
                    {
                        Console.WriteLine("The test is complete.");
                    }
                }

                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
            catch (OperationCanceledException) when (cts?.IsCancellationRequested == true)
            {
                Console.WriteLine("The program was manually terminated.");
            }
            catch (Exception exc)
            {
                Console.WriteLine($"A critical error occurred:\r\n{exc}");
            }
            finally
            {
                Console.WriteLine("Exiting the program...");
                connector?.Dispose();
                cts?.Dispose();
            }
        }

        private static async Task TestEndpointsAsync(
            ApiConnector connector, IRateLimitsRegistry limitsRegistry, ILogger logger, CancellationToken ct)
        {
            if (connector == null)
                throw new ArgumentNullException(nameof(connector));
            if (connector.IsDisposed)
                throw new ArgumentException("The specified API connector is disposed.");
            if (limitsRegistry == null)
                throw new ArgumentNullException(nameof(limitsRegistry));

            using (SpotMarketApiClient client = new SpotMarketApiClient(connector, limitsRegistry, logger))
            {
                await client.InitializeAsync(ct).ConfigureAwait(false);

                Dictionary<BaseEndpoint, EndpointSummary> bepsDict =
                    new Dictionary<BaseEndpoint, EndpointSummary>(ApiV3ClientBase.RESTBaseEndpoints.Count);
                foreach (BaseEndpoint bep in ApiV3ClientBase.RESTBaseEndpoints)
                {
                    if (bep.Type == NetworkType.Live)
                    {
                        bepsDict.Add(bep, new EndpointSummary(bep.Description));
                    }
                }

                int trials = 5;
                Stopwatch sw = new Stopwatch();

                for (int i = 0; i < trials; ++i)
                {
                    foreach (var pair in bepsDict)
                    {
                        client.RESTEndpoint = pair.Key;
                        sw.Restart();
                        _ = await client.TestConnectivityAsync(ct).ConfigureAwait(false);
                        sw.Stop();
                        pair.Value.ShortQueryTests.Add(sw.Elapsed);
                    }
                }

                for (int i = 0; i < trials; ++i)
                {
                    foreach (var pair in bepsDict)
                    {
                        sw.Restart();
                        _ = await client.GetSymbolPriceTickerAsync(ct, symbols: new string[] { "BTCUSDT", "ETHUSDT" })
                            .ConfigureAwait(false);
                        sw.Stop();
                        pair.Value.MediumQueryTests.Add(sw.Elapsed);
                    }
                }

                for (int i = 0; i < trials; ++i)
                {
                    foreach (var pair in bepsDict)
                    {
                        client.RESTEndpoint = pair.Key;
                        sw.Restart();
                        _ = await client.GetCandlestickDataAsync(
                            "BTCUSDT", KlineInterval.Hour1, limit: 1000, startTime: new DateTime(2022, 01, 01), ct: ct)
                            .ConfigureAwait(false);
                        sw.Stop();
                        pair.Value.LongQueryTests.Add(sw.Elapsed);
                    }
                }

                Console.WriteLine("Endpoint;Avg sq time;Avg mq time;Avg lq time");
                foreach (EndpointSummary summary in bepsDict.Values)
                {
                    Console.WriteLine(
                        $"{summary.Endpoint};" +
                        $"{summary.ShortQueryAvgDuration?.TotalMilliseconds};" +
                        $"{summary.MediumQueryAvgDuration?.TotalMilliseconds};" +
                        $"{summary.LongQueryAvgDuration?.TotalMilliseconds}");
                }
            }
        }

        private static FileStream TryOpenApiKeysContainer()
        {
            if (!File.Exists(ApiKeysContainerName))
            {
                return null;
            }

            return new FileStream(ApiKeysContainerName, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        private static async Task<(string, string)> ReadApiKeysAsync(Stream inputStream, CancellationToken ct)
        {
            using (StreamReader sr = new StreamReader(inputStream, Encoding.ASCII))
            {
                ct.ThrowIfCancellationRequested();
                if (sr.EndOfStream)
                {
                    throw new Exception("The specified stream is empty.");
                }

                string key = await sr.ReadLineAsync(ct).ConfigureAwait(false);
                if (key == null || key.Length != 64)
                {
                    throw new Exception(
                        $"The API key in the keys container has an invalid format ({key?.Length}). " +
                        $"The valid key is represented by a 64-character ASCII string.");
                }

                string secret;
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

        /// <summary>
        /// Reads the console input line and checks whether it matches the exit keyword ("exit").
        /// </summary>
        /// <returns><see langword="true"/> if an exit is requested, otherwise <see langword="false"/>.</returns>
        private static Task<bool> WaitForInputLineAsync()
        {
            return Task.Run(() =>
            {
                string rsp = Console.ReadLine();
                return String.Equals(rsp, "exit", StringComparison.InvariantCultureIgnoreCase);
            });
        }

        /// <summary>
        /// Reads the console input key and returns it.
        /// </summary>
        /// <returns><see langword="true"/> if an exit is requested, otherwise <see langword="false"/>.</returns>
        private static Task<ConsoleKeyInfo> WaitForInputKeyAsync()
        {
            return Task.Run(Console.ReadKey);
        }
    }
}