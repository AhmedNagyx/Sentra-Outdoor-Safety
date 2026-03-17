namespace Sentra.API.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private const string API_KEY_HEADER = "X-Api-Key";

        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IConfiguration config)
        {
            // Only protect AI endpoints
            if (context.Request.Path.StartsWithSegments("/api/incidents") ||
                (context.Request.Path.StartsWithSegments("/api/cameras") &&
                 context.Request.Path.Value!.EndsWith("/heartbeat")))
            {
                // Check header exists
                if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedKey))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        message = "API Key missing"
                    });
                    return;
                }

                // Validate key
                var validKey = config["ApiKeys:AiModelKey"];
                if (string.IsNullOrEmpty(validKey) || extractedKey != validKey)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        message = "Invalid API Key"
                    });
                    return;
                }
            }

            await _next(context);
        }
    }
}