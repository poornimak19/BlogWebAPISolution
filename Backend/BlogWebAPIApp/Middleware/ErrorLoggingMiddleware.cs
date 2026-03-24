using System.Net;
using System.Security.Claims;
using System.Text.Json;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models;

namespace BlogWebAPIApp.Middleware
{
    public class ErrorLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorLoggingMiddleware> _logger;

        public ErrorLoggingMiddleware(RequestDelegate next, ILogger<ErrorLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context, IRepository<int, ErrorLog> errorLogs)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Log unhandled exceptions → 500 errors
                await TryLogExceptionAsync(context, errorLogs, ex);
                await WriteSafe500ResponseAsync(context);
                return;
            }

            // 🎯 LOG ALL 4xx RESPONSES (400–499)
            if (context.Response.StatusCode >= 400 && context.Response.StatusCode < 500)
            {
                await TryLogClientErrorAsync(context, errorLogs);
            }
        }

        // =============== LOGGING HELPERS ===============

        private async Task TryLogExceptionAsync(
            HttpContext ctx,
            IRepository<int, ErrorLog> errorLogs,
            Exception ex)
        {
            try
            {
                var log = new ErrorLog
                {
                    ErrorMessage = BuildExceptionMessage(ex, ctx),
                    ErrorNumber = ex.HResult,
                    CreatedAt = DateTime.UtcNow
                };

                await errorLogs.Add(log);
            }
            catch (Exception loggingEx)
            {
                _logger.LogError(loggingEx, "Failed to write 500 ErrorLog.");
            }
        }

        private async Task TryLogClientErrorAsync(
            HttpContext ctx,
            IRepository<int, ErrorLog> errorLogs)
        {
            try
            {
                var log = new ErrorLog
                {
                    ErrorMessage = BuildClientErrorMessage(ctx),
                    ErrorNumber = ctx.Response.StatusCode, // 400–499
                    CreatedAt = DateTime.UtcNow
                };

                await errorLogs.Add(log);
            }
            catch (Exception loggingEx)
            {
                _logger.LogError(loggingEx, "Failed to write 4xx ErrorLog.");
            }
        }

        private async Task WriteSafe500ResponseAsync(HttpContext ctx)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var response = new
            {
                message = "An unexpected error occurred. Please try again later.",
                traceId = ctx.TraceIdentifier
            };

            if (!ctx.Response.HasStarted)
            {
                await ctx.Response.WriteAsJsonAsync(response);
            }
        }

        // =============== MESSAGE BUILDERS ===============

        private static string BuildExceptionMessage(Exception ex, HttpContext ctx)
        {
            var method = ctx.Request.Method;
            var path = ctx.Request.Path;
            var query = ctx.Request.QueryString.HasValue ? ctx.Request.QueryString.Value : "";
            var traceId = ctx.TraceIdentifier;
            var userId = ResolveUserId(ctx);

            return $"500 Exception | {method} {path}{query} | TraceId={traceId} | User={userId ?? "anonymous"} | Error={FlattenExceptionMessages(ex)}";
        }

        private static string BuildClientErrorMessage(HttpContext ctx)
        {
            var method = ctx.Request.Method;
            var path = ctx.Request.Path;
            var query = ctx.Request.QueryString.HasValue ? ctx.Request.QueryString.Value : "";
            var traceId = ctx.TraceIdentifier;
            var status = ctx.Response.StatusCode;
            var userId = ResolveUserId(ctx);

            return $"Client Error {status} | {method} {path}{query} | TraceId={traceId} | User={userId ?? "anonymous"}";
        }

        private static string FlattenExceptionMessages(Exception ex)
        {
            var list = new List<string>();
            var cur = ex;

            while (cur != null)
            {
                list.Add($"{cur.GetType().Name}: {cur.Message}");
                cur = cur.InnerException;
            }

            return string.Join(" -> ", list);
        }

        private static string? ResolveUserId(HttpContext ctx)
        {
            return ctx.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? ctx.User?.FindFirstValue(ClaimTypes.Name)
                ?? ctx.User?.FindFirstValue("sub");
        }
    }
}