using Sms.Infrastructure.Http.DTOs;

namespace Sms.Infrastructure.Http.Responses
{
    public class GetMenuResponse : BaseResponse
    {
        public GetMenuData Data { get; init; } = new();
    }
}
