using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace PMD2
{
    public partial class CalibrationForm : Form
    {
        //private Dictionary<string, CalibrationParams> calibrationParams;
        //private readonly string CALIBRATION_FILE = "calibration_data.json";
        public static Dictionary<string, CalibrationParams> calibrationParams = new Dictionary<string, CalibrationParams>();
        private readonly string CALIBRATION_FILE = "calibration_data.json";
        private List<string> channelList = new List<string>
        {
            "ATX 12V", "ATX 5V", "ATX 5VSB", "ATX 3.3V",
            "HPWR", "EPS1", "EPS2",
            "PCIE1", "PCIE2", "PCIE3"
        };

        private Dictionary<string, TextBox> txtVG = new Dictionary<string, TextBox>();
        private Dictionary<string, TextBox> txtVO = new Dictionary<string, TextBox>();
        private Dictionary<string, TextBox> txtCG = new Dictionary<string, TextBox>();
        private Dictionary<string, TextBox> txtCO = new Dictionary<string, TextBox>();

        public CalibrationForm()
        {
            InitializeComponent(); // 設計師預設產生；若您是純手動生成，可省略

            // 如果第一次 new 這個 form, 但 static dictionary 還是空, 就初始化
            if (calibrationParams.Count == 0)
            {
                foreach (var ch in channelList)
                {
                    calibrationParams[ch] = new CalibrationParams();
                }
            }

            // 載入檔案
            LoadFile();
            GenerateUI();
        }

        private void GenerateUI()
        {
            this.Text = "Calibration Settings";
            this.Width = 500;
            this.Height = 500;
            int startX = 10, startY = 10;
            int spacingY = 25;

            Label title = new Label
            {
                Text = "校正參數 (電壓 & 電流)",
                Left = startX,
                Top = startY,
                AutoSize = true
            };
            this.Controls.Add(title);

            startY += 30;
            Label lVG = new Label { Text = "VoltGain", Left = startX + 150, Top = startY, AutoSize = true };
            Label lVO = new Label { Text = "VoltOffset", Left = startX + 220, Top = startY, AutoSize = true };
            Label lCG = new Label { Text = "CurrGain", Left = startX + 310, Top = startY, AutoSize = true };
            Label lCO = new Label { Text = "CurrOffset", Left = startX + 380, Top = startY, AutoSize = true };
            this.Controls.Add(lVG); this.Controls.Add(lVO);
            this.Controls.Add(lCG); this.Controls.Add(lCO);

            startY += spacingY;
            foreach (var ch in channelList)
            {
                Label lblCh = new Label
                {
                    Text = ch,
                    Left = startX,
                    Top = startY,
                    AutoSize = true
                };
                this.Controls.Add(lblCh);

                double vg = calibrationParams[ch].voltage_gain;
                double vo = calibrationParams[ch].voltage_offset;
                double cg = calibrationParams[ch].current_gain;
                double co = calibrationParams[ch].current_offset;

                TextBox vgBox = new TextBox { Left = startX + 150, Top = startY - 3, Width = 60, Text = vg.ToString("F3") };
                TextBox voBox = new TextBox { Left = startX + 220, Top = startY - 3, Width = 60, Text = vo.ToString("F3") };
                TextBox cgBox = new TextBox { Left = startX + 310, Top = startY - 3, Width = 60, Text = cg.ToString("F3") };
                TextBox coBox = new TextBox { Left = startX + 380, Top = startY - 3, Width = 60, Text = co.ToString("F3") };

                this.Controls.Add(vgBox); this.Controls.Add(voBox);
                this.Controls.Add(cgBox); this.Controls.Add(coBox);

                txtVG[ch] = vgBox;
                txtVO[ch] = voBox;
                txtCG[ch] = cgBox;
                txtCO[ch] = coBox;

                startY += spacingY;
            }

            Button btnReset = new Button { Text = "Reset", Left = startX, Top = startY + 10, Width = 70 };
            btnReset.Click += (s, e) => { ResetAll(); };
            this.Controls.Add(btnReset);

            Button btnWrite = new Button { Text = "Write", Left = startX + 80, Top = startY + 10, Width = 70 };
            btnWrite.Click += (s, e) => { WriteToMemory(); };
            this.Controls.Add(btnWrite);

            Button btnLoad = new Button { Text = "Load", Left = startX + 160, Top = startY + 10, Width = 70 };
            btnLoad.Click += (s, e) => { LoadFile(); FillUI(); };
            this.Controls.Add(btnLoad);

            Button btnStore = new Button { Text = "Store", Left = startX + 240, Top = startY + 10, Width = 70 };
            btnStore.Click += (s, e) => { WriteToMemory(); SaveFile(); };
            this.Controls.Add(btnStore);
        }

        private void ResetAll()
        {
            foreach (var ch in channelList)
            {
                txtVG[ch].Text = "1.0";
                txtVO[ch].Text = "0.0";
                txtCG[ch].Text = "1.0";
                txtCO[ch].Text = "0.0";
            }
        }

        private void WriteToMemory()
        {
            // 將 UI 的值寫進 calibrationParams
            foreach (var ch in channelList)
            {
                double.TryParse(txtVG[ch].Text, out double vg);
                double.TryParse(txtVO[ch].Text, out double vo);
                double.TryParse(txtCG[ch].Text, out double cg);
                double.TryParse(txtCO[ch].Text, out double co);

                calibrationParams[ch].voltage_gain = vg;
                calibrationParams[ch].voltage_offset = vo;
                calibrationParams[ch].current_gain = cg;
                calibrationParams[ch].current_offset = co;
            }
            MessageBox.Show("Written to Memory (Temporary)");
        }

        private void FillUI()
        {
            // 檔案讀完後，更新 UI
            foreach (var ch in channelList)
            {
                txtVG[ch].Text = calibrationParams[ch].voltage_gain.ToString("F3");
                txtVO[ch].Text = calibrationParams[ch].voltage_offset.ToString("F3");
                txtCG[ch].Text = calibrationParams[ch].current_gain.ToString("F3");
                txtCO[ch].Text = calibrationParams[ch].current_offset.ToString("F3");
            }
        }

        private void LoadFile()
        {
            if (!File.Exists(CALIBRATION_FILE)) return;
            try
            {
                string json = File.ReadAllText(CALIBRATION_FILE);
                var dict = JsonSerializer.Deserialize<Dictionary<string, CalibrationParams>>(json);
                if (dict != null)
                {
                    foreach (var key in dict.Keys)
                    {
                        if (!calibrationParams.ContainsKey(key))
                            calibrationParams[key] = new CalibrationParams();
                        calibrationParams[key] = dict[key];
                    }
                }
            }
            catch
            {
            }
        }

        private void SaveFile()
        {
            try
            {
                string json = JsonSerializer.Serialize(calibrationParams, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(CALIBRATION_FILE, json);
                MessageBox.Show("Saved to File");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save Failed: {ex.Message}");
            }
        }
    }

    public class CalibrationParams
    {
        public double voltage_gain { get; set; } = 1.0;
        public double voltage_offset { get; set; } = 0.0;
        public double current_gain { get; set; } = 1.0;
        public double current_offset { get; set; } = 0.0;
    }
}
