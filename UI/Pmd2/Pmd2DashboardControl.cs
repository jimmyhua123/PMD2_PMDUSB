using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;




namespace PMD2
{
    public partial class MainForm : Form
    {
        private PMD.Core.Interfaces.IDeviceBackend _backend;



        // === 介面控制項(左側) ===
        private GroupBox grpControl;
        private Button btnConnect, btnStart, btnStop;
        private Button btnCalibration, btnCsv, btnExportCsv;
        private CheckBox chkEnableMaxMin;
        private Label lblStatus, lblInterval;
        private NumericUpDown numInterval;
        private RadioButton rbMax, rbMin;
        private Button btnShowGraph;

        // === 顯示區 ===
        private Label lblTotalPower;
        private Dictionary<string, Dictionary<string, Label>> atxLabels;
        private Dictionary<string, Dictionary<string, Label>> epsLabels;
        private Dictionary<string, Dictionary<string, Label>> pcieLabels;
        private Dictionary<string, Dictionary<string, Label>> atxRecordLabels;
        private Dictionary<string, Dictionary<string, Label>> epsRecordLabels;
        private Dictionary<string, Dictionary<string, Label>> pcieRecordLabels;

        // 用於儲存 max/min 數值
        private Dictionary<string, Dictionary<string, Dictionary<string, double>>> recordValues;
        private string[] atxChannels = { "12V", "5V", "5VSB", "3.3V" };
        private string[] epsChannels = { "EPS1", "EPS2" };
        private string[] pcieChannels = { "PCIE1", "PCIE2", "PCIE3", "HPWR" };

        // 後台讀取
        private bool reading;
        private CancellationTokenSource cts;
        private SensorStruct? latestSensor;


        public MainForm()
        {
            InitializeComponent(); // Designer 空檔，僅用程式碼生成

            this.Text = "PMD2 MainForm - Full Code Layout";
            this.Width = 1000;  // 調整寬度
            this.Height = 500;  // 調整高度

            // 設置邊框樣式，使用單一固定邊框
            this.FormBorderStyle = FormBorderStyle.FixedSingle;

            // 禁用最大化按鈕
            this.MaximizeBox = false;

            BuildUI();
            this.Load += MainForm_Load;
        }

