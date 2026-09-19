namespace Marketplace.Models;

public sealed class BusinessPhoto
{
    public Guid BusinessId { get; set; }
    public int Slot { get; set; }
    public required string Caption { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public required byte[] Content { get; set; }
}
