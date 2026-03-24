using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Serilog;
using Sms.Z2.Models;
using System.Collections.ObjectModel;
using System.IO;

namespace Sms.Z2.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel()
        {
            SetupLogger();
            LoadVariables();
        }

        public ObservableCollection<EnvVarModel> Variables { get; } = new();

        private void SetupLogger()
        {
            string logName = $"test-sms-wpf-app-{DateTime.Now:yyyyMMdd}.log";
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(logName, outputTemplate: "{Timestamp:HH:mm:ss} | {Message}{NewLine}")
                .CreateLogger();
        }

        private void LoadVariables()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var varNames = config.GetSection("EnvVariables").Get<string[]>();

            foreach (var name in varNames ?? Array.Empty<string>())
            {
                var val = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User) ?? "INIT_VALUE";

                if (Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User) == null)
                {
                    Environment.SetEnvironmentVariable(name, val, EnvironmentVariableTarget.User);
                    Log.Information("Создана переменная: {Name}", name);
                }

                Variables.Add(new EnvVarModel { Name = name, Value = val });
            }
        }

        [RelayCommand]
        private void Save()
        {
            foreach (var item in Variables)
            {
                var oldVal = Environment.GetEnvironmentVariable(item.Name, EnvironmentVariableTarget.User);
                if (oldVal != item.Value)
                {
                    Environment.SetEnvironmentVariable(item.Name, item.Value, EnvironmentVariableTarget.User);
                    Log.Information("Обновлено: {Name} | '{Old}' -> '{New}'", item.Name, oldVal, item.Value);
                }
            }
        }
    }
}
