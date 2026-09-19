using Marketplace.Utility;
using SkiaSharp;

namespace Marketplace.Api.Tests;

public sealed class PhotoProcessingTests
{
    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Webp)]
    public void Supported_images_are_decoded_and_reduced(SKEncodedImageFormat format)
    {
        using var surface = SKSurface.Create(new SKImageInfo(2000, 1000));
        surface.Canvas.Clear(SKColors.Coral);
        using var image = surface.Snapshot();
        using var encoded = image.Encode(format, 90);
        var result = Assert.IsType<NormalizedPhoto>(new SkiaPhotoProcessor().Process(encoded.ToArray()));
        Assert.Equal(1600, result.Width);
        Assert.Equal(800, result.Height);
        Assert.InRange(result.Content.Length, 1, 1024 * 1024);
        using var decoded = SKBitmap.Decode(result.Content);
        Assert.Equal(1600, decoded.Width);
        Assert.Null(new SkiaPhotoProcessor().Process(encoded.ToArray().Take(25).ToArray()));
    }

    [Theory]
    [InlineData(1, 40, 20)]
    [InlineData(2, 40, 20)]
    [InlineData(3, 40, 20)]
    [InlineData(4, 40, 20)]
    [InlineData(5, 20, 40)]
    [InlineData(6, 20, 40)]
    [InlineData(7, 20, 40)]
    [InlineData(8, 20, 40)]
    public void Exif_orientation_is_applied_and_metadata_removed(int origin, int width, int height)
    {
        using var surface = SKSurface.Create(new SKImageInfo(40, 20));
        surface.Canvas.Clear(SKColors.Coral);
        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 95);
        byte[] exif = [255, 225, 0, 34, 69, 120, 105, 102, 0, 0, 73, 73, 42, 0, 8, 0, 0, 0,
            1, 0, 18, 1, 3, 0, 1, 0, 0, 0, (byte)origin, 0, 0, 0, 0, 0, 0, 0];
        var input = encoded.ToArray();
        byte[] withExif = [..input[..2], ..exif, ..input[2..]];
        var result = Assert.IsType<NormalizedPhoto>(new SkiaPhotoProcessor().Process(withExif));
        Assert.Equal(width, result.Width);
        Assert.Equal(height, result.Height);
        using var data = SKData.CreateCopy(result.Content);
        using var codec = SKCodec.Create(data);
        Assert.Equal(SKEncodedOrigin.TopLeft, codec.EncodedOrigin);
        Assert.DoesNotContain("Exif", System.Text.Encoding.Latin1.GetString(result.Content));
    }
}
