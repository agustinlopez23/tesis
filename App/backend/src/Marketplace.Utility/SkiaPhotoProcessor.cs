using SkiaSharp;

namespace Marketplace.Utility;

public sealed record NormalizedPhoto(byte[] Content, int Width, int Height);

public sealed class SkiaPhotoProcessor
{
    public NormalizedPhoto? Process(byte[] input, int maxSide = 1600)
    {
        if (input.Length is 0 or > 5 * 1024 * 1024 || maxSide < 1) return null;
        using var data = SKData.CreateCopy(input);
        using var codec = SKCodec.Create(data);
        if (codec is null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp) ||
            codec.FrameCount > 1 || codec.Info.Width < 1 || codec.Info.Height < 1 ||
            (long)codec.Info.Width * codec.Info.Height > 20_000_000) return null;
        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);
        if (codec.GetPixels(info, bitmap.GetPixels()) != SKCodecResult.Success) return null;
        var swapped = (int)codec.EncodedOrigin >= 5;
        var width = swapped ? info.Height : info.Width;
        var height = swapped ? info.Width : info.Height;
        var scale = Math.Min(1f, (float)maxSide / Math.Max(width, height));
        var outputInfo = new SKImageInfo(Math.Max(1, (int)(width * scale)), Math.Max(1, (int)(height * scale)));
        using var surface = SKSurface.Create(outputInfo);
        surface.Canvas.Clear(SKColors.White);
        surface.Canvas.Scale(scale);
        var matrix = Orientation(codec.EncodedOrigin, info.Width, info.Height);
        surface.Canvas.Concat(matrix);
        using var source = SKImage.FromBitmap(bitmap);
        surface.Canvas.DrawImage(source, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));
        using var image = surface.Snapshot();
        // Re-encoding pixels discards uploaded EXIF/GPS and other metadata.
        foreach (var quality in new[] { 85, 70, 50 })
        {
            using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, quality);
            if (encoded.Size <= 1024 * 1024) return new(encoded.ToArray(), outputInfo.Width, outputInfo.Height);
        }
        return null;
    }

    private static SKMatrix Orientation(SKEncodedOrigin origin, int w, int h)
    {
        var (a, b, c, d, tx, ty) = origin switch
        {
            SKEncodedOrigin.TopRight => (-1, 0, 0, 1, w, 0),
            SKEncodedOrigin.BottomRight => (-1, 0, 0, -1, w, h),
            SKEncodedOrigin.BottomLeft => (1, 0, 0, -1, 0, h),
            SKEncodedOrigin.LeftTop => (0, 1, 1, 0, 0, 0),
            SKEncodedOrigin.RightTop => (0, -1, 1, 0, h, 0),
            SKEncodedOrigin.RightBottom => (0, -1, -1, 0, h, w),
            SKEncodedOrigin.LeftBottom => (0, 1, -1, 0, 0, w),
            _ => (1, 0, 0, 1, 0, 0)
        };
        return new SKMatrix { ScaleX = a, SkewX = b, SkewY = c, ScaleY = d, TransX = tx, TransY = ty, Persp2 = 1 };
    }
}
