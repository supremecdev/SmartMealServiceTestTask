using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using Sms.Test;

namespace Sms.MockServer.Services
{
    public class SmsService : SmsTestService.SmsTestServiceBase
    {
        public override Task<GetMenuResponse> GetMenu(BoolValue request, ServerCallContext context)
        {
            var response = new GetMenuResponse { Success = true };
            response.MenuItems.Add(new MenuItem
            {
                Id = "1",
                Name = "Тестовое Блюдо",
                Price = 100,
                Article = "T001"
            });
            return Task.FromResult(response);
        }

        public override Task<SendOrderResponse> SendOrder(Order request, ServerCallContext context)
        {
            return Task.FromResult(new SendOrderResponse { Success = true });
        }
    }
}
