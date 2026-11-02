using Marketplace.DTOs;
using Marketplace.Models;

namespace Marketplace.BLL;

public sealed record ProcessedPhoto(byte[] Content, int Width, int Height);
public interface IPhotoProcessor
{
    ProcessedPhoto? Process(byte[] input, int maxSide = 1600);
}
public interface IBusinessProfileRepository
{
    Task<PublicBusinessProfileResponse?> GetPublicAsync(Guid id, CancellationToken ct);
    Task<bool> IsOwnedAsync(Guid id, Guid ownerId, CancellationToken ct);
    Task<IReadOnlyList<BusinessPhotoResponse>> GetPhotosAsync(Guid id, CancellationToken ct);
    Task<byte[]?> GetPhotoAsync(Guid id, int slot, Guid? ownerId, CancellationToken ct);
    Task SavePhotoAsync(BusinessPhoto photo, CancellationToken ct);
    Task<bool> DeletePhotoAsync(Guid id, int slot, CancellationToken ct);
}
public enum PhotoSaveStatus { Saved, Invalid, TooLarge }
public sealed record PhotoSaveResult(PhotoSaveStatus Status, BusinessPhotoResponse? Photo = null);

public sealed class BusinessProfileService(IBusinessProfileRepository repository, IPhotoProcessor processor)
{
    public const int MaxBytes = 5 * 1024 * 1024;
    public Task<PublicBusinessProfileResponse?> GetPublicAsync(Guid id, CancellationToken ct) => repository.GetPublicAsync(id, ct);
    public Task<bool> IsOwnedAsync(Guid id, Guid ownerId, CancellationToken ct) => repository.IsOwnedAsync(id, ownerId, ct);
    public Task<IReadOnlyList<BusinessPhotoResponse>> GetPhotosAsync(Guid id, CancellationToken ct) => repository.GetPhotosAsync(id, ct);
    public Task<byte[]?> GetPhotoAsync(Guid id, int slot, Guid? ownerId, CancellationToken ct) => repository.GetPhotoAsync(id, slot, ownerId, ct);
    public Task<bool> DeletePhotoAsync(Guid id, int slot, CancellationToken ct) => repository.DeletePhotoAsync(id, slot, ct);

    public async Task<PhotoSaveResult> SavePhotoAsync(Guid id, int slot, string caption, byte[] input, CancellationToken ct)
    {
        if (input.Length > MaxBytes) return new(PhotoSaveStatus.TooLarge);
        if (slot is < 1 or > 5 || caption.Length > 160 || input.Length == 0) return new(PhotoSaveStatus.Invalid);
        var processed = processor.Process(input);
        if (processed is null) return new(PhotoSaveStatus.Invalid);
        var photo = new BusinessPhoto { BusinessId = id, Slot = slot, Caption = caption.Trim(),
            Width = processed.Width, Height = processed.Height, Content = processed.Content };
        await repository.SavePhotoAsync(photo, ct);
        return new(PhotoSaveStatus.Saved, new(photo.Slot, photo.Caption, photo.Width, photo.Height));
    }
}
