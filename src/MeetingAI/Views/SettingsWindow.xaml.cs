using System.Windows;
using System.Windows.Controls;
using MeetingAI.Models;

namespace MeetingAI.Views;

public partial class SettingsWindow : Window
{
    private readonly Dictionary<int, AIProvider> _providerMap = new()
    {
        [0] = AIProvider.OpenAI,
        [1] = AIProvider.Anthropic,
        [2] = AIProvider.Zhipu,
        [3] = AIProvider.DeepSeek,
        [4] = AIProvider.Ollama
    };

    public SettingsWindow()
    {
        InitializeComponent();
        Loaded += SettingsWindow_Loaded;
    }

    private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AIModelConfig config)
        {
            var index = _providerMap.FirstOrDefault(x => x.Value == config.Provider).Key;
            ProviderComboBox.SelectedIndex = index;
            BaseUrlTextBox.Text = config.BaseUrl;
            ModelTextBox.Text = config.Model;
        }
    }

    private void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is AIModelConfig config && ProviderComboBox.SelectedIndex >= 0)
        {
            var provider = _providerMap[ProviderComboBox.SelectedIndex];
            config.Provider = provider;
            config.ApplyDefaultsForProvider();
            BaseUrlTextBox.Text = config.BaseUrl;
            ModelTextBox.Text = config.Model;
        }
    }

    private void ApiKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // API Key 更改时的处理
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is AIModelConfig config)
        {
            config.Name = NameTextBox.Text;
            config.ApiKey = ApiKeyTextBox.Text;
            config.BaseUrl = BaseUrlTextBox.Text;
            config.Model = ModelTextBox.Text;
            
            if (double.TryParse(TemperatureTextBox.Text, out var temp))
                config.Temperature = temp;
            
            if (int.TryParse(MaxTokensTextBox.Text, out var tokens))
                config.MaxTokens = tokens;
        }
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
