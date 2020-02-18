using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Threading;

namespace Doser
{
    internal class RunnerArgs
    {
        [Required]
        [Option("-u|--url <URL>", "Required. URL to include in calls", CommandOptionType.MultipleValue)]
        public List<string> Urls { get; }

        [Required]
        [AllowedValues("get", "post", IgnoreCase = true)]
        [Option("-m|--method <HTTP_METHOD>", "Required. The HTTP method to use (supports: GET, POST)", CommandOptionType.SingleValue)]
        public string HttpMethod { get; }

        [Required]
        [Option("-g|--gap", "Required. Gap between requests (in milliseconds)", CommandOptionType.SingleValue)]
        public int RequestGap { get; }

        [Required]
        [Option("-d|--duration", "Required. Duration to run the app for (in seconds)", CommandOptionType.SingleValue)]
        public int Duration { get; }

        [Required]
        [Option("-p|--parallel", "Required. The number of parallel instances", CommandOptionType.SingleValue)]
        public int ParallelCount { get; }

        [Option("-am|--accept-mime", "The MimeType for accept", CommandOptionType.SingleValue)]
        public string AcceptMime { get; }

        [FileExists]
        [Option("-pf|--payload-file", "A file to use of post as payload content", CommandOptionType.SingleValue)]
        public string PayloadFile { get; }

        public Lazy<string> PayloadFileContent => new Lazy<string>(() => string.IsNullOrWhiteSpace(PayloadFile) ? string.Empty : File.ReadAllText(PayloadFile, Encoding.UTF8));

        [Option("-pm|--payload-mime", "The MimeType for the payload", CommandOptionType.SingleValue)]
        public string PayloadMime { get; }

        [Option("-np|--no-prompt", "Never prompt user input", CommandOptionType.NoValue)]
        public bool NoPrompt { get; }

        [Option("-v|--verbose", "Verbose logging", CommandOptionType.NoValue)]
        public bool Verbose { get; }

        private int OnExecute(CancellationToken cancellationToken) => Runner.Start(this, cancellationToken);
    }
}
