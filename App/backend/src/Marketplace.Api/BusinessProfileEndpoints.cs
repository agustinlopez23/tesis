using Marketplace.BLL;
using Microsoft.AspNetCore.Http.Features;

namespace Marketplace.Api;

public static class BusinessProfileEndpoints
{
    public static void MapBusinessProfiles(this WebApplication app)
    {
        app.MapGet("/public/businesses/{id:guid}", async (Guid id, HttpResponse response, BusinessProfileService profiles, ActivityTokens tokens, CancellationToken ct) =>
        {
            response.Headers.CacheControl = "no-store";
            var profile = await profiles.GetPublicAsync(id, ct);
            return profile is null ? Results.NotFound() : Results.Ok(profile with { MeasurementToken = tokens.Issue(new(id, "unknown", null, null)) });
        });
        app.MapGet("/public/businesses/{id:guid}/photos/{slot:int}", async (
            Guid id, int slot, HttpResponse response, BusinessProfileService profiles, CancellationToken ct) =>
            await Photo(id, slot, null, response, profiles, ct));
        app.MapGet("/businesses/{id:guid}/photos", async (
            Guid id, HttpRequest request, HttpResponse response, AuthenticationService auth, BusinessProfileService profiles, CancellationToken ct) =>
        {
            response.Headers.CacheControl = "no-store";
            var owner = await auth.GetSessionAsync(request.Cookies["marketplace_session"], ct);
            if (owner is null) return Results.Unauthorized();
            if (!await profiles.IsOwnedAsync(id, owner.Id, ct)) return Results.NotFound();
            return Results.Ok(await profiles.GetPhotosAsync(id, ct));
        });
        app.MapGet("/businesses/{id:guid}/photos/{slot:int}", async (
            Guid id, int slot, HttpRequest request, HttpResponse response, AuthenticationService auth, BusinessProfileService profiles, CancellationToken ct) =>
        {
            response.Headers.CacheControl = "no-store";
            var owner = await auth.GetSessionAsync(request.Cookies["marketplace_session"], ct);
            return owner is null ? Results.Unauthorized() : await Photo(id, slot, owner.Id, response, profiles, ct);
        });
        app.MapPut("/businesses/{id:guid}/photos/{slot:int}", async (
            Guid id, int slot, HttpRequest request, HttpResponse response, IConfiguration config,
            AuthenticationService auth, BusinessProfileService profiles, CancellationToken ct) =>
        {
            response.Headers.CacheControl = "no-store";
            if (!Trusted(request, config)) return Results.StatusCode(403);
            var owner = await auth.GetSessionAsync(request.Cookies["marketplace_session"], ct);
            if (owner is null) return Results.Unauthorized();
            if (!await profiles.IsOwnedAsync(id, owner.Id, ct)) return Results.NotFound();
            if (slot is < 1 or > 5) return InvalidPhoto();
            if (request.ContentLength > BusinessProfileService.MaxBytes + 65536) return Results.StatusCode(413);
            if (!request.HasFormContentType) return InvalidPhoto();
            try
            {
                var form = await request.ReadFormAsync(new FormOptions {
                    MultipartBodyLengthLimit = BusinessProfileService.MaxBytes + 1,
                    ValueLengthLimit = 1024, ValueCountLimit = 4
                }, ct);
                var file = form.Files.GetFile("file");
                if (file is null || form.Files.Count != 1) return InvalidPhoto();
                if (file.Length > BusinessProfileService.MaxBytes) return Results.StatusCode(413);
                await using var buffer = new MemoryStream();
                await file.CopyToAsync(buffer, ct);
                var result = await profiles.SavePhotoAsync(id, slot, form["caption"].ToString(), buffer.ToArray(), ct);
                return result.Status switch
                {
                    PhotoSaveStatus.Saved => Results.Ok(result.Photo),
                    PhotoSaveStatus.TooLarge => Results.StatusCode(413),
                    _ => InvalidPhoto()
                };
            }
            catch (InvalidDataException error) when (error.Message.Contains("length limit", StringComparison.OrdinalIgnoreCase))
            {
                return Results.StatusCode(413);
            }
            catch (InvalidDataException) { return InvalidPhoto(); }
            catch (BadHttpRequestException error) { return Results.StatusCode(error.StatusCode); }
        });
        app.MapDelete("/businesses/{id:guid}/photos/{slot:int}", async (
            Guid id, int slot, HttpRequest request, HttpResponse response, IConfiguration config,
            AuthenticationService auth, BusinessProfileService profiles, CancellationToken ct) =>
        {
            response.Headers.CacheControl = "no-store";
            if (!Trusted(request, config)) return Results.StatusCode(403);
            var owner = await auth.GetSessionAsync(request.Cookies["marketplace_session"], ct);
            if (owner is null) return Results.Unauthorized();
            if (!await profiles.IsOwnedAsync(id, owner.Id, ct)) return Results.NotFound();
            return await profiles.DeletePhotoAsync(id, slot, ct) ? Results.NoContent() : Results.NotFound();
        });
    }
    private static bool Trusted(HttpRequest request, IConfiguration config) =>
        request.Headers.Origin == config["FrontendOrigin"] && request.Headers["X-Requested-With"] == "marketplace";
    private static IResult InvalidPhoto() => Results.BadRequest(new {
        message = "Elegí una foto JPEG, PNG o WebP válida, de hasta 5 MB y 20 megapíxeles, y una descripción de hasta 160 caracteres."
    });
    private static async Task<IResult> Photo(Guid id, int slot, Guid? ownerId, HttpResponse response, BusinessProfileService profiles, CancellationToken ct)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers["X-Content-Type-Options"] = "nosniff";
        var bytes = await profiles.GetPhotoAsync(id, slot, ownerId, ct);
        return bytes is null ? Results.NotFound() : Results.Bytes(bytes, "image/jpeg");
    }
}
