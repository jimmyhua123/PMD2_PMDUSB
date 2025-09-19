using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace PMD2
{
    public partial class CsvSettingsForm : Form
    {
        private List<string> allFields = new List<string>
        {
            "Timestamp",
            "TotalPower(W)",
            "TotalEPS Power(W)",
            "TotalPCIE Power(W)",
            "TotalCurrent(A)",
            "TotalEPS Current(A)",
            "TotalPCIE Current(A)",
            "MB Current(A)",
            "ATX 12V Current(A)",
            "ATX 5V Current(A)",
            "ATX 5VSB Current(A)",
            "ATX 3.3V Current(A)",
            "HPWR Current(A)",
            "EPS1 Current(A)",
            "EPS2 Current(A)",
            "PCIE1 Current(A)",
            "PCIE2 Current(A)",
            "PCIE3 Current(A)"
        };

        private List<string> selectedFields = new List<string>();
        private readonly string SETTINGS_FILE = "csv_settings.json";

        public CsvSettingsForm()
        {
            InitializeComponent(); // 若使用 WinForms Designer
            LoadSettings();
            GenerateUI();
        }

        private void GenerateUI()
        {
            this.Text = "CSV Settings";
            this.Width = 350;
            this.Height = 350;

            Label lbl = new Label
            {
                Text = "Please select the fields to export.：",
                Left = 10,
                Top = 10,
                AutoSize = true
            };
            this.Controls.Add(lbl);

            CheckedListBox chkList = new CheckedListBox
            {
                Left = 10,
                Top = 40,
                Width = 300,
                Height = 200
            };
            for (int i = 0; i < allFields.Count; i++)
            {
                bool isChecked = selectedFields.Contains(allFields[i]);
                chkList.Items.Add(allFields[i], isChecked);
            }
            this.Controls.Add(chkList);

            Button btnSave = new Button
            {
                Text = "Save",
                Left = 10,
                Top = 250,
                Width = 80
            };
            btnSave.Click += (s, e) =>
            {
                // 收集勾選
                selectedFields.Clear();
                foreach (var item in chkList.CheckedItems)
                {
                    selectedFields.Add(item.ToString());
                }
                SaveSettings();
                MessageBox.Show("CSV Field Settings Saved");
                this.Close();
            };
            this.Controls.Add(btnSave);

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Left = 100,
                Top = 250,
                Width = 80
            };
            btnCancel.Click += (s, e) => { this.Close(); };
            this.Controls.Add(btnCancel);
        }

        private void LoadSettings()
        {
            if (!File.Exists(SETTINGS_FILE)) return;
            try
            {
                string json = File.ReadAllText(SETTINGS_FILE);
                var obj = JsonSerializer.Deserialize<CsvSettingObject>(json);
                if (obj != null && obj.selected_fields != null)
                {
                    selectedFields = obj.selected_fields;
                }
            }
            catch
            {
            }
        }

        private void SaveSettings()
        {
            try
            {
                var obj = new CsvSettingObject { selected_fields = selectedFields };
                string json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SETTINGS_FILE, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save Failed: {ex.Message}");
            }
        }
    }

    public class CsvSettingObject
    {
        public List<string> selected_fields { get; set; }
    }
}
