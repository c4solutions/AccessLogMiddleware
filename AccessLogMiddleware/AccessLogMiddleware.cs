using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AccessLogMiddleware
{
    internal class AccessLogMiddleware
    {
        private readonly RequestDelegate _next;

        public ILogger<AccessLogMiddleware> Logger { get; }

        public AccessLogMiddleware(RequestDelegate next, ILogger<AccessLogMiddleware> logger)
        {
            _next = next;
            Logger = logger;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            string rawTarget = httpContext.Features.Get<IHttpRequestFeature>().RawTarget;
            System.IO.Stream originalBody = httpContext.Response.Body;

            LogState state = new LogState()
            {
                HttpContext = httpContext,
                ResponseStream = new CountingStream(httpContext.Response.Body),
                StartDate = DateTime.Now,
                RemoteHost = httpContext.Connection.RemoteIpAddress.ToString(),
                RequestLine = $"{httpContext.Request.Method} {rawTarget} {httpContext.Request.Protocol}",
            };

            httpContext.Response.Body = state.ResponseStream;
            httpContext.Response.OnStarting(OnStarting, state);
            httpContext.Response.OnCompleted(OnCompleted, state);

            try
            {
                await _next(httpContext);
            }
            catch (Exception)
            {
                state.StatusCode = 500;
                throw;
            }
            finally
            {
                httpContext.Response.Body = originalBody;
            }
        }

        private Task OnStarting(object arg)
        {
            LogState state = (LogState)arg;
            HttpContext httpContext = state.HttpContext;

            state.StatusCode = httpContext.Response.StatusCode;

            // Authentication is run later in the pipeline so this may be our first chance to see the result
            state.AuthUser = httpContext.User.Identity.Name ?? "Anonymous";

            return Task.CompletedTask;
        }

        private Task OnCompleted(object arg)
        {
            LogState state = (LogState)arg;
            state.Bytes = state.ResponseStream.BytesWritten;

            WriteLog(state);
            return Task.CompletedTask;
        }

        private void WriteLog(LogState state)
        {
            Logger.LogInformation($"{state.RemoteHost} {state.Rfc931} {state.AuthUser} [{state.StartDate:dd/MMM/yyyy:HH:mm:ss zzz}] \"{state.RequestLine}\" {state.StatusCode} {state.Bytes}");
        }

        private class LogState
        {
            public HttpContext HttpContext { get; set; }
            public CountingStream ResponseStream { get; set; }

            public string RemoteHost { get; set; }
            public string Rfc931 { get; set; } = "Rfc931"; // ??
            public string AuthUser { get; set; }
            public DateTime StartDate { get; set; }
            public string RequestLine { get; set; }
            public int StatusCode { get; set; }
            public long Bytes { get; set; }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class AccessLogMiddlewareExtensions
    {
        public static IApplicationBuilder UseAccessLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AccessLogMiddleware>();
        }
    }
}