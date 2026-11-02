using Marketplace.BLL;
using Marketplace.Utility;

namespace Marketplace.IoC;

internal sealed class PhotoProcessorAdapter : IPhotoProcessor
{
    public ProcessedPhoto? Process(byte[] input, int maxSide = 1600)
    {
        var photo = new SkiaPhotoProcessor().Process(input, maxSide);
        return photo is null ? null : new(photo.Content, photo.Width, photo.Height);
    }
}
