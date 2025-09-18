// File: App/Views/PmdUsbView.cs
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PMD2_PMDUSB.App.Views
{
    /// <summary>
    /// PMD-USB 的主視圖（UI 骨架）。
    /// 後續把實際協議事件（樣本、狀態、裝置資訊）接進來更新畫面。
    /// </summary>
    public sealed class PmdUsbView : UserControl
    {
        // App 全域服務（Log/設定/派送）
        private AppServices? _services;

        // --- UI ---
        private readonly Panel panelToolbar;
        private readonly Button btnStart;
        private readonly Button btnStop;
        private readonly Button btnExport;
        private readonly Button btnReadId;
        private readonly Button btnSettings;

        private readonly SplitContainer splitMain;
        private readonly Panel panelInfo;
        private readonly Label lblDevice;
        private readonly Label lblConn;
        private readonly Label lblRate;
        private readonly Label lblFw;

        private readonly ListView lvSamples;

        public PmdUsbView()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.White;

            // 工具列
            panelToolbar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(245, 245, 245) };

            btnStart = MakeToolbarButton("Start", 12, 8, OnStartClick);
            btnStop = MakeToolbarButton("Stop", 92, 8, OnStopClick);
            btnExport = MakeToolbarButton("Export CSV", 172, 8, OnExportClick, width: 110);
            btnReadId = MakeToolbarButton("Read ID", 292, 8, OnReadIdClick, width: 90);
            btnSettings = MakeToolbarButton("Settings…", 392, 8, OnSettingsClick, width: 96);

            panelToolbar.Controls.AddRange(new Control[] { btnStart, btnStop, btnExport, btnReadId, btnSettings });

            // 主區塊（左資料/右資訊）
            splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 760,
                FixedPanel = FixedPanel.Panel2
            };

            // 左側：樣本清單（可日後替換為圖表）
            lvSamples = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            lvSamples.Columns.Add("Time", 140);
            lvSamples.Columns.Add("CH1 V", 90);
            lvSamples.Columns.Add("CH2 V", 90);
            lvSamples.Columns.Add("CH3 V", 90);
            lvSamples.Columns.Add("CH4 V", 90);
            lvSamples.Columns.Add("I (A)", 90);
            lvSamples.Columns.Add("P (W)", 90);

            splitMain.Panel1.Controls.Add(lvSamples);

            // 右側：裝置/狀態資訊
            panelInfo = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            var infoTitle = new Label
            {
                Text = "PMD-USB – Info",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 36,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };
            lblDevice = MakeInfoLabel("Device: PMD-USB");
            lblConn = MakeInfoLabel("Connection: Idle");
            lblRate = MakeInfoLabel("Sampling: (n/a)");
            lblFw = MakeInfoLabel("Firmware: (unknown)");

            panelInfo.Controls.Add(lblFw);
            panelInfo.Controls.Add(lblRate);
            panelInfo.Controls.Add(lblConn);
            panelInfo.Controls.Add(lblDevice);
            panelInfo.Controls.Add(infoTitle);

            splitMain.Panel2.Padding = new Padding(8);
            splitMain.Panel2.Controls.Add(panelInfo);

            // 組合
            Controls.Add(splitMain);
            Controls.Add(panelToolbar);

            // 預設狀態
            btnStop.Enabled = false;
        }

        /// <summary>由 MainForm 在建立視圖後呼叫，注入 AppServices。</summary>
        public void AttachServices(AppServices services)
        {
            _services = services;
            _services?.LogInfo("PmdUsbView attached to services.");
        }

        // ---------------- 事件處理 ----------------

        private void OnStartClick(object? sender, EventArgs e)
        {
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            lblConn.Text = "Connection: Capturing…";
            _services?.LogInfo("[PMD-USB] Start capture requested.");

            // Demo 假資料（等你接協議事件取代）
            AppendSample(DateTime.Now, 5.01, 5.02, 5.03, 5.02, 0.12, 0.60);
            AppendSample(DateTime.Now.AddMilliseconds(50), 5.02, 5.01, 5.04, 5.03, 0.13, 0.65);
        }

        private void OnStopClick(object? sender, EventArgs e)
        {
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            lblConn.Text = "Connection: Stopped";
            _services?.LogInfo("[PMD-USB] Stop capture requested.");
        }

        private void OnExportClick(object? sender, EventArgs e)
        {
            _services?.LogInfo("[PMD-USB] Export CSV requested.");
            if (_services == null) return;

            var lines = new System.Collections.Generic.List<string>();
            lines.Add("time,ch1_v,ch2_v,ch3_v,ch4_v,i,p");
            foreach (ListViewItem it in lvSamples.Items)
            {
                var row = string.Join(",",
                    it.SubItems[0].Text,
                    it.SubItems[1].Text,
                    it.SubItems[2].Text,
                    it.SubItems[3].Text,
                    it.SubItems[4].Text,
                    it.SubItems[5].Text,
                    it.SubItems[6].Text
                );
                lines.Add(row);
            }

            var path = _services.WriteExportText("pmdusb_samples", ".csv", lines);
            _services.LogInfo($"[PMD-USB] Exported: {path}");
        }

        private void OnReadIdClick(object? sender, EventArgs e)
        {
            // 之後這裡會發送 PMD-USB 的 READ_ID 命令，並解析回覆
            _services?.LogInfo("[PMD-USB] Read ID requested (TODO: send UART_CMD/READ_ID).");

            // 先做 UI 提示
            MessageBox.Show(
                "即將在接上協議後，於此處發送 READ_ID 命令並顯示韌體/序號。",
                "PMD-USB",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            // 也可暫時更新右側資訊占位
            lblFw.Text = "Firmware: (reading…)";
        }

        private void OnSettingsClick(object? sender, EventArgs e)
        {
            _services?.LogInfo("[PMD-USB] Open Settings dialog (TODO).");
            MessageBox.Show(
                "Settings 對話框尚未建立。\n之後改為開啟 UI/Dialogs/PmdUsbSettingsDialog。",
                "PMD-USB",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        // ---------------- 輔助 ----------------

        private static Button MakeToolbarButton(string text, int left, int top, EventHandler onClick, int width = 72)
        {
            var b = new Button
            {
                Text = text,
                Left = left,
                Top = top,
                Width = width,
                Height = 28
            };
            b.Click += onClick;
            return b;
        }

        private static Label MakeInfoLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };
        }

        private void AppendSample(DateTime t, double ch1, double ch2, double ch3, double ch4, double i, double p)
        {
            var it = new ListViewItem(new[]
            {
                t.ToString("HH:mm:ss.fff"),
                ch1.ToString("0.000"),
                ch2.ToString("0.000"),
                ch3.ToString("0.000"),
                ch4.ToString("0.000"),
                i.ToString("0.000"),
                p.ToString("0.000")
            });
            lvSamples.Items.Add(it);

            if (lvSamples.Items.Count > 0)
                lvSamples.EnsureVisible(lvSamples.Items.Count - 1);
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();

            // 與 Pmd2View 相同，這裡保留從宿主窗體擷取 AppServices 的示範位：
            // 建議讓 MainForm 暴露 internal AppServices Services { get; }
            // 然後：
            // if (_services == null && FindForm() is PMD2_PMDUSB.App.MainForm mf)
            //     _services = mf.Services;
        }
    }
}
