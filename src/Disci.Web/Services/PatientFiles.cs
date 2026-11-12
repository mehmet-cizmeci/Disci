using Disci.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Disci.Web.Services;

public sealed class FileStorageOptions
{
    public const string Section = "FileStorage";
    /// <summary>Root folder for patient files (absolute, or relative to the app's content root); PatientPhoto.FilePath is relative to it.</summary>
    public string Root { get; set; } = "";
}

public static class PatientFileEndpoints
{
    /// <summary>Photos are served only to signed-in users and only from inside the storage root.</summary>
    public static void MapPatientFiles(this IEndpointRouteBuilder app) =>
        app.MapGet("/files/photos/{id:int}", async (int id, DisciDbContext db, IOptions<FileStorageOptions> options, IWebHostEnvironment env) =>
        {
            var relative = await db.PatientPhotos.Where(p => p.Id == id).Select(p => p.FilePath).FirstOrDefaultAsync();
            if (relative is null || string.IsNullOrWhiteSpace(options.Value.Root))
                return Results.NotFound();

            var root = Path.GetFullPath(Path.Combine(env.ContentRootPath, options.Value.Root));
            var path = Path.GetFullPath(Path.Combine(root, relative));
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
                return Results.NotFound();

            var contentType = Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream",
            };
            return Results.File(path, contentType);
        }).RequireAuthorization();
}
