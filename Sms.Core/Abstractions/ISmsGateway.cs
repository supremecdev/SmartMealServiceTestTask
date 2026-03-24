using Sms.Core.Models;

namespace Sms.Core.Abstractions
{
    public interface ISmsGateway
    {
        Task<IEnumerable<MenuItem>> GetMenuAsync();
        Task SendOrderAsync(Order order);
    }
}