        private void BuildUI()
        {
            // 1. 左側控制 GroupBox
            grpControl = new GroupBox
            {
                Text = "Device Control",
                Left = 10,
                Top = 10,
                Width = 180,
                Height = 420
            };
            this.Controls.Add(grpControl);

            int bx = 10, by = 20; // 用於在 grpControl 中定位子控制項

            // Connect
            btnConnect = new Button
            {
                Text = "Connect Device",
                Left = bx,
                Top = by,
                Width = 150
            };
            btnConnect.Click += BtnConnect_Click;
            grpControl.Controls.Add(btnConnect);
            by += 30;

            // Start
            btnStart = new Button
            {
                Text = "Start Read",
                Left = bx,
                Top = by,
                Width = 150
            };
            btnStart.Click += BtnStart_Click;
            grpControl.Controls.Add(btnStart);
            by += 30;

            // Stop
            btnStop = new Button
            {
                Text = "Stop Read",
                Left = bx,
                Top = by,
                Width = 150
            };
            btnStop.Click += BtnStop_Click;
            grpControl.Controls.Add(btnStop);
            by += 30;

            // 狀態
            lblStatus = new Label
            {
                Text = "Status: Disconnected",
                Left = bx,
                Top = by,
                AutoSize = true
            };
            grpControl.Controls.Add(lblStatus);
            by += 30;

            // Calibration
            btnCalibration = new Button
            {
                Text = "Calibration",
                Left = bx,
                Top = by,
                Width = 150
            };
            btnCalibration.Click += (s, e) =>
            {
                var f = new CalibrationForm();
                f.ShowDialog();
            };
            grpControl.Controls.Add(btnCalibration);
            by += 30;

            // CSV Settings
            btnCsv = new Button
            {
                Text = "CSV Settings",
                Left = bx,
                Top = by,
                Width = 150
            };
            btnCsv.Click += (s, e) =>
            {
                var csvForm = new CsvSettingsForm();
                csvForm.ShowDialog();
            };
            grpControl.Controls.Add(btnCsv);
            by += 30;

            // Export CSV
            btnExportCsv = new Button
            {
                Text = "Export CSV Now",
                Left = bx,
                Top = by,
                Width = 150
            };
            btnExportCsv.Click += (s, e) =>
            {
                if (latestSensor.HasValue)
                {
                    Exporter.ExportCsv(latestSensor.Value, null);
                }
                else
                {
                    MessageBox.Show("No Data");
                }
            };
            grpControl.Controls.Add(btnExportCsv);
            by += 30;

            Button btnExportMaxMin = new Button
            {
                Text = "Export Max/Min CSV",
                Left = bx,
                Top = by,
                Width = 150
            };
            btnExportMaxMin.Click += (s, e) =>
            {
                // 假設您已有 recordValues 作為 max/min 記錄的巢狀字典
                // 直接呼叫 Exporter.ExportCsvMaxMin
                if (recordValues != null)
                {
                    // 若您想使用某些自訂欄位：List<string> fields = ...
                    // 否則就傳入 null 讓它使用預設欄位
                    Exporter.ExportCsvMaxMin(recordValues, null);
                }
                else
                {
                    MessageBox.Show("No max/min record to export.");
                }
            };
            grpControl.Controls.Add(btnExportMaxMin);
            by += 30;
            // Enable MaxMin
            chkEnableMaxMin = new CheckBox
            {
                Text = "Enable Max/Min",
                Left = bx,
                Top = by,
                Width = 150
            };
            chkEnableMaxMin.Checked = false;
            grpControl.Controls.Add(chkEnableMaxMin);
            by += 30;

            // Max/Min Radiobuttons
            rbMax = new RadioButton
            {
                Text = "Max",
                Left = bx,
                Top = by,
                Checked = true  // 預設為 Max
            };
            grpControl.Controls.Add(rbMax);

            rbMin = new RadioButton
            {
                Text = "Min",
                Left = rbMax.Right,
                Top = by
            };
            grpControl.Controls.Add(rbMin);
            by += 30;

            // Interval
            lblInterval = new Label
            {
                Text = "Interval(sec):",
                Left = bx,
                Top = by,
                AutoSize = true
            };
            grpControl.Controls.Add(lblInterval);

            numInterval = new NumericUpDown
            {
                Left = bx + 80,
                Top = by - 3,
                Width = 60,
                Minimum = 0.05m,
                Maximum = 10m,
                DecimalPlaces = 2,
                Increment = 0.05m,
                Value = 0.10m
            };
            grpControl.Controls.Add(numInterval);
            by += 40;

            btnShowGraph = new Button
            {
                Text = "Open Graph Window",
                Left = bx,
                Top = by,
                Width = 150
            };
            btnShowGraph.Click += BtnShowGraph_Click;
            grpControl.Controls.Add(btnShowGraph);
            by += 30;


            // ============== 右半部分 ==============
            // 動態表格: ATX24, EPS, PCIE 以及各自對應的 Recorded
            int mainTop = 10;
            int groupHeight = 120;
            int groupWeight = 380;
            // ATX 即時
            GroupBox gbAtx = new GroupBox
            {
                Text = "ATX24 (12V, 5V, 5VSB, 3.3V)",
                Left = 200,
                Top = mainTop,
                Width = groupWeight,
                Height = groupHeight
            };
            this.Controls.Add(gbAtx);
            atxLabels = DisplayHelper.CreateInvertedTable(gbAtx, atxChannels, 10, 20);

            // ATX Recorded
            GroupBox gbAtxRec = new GroupBox
            {
                Text = "ATX24 (Recorded Max/Min)",
                Left = gbAtx.Right + 10,
                Top = mainTop,
                Width = groupWeight,
                Height = groupHeight
            };
            this.Controls.Add(gbAtxRec);
            atxRecordLabels = DisplayHelper.CreateInvertedTable(gbAtxRec, atxChannels, 10, 20);

            // EPS 即時
            GroupBox gbEps = new GroupBox
            {
                Text = "EPS (EPS1, EPS2)",
                Left = 200,
                Top = gbAtx.Bottom + 10,
                Width = groupWeight,
                Height = groupHeight
            };
            this.Controls.Add(gbEps);
            epsLabels = DisplayHelper.CreateInvertedTable(gbEps, epsChannels, 10, 20);

            // EPS Recorded
            GroupBox gbEpsRec = new GroupBox
            {
                Text = "EPS (Recorded)",
                Left = gbEps.Right + 10,
                Top = gbAtxRec.Bottom + 10, // 依需要微調
                Width = groupWeight,
                Height = groupHeight
            };
            this.Controls.Add(gbEpsRec);
            epsRecordLabels = DisplayHelper.CreateInvertedTable(gbEpsRec, epsChannels, 10, 20);

            // PCIE 即時
            GroupBox gbPcie = new GroupBox
            {
                Text = "PCIE & HPWR",
                Left = 200,
                Top = gbEps.Bottom + 10,
                Width = groupWeight,
                Height = groupHeight
            };
            this.Controls.Add(gbPcie);
            pcieLabels = DisplayHelper.CreateInvertedTable(gbPcie, pcieChannels, 10, 20);

            // PCIE Recorded
            GroupBox gbPcieRec = new GroupBox
            {
                Text = "PCIE & HPWR (Recorded)",
                Left = gbPcie.Right + 10,
                Top = gbEpsRec.Bottom + 10,
                Width = groupWeight,
                Height = groupHeight
            };
            this.Controls.Add(gbPcieRec);
            pcieRecordLabels = DisplayHelper.CreateInvertedTable(gbPcieRec, pcieChannels, 10, 20);



            // 最底部：Total Power
            lblTotalPower = new Label
            {
                Text = "Total Power: 0.000 W",
                Left = 200,
                Top = gbPcieRec.Bottom + 10,
                AutoSize = true,
                Font = new Font("微軟正黑體", 12, FontStyle.Bold)
            };
            this.Controls.Add(lblTotalPower);
        }

