using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using PMD.Core;

namespace PMD.Backends.PMDUSB
{
    /// <summary>
    /// ElmorLabs PMD-USB：透過 USB-Serial 持續輸出 CSV，每行一筆感測資料。
    /// 典型鮑率 115200，8N1，換行分隔。欄位順序依韌體而定（會帶電壓/電流/功率、各路 EPS/PCIe/ATX 等）。
    /// 本類別做「行為單位」解析：把每行的數字欄位全數讀成 double[]，再交由上層以欄位名稱對應。
    /// </summary>
    public sealed class PmdUsbBackend : IBackend, IDisposable
    {
        private readonly IAppServices _svc;
        private SerialPort _port;
        private Thread _rxThread;
        private CancellationTokenSource _cts;
        private readonly object _gate = new();

        // 依你 Core 定義調整：用事件把解析好的 sample 往上拋
        public event Action<SensorSample> OnSample;

        public string DisplayName => "ElmorLabs PMD-USB";
        public bool IsOpen => _port != null && _port.IsOpen;

        public PmdUsbBackend(IAppServices services)
        {
            _svc = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>
        /// 自動偵測是否存在 PMD-USB（以 WMI/FriendlyName 搜尋 CH340 / PMD-USB 字樣；也可直接嘗試開啟）
        /// </summary>
        public static bool IsAvailable(IAppServices services = null)
        {
            try
            {
                foreach (var info in EnumerateSerials())
                {
                    if (info.FriendlyName.Contains("CH340", StringComparison.OrdinalIgnoreCase) ||
                        info.FriendlyName.Contains("USB-SERIAL", StringComparison.OrdinalIgnoreCase) ||
                        info.FriendlyName.Contains("PMD-USB", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch { /* ignore */ }
            return false;
        }

        public void Open(BackendOpenArgs args)
        {
            if (IsOpen) return;

            var portName = args?.PortName;
            if (string.IsNullOrWhiteSpace(portName))
            {
                // 嘗試自動挑選較可能是 PMD-USB 的 COM
                var candidate = EnumerateSerials()
                    .OrderByDescending(s =>
                        s.FriendlyName.Contains("PMD", StringComparison.OrdinalIgnoreCase) ? 3 :
                        s.FriendlyName.Contains("CH340", StringComparison.OrdinalIgnoreCase) ? 2 :
                        s.FriendlyName.Contains("USB-SERIAL", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                    .FirstOrDefault();
                portName = candidate?.Port ?? throw new InvalidOperationException("找不到可用的 PMD-USB COM 連接埠");
            }

            _port = new SerialPort(portName, args?.BaudRate ?? 115200, Parity.None, 8, StopBits.One)
            {
                NewLine = "\n",
                Encoding = Encoding.ASCII,
                ReadTimeout = 2000,
                WriteTimeout = 2000,
                DtrEnable = true,
                RtsEnable = true
            };

            _port.Open();
            _svc?.LogInfo($"[PMD-USB] Open {_port.PortName} @ {_port.BaudRate}");

            // 啟動背景收資料
            _cts = new CancellationTokenSource();
            _rxThread = new Thread(() => RxLoop(_cts.Token)) { IsBackground = true, Name = "PMDUSB-RX" };
            _rxThread.Start();
        }

        public void Close()
        {
            lock (_gate)
            {
                try { _cts?.Cancel(); } catch { }
                try { _rxThread?.Join(500); } catch { }
                try { if (_port != null && _port.IsOpen) _port.Close(); } catch { }
                _port = null;
                _rxThread = null;
                _cts = null;
            }
            _svc?.LogInfo("[PMD-USB] Closed.");
        }

        private void RxLoop(CancellationToken ct)
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder(256);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    string line = _port.ReadLine(); // 以 \n 分隔
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // 去掉 CR 與空白
                    line = line.Trim('\r', '\n', ' ');

                    // 解析 CSV：允許 "x,y,z" 或帶欄位名的 "t=...,v=...,i=..." 都盡量擷取數字
                    var values = ParseNumericCsv(line, ci);
                    if (values.Count == 0)
                        continue;

                    var sample = new SensorSample
                    {
                        // 你 Core 的結構可自行調整；這裡放時間戳與所有欄位值
                        Timestamp = DateTimeOffset.Now,
                        Values = values.ToArray(),
                        Raw = line
                    };

                    OnSample?.Invoke(sample);
                }
                catch (TimeoutException)
                {
                    // 允許超時，繼續
                }
                catch (Exception ex)
                {
                    _svc?.LogWarn($"[PMD-USB] RX err: {ex.Message}");
                    Thread.Sleep(50);
                }
            }
        }

        private static List<double> ParseNumericCsv(string line, CultureInfo ci)
        {
            var ret = new List<double>(16);
            // 以逗號或分號切；同時支援形如 "v=12.1" 取右邊數字
            var tokens = line.Split(new[] { ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var tk in tokens)
            {
                var s = tk.Trim();
                int eq = s.IndexOf('=');
                if (eq >= 0 && eq < s.Length - 1)
                    s = s.Substring(eq + 1).Trim();

                if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, ci, out var d))
                    ret.Add(d);
            }
            return ret;
        }

        public void Dispose() => Close();

        // --- 小工具：列舉序列埠（含 FriendlyName、PID/VID 若可得） ---
        private sealed class SerialInfo
        {
            public string Port { get; init; }
            public string FriendlyName { get; init; }
        }

        private static IEnumerable<SerialInfo> EnumerateSerials()
        {
            // Windows WMI
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");
                foreach (var obj in searcher.Get())
                {
                    var name = (obj["Name"] as string) ?? "";
                    var port = ExtractComName(name);
                    if (!string.IsNullOrWhiteSpace(port))
                        yield return new SerialInfo { Port = port, FriendlyName = name };
                }
            }
            catch
            {
                // 退回僅列舉名稱
                foreach (var p in SerialPort.GetPortNames())
                    yield return new SerialInfo { Port = p, FriendlyName = p };
            }
        }

        private static string ExtractComName(string friendly)
        {
            // 例如 "USB-SERIAL CH340 (COM5)" → COM5
            int l = friendly.LastIndexOf("(COM", StringComparison.OrdinalIgnoreCase);
            if (l < 0) return null;
            int r = friendly.IndexOf(')', l);
            if (r < 0) return null;
            return friendly.Substring(l + 1, r - l - 1);
        }
    }
}
