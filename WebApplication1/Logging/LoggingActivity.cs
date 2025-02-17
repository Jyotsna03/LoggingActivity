using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly string logFilePath = "Logs/RequestResponseLog.txt"; // File path for logs

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        // Log Request (optional, can be removed if not needed)
        LogRequest(context);

        // Save original response body stream
        var originalBodyStream = context.Response.Body;

        using (var responseBodyStream = new MemoryStream())
        {
            context.Response.Body = responseBodyStream;

            // Process request
            await _next(context);

            // Log Response to File
            await LogResponseToFile(context);

            // Copy response body back to original stream
            await responseBodyStream.CopyToAsync(originalBodyStream);
        }
    }

    private void LogRequest(HttpContext context)
    {
        var request = context.Request;
        var logMessage = new StringBuilder();
        logMessage.AppendLine($"[Request] {DateTime.Now}");
        logMessage.AppendLine($"Method: {request.Method}");
        logMessage.AppendLine($"URL: {request.Path}");
        logMessage.AppendLine($"Content-Type: {request.ContentType ?? "Not Provided"}");
        logMessage.AppendLine($"----------------------------------");

        _logger.LogInformation(logMessage.ToString());
    }

    private async Task LogResponseToFile(HttpContext context)
    {
        var response = context.Response;

        // Read response body
        response.Body.Seek(0, SeekOrigin.Begin);
        string responseBody = await new StreamReader(response.Body).ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);

        // Prepare log message
        var logMessage = new StringBuilder();
        logMessage.AppendLine($"[Response] {DateTime.Now}");
        logMessage.AppendLine($"Status Code: {response.StatusCode}");
        logMessage.AppendLine($"Content-Type: {response.ContentType ?? "Not Provided"}");
        logMessage.AppendLine($"Response Body: {responseBody}");
        logMessage.AppendLine($"----------------------------------");

        // Ensure directory exists
        string directory = Path.GetDirectoryName(logFilePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write log to file
        await File.AppendAllTextAsync(logFilePath, logMessage.ToString());
    }
}
