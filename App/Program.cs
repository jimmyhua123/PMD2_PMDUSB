// File: App/Program.cs
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using PMD2;

namespace PMD2_PMDUSB.App
{
    internal static class Program
    {
        /// <summary>
        /// 單一實例用的全域 Mutex 名稱（全系統唯一）
        /// </summary>
        private const string MUTEX_NAME = "Global\\PMD2_PMDUSB_7E9E19A7-0C0B-4C6C-9E9E-6E2E7B9C1C12";

        /// <summary>
        /// 應用程式資料目錄，例如：
        /// C:\Users\<User>\AppData\Local\PMD2_PMDUSB
        /// </summary>
        public static readonly string AppDataDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PMD2_PMDUSB");

        /// <summary>
        /// 日誌目錄：…\PMD2_PMDUSB\logs
        /// </summary>
        public static readonly string LogDir = Path.Combine(AppDataDir, "logs");

        [STAThread]
        private static void Main()
        {
            // WinForms 基本設定
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 準備資料夾
            TryEnsureDirectories();

            // 全域例外攔截
            Application.ThreadException += OnUiThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnNonUiThreadException;

            // 單一實例保護
            using (var mutex = new Mutex(initiallyOwned: true, name: MUTEX_NAME, createdNew: out bool isNew))
            {
                if (!isNew)
                {
                    MessageBox.Show(
                        "PMD2_PMDUSB 已在執行中。",
                        "PMD2_PMDUSB",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }

                // 啟動主視窗
                try
                {
                    // 注意：MainForm.cs 應位於命名空間 PMD2_PMDUSB.App
                    Application.Run(new MainForm());
                }
                catch (Exception ex)
                {
                    // 這裡是 Application.Run 之外的最後保險
                    LogException("FATAL during Application.Run", ex);
                    MessageBox.Show(
                        "程式發生未預期錯誤並即將關閉。\n\n" +
                        "詳細資訊已寫入日誌檔（logs）。",
                        "PMD2_PMDUSB - 致命錯誤",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        private static void OnUiThreadException(object sender, ThreadExceptionEventArgs e)
        {
            LogException("UI thread exception", e.Exception);
            MessageBox.Show(
                "發生未處理的 UI 例外。\n\n" +
                "詳細資訊已寫入日誌檔（logs）。",
                "PMD2_PMDUSB - 錯誤",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }

        private static void OnNonUiThreadException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception
                     ?? new Exception("Unknown non-UI exception (no Exception instance).");
            LogException("Non-UI thread exception", ex);
            // 非 UI 執行緒不一定能安全呼叫 MessageBox；保守只寫 log
        }

        private static void TryEnsureDirectories()
        {
            try
            {
                if (!Directory.Exists(AppDataDir)) Directory.CreateDirectory(AppDataDir);
                if (!Directory.Exists(LogDir)) Directory.CreateDirectory(LogDir);
            }
            catch
            {
                // 若資料夾建立失敗，不要阻斷啟動流程；之後寫檔會再嘗試。
            }
        }

        private static void LogException(string tag, Exception ex)
        {
            try
            {
                if (!Directory.Exists(LogDir)) Directory.CreateDirectory(LogDir);

                var file = Path.Combine(
                    LogDir,
                    $"error-{DateTime.Now:yyyyMMdd-HHmmss-fff}.log"
                );

                using (var sw = new StreamWriter(file, append: false))
                {
                    sw.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {tag}");
                    sw.WriteLine(ex.ToString());
                }
            }
            catch
            {
                // 若連寫檔都失敗，就無能為力了；避免在例外處理中再丟例外。
            }
        }
    }
}
