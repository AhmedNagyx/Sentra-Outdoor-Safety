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
            var path = context.Request.Path;
            var method = context.Request.Method;

            // Only protect AI-facing endpoints
            var isIncidentPost = path.StartsWithSegments("/api/incidents")
                                 && method == HttpMethods.Post;

            var isHeartbeat = path.StartsWithSegments("/api/cameras")
                              && path.Value!.EndsWith("/heartbeat")
                              && method == HttpMethods.Patch;

            if (isIncidentPost || isHeartbeat)
            {
                if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedKey))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { message = "API Key missing" });
                    return;
                }

                var validKey = config["ApiKeys:AiModelKey"];
                if (string.IsNullOrEmpty(validKey) || extractedKey != validKey)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { message = "Invalid API Key" });
                    return;
                }
            }

            await _next(context);
        }
    }
}