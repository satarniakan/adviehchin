namespace Adviehchin.App.Middleware;

public sealed class OptimizedImageMiddleware(RequestDelegate next, IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var originalPath = context.Request.Path;
        var extension = Path.GetExtension(originalPath.Value);
        if (!(HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)) ||
            !originalPath.StartsWithSegments("/images") ||
            !(string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) ||
              string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
              string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Append("Vary", "Accept");
            return Task.CompletedTask;
        });
        var acceptsWebP = context.Request.GetTypedHeaders().Accept?.Any(value =>
            string.Equals(value.MediaType.Value, "image/webp", StringComparison.OrdinalIgnoreCase) &&
            (value.Quality ?? 1) > 0) == true;
        if (acceptsWebP)
        {
            var source = environment.WebRootFileProvider.GetFileInfo(originalPath.Value!);
            var optimizedPath = originalPath.Value + ".webp";
            var optimized = environment.WebRootFileProvider.GetFileInfo(optimizedPath);
            // A manually replaced original must not be hidden by an older optimized copy.
            if (source.Exists && optimized.Exists && optimized.LastModified >= source.LastModified)
                context.Request.Path = optimizedPath;
        }

        try
        {
            await next(context);
        }
        finally
        {
            context.Request.Path = originalPath;
        }
    }
}
