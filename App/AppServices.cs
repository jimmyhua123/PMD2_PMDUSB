// File: App/AppServices.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace PMD2_PMDUSB.App
{
    /// <summary>
    /// 應用層服務集合：Log、設定存取、UI 派送工具、簡易事件匯流排。
    /// 先獨立運作，不依賴 Core/ 介面；後續可用 Core.IAppServices 介面抽象化。
    /// </summary>
    public sealed class AppServices : IDisposable
    {
        // ---------- 路徑/檔案 ----------
        public string AppDataDir { get; }
        public string LogDir { get; }
        public string ExportDir { get; }
        public string ConfigFilePath { get; }

        // ---------- UI 派送 ----------
        private readonly SynchronizationContext _uiContext;

        // ---------- 設定 ----------
        private readonly object _cfgLock = new object();
        private Dictionary<string, JsonElement> _config = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        // ---------- Log ----------
        public event Action<LogEntry>? LogEmitted; // UI 可訂閱顯示
        private readonly object _logFileLock = new object();
        private string CurrentLogFile =>
            Path.Combine(LogDir, $"app-{DateTime.Now:yyyyMMdd}.log");

        // ---------- 其他 ----------
        private bool _disposed;

        public AppServices(SynchronizationContext uiContext, string? appDataOverride = null)
        {
            _uiContext = uiContext ?? throw new ArgumentNullException(nameof(uiContext));

            // 基礎目錄
            AppDataDir = appDataOverride ??
                         Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PMD2_PMDUSB");
            LogDir = Path.Combine(AppDataDir, "logs");
            ExportDir = Path.Combine(AppDataDir, "export");
            ConfigFilePath = Path.Combine(AppDataDir, "config.json");

            TryEnsureDirectories();
            LoadConfigSafe();
        }

        #region 公開：UI 派送

        /// <summary>在 UI 同步情境下執行（Post），不阻塞呼叫執行緒。</summary>
        public void UiPost(Action action)
        {
            if (action == null) return;
            _uiContext.Post(_ => SafeInvoke(action), null);
        }

        /// <summary>在 UI 同步情境下執行（Send），會阻塞直到完成。</summary>
        public void UiSend(Action action)
        {
            if (action == null) return;
            _uiContext.Send(_ => SafeInvoke(action), null);
        }

        private static void SafeInvoke(Action a)
        {
            try { a(); }
            catch (Exception ex)
            {
                // 派送內部錯誤也寫入 log
                // 注意：這裡不能呼叫 UiSend/UiPost 以免遞迴
                System.Diagnostics.Debug.WriteLine($"[UiDispatchError] {ex}");
            }
        }

        #endregion

        #region 公開：設定（JSON Key-Value）

        /// <summary>讀取設定；若不存在則回傳預設值。</summary>
        public T GetSetting<T>(string key, T defaultValue = default!)
        {
            if (string.IsNullOrWhiteSpace(key)) return defaultValue;

            lock (_cfgLock)
            {
                if (!_config.TryGetValue(key, out var val)) return defaultValue;

                try
                {
                    if (val.ValueKind == JsonValueKind.Null) return defaultValue;
                    var json = val.GetRawText();
                    // 特別處理 string：避免被當作 JSON 字串再包一層
                    if (typeof(T) == typeof(string))
                    {
                        return (T)(object)(val.GetString() ?? (defaultValue?.ToString() ?? ""));
                    }
                    return JsonSerializer.Deserialize<T>(json) ?? defaultValue;
                }
                catch
                {
                    return defaultValue;
                }
            }
        }

        /// <summary>寫入設定並立即持久化到 config.json。</summary>
        public void SetSetting<T>(string key, T value)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            lock (_cfgLock)
            {
                // 用 JsonDocument 包裝成 JsonElement 存入 Dictionary
                using var doc = JsonDocument.Parse(JsonSerializer.Serialize(value));
                _config[key] = doc.RootElement.Clone();
                SaveConfigSafe();
            }
        }

        /// <summary>移除某個設定鍵並存檔。</summary>
        public bool RemoveSetting(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            lock (_cfgLock)
            {
                var removed = _config.Remove(key);
                if (removed) SaveConfigSafe();
                return removed;
            }
        }

        /// <summary>取得目前所有設定鍵（唯讀快照）。</summary>
        public IReadOnlyCollection<string> GetAllSettingKeys()
        {
            lock (_cfgLock)
            {
                return new List<string>(_config.Keys).AsReadOnly();
            }
        }

        #endregion

        #region 公開：Log

        public void LogInfo(string message) => WriteLog(LogLevel.Info, message, null);
        public void LogWarn(string message) => WriteLog(LogLevel.Warn, message, null);
        public void LogError(string message, Exception? ex = null) => WriteLog(LogLevel.Error, message, ex);
        public void LogDebug(string message) => WriteLog(LogLevel.Debug, message, null);

        private void WriteLog(LogLevel level, string message, Exception? ex)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message ?? string.Empty,
                Exception = ex
            };

            // 1) 寫入檔案
            try
            {
                lock (_logFileLock)
                {
                    if (!Directory.Exists(LogDir)) Directory.CreateDirectory(LogDir);
                    using var sw = new StreamWriter(CurrentLogFile, append: true, Encoding.UTF8);
                    sw.WriteLine(FormatLogLine(entry));
                    if (ex != null)
                    {
                        sw.WriteLine(ex.ToString());
                    }
                }
            }
            catch
            {
                // 檔案寫入錯誤不再往外拋，避免影響主流程
            }

            // 2) 廣播給 UI（例如狀態列 / Log 視窗）
            try
            {
                LogEmitted?.Invoke(entry);
            }
            catch
            {
                // 事件訂閱端拋例外也吞掉
            }
        }

        private static string FormatLogLine(LogEntry e)
        {
            var lvl = e.Level.ToString().ToUpperInvariant().PadRight(5);
            return $"[{e.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {lvl} {e.Message}";
        }

        #endregion

        #region 公開：匯出目錄輔助

        /// <summary>取得預設匯出資料夾（…\export），不存在則建立。</summary>
        public string EnsureExportDir()
        {
            if (!Directory.Exists(ExportDir))
                Directory.CreateDirectory(ExportDir);
            return ExportDir;
        }

        /// <summary>
        /// 以 UTF-8 無 BOM 將多行文字寫入匯出檔（例如 CSV）。
        /// 這是通用工具；真正 CSV 邏輯可放到 Core.Export 裡。
        /// </summary>
        public string WriteExportText(string fileNameNoExt, string extWithDot, IEnumerable<string> lines)
        {
            if (string.IsNullOrWhiteSpace(fileNameNoExt)) fileNameNoExt = "export";
            if (string.IsNullOrWhiteSpace(extWithDot)) extWithDot = ".txt";
            var dir = EnsureExportDir();
            var file = Path.Combine(dir, $"{SanitizeFileName(fileNameNoExt)}_{DateTime.Now:yyyyMMdd-HHmmss}{extWithDot}");
            File.WriteAllLines(file, lines ?? Array.Empty<string>(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return file;
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        #endregion

        #region 內部：設定載入/儲存 與 目錄建立

        private void TryEnsureDirectories()
        {
            try
            {
                if (!Directory.Exists(AppDataDir)) Directory.CreateDirectory(AppDataDir);
                if (!Directory.Exists(LogDir)) Directory.CreateDirectory(LogDir);
                if (!Directory.Exists(ExportDir)) Directory.CreateDirectory(ExportDir);
            }
            catch
            {
                // 建立失敗不阻斷；稍後需要時再嘗試
            }
        }

        private void LoadConfigSafe()
        {
            lock (_cfgLock)
            {
                try
                {
                    if (File.Exists(ConfigFilePath))
                    {
                        using var fs = File.OpenRead(ConfigFilePath);
                        using var doc = JsonDocument.Parse(fs);
                        var dict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            dict[prop.Name] = prop.Value.Clone();
                        }
                        _config = dict;
                    }
                    else
                    {
                        _config = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                    }
                }
                catch
                {
                    // 若 config 損毀，改用空白設定，避免卡啟動
                    _config = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        private void SaveConfigSafe()
        {
            lock (_cfgLock)
            {
                try
                {
                    if (!Directory.Exists(AppDataDir)) Directory.CreateDirectory(AppDataDir);
                    using var ms = new MemoryStream();
                    using (var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true }))
                    {
                        writer.WriteStartObject();
                        foreach (var kv in _config)
                        {
                            writer.WritePropertyName(kv.Key);
                            kv.Value.WriteTo(writer);
                        }
                        writer.WriteEndObject();
                    }
                    File.WriteAllBytes(ConfigFilePath, ms.ToArray());
                }
                catch
                {
                    // 靜默失敗；不阻斷應用
                }
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            // 目前沒有非受控資源；保留擴充空間（例如串口、檔案握把）。
        }

        #endregion
    }

    // -------- 輔助型別 --------

    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Error = 3,
    }

    public sealed class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
    }
}
