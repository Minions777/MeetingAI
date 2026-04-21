using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace MeetingAI.Services
{
    public class ThemeService : INotifyPropertyChanged
    {
        private readonly string _themesPath = "pack://application:,,,/MeetingAI;component/Themes/";
        private string _currentThemeName;
        
        public string CurrentThemeName
        {
            get => _currentThemeName;
            set
            {
                if (_currentThemeName != value)
                {
                    _currentThemeName = value;
                    ApplyTheme(value);
                    OnPropertyChanged(nameof(CurrentThemeName));
                }
            }
        }

        public IReadOnlyList<string> AvailableThemes { get; } = new List<string> { "ModernDark", "CleanLight", "AuroraGradient" };

        public void Initialize(string defaultTheme = "ModernDark") => CurrentThemeName = defaultTheme;

        private void ApplyTheme(string themeName)
        {
            var uri = new Uri($"{_themesPath}{themeName}.xaml", UriKind.Absolute);
            var themeDict = new ResourceDictionary { Source = uri };
            var merged = Application.Current.Resources.MergedDictionaries;
            if (merged.Count > 0) merged.RemoveAt(merged.Count - 1);
            merged.Add(themeDict);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}