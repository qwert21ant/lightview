using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Persistence;
using Persistence.Models;
using SettingsManager.Interfaces;
using Lightview.Shared.Contracts.Settings;

namespace SettingsManager.Services;

public class SettingsService : ISettingsService
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SettingsService> _logger;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(15);
    
    private const string CAMERA_MONITORING_DEFAULTS = "CameraMonitoringDefaults";

    public SettingsService(AppDbContext context, IMemoryCache cache, ILogger<SettingsService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetSettingAsync<T>(string name) where T : class
    {
        var cacheKey = $"setting_{name}";
        
        if (_cache.TryGetValue(cacheKey, out T? cachedValue))
            return cachedValue;

        var setting = await _context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Name == name);

        if (setting == null) return null;

        try
        {
            var value = JsonSerializer.Deserialize<T>(setting.Value);
            _cache.Set(cacheKey, value, _cacheExpiry);
            return value;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize setting {Name}", name);
            return null;
        }
    }

    public async Task SetSettingAsync<T>(string name, T value) where T : class
    {
        var jsonValue = JsonSerializer.Serialize(value);
        
        var setting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Name == name);

        if (setting == null)
        {
            setting = new SystemSetting
            {
                Name = name,
                Value = jsonValue,
                UpdatedAt = DateTime.UtcNow
            };
            _context.SystemSettings.Add(setting);
        }
        else
        {
            setting.Value = jsonValue;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        
        // Clear cache
        var cacheKey = $"setting_{name}";
        _cache.Remove(cacheKey);
        
        _logger.LogDebug("Setting {Name} updated", name);
    }

    public async Task<CameraMonitoringSettings> GetCameraMonitoringSettingsAsync()
    {
        return await GetSettingAsync<CameraMonitoringSettings>(CAMERA_MONITORING_DEFAULTS) ?? throw new Exception("Camera monitoring defaults not found");
    }

    public async Task UpdateCameraMonitoringSettingsAsync(CameraMonitoringSettings settings)
    {
        await SetSettingAsync(CAMERA_MONITORING_DEFAULTS, settings);
    }
}