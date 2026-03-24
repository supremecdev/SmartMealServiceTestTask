using Sms.Core.Abstractions;
using Sms.Core.Models;
using Sms.Z1.Services;
using Sms.Z1.Utils;
using Serilog;

namespace Sms.Z1.Infrastructure
{
    public class SmsApp(
        ISmsGateway gateway,
        IDatabaseService dbService,
        ILogger logger)
    {
        public async Task RunAsync()
        {
            logger.Information("Приложение запущено.");

            await dbService.InitSchemaAsync();

            // Получение меню
            var menu = await FetchMenuAsync();
            if (menu == null) return;

            // Сохранение и вывод
            await dbService.SaveMenuItemsAsync(menu);
            PrintMenu(menu);

            // Работа с заказом
            var items = InteractionHelper.GetOrderFromUser(menu, logger);
            var order = new Order() { MenuItems = items };

            await SendOrderAsync(order);
        }

        private async Task<List<MenuItem>?> FetchMenuAsync()
        {
            try
            {
                return (await gateway.GetMenuAsync()).ToList();
            }
            catch (Exception ex)
            {
                logger.Error("Критическая ошибка получения меню: {Message}", ex.Message);
                return null;
            }
        }

        private async Task SendOrderAsync(Order order)
        {
            try
            {
                await gateway.SendOrderAsync(order);
                logger.Information("Результат: УСПЕХ");
            }
            catch (Exception ex)
            {
                logger.Error("Результат: ОШИБКА - {Message}", ex.Message);
            }
        }

        private void PrintMenu(List<MenuItem> menu) =>
            menu.ForEach(i => Console.WriteLine($"{i.Name} – {i.Article} – {i.Price}"));
    }
}
