using Sms.Core.Abstractions;
using Sms.Core.Models;
using Sms.Infrastructure.Http.Responses;
using System.Net.Http.Json;

namespace Sms.Infrastructure.Http
{
    public class HttpSmsGateway : ISmsGateway
    {
        private readonly HttpClient _httpClient;

        public HttpSmsGateway(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<MenuItem>> GetMenuAsync()
        {
            var request = new
            {
                Command = "GetMenu",
                CommandParameters = new { WithPrice = true }
            };

            var response = await _httpClient.PostAsJsonAsync("", request);

            var content = await response.Content.ReadFromJsonAsync<GetMenuResponse>()
                ?? throw new Exception("Empty response content");

            if (!content.Success)
                throw new Exception(content.ErrorMessage);

            // Если Data или MenuItems будут null, вернется пустая коллекция вместо ошибки
            return content.Data?.MenuItems?.Select(x => new MenuItem
            {
                Id = x.Id,
                Article = x.Article,
                Barcodes = x.Barcodes,
                FullPath = x.FullPath,
                IsWeighted = x.IsWeighted,
                Name = x.Name,
                Price = x.Price
            }) ?? Enumerable.Empty<MenuItem>();
        }

        public async Task SendOrderAsync(Order order)
        {
            var request = new
            {
                Command = "SendOrder",
                CommandParameters = new
                {
                    OrderId = order.Id,
                    MenuItems = order.MenuItems
                }
            };

            var response = await _httpClient.PostAsJsonAsync("", request);

            var content = await response.Content.ReadFromJsonAsync<BaseResponse>()
                ?? throw new Exception("Empty response content");

            if (!content.Success)
                throw new Exception(content.ErrorMessage);
        }
    }
}
