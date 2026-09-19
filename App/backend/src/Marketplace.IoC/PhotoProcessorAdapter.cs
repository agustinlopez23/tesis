using Marketplace.BLL;
using Marketplace.Utility;

namespace Marketplace.IoC;

internal sealed class PhotoProcessorAdapter : IPhotoProcessor
{
    public ProcessedPhoto? Process(byte[] input)
    {
        var photo = new SkiaPhotoProcessor().Process(input);
        return photo is null ? null : new(photo.Content, photo.Width, photo.Height);
    }
}
