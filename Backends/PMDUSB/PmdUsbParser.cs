using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using PMD.Core;

namespace PMD.Backends.PMD2
{
    /// <summary>
    /// ElmorLabs PMD2：新版裝置，官方軟體與 Python 範例同樣走 USB/Serial 串流。
    /// 協議同樣採「每行一筆 CSV」，但欄位數量/順序可能比 PMD-USB 更多（含 ATX24、12VHPWR 等）。
    /// 本類別與 PMD-USB 共用思路：逐行解析所有數字欄位為 double[]。
    /// </summary>
    public sealed class Pmd2Backend : IBackend, IDisposable
    {
        private readonly IAppServices _svc;
        private SerialPort _port;
        private Thread _rxThread;
        private CancellationTokenSource _cts;

        public event Action<SensorSample> OnSample;

        public string DisplayName => "ElmorLabs PMD2";
        public bool IsOpen => _port != null && _port.IsOpen;

        public Pmd2Backend(IAppServices services)
        {
            _svc = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>
        /// 以 FriendlyName 關鍵字粗略判斷（PMD2/PMD），或直接存在任何可開啟的 USB-Serial 皆視為可用。
        /// </summary>
        public static bool IsAvailable(IAppServices services = null)
        {
            try
            {
                foreach (var name in SerialPort.GetPortNames())
                {
                    // 實務上 PMD2 也常見 CH340，且 FriendlyName 會帶裝置名；這裡僅提供快速偵測。
                    if (name.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch { }
            return false;
        }

        public void Open(BackendOpenArgs args)
        {
            if (IsOpen) return;

            var portName = args?.PortName;
            if (string.IsNullOrWhiteSpace(portName))
            {
                // 若未指定，挑選系統中號碼最小的可用 COM；你也可以仿 PMD-USB 那樣做 WMI 篩選
                portName = SerialPort.GetPortNames().OrderBy(n =>
                {
                    if (n.StartsWith("COM", StringComparison.OrdinalIgnoreCase) &&
                        int.TryParse(n.Substring(3), out var id)) return id;
                    return int.MaxValue;
                }).FirstOrDefault() ?? throw new InvalidOperationException("找不到可用的 PMD2 COM 連接埠");
            }

            int baud = args?.BaudRate ?? 115200;

            _port = new SerialPort(portName, baud, Parity.None, 8, StopBits.One)
            {
                NewLine = "\n",
                Encoding = Encoding.ASCII,
                ReadTimeout = 2000,
                WriteTimeout = 2000,
                DtrEnable = true,
                RtsEnable = true
            };

            _port.Open();
            _svc?.LogInfo($"[PMD2] Open {_port.PortName} @ {_port.BaudRate}");

            _cts = new CancellationTokenSource();
            _rxThread = new Thread(() => RxLoop(_cts.Token)) { IsBackground = true, Name = "PMD2-RX" };
            _rxThread.Start();
        }

        public void Close()
        {
            try { _cts?.Cancel(); } catch { }
            try { _rxThread?.Join(500); } catch { }
            try { if (_port != null && _port.IsOpen) _port.Close(); } catch { }
            _port = null;
            _rxThread = null;
            _cts = null;
            _svc?.LogInfo("[PMD2] Closed.");
        }

        private void RxLoop(CancellationToken ct)
        {
            var ci = CultureInfo.InvariantCulture;
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var line = _port.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    line = line.Trim('\r', '\n', ' ');

                    // 與 PMD-USB 相同策略：只要是數字就吃，欄位數動態因應不同版本
                    var vals = ParseNumericCsv(line, ci);
                    if (vals.Count == 0) continue;

                    var sample = new SensorSample
                    {
                        Timestamp = DateTimeOffset.Now,
                        Values = vals.ToArray(),
                        Raw = line
                    };
                    OnSample?.Invoke(sample);
                }
                catch (TimeoutException)
                {
                    // ok
                }
                catch (Exception ex)
                {
                    _svc?.LogWarn($"[PMD2] RX err: {ex.Message}");
                    Thread.Sleep(50);
                }
            }
        }

        private static List<double> ParseNumericCsv(string line, CultureInfo ci)
        {
            var ret = new List<double>(24);
            var tokens = line.Split(new[] { ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var tk in tokens)
            {
                var s = tk.Trim();
                int eq = s.IndexOf('=');
                if (eq >= 0 && eq < s.Length - 1)
                    s = s[(eq + 1)..].Trim();

                if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, ci, out var d))
                    ret.Add(d);
            }
            return ret;
        }

        public void Dispose() => Close();
    }
}
