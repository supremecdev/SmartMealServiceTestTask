namespace Sms.Core.Models
{
    public class Order
    {
        public Guid Id { get; } = Guid.NewGuid();
        public IReadOnlyList<OrderItem> MenuItems = Array.Empty<OrderItem>();
    }
}
