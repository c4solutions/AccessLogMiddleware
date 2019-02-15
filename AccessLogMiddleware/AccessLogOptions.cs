using Microsoft.Extensions.Logging;

namespace AccessLogMiddleware
{
    public class AccessLogOptions
    {
        public LogLevel LogLevel { get; set; } = LogLevel.Information;
        public string StatusCodes { get; set; } = string.Empty;
    }
}