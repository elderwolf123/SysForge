using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RamOptimizerNova.ViewModels;

namespace RamOptimizerNova.Services;

public interface ISettingsService
{
    Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default);
    Task SetSettingAsync<T>(string key, T value);
    Task<bool> RemoveSettingAsync(string key);
    Task<bool> HasSettingAsync(string key);
    Task<IEnumerable<string>> GetAllKeysAsync();
    Task ClearAllAsync();
    Task<string> GetThemeAsync();
    Task SetThemeAsync(string theme);
    Task<bool> GetAutoUpdateAsync();
    Task SetAutoUpdateAsync(bool autoUpdate);
    Task<int> GetUpdateIntervalAsync();
    Task SetUpdateIntervalAsync(int interval);
    Task<bool> GetAutoOptimizeAsync();
    Task SetAutoOptimizeAsync(bool autoOptimize);
    Task<int> GetOptimizationIntervalAsync();
    Task SetOptimizationIntervalAsync(int interval);
    Task<bool> GetShowNotificationsAsync();
    Task SetShowNotificationsAsync(bool showNotifications);
    Task<bool> GetEnableHardwareControlAsync();
    Task SetEnableHardwareControlAsync(bool enable);
    Task<bool> GetEnableCompressionAsync();
    Task SetEnableCompressionAsync(bool enable);
    Task<bool> GetEnableNetworkQoSAsync();
    Task SetEnableNetworkQoSAsync(bool enable);
    Task<string> GetLanguageAsync();
    Task SetLanguageAsync(string language);
    Task<string> GetLogFileAsync();
    Task LogAsync(string message);
    Task InitializeAsync();
}

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private readonly string _logFilePath;
    private Dictionary<string, object> _settings = new();
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _isInitialized = false;

    public SettingsService()
    {
        // Get application data directory
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDirectory = Path.Combine(appDataPath, "RamOptimizerNova");
        
        // Create directories if they don't exist
        Directory.CreateDirectory(appDirectory);
        
        _settingsFilePath = Path.Combine(appDirectory, "settings.json");
        _logFilePath = Path.Combine(appDirectory, "logs", "application.log");
        
        // Create logs directory if it doesn't exist
        Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath) ?? string.Empty);
        
        // Configure JSON options
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        try
        {
            await LoadSettingsAsync();
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            await LogAsync($"Error initializing settings: {ex.Message}");
            _settings = new Dictionary<string, object>();
        }
    }

    public async Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default)
    {
        if (!_isInitialized)
            await InitializeAsync();

        try
        {
            if (_settings.TryGetValue(key, out var value))
            {
                if (value is JsonElement jsonElement)
                {
                    return jsonElement.Deserialize<T>(_jsonOptions);
                }
                return (T)value;
            }
            
            return defaultValue;
        }
        catch (Exception ex)
        {
            await LogAsync($"Error getting setting '{key}': {ex.Message}");
            return defaultValue;
        }
    }

    public async Task SetSettingAsync<T>(string key, T value)
    {
        if (!_isInitialized)
            await InitializeAsync();

        try
        {
            _settings[key] = value;
            await SaveSettingsAsync();
        }
        catch (Exception ex)
        {
            await LogAsync($"Error setting setting '{key}': {ex.Message}");
            throw;
        }
    }

    public async Task<bool> RemoveSettingAsync(string key)
    {
        if (!_isInitialized)
            await InitializeAsync();

        try
        {
            var removed = _settings.Remove(key);
            if (removed)
            {
                await SaveSettingsAsync();
            }
            return removed;
        }
        catch (Exception ex)
        {
            await LogAsync($"Error removing setting '{key}': {ex.Message}");
            return false;
        }
    }

    public async Task<bool> HasSettingAsync(string key)
    {
        if (!_isInitialized)
            await InitializeAsync();

        try
        {
            return _settings.ContainsKey(key);
        }
        catch (Exception ex)
        {
            await LogAsync($"Error checking setting '{key}': {ex.Message}");
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetAllKeysAsync()
    {
        if (!_isInitialized)
            await InitializeAsync();

        try
        {
            return _settings.Keys.ToList();
        }
        catch (Exception ex)
        {
            await LogAsync($"Error getting all keys: {ex.Message}");
            return Enumerable.Empty<string>();
        }
    }

    public async Task ClearAllAsync()
    {
        if (!_isInitialized)
            await InitializeAsync();

        try
        {
            _settings.Clear();
            await SaveSettingsAsync();
        }
        catch (Exception ex)
        {
            await LogAsync($"Error clearing all settings: {ex.Message}");
            throw;
        }
    }

    public async Task<string> GetThemeAsync()
    {
        return await GetSettingAsync<string>("Theme", "Default");
    }

    public async Task SetThemeAsync(string theme)
    {
        await SetSettingAsync("Theme", theme);
    }

    public async Task<bool> GetAutoUpdateAsync()
    {
        return await GetSettingAsync<bool>("AutoUpdate", true);
    }

    public async Task SetAutoUpdateAsync(bool autoUpdate)
    {
        await SetSettingAsync("AutoUpdate", autoUpdate);
    }

    public async Task<int> GetUpdateIntervalAsync()
    {
        return await GetSettingAsync<int>("UpdateInterval", 60); // 60 minutes
    }

    public async Task SetUpdateIntervalAsync(int interval)
    {
        await SetSettingAsync("UpdateInterval", interval);
    }

    public async Task<bool> GetAutoOptimizeAsync()
    {
        return await GetSettingAsync<bool>("AutoOptimize", false);
    }

    public async Task SetAutoOptimizeAsync(bool autoOptimize)
    {
        await SetSettingAsync("AutoOptimize", autoOptimize);
    }

    public async Task<int> GetOptimizationIntervalAsync()
    {
        return await GetSettingAsync<int>("OptimizationInterval", 30); // 30 minutes
    }

    public async Task SetOptimizationIntervalAsync(int interval)
    {
        await SetSettingAsync("OptimizationInterval", interval);
    }

    public async Task<bool> GetShowNotificationsAsync()
    {
        return await GetSettingAsync<bool>("ShowNotifications", true);
    }

    public async Task SetShowNotificationsAsync(bool showNotifications)
    {
        await SetSettingAsync("ShowNotifications", showNotifications);
    }

    public async Task<bool> GetEnableHardwareControlAsync()
    {
        return await GetSettingAsync<bool>("EnableHardwareControl", true);
    }

    public async Task SetEnableHardwareControlAsync(bool enable)
    {
        await SetSettingAsync("EnableHardwareControl", enable);
    }

    public async Task<bool> GetEnableCompressionAsync()
    {
        return await GetSettingAsync<bool>("EnableCompression", true);
    }

    public async Task SetEnableCompressionAsync(bool enable)
    {
        await SetSettingAsync("EnableCompression", enable);
    }

    public async Task<bool> GetEnableNetworkQoSAsync()
    {
        return await GetSettingAsync<bool>("EnableNetworkQoS", true);
    }

    public async Task SetEnableNetworkQoSAsync(bool enable)
    {
        await SetSettingAsync("EnableNetworkQoS", enable);
    }

    public async Task<string> GetLanguageAsync()
    {
        return await GetSettingAsync<string>("Language", "en-US");
    }

    public async Task SetLanguageAsync(string language)
    {
        await SetSettingAsync("Language", language);
    }

    public async Task<string> GetLogFileAsync()
    {
        return _logFilePath;
    }

    public async Task LogAsync(string message)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logMessage = $"[{timestamp}] {message}{Environment.NewLine}";
            
            await File.AppendAllTextAsync(_logFilePath, logMessage);
        }
        catch (Exception ex)
        {
            // Fallback to console logging if file logging fails
            Console.WriteLine($"Error logging to file: {ex.Message}");
            Console.WriteLine($"Log message: {message}");
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                _settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions) ?? new Dictionary<string, object>();
            }
            else
            {
                _settings = new Dictionary<string, object>();
                await SaveSettingsAsync();
            }
        }
        catch (Exception ex)
        {
            await LogAsync($"Error loading settings: {ex.Message}");
            _settings = new Dictionary<string, object>();
        }
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, _jsonOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            await LogAsync($"Error saving settings: {ex.Message}");
            throw;
        }
    }

    // Helper methods for common settings
    public async Task<SettingsModel> GetAllSettingsAsync()
    {
        return new SettingsModel
        {
            Theme = await GetThemeAsync(),
            AutoUpdate = await GetAutoUpdateAsync(),
            UpdateInterval = await GetUpdateIntervalAsync(),
            AutoOptimize = await GetAutoOptimizeAsync(),
            OptimizationInterval = await GetOptimizationIntervalAsync(),
            ShowNotifications = await GetShowNotificationsAsync(),
            EnableHardwareControl = await GetEnableHardwareControlAsync(),
            EnableCompression = await GetEnableCompressionAsync(),
            EnableNetworkQoS = await GetEnableNetworkQoSAsync(),
            Language = await GetLanguageAsync()
        };
    }

    public async Task SetAllSettingsAsync(SettingsModel settings)
    {
        await SetThemeAsync(settings.Theme);
        await SetAutoUpdateAsync(settings.AutoUpdate);
        await SetUpdateIntervalAsync(settings.UpdateInterval);
        await SetAutoOptimizeAsync(settings.AutoOptimize);
        await SetOptimizationIntervalAsync(settings.OptimizationInterval);
        await SetShowNotificationsAsync(settings.ShowNotifications);
        await SetEnableHardwareControlAsync(settings.EnableHardwareControl);
        await SetEnableCompressionAsync(settings.EnableCompression);
        await SetEnableNetworkQoSAsync(settings.EnableNetworkQoS);
        await SetLanguageAsync(settings.Language);
    }
}

public class SettingsModel
{
    public string Theme { get; set; } = "Default";
    public bool AutoUpdate { get; set; } = true;
    public int UpdateInterval { get; set; } = 60;
    public bool AutoOptimize { get; set; } = false;
    public int OptimizationInterval { get; set; } = 30;
    public bool ShowNotifications { get; set; } = true;
    public bool EnableHardwareControl { get; set; } = true;
    public bool EnableCompression { get; set; } = true;
    public bool EnableNetworkQoS { get; set; } = true;
    public string Language { get; set; } = "en-US";
}