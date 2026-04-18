using NakhlaBelal.ViewModels;
using System.Text.Json;

namespace NakhlaBelal.Utitlies
{
    /// <summary>
    /// خدمة قراءة وحفظ إعدادات الموقع من/إلى ملف JSON
    /// </summary>
    public interface ISiteSettingsService
    {
        SiteSettingsVM Get();
        Task SaveAsync(SiteSettingsVM settings, string? updatedBy = null);
    }

    public class SiteSettingsService : ISiteSettingsService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SiteSettingsService> _logger;
        private static readonly object _fileLock = new();
        private static SiteSettingsVM? _cache;

        public SiteSettingsService(IWebHostEnvironment env, ILogger<SiteSettingsService> logger)
        {
            _env = env;
            _logger = logger;
        }

        private string FilePath => Path.Combine(_env.WebRootPath, "app-data", "site-settings.json");

        public SiteSettingsVM Get()
        {
            if (_cache != null) return _cache;

            lock (_fileLock)
            {
                if (_cache != null) return _cache;

                if (!File.Exists(FilePath))
                {
                    _cache = new SiteSettingsVM();
                    return _cache;
                }

                try
                {
                    var json = File.ReadAllText(FilePath);
                    _cache = JsonSerializer.Deserialize<SiteSettingsVM>(json) ?? new SiteSettingsVM();
                    return _cache;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load site settings; using defaults");
                    _cache = new SiteSettingsVM();
                    return _cache;
                }
            }
        }

        public async Task SaveAsync(SiteSettingsVM settings, string? updatedBy = null)
        {
            settings.LastUpdated = DateTime.Now;
            settings.UpdatedBy = updatedBy;

            var dir = Path.GetDirectoryName(FilePath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            await File.WriteAllTextAsync(FilePath, json);

            // تفريغ الكاش ليتم إعادة التحميل في المرة القادمة
            lock (_fileLock)
            {
                _cache = settings;
            }
        }
    }
}
