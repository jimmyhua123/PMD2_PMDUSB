using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace PMD2_PMDUSB.App.Components
{
    /// <summary>
    /// 殼層底部的狀態列（顯示目前模式、附帶一段狀態訊息）
    /// </summary>
    public sealed class StatusBar : UserControl
    {
        private readonly Label _modeLabel;
        private readonly Label _statusLabel;

        public StatusBar()
        {
            Height = 28;
            Dock = DockStyle.Bottom;
            BackColor = SystemColors.ControlLightLight;

            var sep = new Label
            {
                Text = "│",
                AutoSize = true,
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(4, 6, 4, 6),
                ForeColor = Color.DimGray
            };

            _modeLabel = new Label
            {
                Text = "Mode: PMD2",
                AutoSize = true,
                Dock = DockStyle.Left,
                Padding = new Padding(8, 6, 8, 6),
                ForeColor = Color.Black
            };

            _statusLabel = new Label
            {
                Text = "Ready",
                AutoSize = false,
                Dock = DockStyle.Fill,
                Padding = new Padding(8, 6, 8, 6),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.DimGray
            };

            Controls.Add(_statusLabel);
            Controls.Add(sep);
            Controls.Add(_modeLabel);
        }

        [Browsable(true)]
        [Category("Appearance")]
        public string ModeText
        {
            get => _modeLabel.Text;
            set => _modeLabel.Text = value;
        }

        [Browsable(true)]
        [Category("Appearance")]
        public string StatusText
        {
            get => _statusLabel.Text;
            set => _statusLabel.Text = value;
        }

        /// <summary>快速設定模式：PMD2 / PMD-USB。</summary>
        public void SetMode(bool isPmd2)
        {
            ModeText = $"Mode: {(isPmd2 ? "PMD2" : "PMD-USB")}";
        }
    }
}
