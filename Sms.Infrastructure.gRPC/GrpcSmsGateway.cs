using Google.Protobuf.WellKnownTypes;
using Sms.Core.Abstractions;
using Sms.Test; // proto

namespace Sms.Infrastructure.gRPC
{
    public class GrpcSmsGateway : ISmsGateway
    {
        private readonly SmsTestService.SmsTestServiceClient _client;

        public GrpcSmsGateway(SmsTestService.SmsTestServiceClient client)
        {
            _client = client;
        }

        public async Task<IEnumerable<Core.Models.MenuItem>> GetMenuAsync()
        {
            // Вызываем RPC метод. 
            // GetMenu принимает BoolValue (WithPrice = true)
            var response = await _client.GetMenuAsync(new BoolValue { Value = true });

            if (!response.Success)
                throw new Exception(response.ErrorMessage);

            // Маппинг из gRPC моделей в ваши Core модели
            return response.MenuItems.Select(x => new Core.Models.MenuItem
            {
                Id = x.Id,
                Article = x.Article,
                Name = x.Name,
                Price = (decimal)x.Price, // В proto double, в Core decimal
                IsWeighted = x.IsWeighted,
                FullPath = x.FullPath,
                Barcodes = x.Barcodes.ToList()
            });
        }

        public async Task SendOrderAsync(Core.Models.Order order)
        {
            // Собираем gRPC модель заказа
            var grpcOrder = new Order
            {
                Id = order.Id.ToString()
            };

            // Добавляем элементы заказа
            foreach (var item in order.MenuItems)
            {
                grpcOrder.OrderItems.Add(new OrderItem
                {
                    Id = item.Id,
                    Quantity = item.Quantity
                });
            }

            var response = await _client.SendOrderAsync(grpcOrder);

            if (!response.Success)
                throw new Exception(response.ErrorMessage);
        }
    }
}
