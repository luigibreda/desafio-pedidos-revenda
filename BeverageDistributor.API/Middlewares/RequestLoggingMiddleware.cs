using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeverageDistributor.API.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;
            var requestInfo = new
            {
                Method = request.Method,
                Path = request.Path,
                QueryString = request.QueryString,
                Headers = request.Headers
                    .Where(h => !h.Key.StartsWith("Authorization"))
                    .ToDictionary(h => h.Key, h => h.Value.ToString()),
                RemoteIp = context.Connection.RemoteIpAddress?.ToString()
            };

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestId"] = context.TraceIdentifier,
                ["RequestPath"] = request.Path
            }))
            {
                _logger.LogInformation("Requisição recebida: {@RequestInfo}", requestInfo);

                var originalBodyStream = context.Response.Body;
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                var sw = Stopwatch.StartNew();
                try
                {
                    await _next(context);
                    sw.Stop();

                    responseBody.Seek(0, SeekOrigin.Begin);
                    var responseBodyText = await new StreamReader(responseBody).ReadToEndAsync();
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream);

                    _logger.LogInformation("Requisição concluída em {ElapsedMilliseconds}ms com {StatusCode}: {ResponseBody}",
                        sw.ElapsedMilliseconds, context.Response.StatusCode, responseBodyText);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    _logger.LogError(ex, "Requisição falhou após {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
                    throw;
                }
                finally
                {
                    context.Response.Body = originalBodyStream;
                }
            }
        }
    }

    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}
