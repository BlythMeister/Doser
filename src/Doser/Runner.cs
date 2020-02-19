using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Doser
{
    internal static class Runner
    {
        private static ConcurrentBag<WebResult> results;

        public static int Start(RunnerArgs runnerArgs, CancellationToken cancellationToken)
        {
            try
            {
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                var version = FileVersionInfo.GetVersionInfo(assemblyLocation).ProductVersion;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("");
                Console.WriteLine("----------------------------");
                Console.WriteLine("|                          |");
                Console.WriteLine("|           Doser          |");
                Console.WriteLine($"|{version.PadLeft(10 + version.Length).PadRight(26)}|");
                Console.WriteLine("|                          |");
                Console.WriteLine("|    Author: Chris Blyth   |");
                Console.WriteLine("|                          |");
                Console.WriteLine("----------------------------");
                Console.WriteLine("");
                Console.WriteLine($"Starting at {DateTime.UtcNow:u}");
                Console.WriteLine("");
                Console.ResetColor();
                return Run(runnerArgs, cancellationToken).Result;
            }
            catch (Exception e)
            {
                Console.WriteLine("-----------------------------------------------------");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("An Error Occured:");
                Console.WriteLine("");
                if (runnerArgs.Verbose)
                {
                    Console.WriteLine(e);
                }
                else
                {
                    Console.WriteLine(e.Message);
                }
                Console.WriteLine("");
                Console.ResetColor();
                Console.WriteLine("-----------------------------------------------------");
                return -1;
            }
            finally
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Done at {DateTime.UtcNow:u}");
                if (!runnerArgs.NoPrompt)
                {
                    Console.WriteLine("Press Enter To Close...");
                    Console.ReadLine();
                }
                Console.ResetColor();
            }
        }

        private static async Task<int> Run(RunnerArgs runnerArgs, CancellationToken cancellationToken)
        {
            Console.WriteLine("URLs:");
            foreach (var url in runnerArgs.Urls)
            {
                Console.WriteLine($"  * {url}");
            }

            Console.WriteLine("");
            Console.WriteLine("Run Details:");
            Console.WriteLine($"  * Request type: {runnerArgs.HttpMethod}");
            Console.WriteLine($"  * Gap between requests: {runnerArgs.RequestGap}ms");
            Console.WriteLine($"  * Parallel runners: {runnerArgs.ParallelCount}");
            Console.WriteLine($"  * Run duration: {runnerArgs.Duration}");

            Console.WriteLine("");
            Console.WriteLine("Content Details:");
            Console.WriteLine(!string.IsNullOrWhiteSpace(runnerArgs.AcceptMime) ? $"  * Accept MIME type: {runnerArgs.AcceptMime}" : "  * Accept MIME type: <NONE>");
            if (runnerArgs.HttpMethod.Equals("post", StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine(!string.IsNullOrWhiteSpace(runnerArgs.PayloadFile) ? $"  * Upload payload file: {runnerArgs.PayloadFile}" : "  * Upload payload file: <NONE>");
                Console.WriteLine(!string.IsNullOrWhiteSpace(runnerArgs.PayloadMime) ? $"  * Payload file MIME type: {runnerArgs.PayloadMime}" : "  * Payload file MIME type: <NONE>");
            }

            Console.WriteLine("-----------------------------------------------------");
            if (cancellationToken.IsCancellationRequested) return -2;

            var runTimer = new Stopwatch();
            var runDuration = TimeSpan.FromSeconds(runnerArgs.Duration);
            var gapDuration = TimeSpan.FromMilliseconds(runnerArgs.RequestGap);
            results = new ConcurrentBag<WebResult>();

            using var clientHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                AllowAutoRedirect = true,
                MaxConnectionsPerServer = 500
            };

            var client = new HttpClient(clientHandler);

            await Task.WhenAll(Enumerable.Range(1, runnerArgs.ParallelCount).Select(i => Task.Run(async () =>
            {
                runTimer.Start();

                if (!string.IsNullOrWhiteSpace(runnerArgs.AcceptMime))
                {
                    client.DefaultRequestHeaders.Add("Accept", runnerArgs.AcceptMime);
                }

                while (runTimer.Elapsed <= runDuration && !cancellationToken.IsCancellationRequested)
                {
                    await DoRun(runnerArgs, client);
                    await Task.Delay(gapDuration, cancellationToken);
                }
            }, cancellationToken)));
            if (cancellationToken.IsCancellationRequested) return -2;

            Console.WriteLine("");
            Console.WriteLine("Results:");
            foreach (var result in results.GroupBy(x => x.StatusCode))
            {
                Console.WriteLine($"HTTP Status '{result.Key}': {result.Count()} - Average time - {Math.Round(result.Average(x => x.Duration.TotalMilliseconds), 2)}ms");
            }

            return 0;
        }

        private static async Task DoRun(RunnerArgs runnerArgs, HttpClient client)
        {
            var rand = new Random();
            var url = runnerArgs.Urls[rand.Next(runnerArgs.Urls.Count)];
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            HttpResponseMessage response;

            if (runnerArgs.HttpMethod.Equals("get", StringComparison.InvariantCultureIgnoreCase))
            {
                response = await client.GetAsync(url);
            }
            else if (runnerArgs.HttpMethod.Equals("post", StringComparison.InvariantCultureIgnoreCase))
            {
                response = await client.PostAsync(url, new StringContent(runnerArgs.PayloadFileContent.Value, Encoding.UTF8, runnerArgs.PayloadMime));
            }
            else
            {
                Console.WriteLine($"Unknown method {runnerArgs.HttpMethod}");
                return;
            }

            stopwatch.Stop();
            var contentLength = (await response.Content.ReadAsByteArrayAsync()).Length;
            var result = new WebResult(response.StatusCode, stopwatch.Elapsed, contentLength);
            results.Add(result);
            Console.WriteLine($"Got {result.StatusCode} after {result.DurationMs}ms with a content length of {result.SizeKb}kb on {url}");
        }
    }
}
