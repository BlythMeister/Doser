using System;
using System.Net;

namespace Doser
{
    internal class WebResult
    {
        public HttpStatusCode StatusCode { get; }
        public TimeSpan Duration { get; }
        public int Size { get; }

        public double DurationMs => Math.Round(Duration.TotalMilliseconds, 2);

        public double SizeKb => Math.Round(Size / 1000d, 2);

        public WebResult(HttpStatusCode statusCode, TimeSpan duration, int size)
        {
            StatusCode = statusCode;
            Duration = duration;
            Size = size;
        }
    }
}
