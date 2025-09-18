// File: PMD2/Core/SensorSample.cs
using System;

namespace PMD.Core
{
    /// <summary>
    /// 一筆感測資料（對應裝置輸出的一行）。
    /// </summary>
    [Serializable]
    public sealed class SensorSample
    {
        /// <summary>時間戳（本地系統時間，必要時可改由裝置時間）。</summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// 解析出的數值欄位（依裝置/韌體不同，欄位數量可變）。
        /// 例如：電壓、電流、功率、各路軌電流等。
        /// </summary>
        public double[] Values { get; set; } = Array.Empty<double>();

        /// <summary>原始字串（CSV 一整行，方便除錯或重新解析）。</summary>
        public string Raw { get; set; }

        public SensorSample() { }

        public SensorSample(DateTimeOffset ts, double[] values, string raw)
        {
            Timestamp = ts;
            Values = values ?? Array.Empty<double>();
            Raw = raw;
        }

        public override string ToString()
            => $"{Timestamp:HH:mm:ss.fff} | {Values?.Length ?? 0} vals | {Raw}";
    }
}
