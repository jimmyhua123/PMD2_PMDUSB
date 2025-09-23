using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;

namespace PMD2_PMDUSB.App.Components
{
    public sealed class ComBar : UserControl
    {
        private readonly ComboBox _cbPorts = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
        private readonly ComboBox _cbBaud = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90 };
        private readonly Button _btnConn = new Button { Text = "Connect", Width = 90 };
        private readonly Button _btnDisc = new Button { Text = "Disconnect", Width = 90, Enabled = false };
        private readonly Button _btnRef = new Button { Text = "Refresh", Width = 80 };

        public ComBar()
        {
            Height = 36;
            Dock = DockStyle.Top;
            Padding = new Padding(6);

            _cbBaud.Items.AddRange(new object[] { 115200, 256000, 230400, 128000, 57600, 38400, 19200, 9600 });
            _cbBaud.SelectedIndex = 0;

            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            flow.Controls.Add(new Label { Text = "Port:", AutoSize = true, Padding = new Padding(0, 8, 6, 0) });
            flow.Controls.Add(_cbPorts);
            flow.Controls.Add(new Label { Text = "Baud:", AutoSize = true, Padding = new Padding(8, 8, 6, 0) });
            flow.Controls.Add(_cbBaud);
            flow.Controls.Add(_btnConn);
            flow.Controls.Add(_btnDisc);
            flow.Controls.Add(_btnRef);

            Controls.Add(flow);

            _btnRef.Click += (_, __) => ReloadPorts();
            _btnConn.Click += (_, __) =>
            {
                if (_cbPorts.SelectedItem == null) { MessageBox.Show("No COM selected."); return; }
                var port = _cbPorts.SelectedItem.ToString();
                var baud = (int)_cbBaud.SelectedItem;
                ConnectRequested?.Invoke(this, new ConnectArgs(port, baud));
            };
            _btnDisc.Click += (_, __) => DisconnectRequested?.Invoke(this, EventArgs.Empty);

            ReloadPorts();
        }

        public void SetConnectedState(bool connected)
        {
            _btnConn.Enabled = !connected;
            _btnDisc.Enabled = connected;
            _cbPorts.Enabled = !connected;
            _cbBaud.Enabled = !connected;
        }

        public void ReloadPorts()
        {
            var cur = _cbPorts.SelectedItem?.ToString();
            var ports = SerialPort.GetPortNames().OrderBy(s => s).ToArray();
            _cbPorts.Items.Clear();
            _cbPorts.Items.AddRange(ports);
            if (ports.Length > 0)
            {
                var idx = Array.IndexOf(ports, cur);
                _cbPorts.SelectedIndex = (idx >= 0) ? idx : 0;
            }
        }

        public event EventHandler<ConnectArgs> ConnectRequested;
        public event EventHandler DisconnectRequested;
    }

    public sealed class ConnectArgs : EventArgs
    {
        public string Port { get; }
        public int Baud { get; }
        public ConnectArgs(string port, int baud) { Port = port; Baud = baud; }
    }
}
