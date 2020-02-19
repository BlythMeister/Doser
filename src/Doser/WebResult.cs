using System;
using System.Net;

namespace Doser
{
    internal class WebResult
    {
        public HttpStatusCode StatusCode { get; }
        public TimeSpan Duration { get; }
        public double DurationMs => Math.Round(Duration.TotalMilliseconds, 2);

        public WebResult(HttpStatusCode statusCode, TimeSpan duration)
        {
            StatusCode = statusCode;
            Duration = duration;
        }
    }
}
