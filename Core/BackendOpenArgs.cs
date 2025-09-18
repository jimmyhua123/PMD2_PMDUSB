// File: PMD2/Core/BackendOpenArgs.cs
using System;

namespace PMD.Core
{
    /// <summary>
    /// 後端開啟連線所需的參數（目前僅包含序列阜名稱與鮑率）。
    /// </summary>
    [Serializable]
    public sealed class BackendOpenArgs
    {
        /// <summary>目標序列阜，例如 "COM5"。留空時由後端自行自動偵測。</summary>
        public string PortName { get; set; }

        /// <summary>鮑率（預設 115200）。</summary>
        public int BaudRate { get; set; } = 115200;

        public BackendOpenArgs() { }

        public BackendOpenArgs(string portName, int baudRate = 115200)
        {
            PortName = portName;
            BaudRate = baudRate;
        }

        public override string ToString() => $"Port={PortName ?? "(auto)"}, Baud={BaudRate}";
    }
}
