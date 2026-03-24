namespace Sms.Infrastructure.Http.Responses
{
    public class BaseResponse
    {
        public string Command { get; init; } = string.Empty;
        public bool Success { get; init; }
        public string ErrorMessage { get; init; } = string.Empty;
    }
}
