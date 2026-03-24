using System.Globalization;
using System.Text.RegularExpressions;
using Sms.Core.Models;
using Serilog;

namespace Sms.Z1.Utils;

public static class InteractionHelper
{
    public static List<OrderItem> GetOrderFromUser(List<MenuItem> menu, ILogger logger)
    {
        while (true)
        {
            Console.WriteLine("\n>>> Введите заказ (Формат: Код1:Количество1;Код2:Количество2...):");
            string input = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(input))
            {
                logger.Warning("Ввод не может быть пустым.");
                continue;
            }

            var result = new List<OrderItem>();
            bool hasError = false;

            // Разделяем по точке с запятой
            var parts = input.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                // Регулярка для проверки формата Код:Количество
                var match = Regex.Match(part.Trim(), @"^(?<code>.+):(?<qty>.+)$");

                if (!match.Success)
                {
                    logger.Warning("Неверный формат позиции '{Part}'. Ожидается Код:Количество", part);
                    hasError = true;
                    break;
                }

                string code = match.Groups["code"].Value;
                string qtyStr = match.Groups["qty"].Value.Replace(',', '.'); // Поддержка и точки, и запятой

                // Проверка существования кода (артикула)
                var menuItem = menu.FirstOrDefault(m => m.Article == code);
                if (menuItem == null)
                {
                    logger.Warning("Блюдо с кодом '{Code}' не найдено в меню.", code);
                    hasError = true;
                    break;
                }

                // Проверка количеств
                if (!double.TryParse(qtyStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double qty) || qty <= 0)
                {
                    logger.Warning("Некорректное количество '{Qty}' для кода {Code}. Должно быть число > 0.", qtyStr, code);
                    hasError = true;
                    break;
                }

                result.Add(new OrderItem { Id = menuItem.Id, Quantity = qty });
            }

            if (!hasError && result.Count > 0)
                return result;

            Console.WriteLine("Пожалуйста, исправьте ошибки и попробуйте снова.");
        }
    }
}