using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Sms.Core.Abstractions;
using Sms.Infrastructure.gRPC;
using Sms.Test;
using Sms.Z1.Infrastructure;
using Sms.Z1.Services;

namespace Sms.Z1
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Настройка логгера
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File($"test-sms-console-app-{DateTime.Now:yyyyMMdd}.log")
                .CreateLogger();

            // Настройка DI-контейнера
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            // Регистрация сервисов
            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton<ILogger>(Log.Logger);
            services.AddSingleton<IDatabaseService>(new PgDatabaseService(config.GetConnectionString("DefaultConnection")!));

            // Отключение прокси для процесса, стандартная регистрация грпс не взлетает в моем случае
            HttpClient.DefaultProxy = new System.Net.WebProxy();

            // 2. Регистрация клиента в DI
            services.AddSingleton<SmsTestService.SmsTestServiceClient>(sp =>
            {
                // Игнорирование прокси и тлс
                var handler = new HttpClientHandler
                {
                    Proxy = null,
                    UseProxy = false,
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                var channel = GrpcChannel.ForAddress(config["GrpcSettings:ServiceUrl"]!, new GrpcChannelOptions
                {
                    HttpHandler = handler
                });

                return new SmsTestService.SmsTestServiceClient(channel);
            });

            services.AddScoped<ISmsGateway, GrpcSmsGateway>();
            services.AddScoped<SmsApp>();

            // Запуск
            var provider = services.BuildServiceProvider();
            var app = provider.GetRequiredService<SmsApp>();

            await app.RunAsync();

            Console.WriteLine("Нажмите любую клавишу для выхода из приложения");
            Console.ReadKey();
        }
    }
}
