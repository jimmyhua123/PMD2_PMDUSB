// File: App/Components/DeviceToggle.cs
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PMD2_PMDUSB.App.Components
{
    /// <summary>
    /// 提供 PMD2 / PMD-USB 的切換控制。
    /// - 以 Segmented Buttons（兩顆 Button）實作，容易看、容易點。
    /// - 也提供 DropDown 模式（選配），可在建構子參數指定。
    /// - 觸發 DeviceChanged 事件，SelectedDevice 為 "PMD2" 或 "PMD-USB"。
    /// </summary>
    public sealed class DeviceToggle : UserControl
    {
        public event EventHandler<string>? DeviceChanged; // 參數為 "PMD2" 或 "PMD-USB"

        private readonly bool _useDropdown;
        private readonly Button _btnPmd2;
        private readonly Button _btnPmdUsb;
        private readonly ComboBox _cmb;

        private string _selected = "PMD2";

        /// <summary>
        /// 建構子。
        /// </summary>
        /// <param name="useDropdown">若為 true 則顯示下拉選單；false 則顯示兩顆 Segment 按鈕。</param>
        public DeviceToggle(bool useDropdown = false)
        {
            _useDropdown = useDropdown;
            Height = 32;

            if (_useDropdown)
            {
                _cmb = new ComboBox
                {
                    Dock = DockStyle.Fill,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                _cmb.Items.AddRange(new object[] { "PMD2", "PMD-USB" });
                _cmb.SelectedIndexChanged += (_, __) =>
                {
                    var val = _cmb.SelectedItem?.ToString() ?? "PMD2";
                    SetSelected(val, raiseEvent: true);
                };
                Controls.Add(_cmb);

                // 預設
                _cmb.SelectedIndex = 0;

                // 佔位用，Segment 模式不顯示
                _btnPmd2 = new Button();
                _btnPmdUsb = new Button();
            }
            else
            {
                // Segment Buttons
                var panel = new Panel { Dock = DockStyle.Fill };
                _btnPmd2 = new Button
                {
                    Text = "PMD2",
                    Left = 0,
                    Top = 0,
                    Width = 80,
                    Height = 28
                };
                _btnPmdUsb = new Button
                {
                    Text = "PMD-USB",
                    Left = _btnPmd2.Right - 1, // 貼齊
                    Top = 0,
                    Width = 96,
                    Height = 28
                };

                panel.Controls.Add(_btnPmd2);
                panel.Controls.Add(_btnPmdUsb);
                Controls.Add(panel);

                _btnPmd2.Click += (_, __) => SetSelected("PMD2", raiseEvent: true);
                _btnPmdUsb.Click += (_, __) => SetSelected("PMD-USB", raiseEvent: true);

                ApplySegmentVisual();
                SetSelected("PMD2", raiseEvent: false);
            }
        }

        /// <summary>目前選擇的裝置（"PMD2" 或 "PMD-USB"）。設定時會更新外觀並可選擇觸發事件。</summary>
        public string SelectedDevice
        {
            get => _selected;
            set => SetSelected(value, raiseEvent: false);
        }

        private void SetSelected(string value, bool raiseEvent)
        {
            value = (value == "PMD-USB") ? "PMD-USB" : "PMD2"; // 僅允許兩種
            if (_selected == value)
            {
                // 同值亦需更新外觀（避免外部先改 UI 再設值）
                UpdateVisualSelected();
                return;
            }

            _selected = value;
            UpdateVisualSelected();

            if (_useDropdown)
            {
                if (_cmb.SelectedItem?.ToString() != _selected)
                {
                    _cmb.SelectedItem = _selected;
                }
            }

            if (raiseEvent)
                DeviceChanged?.Invoke(this, _selected);
        }

        private void UpdateVisualSelected()
        {
            if (_useDropdown) return;

            if (_selected == "PMD2")
            {
                _btnPmd2.BackColor = Color.DodgerBlue;
                _btnPmd2.ForeColor = Color.White;
                _btnPmdUsb.BackColor = SystemColors.Control;
                _btnPmdUsb.ForeColor = SystemColors.ControlText;
            }
            else
            {
                _btnPmdUsb.BackColor = Color.DodgerBlue;
                _btnPmdUsb.ForeColor = Color.White;
                _btnPmd2.BackColor = SystemColors.Control;
                _btnPmd2.ForeColor = SystemColors.ControlText;
            }
        }

        private void ApplySegmentVisual()
        {
            // 圓角 + 貼齊的視覺（簡單版）
            _btnPmd2.FlatStyle = FlatStyle.Standard;
            _btnPmdUsb.FlatStyle = FlatStyle.Standard;

            // 讓右鍵距離左鍵 1px，像 Segment
            _btnPmdUsb.Left = _btnPmd2.Right - 1;
        }
    }
}
