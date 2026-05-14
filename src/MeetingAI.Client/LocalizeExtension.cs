using Avalonia.Data;
using Avalonia.Markup.Xaml;
using MeetingAI.Shared.i18n;

namespace MeetingAI.Client;

public class LocalizeExtension : MarkupExtension
{
    public string Key { get; }

    public LocalizeExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding
        {
            Source = LocalizationManager.Instance,
            Path = $"[{Key}]"
        };
    }
}