        private MonitorGraphForm graphForm;
        private void BtnShowGraph_Click(object sender, EventArgs e)
        {
            if (graphForm == null || graphForm.IsDisposed)
            {
                graphForm = new MonitorGraphForm();
            }
            graphForm.Show(); // 顯示 (非模態)
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            comboSource.Items.AddRange(new[] { "PMD2", "PMD-USB" });
            comboSource.SelectedIndex = 0;
            SwitchBackend(PMD.App.DeviceType.PMD2);

            // 初始化
            serialComm = new SerialComm();

            recordValues = new Dictionary<string, Dictionary<string, Dictionary<string, double>>>();
            recordValues["ATX"] = new Dictionary<string, Dictionary<string, double>>();
            recordValues["EPS"] = new Dictionary<string, Dictionary<string, double>>();
            recordValues["PCIE"] = new Dictionary<string, Dictionary<string, double>>();

            string[] keys = { "Voltage", "Current", "Power" };
            foreach (var g in recordValues.Keys)
            {
                foreach (var k in keys)
                {
                    recordValues[g][k] = new Dictionary<string, double>();
                }
            }

            foreach (var ch in atxChannels)
            {
                recordValues["ATX"]["Voltage"][ch] = 0.0;
                recordValues["ATX"]["Current"][ch] = 0.0;
                recordValues["ATX"]["Power"][ch] = 0.0;
            }
            foreach (var ch in epsChannels)
            {
                recordValues["EPS"]["Voltage"][ch] = 0.0;
                recordValues["EPS"]["Current"][ch] = 0.0;
                recordValues["EPS"]["Power"][ch] = 0.0;
            }
            foreach (var ch in pcieChannels)
            {
                recordValues["PCIE"]["Voltage"][ch] = 0.0;
                recordValues["PCIE"]["Current"][ch] = 0.0;
                recordValues["PCIE"]["Power"][ch] = 0.0;
            }
        }

