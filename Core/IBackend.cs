// File: PMD2/Core/IBackend.cs
using System;

namespace PMD.Core
{
    /// <summary>
    /// 所有資料來源後端的共通介面（PMD2、PMD-USB 等）。
    /// </summary>
    public interface IBackend : IDisposable
    {
        /// <summary>顯示用名稱（例如 "ElmorLabs PMD2"）。</summary>
        string DisplayName { get; }

        /// <summary>是否已成功開啟連線。</summary>
        bool IsOpen { get; }

        /// <summary>
        /// 當收到一筆解析完成的感測資料時觸發。
        /// 事件處理常在背景執行緒觸發；若需觸發 UI，請自行 marshal 到 UI 執行緒。
        /// </summary>
        event Action<SensorSample> OnSample;

        /// <summary>
        /// 開啟後端連線（序列埠、鮑率等參數由 <see cref="BackendOpenArgs"/> 指定）。
        /// 重複呼叫在已開啟狀態時應為 no-op。
        /// </summary>
        void Open(BackendOpenArgs args);

        /// <summary>
        /// 關閉後端連線；應可在任何狀態安全呼叫（具冪等性）。
        /// </summary>
        void Close();
    }
}
