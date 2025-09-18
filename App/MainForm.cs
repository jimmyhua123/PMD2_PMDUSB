// File: App/MainForm.cs
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace PMD2_PMDUSB.App
{
    public sealed class MainForm : Form
    {
        private readonly AppServices _services;

        // --- UI ---
        private readonly Panel panelTop;
        private readonly ComboBox cmbDevice;
        private readonly Button btnConnect;
        private readonly Button btnDisconnect;
        private readonly ComboBox cmbPort;           // 預留：選 COM（先不做實作掃描）
        private readonly ComboBox cmbBaud;           // 預留：波特率（PMD-USB/PMD2 若有差異日後再調整）
        private readonly Panel panelHost;            // 兩種 View 的容器
        private readonly StatusStrip statusStrip;
        private readonly ToolStripStatusLabel statusLeft;
        private readonly ToolStripStatusLabel statusRight;

        // --- 狀態 ---
        private UserControl? _currentView;
        private string _currentDevice = "PMD2"; // "PMD2" or "PMD-USB"
        private bool _connected;

        public MainForm()
        {
            Text = "PMD2 / PMD-USB - Unified Tool";
            MinimumSize = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;

            // 建立 AppServices（抓取 UI 同步情境）
            _services = new AppServices(SynchronizationContext.Current);
            _services.LogEmitted += OnLogEmitted;

            // --- 建 UI ---
            panelTop = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.FromArgb(245, 245, 245) };

            var lblDevice = new Label { Text = "Device:", AutoSize = true, Left = 12, Top = 15 };
            cmbDevice = new ComboBox
            {
                Left = lblDevice.Right + 8,
                Top = 10,
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbDevice.Items.AddRange(new object[] { "PMD2", "PMD-USB" });
            cmbDevice.SelectedIndexChanged += (_, __) => OnDeviceChanged();

            var lblPort = new Label { Text = "Port:", AutoSize = true, Left = cmbDevice.Right + 16, Top = 15 };
            cmbPort = new ComboBox
            {
                Left = lblPort.Right + 8,
                Top = 10,
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            cmbPort.Items.AddRange(new object[] { "AUTO", "COM3", "COM4", "COM5" }); // 先給假選項，日後做掃描替換

            var lblBaud = new Label { Text = "Baud:", AutoSize = true, Left = cmbPort.Right + 16, Top = 15 };
            cmbBaud = new ComboBox
            {
                Left = lblBaud.Right + 8,
                Top = 10,
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbBaud.Items.AddRange(new object[] { "AUTO", "115200", "230400", "460800", "921600" });
            cmbBaud.SelectedIndex = 0;

            btnConnect = new Button
            {
                Text = "Connect",
                Left = cmbBaud.Right + 24,
                Top = 8,
                Width = 100,
                Height = 30
            };
            btnConnect.Click += (_, __) => DoConnect();

            btnDisconnect = new Button
            {
                Text = "Disconnect",
                Left = btnConnect.Right + 8,
                Top = 8,
                Width = 110,
                Height = 30,
                Enabled = false
            };
            btnDisconnect.Click += (_, __) => DoDisconnect();

            panelTop.Controls.Add(lblDevice);
            panelTop.Controls.Add(cmbDevice);
            panelTop.Controls.Add(lblPort);
            panelTop.Controls.Add(cmbPort);
            panelTop.Controls.Add(lblBaud);
            panelTop.Controls.Add(cmbBaud);
            panelTop.Controls.Add(btnConnect);
            panelTop.Controls.Add(btnDisconnect);

            panelHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            statusStrip = new StatusStrip();
            statusLeft = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleLeft, Text = "Ready." };
            statusRight = new ToolStripStatusLabel { Spring = false, TextAlign = ContentAlignment.MiddleRight, Text = "Idle" };
            statusStrip.Items.Add(statusLeft);
            statusStrip.Items.Add(statusRight);

            Controls.Add(panelHost);
            Controls.Add(panelTop);
            Controls.Add(statusStrip);

            // 快捷鍵
            KeyDown += MainForm_KeyDown;

            // 載入設定（裝置/Port）
            LoadPersistedUiState();

            // 依裝置載入對應 View
            EnsureViewForDevice(_currentDevice);

            // 初始 Log
            _services.LogInfo("MainForm initialized.");
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _services?.Dispose();
        }

        // ---------------- UI 事件 ----------------

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.D1) // Ctrl+1 -> PMD2
            {
                cmbDevice.SelectedItem = "PMD2";
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.D2) // Ctrl+2 -> PMD-USB
            {
                cmbDevice.SelectedItem = "PMD-USB";
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.E) // Ctrl+E -> 開 export 資料夾
            {
                try
                {
                    var dir = _services.EnsureExportDir();
                    if (Directory.Exists(dir))
                        Process.Start("explorer.exe", dir);
                }
                catch { /* ignore */ }
                e.Handled = true;
            }
        }

        private void OnDeviceChanged()
        {
            var sel = (cmbDevice.SelectedItem?.ToString() ?? "PMD2").Trim();
            if (sel != "PMD2" && sel != "PMD-USB") sel = "PMD2";

            if (_currentDevice == sel) return;
            _currentDevice = sel;

            // 記住使用者選擇
            _services.SetSetting("SelectedDevice", _currentDevice);

            // 切換 View
            EnsureViewForDevice(_currentDevice);

            _services.LogInfo($"Switched device UI to: {_currentDevice}");
            SetStatus($"Device: {_currentDevice}");
        }

        private void OnLogEmitted(LogEntry e)
        {
            // 更新狀態列（左側）
            _services.UiPost(() =>
            {
                statusLeft.Text = $"{e.Timestamp:HH:mm:ss} {e.Level}: {e.Message}";
            });
        }

        // ---------------- 連線流程（之後接 Backend） ----------------

        private void DoConnect()
        {
            if (_connected) return;

            var dev = _currentDevice; // PMD2 / PMD-USB
            var port = (cmbPort.Text ?? "AUTO").Trim();
            var baud = (cmbBaud.Text ?? "AUTO").Trim();

            // 記住選擇
            _services.SetSetting("LastComPort", port);
            _services.SetSetting("LastBaud", baud);

            // 這裡先不創建真正 Backend；先當作成功，方便串 UI。
            _connected = true;
            btnConnect.Enabled = false;
            btnDisconnect.Enabled = true;
            cmbDevice.Enabled = false;
            cmbPort.Enabled = false;
            cmbBaud.Enabled = false;

            _services.LogInfo($"Connect requested: device={dev}, port={port}, baud={baud}");
            SetStatus($"Connected ({dev})");
            // 後續你會要我加：BackendFactory + IBackend.Open(BackendOpenArgs)
        }

        private void DoDisconnect()
        {
            if (!_connected) return;

            // 後續在這裡釋放 Backend、關閉串流
            _connected = false;
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            cmbDevice.Enabled = true;
            cmbPort.Enabled = true;
            cmbBaud.Enabled = true;

            _services.LogInfo("Disconnected.");
            SetStatus("Disconnected");
        }

        // ---------------- 輔助 ----------------

        private void SetStatus(string msg)
        {
            statusRight.Text = msg;
        }

        private void LoadPersistedUiState()
        {
            try
            {
                var selDev = _services.GetSetting("SelectedDevice", "PMD2");
                if (selDev != "PMD2" && selDev != "PMD-USB") selDev = "PMD2";
                _currentDevice = selDev;
                cmbDevice.SelectedItem = _currentDevice;

                var lastPort = _services.GetSetting("LastComPort", "AUTO");
                if (!string.IsNullOrWhiteSpace(lastPort) && !cmbPort.Items.Contains(lastPort))
                    cmbPort.Items.Add(lastPort);
                cmbPort.Text = lastPort;

                var lastBaud = _services.GetSetting("LastBaud", "AUTO");
                if (!string.IsNullOrWhiteSpace(lastBaud) && cmbBaud.Items.Contains(lastBaud))
                    cmbBaud.SelectedItem = lastBaud;
                else
                    cmbBaud.SelectedIndex = 0;
            }
            catch
            {
                cmbDevice.SelectedItem = "PMD2";
                cmbPort.Text = "AUTO";
                cmbBaud.SelectedIndex = 0;
            }
        }

        private void EnsureViewForDevice(string dev)
        {
            // 卸載舊的
            if (_currentView != null)
            {
                panelHost.Controls.Remove(_currentView);
                _currentView.Dispose();
                _currentView = null;
            }

            // 建立新的（先嘗試載入預期的 View；若還沒做，fallback 一個空白面板避免編譯炸裂）
            try
            {
                if (dev == "PMD-USB")
                {
                    // 目標：App.Views.PmdUsbView（之後你叫我給）
                    var t = Type.GetType("PMD2_PMDUSB.App.Views.PmdUsbView, PMD2_PMDUSB");
                    _currentView = t != null ? (UserControl?)Activator.CreateInstance(t) : null;
                }
                else
                {
                    // 目標：App.Views.Pmd2View
                    var t = Type.GetType("PMD2_PMDUSB.App.Views.Pmd2View, PMD2_PMDUSB");
                    _currentView = t != null ? (UserControl?)Activator.CreateInstance(t) : null;
                }
            }
            catch
            {
                _currentView = null;
            }

            if (_currentView == null)
            {
                // 後備空白頁，避免在尚未建立 Views/* 時無法執行
                _currentView = new PlaceholderView(dev);
            }

            _currentView.Dock = DockStyle.Fill;
            panelHost.Controls.Add(_currentView);
        }

        // 一個簡單的占位 View（還沒做真正 Pmd2View/PmdUsbView 前用）
        private sealed class PlaceholderView : UserControl
        {
            public PlaceholderView(string dev)
            {
                BackColor = Color.White;
                var lbl = new Label
                {
                    AutoSize = false,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 16, FontStyle.Regular),
                    Text = $"{dev} View\n(尚未建立 App/Views/{(dev == "PMD-USB" ? "PmdUsbView" : "Pmd2View")}.cs)"
                };
                Controls.Add(lbl);
            }
        }
    }
}