        private void comboSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            var type = comboSource.SelectedIndex == 0 ? PMD.App.DeviceType.PMD2 : PMD.App.DeviceType.PMD_USB;
            SwitchBackend(type);
        }

        private void SwitchBackend(PMD.App.DeviceType type)
        {
            _backend?.Dispose();
            _backend = PMD.App.BackendFactory.Create(type);
            _backend.Status += (_, msg) => AppendLog(msg);
            _backend.DataReceived += (_, s) => OnSensorData(s); // 這裡更新你的圖表/表格
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            await _backend!.ConnectAsync(txtPort.Text, int.Parse(txtBaud.Text));
        }

        private async void btnDisconnect_Click(object sender, EventArgs e)
        {
            await _backend!.DisconnectAsync();
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {

            if (serialComm.IsOpen)
            {
                lblStatus.Text = "Status：Connected";
                MessageBox.Show("Connected");

                return;
            }
            if (serialComm.OpenPort())
            {
                if (serialComm.DeviceHandshake())
                {
                    lblStatus.Text = "Status：Connected";
                }
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (!serialComm.IsOpen)
            {
                MessageBox.Show("Please Connect Device");
                return;
            }
            if (!reading)
            {
                reading = true;
                cts = new CancellationTokenSource();
                Task.Run(() => ReadLoop(cts.Token));
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            reading = false;
            if (cts != null)
            {
                cts.Cancel();
            }
        }

        private void ReadLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!serialComm.IsOpen)
                {
                    // 序列埠已經關閉或失效，就暫停一小段時間
                    Thread.Sleep(500);
                    continue;
                }

                try
                {
                    var sensor = serialComm.ReadSensorStruct();
                    if (sensor.HasValue)
                    {
                        latestSensor = sensor;
                        this.Invoke((MethodInvoker)delegate
                        {
                            UpdateUI(sensor.Value);
                        });
                    }
                }
                catch (InvalidOperationException ex)
                {
                    // 代表裝置或序列埠不允許這樣操作
                    // 可以在這裡顯示錯誤或記錄
                    this.Invoke((MethodInvoker)delegate
                    {
                        MessageBox.Show("SerialPort Abnormal：" + ex.Message);
                    });
                    // 也可停止讀取
                    break;
                }
                catch (Exception ex)
                {
                    // 其他例外處理
                    this.Invoke((MethodInvoker)delegate
                    {
                        MessageBox.Show("Reading Error Occurred：" + ex.Message);
                    });
                    break;
                }

                double interval = (double)numInterval.Value;
                if (interval < 0.05) interval = 0.05;
                Thread.Sleep((int)(interval * 1000));
            }
        }


        private (double voltage, double current, double power) GetCalibratedValues(string group, string ch, PowerSensor ps)
        {
            double v = ps.Voltage / 1000.0;
            double c = ps.Current / 1000.0;
            string key = group + " " + ch;
            if (CalibrationForm.calibrationParams.ContainsKey(key))
            {
                var cal = CalibrationForm.calibrationParams[key];
                v = v * cal.voltage_gain + cal.voltage_offset;
                c = c * cal.current_gain + cal.current_offset;
            }
            double p = v * c;
            return (v, c, p);
        }

