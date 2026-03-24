using Sms.MockServer.Services;

namespace Sms.MockServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddGrpc();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.MapGrpcService<SmsService>();

            app.Run();
        }
    }
}