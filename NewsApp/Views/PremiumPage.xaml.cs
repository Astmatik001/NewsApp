using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;

namespace NewsApp.Views
{
    public partial class PremiumPage : ContentPage
    {
        public PremiumPage()
        {
            InitializeComponent();
            UpdateUI();
        }

        private void UpdateUI()
        {
            var currentPlan = Preferences.Get("user_plan", "free");
            
            // Reset all buttons
            FreeButton.BackgroundColor = Colors.Transparent;
            FreeButton.TextColor = Color.FromArgb("#2196F3");
            FreeButton.Text = "Выбрать";
            FreeButton.IsEnabled = true;
            
            PremiumButton.BackgroundColor = Colors.Transparent;
            PremiumButton.TextColor = Color.FromArgb("#FF9800");
            PremiumButton.Text = "Выбрать";
            PremiumButton.IsEnabled = true;
            
            ProButton.BackgroundColor = Colors.Transparent;
            ProButton.TextColor = Color.FromArgb("#9C27B0");
            ProButton.Text = "Выбрать";
            ProButton.IsEnabled = true;
            
            // Mark current plan
            switch (currentPlan)
            {
                case "free":
                    FreeButton.BackgroundColor = Colors.LightGray;
                    FreeButton.Text = "Текущий план";
                    FreeButton.IsEnabled = false;
                    break;
                case "premium":
                    PremiumButton.BackgroundColor = Colors.LightGray;
                    PremiumButton.Text = "Текущий план";
                    PremiumButton.IsEnabled = false;
                    break;
                case "pro":
                    ProButton.BackgroundColor = Colors.LightGray;
                    ProButton.Text = "Текущий план";
                    ProButton.IsEnabled = false;
                    break;
            }
        }

        private async void OnSelectFree(object sender, EventArgs e)
        {
            Preferences.Set("user_plan", "free");
            await DisplayAlert("Успех", "Тариф изменен на Бесплатный", "OK");
            UpdateUI();
        }

        private async void OnSelectPremium(object sender, EventArgs e)
        {
            Preferences.Set("user_plan", "premium");
            await DisplayAlert("Успех", "Тариф изменен на Премиум (тестовый режим)", "OK");
            UpdateUI();
        }

        private async void OnSelectPro(object sender, EventArgs e)
        {
            Preferences.Set("user_plan", "pro");
            await DisplayAlert("Успех", "Тариф изменен на Pro (тестовый режим)", "OK");
            UpdateUI();
        }
    }
}