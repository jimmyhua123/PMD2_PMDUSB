// File: App/Components/StatusBar.cs
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PMD2_PMDUSB.App.Components
{
    /// <summary>
    /// 可繫結 AppServices 的狀態列控制項。
    /// - 左側顯示最近一筆 Log 訊息（自動訂閱 AppServices.LogEmitted）
    /// - 右側可程式設定自由文字（例如：Connected / Disconnected）
    /// </summary>
    public sealed class StatusBar : UserControl
    {
        private readonly StatusStrip _strip;
        private readonly ToolStripStatusLabel _left;
        private readonly ToolStripStatusLabel _right;

        // 參考到的 AppServices（用於自動更新左側訊息）
        private App.AppServices? _services;

        public StatusBar()
        {
            Dock = DockStyle.Bottom;
            Height = 24;

            _strip = new StatusStrip
            {
                Dock = DockStyle.Fill,
                SizingGrip = false
            };

            _left = new ToolStripStatusLabel
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Ready."
            };
            _right = new ToolStripStatusLabel
            {
                Spring = false,
                TextAlign = ContentAlignment.MiddleRight,
                Text = "Idle"
            };

            _strip.Items.Add(_left);
            _strip.Items.Add(_right);
            Controls.Add(_strip);
        }

        /// <summary>繫結 AppServices，左側會顯示最新 Log。</summary>
        public void Bind(App.AppServices services)
        {
            // 解除舊訂閱
            if (_services != null)
                _services.LogEmitted -= OnLogEmitted;

            _services = services ?? throw new ArgumentNullException(nameof(services));
            _services.LogEmitted += OnLogEmitted;
        }

        /// <summary>設定右側的狀態訊息。</summary>
        public void SetRightText(string text)
        {
            _right.Text = text ?? string.Empty;
        }

        /// <summary>直接設定左側文字（不經由 Log）。</summary>
        public void SetLeftText(string text)
        {
            _left.Text = text ?? string.Empty;
        }

        private void OnLogEmitted(App.LogEntry e)
        {
            if (IsDisposed) return;
            if (_services == null) return;

            // 使用 AppServices 的 UI 派送，確保在 UI 執行緒更新
            _services.UiPost(() =>
            {
                if (!IsDisposed)
                {
                    _left.Text = $"{e.Timestamp:HH:mm:ss} {e.Level}: {e.Message}";
                }
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _services != null)
            {
                _services.LogEmitted -= OnLogEmitted;
                _services = null;
            }
            base.Dispose(disposing);
        }
    }
}
