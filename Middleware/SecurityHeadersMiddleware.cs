namespace WebHS.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
            
            // Content Security Policy
            var csp = "default-src 'self'; " +
                     "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://unpkg.com; " +
                     "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.googleapis.com https://unpkg.com; " +
                     "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com; " +
                     "img-src 'self' data: https: blob: https://*.tile.openstreetmap.org; " +
                     "connect-src 'self' https://api.stripe.com https://provinces.open-api.vn https://nominatim.openstreetmap.org wss://localhost:* ws://localhost:* http://localhost:*;";
            
            context.Response.Headers["Content-Security-Policy"] = csp;

            await _next(context);
        }
    }
}
