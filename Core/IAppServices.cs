// File: PMD2/Core/IAppServices.cs
using System;

namespace PMD.Core
{
    /// <summary>
    /// 提供給後端使用的應用層服務（目前以 Log 為主，必要時可再擴充設定存取、UI 提示等）。
    /// </summary>
    public interface IAppServices
    {
        /// <summary>一般資訊訊息。</summary>
        void LogInfo(string message);

        /// <summary>警告訊息（非致命）。</summary>
        void LogWarn(string message);

        /// <summary>錯誤訊息（可帶例外）。</summary>
        void LogError(string message, Exception ex = null);
    }
}
