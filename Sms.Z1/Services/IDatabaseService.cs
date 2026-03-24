using Sms.Core.Models;

namespace Sms.Z1.Services
{
    public interface IDatabaseService
    {
        Task InitSchemaAsync();
        Task SaveMenuItemsAsync(IEnumerable<MenuItem> items);
    }
}
