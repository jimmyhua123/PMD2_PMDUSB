// File: App/Views/Pmd2View.cs
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PMD2_PMDUSB.App.Views
{
    /// <summary>
    /// PMD2 的主視圖（UI 骨架）。
    /// 日後把真正資料流事件（OnSample, OnStatus 等）接進來更新畫面。
    /// </summary>
    public sealed class Pmd2View : UserControl
    {
        // 讓 MainForm 傳入全域服務（Log/設定/派送）
        private AppServices? _services;

        // --- UI ---
        private readonly Panel panelToolbar;
        private readonly Button btnStart;
        private readonly Button btnStop;
        private readonly Button btnExport;
        private readonly Button btnCalib;

        private readonly SplitContainer splitMain;
        private readonly Panel panelInfo;
        private readonly Label lblDevice;
        private readonly Label lblConn;
        private readonly Label lblRate;

        private readonly ListView lvSamples;

        public Pmd2View()
        {
            // 外觀
            Dock = DockStyle.Fill;
            BackColor = Color.White;

            // 工具列
            panelToolbar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(245, 245, 245) };
            btnStart = MakeToolbarButton("Start", 12, 8, OnStartClick);
            btnStop = MakeToolbarButton("Stop", 92, 8, OnStopClick);
            btnExport = MakeToolbarButton("Export CSV", 172, 8, OnExportClick, width: 110);
            btnCalib = MakeToolbarButton("Calibration…", 292, 8, OnCalibClick, width: 120);

            panelToolbar.Controls.Add(btnStart);
            panelToolbar.Controls.Add(btnStop);
            panelToolbar.Controls.Add(btnExport);
            panelToolbar.Controls.Add(btnCalib);

            // 主區塊（左資料/右資訊）
            splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 760,
                FixedPanel = FixedPanel.Panel2
            };

            // 左側：樣本清單（之後可換成圖表或雙欄布局）
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
                Text = "PMD2 – Info",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 36,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };
            lblDevice = MakeInfoLabel("Device: PMD2");
            lblConn = MakeInfoLabel("Connection: Idle");
            lblRate = MakeInfoLabel("Sampling: (n/a)");

            panelInfo.Controls.Add(lblRate);
            panelInfo.Controls.Add(lblConn);
            panelInfo.Controls.Add(lblDevice);
            panelInfo.Controls.Add(infoTitle);

            splitMain.Panel2.Padding = new Padding(8, 8, 8, 8);
            splitMain.Panel2.Controls.Add(panelInfo);

            // 組合
            Controls.Add(splitMain);
            Controls.Add(panelToolbar);

            // 預設狀態
            btnStop.Enabled = false;
        }

        /// <summary>
        /// 由 MainForm 在建立視圖後呼叫，注入 AppServices。
        /// </summary>
        public void AttachServices(AppServices services)
        {
            _services = services;
            _services?.LogInfo("Pmd2View attached to services.");
        }

        // ---------------- 事件處理 ----------------

        private void OnStartClick(object? sender, EventArgs e)
        {
            // 之後接 IBackend.Start()；這裡先做 UI 狀態與 Log
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            lblConn.Text = "Connection: Capturing…";
            _services?.LogInfo("[PMD2] Start capture requested.");

            // Demo：塞幾筆假資料，等你把後端接上後用事件取代
            AppendSample(DateTime.Now, 12.01, 11.98, 12.05, 11.99, 0.45, 5.4);
            AppendSample(DateTime.Now.AddMilliseconds(50), 12.02, 11.99, 12.06, 12.01, 0.47, 5.6);
        }

        private void OnStopClick(object? sender, EventArgs e)
        {
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            lblConn.Text = "Connection: Stopped";
            _services?.LogInfo("[PMD2] Stop capture requested.");
        }

        private void OnExportClick(object? sender, EventArgs e)
        {
            _services?.LogInfo("[PMD2] Export CSV requested.");
            // 之後接 Core.Export；這裡先簡單把 ListView 的資料輸出純文字
            if (_services == null)
                return;

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

            var path = _services.WriteExportText("pmd2_samples", ".csv", lines);
            _services.LogInfo($"[PMD2] Exported: {path}");
        }

        private void OnCalibClick(object? sender, EventArgs e)
        {
            _services?.LogInfo("[PMD2] Open Calibration dialog (TODO).");
            MessageBox.Show(
                "Calibration 對話框尚未建立。\n之後改為開啟 UI/Dialogs/CalibrationDialog。",
                "PMD2",
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

            // 滾到最後
            if (lvSamples.Items.Count > 0)
                lvSamples.EnsureVisible(lvSamples.Items.Count - 1);
        }

        // 讓 MainForm 的占位載入時，自動注入 AppServices（若有）
        protected override void OnCreateControl()
        {
            base.OnCreateControl();

            // 嘗試從宿主窗體取出 AppServices（如果 MainForm 有公開屬性可取）
            if (_services == null && FindForm() is PMD2_PMDUSB.App.MainForm mf)
            {
                // 目前 MainForm 沒有公開 Services 屬性，因此這段僅保留為將來擴充示範。
                // 建議：之後在 MainForm 暴露 internal AppServices Services { get; }
                // 然後這裡就可：
                // _services = mf.Services;
            }
        }
    }
}
