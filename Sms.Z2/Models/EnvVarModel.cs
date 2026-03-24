using CommunityToolkit.Mvvm.ComponentModel;

namespace Sms.Z2.Models
{
    public partial class EnvVarModel : ObservableObject
    {
        public string Name { get; init; } = string.Empty;

        [ObservableProperty]
        private string _value = string.Empty;

        [ObservableProperty]
        private string _comment = "Comment";
    }
}