        // 更新個別通道的即時顯示與最大/最小記錄，並同步更新圖表 (不改變原有 graph 設定)
        private void UpdateChannelUI(string group, string ch, double v, double c, double p)
        {
            if (group == "ATX")
            {
                atxLabels["Voltage"][ch].Text = v.ToString("F3");
                atxLabels["Current"][ch].Text = c.ToString("F3");
                atxLabels["Power"][ch].Text = p.ToString("F3");
            }
            else if (group == "EPS")
            {
                epsLabels["Voltage"][ch].Text = v.ToString("F3");
                epsLabels["Current"][ch].Text = c.ToString("F3");
                epsLabels["Power"][ch].Text = p.ToString("F3");
            }
            else if (group == "PCIE")
            {
                pcieLabels["Voltage"][ch].Text = v.ToString("F3");
                pcieLabels["Current"][ch].Text = c.ToString("F3");
                pcieLabels["Power"][ch].Text = p.ToString("F3");
            }

            if (chkEnableMaxMin.Checked)
            {
                bool useMax = rbMax.Checked;
                double oldV = recordValues[group]["Voltage"][ch];
                double oldC = recordValues[group]["Current"][ch];
                double oldP = recordValues[group]["Power"][ch];
                double newV = useMax ? Math.Max(oldV, v) : (oldV == 0 ? v : Math.Min(oldV, v));
                double newC = useMax ? Math.Max(oldC, c) : (oldC == 0 ? c : Math.Min(oldC, c));
                double newP = useMax ? Math.Max(oldP, p) : (oldP == 0 ? p : Math.Min(oldP, p));
                recordValues[group]["Voltage"][ch] = newV;
                recordValues[group]["Current"][ch] = newC;
                recordValues[group]["Power"][ch] = newP;

                if (group == "ATX")
                {
                    atxRecordLabels["Voltage"][ch].Text = newV.ToString("F3");
                    atxRecordLabels["Current"][ch].Text = newC.ToString("F3");
                    atxRecordLabels["Power"][ch].Text = newP.ToString("F3");
                }
                else if (group == "EPS")
                {
                    epsRecordLabels["Voltage"][ch].Text = newV.ToString("F3");
                    epsRecordLabels["Current"][ch].Text = newC.ToString("F3");
                    epsRecordLabels["Power"][ch].Text = newP.ToString("F3");
                }
                else if (group == "PCIE")
                {
                    pcieRecordLabels["Voltage"][ch].Text = newV.ToString("F3");
                    pcieRecordLabels["Current"][ch].Text = newC.ToString("F3");
                    pcieRecordLabels["Power"][ch].Text = newP.ToString("F3");
                }
            }

            if (graphForm != null && !graphForm.IsDisposed)
            {
                graphForm.AddValue(ch, v, c, p);
            }
        }

        // 修改後的 UpdateUI：直接將各通道校正後的 Voltage、Current 與 Power 累加後更新 Total 通道
        private void UpdateUI(SensorStruct sensor)
        {
            var pr = sensor.PowerReadings;
            double totalVoltage = 0.0;
            double totalCurrent = 0.0;
            double totalPower = 0.0;

            // ATX 通道 (索引 0~3)
            for (int i = 0; i < 4; i++)
            {
                var (v, c, p) = GetCalibratedValues("ATX", atxChannels[i], pr[i]);
                totalVoltage += v;
                totalCurrent += c;
                totalPower += p;
                UpdateChannelUI("ATX", atxChannels[i], v, c, p);
            }
            // EPS 通道 (索引 5、6)
            for (int i = 5; i <= 6; i++)
            {
                var (v, c, p) = GetCalibratedValues("EPS", epsChannels[i - 5], pr[i]);
                totalVoltage += v;
                totalCurrent += c;
                totalPower += p;
                UpdateChannelUI("EPS", epsChannels[i - 5], v, c, p);
            }
            // PCIE 通道 (索引依序為 7, 8, 9, 4)
            int[] pcieIdx = { 7, 8, 9, 4 };
            for (int i = 0; i < 4; i++)
            {
                var (v, c, p) = GetCalibratedValues("PCIE", pcieChannels[i], pr[pcieIdx[i]]);
                totalVoltage += v;
                totalCurrent += c;
                totalPower += p;
                UpdateChannelUI("PCIE", pcieChannels[i], v, c, p);
            }

            lblTotalPower.Text = $"Total Power: {totalPower:F3} W";
            if (graphForm != null && !graphForm.IsDisposed)
            {
                graphForm.AddValue("Total", totalVoltage, totalCurrent, totalPower);
            }
        }




    }
}
