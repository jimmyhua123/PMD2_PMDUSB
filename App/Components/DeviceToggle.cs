using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace PMD2_PMDUSB.App.Components
{
    /// <summary>
    /// 切換裝置的簡單元件：PMD2 / PMD-USB
    /// </summary>
    public enum DeviceKind
    {
        PMD2,
        PMD_USB
    }

    public sealed class DeviceToggle : UserControl
    {
        private readonly ComboBox _combo;
        private readonly Label _label;

        public DeviceToggle()
        {
            this.Padding = new Padding(8);
            this.Height = 48;

            _label = new Label
            {
                AutoSize = true,
                Text = "Peripheral：",
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Left,
                Margin = new Padding(0, 12, 6, 0)
            };

            _combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 8, 0, 8)
            };
            _combo.Items.Add("PMD2");
            _combo.Items.Add("PMD-USB");
            _combo.SelectedIndex = 0;
            _combo.SelectedIndexChanged += (_, __) =>
            {
                SelectedKindChanged?.Invoke(this, EventArgs.Empty);
            };

            var panel = new Panel { Dock = DockStyle.Fill };
            panel.Controls.Add(_combo);
            panel.Controls.Add(_label);

            this.Controls.Add(panel);
        }

        [Browsable(true)]
        [Category("Behavior")]
        [Description("目前選擇的裝置")]
        public DeviceKind SelectedKind
        {
            get => _combo.SelectedIndex == 1 ? DeviceKind.PMD_USB : DeviceKind.PMD2;
            set => _combo.SelectedIndex = (value == DeviceKind.PMD_USB) ? 1 : 0;
        }

        /// <summary>
        /// 使用者變更選擇時觸發
        /// </summary>
        public event EventHandler SelectedKindChanged;
    }
}
