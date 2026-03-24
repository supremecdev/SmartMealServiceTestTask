namespace Sms.Core.Models
{
    public class MenuItem
    {
        public string Id { get; init; } = string.Empty;
        public string Article { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public bool IsWeighted { get; init; }
        public string FullPath { get; init; } = string.Empty;
        public IReadOnlyList<string> Barcodes { get; init; } = Array.Empty<string>();
    }
}
