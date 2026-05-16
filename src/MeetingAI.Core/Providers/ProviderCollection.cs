using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Providers;

public sealed class ProviderCollection : IDisposable
{
    private readonly IConfigurationService _configService;
    private readonly Func<ProviderConfig, bool> _filter;
    private readonly object _lock = new();
    private IReadOnlyDictionary<string, IAIProvider> _providers = new Dictionary<string, IAIProvider>();
    private readonly Lazy<Task> _initialization;
    private bool _disposed;

    public ProviderCollection(IConfigurationService configService, Func<ProviderConfig, bool> filter)
    {
        _configService = configService;
        _filter = filter;
        _configService.SettingsChanged += OnSettingsChanged;
        _initialization = new Lazy<Task>(() => Task.Run(RefreshProviders));
    }

    public async Task<IReadOnlyDictionary<string, IAIProvider>> GetProvidersAsync()
    {
        await _initialization.Value;
        return _providers;
    }

    public IReadOnlyDictionary<string, IAIProvider> GetProviders()
    {
        if (!_initialization.IsValueCreated)
            return _providers;

        try
        {
            Task.Run(() => _initialization.Value).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            LoggerService.Error("Failed to initialize providers synchronously", ex);
        }

        return _providers;
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await RefreshProvidersAsync();
            }
            catch (Exception ex)
            {
                LoggerService.Error("Failed to refresh providers after settings change", ex);
            }
        });
    }

    private async Task RefreshProvidersAsync()
    {
        var providers = await Task.Run(CreateProviders);
        ReplaceProviders(providers);
    }

    private void RefreshProviders()
    {
        ReplaceProviders(CreateProviders());
    }

    private IReadOnlyDictionary<string, IAIProvider> CreateProviders()
    {
        var settings = _configService.Load();
        var providers = new Dictionary<string, IAIProvider>();

        foreach (var providerConfig in settings.Providers.Where(p => p.IsEnabled && _filter(p)))
        {
            try
            {
                var provider = ProviderFactory.Create(providerConfig);
                providers[providerConfig.Id] = provider;
            }
            catch (Exception ex)
            {
                LoggerService.Error($"Failed to load provider {providerConfig.Name}", ex);
            }
        }

        return providers;
    }

    private IReadOnlyDictionary<string, IAIProvider> ReplaceProviders(IReadOnlyDictionary<string, IAIProvider> providers)
    {
        lock (_lock)
        {
            var oldProviders = _providers;
            _providers = providers;
            return oldProviders;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _configService.SettingsChanged -= OnSettingsChanged;
        DisposeProviders(ReplaceProviders(new Dictionary<string, IAIProvider>()));
    }

    private static void DisposeProviders(IReadOnlyDictionary<string, IAIProvider> providers)
    {
        foreach (var provider in providers.Values)
        {
            if (provider is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
