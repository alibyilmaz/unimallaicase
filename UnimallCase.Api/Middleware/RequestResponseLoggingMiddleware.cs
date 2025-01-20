using System.Text;

namespace UnimallCase.Api.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Log the request
            var request = await FormatRequest(context.Request);
            _logger.LogInformation("Request: {Request}", request);

            // Copy a pointer to the original response body stream
            var originalBodyStream = context.Response.Body;

            // Create a new memory stream...
            using var responseBody = new MemoryStream();
            // ...and use it for the temporary response body
            context.Response.Body = responseBody;

            // Continue down the Middleware pipeline
            await _next(context);

            // Format the response from the server
            var response = await FormatResponse(context.Response);
            _logger.LogInformation("Response: {Response}", response);

            // Copy the contents of the new memory stream (which contains the response) to the original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }

        private async Task<string> FormatRequest(HttpRequest request)
        {
            request.EnableBuffering();

            var body = await new StreamReader(request.Body).ReadToEndAsync();
            request.Body.Position = 0;

            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("REQUEST");
            messageBuilder.AppendLine($"Path: {request.Path}");
            messageBuilder.AppendLine($"QueryString: {request.QueryString}");
            messageBuilder.AppendLine($"Request Body: {body}");

            return messageBuilder.ToString();
        }

        private async Task<string> FormatResponse(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("RESPONSE");
            messageBuilder.AppendLine($"StatusCode: {response.StatusCode}");
            messageBuilder.AppendLine($"Response Body: {text}");

            return messageBuilder.ToString();
        }
    }
}
