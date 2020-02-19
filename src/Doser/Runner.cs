using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
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
        private static Random rand;
        private static int requestNumber;
        private static Guid testRunIdentifier;

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
            testRunIdentifier = Guid.NewGuid();
            if (runnerArgs.Verbose)
            {
                Console.WriteLine("URLs:");
                foreach (var url in runnerArgs.Urls)
                {
                    Console.WriteLine($"  * {url}");
                }

                Console.WriteLine("");
                Console.WriteLine("Run Details:");
                Console.WriteLine($"  * Test run identifier: {testRunIdentifier}");
                Console.WriteLine($"  * Request type: {runnerArgs.HttpMethod}");
                Console.WriteLine($"  * Gap between requests: {runnerArgs.RequestGap}ms");
                Console.WriteLine($"  * Parallel runners: {runnerArgs.ParallelCount}");
                Console.WriteLine($"  * Run duration: {runnerArgs.Duration}");

                Console.WriteLine("");
                Console.WriteLine("Content Details:");
                Console.WriteLine(!string.IsNullOrWhiteSpace(runnerArgs.AcceptMime) ? $"  * Accept MIME type: {runnerArgs.AcceptMime}" : "  * Accept MIME type: <NONE>");
                if (runnerArgs.HttpMethod.Equals("post", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (runnerArgs.PayloadFiles.Any())
                    {
                        Console.WriteLine("  *Payload files:");
                        foreach (var payloadFile in runnerArgs.PayloadFiles)
                        {
                            Console.WriteLine($"    * {payloadFile}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Payload files: <NONE>");
                    }
                    Console.WriteLine(!string.IsNullOrWhiteSpace(runnerArgs.PayloadMime) ? $"  * Payload file MIME type: {runnerArgs.PayloadMime}" : "  * Payload file MIME type: <NONE>");
                }

                Console.WriteLine("-----------------------------------------------------");
                if (cancellationToken.IsCancellationRequested) return -2;
            }

            var runTimer = new Stopwatch();
            var runDuration = TimeSpan.FromSeconds(runnerArgs.Duration);
            var gapDuration = TimeSpan.FromMilliseconds(runnerArgs.RequestGap);
            results = new ConcurrentBag<WebResult>();
            rand = new Random();
            requestNumber = 0;

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
            var url = runnerArgs.Urls[rand.Next(runnerArgs.Urls.Length)];
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            HttpResponseMessage response;

            if (runnerArgs.HttpMethod.Equals("get", StringComparison.InvariantCultureIgnoreCase))
            {
                response = await client.GetAsync(url);
            }
            else if (runnerArgs.HttpMethod.Equals("post", StringComparison.InvariantCultureIgnoreCase))
            {
                var payloadFileName = runnerArgs.PayloadFiles[rand.Next(runnerArgs.PayloadFiles.Length)];
                response = await client.PostAsync(url, new StringContent(runnerArgs.PayloadFilesContent.Value[payloadFileName], Encoding.UTF8, runnerArgs.PayloadMime));
            }
            else
            {
                Console.WriteLine($"Unknown method {runnerArgs.HttpMethod}");
                return;
            }

            stopwatch.Stop();
            var result = new WebResult(response.StatusCode, stopwatch.Elapsed);
            results.Add(result);
#pragma warning disable 4014
            Task.Run(() =>
            {
                var runRequestNumber = Interlocked.Increment(ref requestNumber);
                if (runnerArgs.LogFailuresOnly)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[{runRequestNumber}] - Got {result.StatusCode} after {result.DurationMs}ms on {url}");
                    }
                }
                else
                {
                    Console.WriteLine($"[{runRequestNumber}] - Got {result.StatusCode} after {result.DurationMs}ms on {url}");
                }

                if (!string.IsNullOrWhiteSpace(runnerArgs.OutputDir))
                {
                    var subDirNumber = Math.Floor(runRequestNumber / 10d) * 10;
                    var subDir = $"{subDirNumber}-{subDirNumber + 9}";
                    var outputDir = Path.Combine(runnerArgs.OutputDir, testRunIdentifier.ToString(), subDir);
                    Directory.CreateDirectory(outputDir);
                    File.WriteAllText(Path.Combine(outputDir, $"{runRequestNumber}_ResponseHeaders.txt"), response.Headers.ToString());
                    File.WriteAllText(Path.Combine(outputDir, $"{runRequestNumber}_ContentHeaders.txt"), response.Content.Headers.ToString());
                    File.WriteAllText(Path.Combine(outputDir, $"{runRequestNumber}_ContentBody.txt"), response.Content.ReadAsStringAsync().Result);
                }
            });
#pragma warning restore 4014
        }
    }
}
