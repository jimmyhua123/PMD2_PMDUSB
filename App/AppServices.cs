// File: PMD2/App/BackendFactory.cs
using System;
using PMD.Core; // 這裡會提供 IBackend / IAppServices / BackendOpenArgs 等介面與基礎型別（稍後給你 Core/ 的碼）

// 重要：用「型別別名」避免命名衝突與含糊不清。
// 注意 PMD-USB 這邊請把類名定為 PmdUsbBackend（不是 Pmd2Backend），否則會再次衝突。
using Pmd2Backend = PMD.Backends.PMD2.Pmd2Backend;
using PmdUsbBackend = PMD.Backends.PMDUSB.PmdUsbBackend;

namespace PMD.App
{
    /// <summary>
    /// 支援的後端種類。
    /// Auto：依可用性自動選擇（優先 PMD2，再來 PMD-USB，最後 Dummy）。
    /// </summary>
    public enum BackendKind
    {
        Auto = 0,
        PMD2 = 1,
        PMDUSB = 2,
    }

    /// <summary>
    /// 負責建立 IBackend 實例的工廠。
    /// </summary>
    public static class BackendFactory
    {
        /// <summary>
        /// 建立指定種類的 Backend。
        /// </summary>
        /// <param name="kind">後端種類（Auto/PMD2/PMDUSB/Dummy）。</param>
        /// <param name="services">應用層服務（log、UI 對話框、設定存取等）。</param>
        /// <returns>IBackend 實例。</returns>
        public static IBackend Create(BackendKind kind, IAppServices services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            switch (kind)
            {
                case BackendKind.PMD2:
                    EnsureAvailable(() => Pmd2Backend.IsAvailable(services), "PMD2 backend not available.");
                    return new Pmd2Backend(services);

                case BackendKind.PMDUSB:
                    EnsureAvailable(() => PmdUsbBackend.IsAvailable(services), "PMD-USB backend not available.");
                    return new PmdUsbBackend(services);

                case BackendKind.Auto:
                default:
                    return CreateAuto(services);
            }
        }

        /// <summary>
        /// 依可用性自動選擇 Backend：PMD2 → PMD-USB → Dummy。
        /// </summary>
        private static IBackend CreateAuto(IAppServices services)
        {
            try
            {
                if (SafeIsAvailable(() => Pmd2Backend.IsAvailable(services)))
                {
                    services.LogInfo("[BackendFactory] Auto choose: PMD2");
                    return new Pmd2Backend(services);
                }
            }
            catch (Exception ex)
            {
                services.LogWarn($"[BackendFactory] PMD2.IsAvailable() exception: {ex.Message}");
            }

            try
            {
                if (SafeIsAvailable(() => PmdUsbBackend.IsAvailable(services)))
                {
                    services.LogInfo("[BackendFactory] Auto choose: PMD-USB");
                    return new PmdUsbBackend(services);
                }
            }
            catch (Exception ex)
            {
                services.LogWarn($"[BackendFactory] PMD-USB.IsAvailable() exception: {ex.Message}");
            }

        }

        private static void EnsureAvailable(Func<bool> check, string messageIfNot)
        {
            if (!SafeIsAvailable(check))
                throw new InvalidOperationException(messageIfNot);
        }

        private static bool SafeIsAvailable(Func<bool> check)
        {
            try { return check?.Invoke() == true; }
            catch { return false; }
        }
    }
}
