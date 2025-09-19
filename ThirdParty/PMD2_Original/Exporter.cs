using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PMD2
{
    public static class Exporter
    {
        // 預設輸出欄位順序
        public static List<string> DefaultFields = new List<string>
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

        /// <summary>
        /// 輸出感測器數值至 CSV (原始數值單位假設為 mW 與 mA，輸出前除以 1000)
        /// </summary>
        /// <param name="sensor">包含感測數據的 SensorStruct 物件</param>
        /// <param name="selectedFields">欲輸出的欄位，若為 null 或空則輸出全部欄位</param>
        public static void ExportCsv(SensorStruct sensor, List<string> selectedFields = null)
        {
            if (selectedFields == null || selectedFields.Count == 0)
            {
                selectedFields = DefaultFields;
            }

            var pr = sensor.PowerReadings;
            // 計算各組電力（單位：W，除以 1000）
            double eps_power = (pr[5].Power + pr[6].Power) / 1000.0;
            double pcie_power = (pr[7].Power + pr[8].Power + pr[9].Power + pr[4].Power) / 1000.0;
            double mb_power = (pr[0].Power + pr[1].Power + pr[2].Power + pr[3].Power) / 1000.0;
            double total_power = eps_power + pcie_power + mb_power;

            // 計算各組電流（單位：A，除以 1000）
            double eps_current = (pr[5].Current + pr[6].Current) / 1000.0;
            double pcie_current = (pr[7].Current + pr[8].Current + pr[9].Current + pr[4].Current) / 1000.0;
            double mb_current = (pr[0].Current + pr[1].Current + pr[2].Current + pr[3].Current) / 1000.0;
            double total_current = eps_current + pcie_current + mb_current;

            // 各接頭個別電流
            double atx12v_current = pr[0].Current / 1000.0;
            double atx5v_current = pr[1].Current / 1000.0;
            double atx5vsb_current = pr[2].Current / 1000.0;
            double atx3v_current = pr[3].Current / 1000.0;
            double hpwr_current = pr[4].Current / 1000.0;
            double eps1_current = pr[5].Current / 1000.0;
            double eps2_current = pr[6].Current / 1000.0;
            double pcie1_current = pr[7].Current / 1000.0;
            double pcie2_current = pr[8].Current / 1000.0;
            double pcie3_current = pr[9].Current / 1000.0;

            // 取得時間戳記 (前置單引號避免 Excel 誤判格式)
            string timestamp = "'" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

            // 建立輸出資料字典 (依照指定格式轉換為字串)
            var data = new Dictionary<string, string>
            {
                { "Timestamp", timestamp },
                { "TotalPower(W)", total_power.ToString("F3", CultureInfo.InvariantCulture) },
                { "TotalEPS Power(W)", eps_power.ToString("F3", CultureInfo.InvariantCulture) },
                { "TotalPCIE Power(W)", pcie_power.ToString("F3", CultureInfo.InvariantCulture) },
                { "TotalCurrent(A)", total_current.ToString("F3", CultureInfo.InvariantCulture) },
                { "TotalEPS Current(A)", eps_current.ToString("F3", CultureInfo.InvariantCulture) },
                { "TotalPCIE Current(A)", pcie_current.ToString("F3", CultureInfo.InvariantCulture) },
                { "MB Current(A)", mb_current.ToString("F3", CultureInfo.InvariantCulture) },
                { "ATX 12V Current(A)", atx12v_current.ToString("F3", CultureInfo.InvariantCulture) },
                { "ATX 5V Current(A)", atx5v_current.ToString("F3", CultureInfo.InvariantCulture) },
                { "ATX 5VSB Current(A)", atx5vsb_current.ToString("F3", CultureInfo.InvariantCulture) },
                { "ATX 3.3V Current(A)", atx3v_current.ToString("F3", CultureInfo.InvariantCulture) },
                { "HPWR Current(A)", hpwr_current.ToString("F3", CultureInfo.InvariantCulture) },
                { "EPS1 Current(A)", eps1_current.ToString("F3", CultureInfo.InvariantCulture) },
                { "EPS2 Current(A)", eps2_current.ToString("F3", CultureInfo.InvariantCulture) },
                { "PCIE1 Current(A)", pcie1_current.ToString("F3", CultureInfo.InvariantCulture) },
                { "PCIE2 Current(A)", pcie2_current.ToString("F3", CultureInfo.InvariantCulture) },
                { "PCIE3 Current(A)", pcie3_current.ToString("F3", CultureInfo.InvariantCulture) }
            };

            // 檔案名稱格式：pmd2_YYYYMMDD_HHmmss.csv
            string filename = $"pmd2_{DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)}.csv";

            try
            {
                using (var writer = new StreamWriter(filename, false, Encoding.UTF8))
                {
                    // 輸出欄位列
                    writer.WriteLine(string.Join(",", selectedFields));
                    // 輸出數值列 (依照欄位順序)
                    var row = selectedFields.Select(field => data.ContainsKey(field) ? data[field] : "");
                    writer.WriteLine(string.Join(",", row));
                }
                MessageBox.Show($"**CSV File Exported**：{filename}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export CSV failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 輸出 Max/Min 記錄數值至 CSV，recordValues 結構如下：
        /// {
        ///   "ATX": {"Voltage": {...}, "Current": {...}, "Power": {...}},
        ///   "EPS": {"Voltage": {...}, "Current": {...}, "Power": {...}},
        ///   "PCIE": {"Voltage": {...}, "Current": {...}, "Power": {...}}
        /// }
        /// </summary>
        /// <param name="recordValues">記錄數值的巢狀字典</param>
        /// <param name="selectedFields">欲輸出的欄位，若為 null 或空則輸出全部預設欄位</param>
        public static void ExportCsvMaxMin(Dictionary<string, Dictionary<string, Dictionary<string, double>>> recordValues, List<string> selectedFields = null)
        {
            if (selectedFields == null || selectedFields.Count == 0)
            {
                selectedFields = DefaultFields;
            }

            // 計算 ATX 組總和（Current 與 Power）
            double atx_current_sum = (recordValues["ATX"]["Current"].ContainsKey("12V") ? recordValues["ATX"]["Current"]["12V"] : 0)
                                    + (recordValues["ATX"]["Current"].ContainsKey("5V") ? recordValues["ATX"]["Current"]["5V"] : 0)
                                    + (recordValues["ATX"]["Current"].ContainsKey("5VSB") ? recordValues["ATX"]["Current"]["5VSB"] : 0)
                                    + (recordValues["ATX"]["Current"].ContainsKey("3.3V") ? recordValues["ATX"]["Current"]["3.3V"] : 0);
            double atx_power_sum = (recordValues["ATX"]["Power"].ContainsKey("12V") ? recordValues["ATX"]["Power"]["12V"] : 0)
                                  + (recordValues["ATX"]["Power"].ContainsKey("5V") ? recordValues["ATX"]["Power"]["5V"] : 0)
                                  + (recordValues["ATX"]["Power"].ContainsKey("5VSB") ? recordValues["ATX"]["Power"]["5VSB"] : 0)
                                  + (recordValues["ATX"]["Power"].ContainsKey("3.3V") ? recordValues["ATX"]["Power"]["3.3V"] : 0);

            // EPS 組
            double eps_current_sum = (recordValues["EPS"]["Current"].ContainsKey("EPS1") ? recordValues["EPS"]["Current"]["EPS1"] : 0)
                                    + (recordValues["EPS"]["Current"].ContainsKey("EPS2") ? recordValues["EPS"]["Current"]["EPS2"] : 0);
            double eps_power_sum = (recordValues["EPS"]["Power"].ContainsKey("EPS1") ? recordValues["EPS"]["Power"]["EPS1"] : 0)
                                  + (recordValues["EPS"]["Power"].ContainsKey("EPS2") ? recordValues["EPS"]["Power"]["EPS2"] : 0);

            // PCIE 組
            double pcie_current_sum = (recordValues["PCIE"]["Current"].ContainsKey("PCIE1") ? recordValues["PCIE"]["Current"]["PCIE1"] : 0)
                                     + (recordValues["PCIE"]["Current"].ContainsKey("PCIE2") ? recordValues["PCIE"]["Current"]["PCIE2"] : 0)
                                     + (recordValues["PCIE"]["Current"].ContainsKey("PCIE3") ? recordValues["PCIE"]["Current"]["PCIE3"] : 0)
                                     + (recordValues["PCIE"]["Current"].ContainsKey("HPWR") ? recordValues["PCIE"]["Current"]["HPWR"] : 0);
            double pcie_power_sum = (recordValues["PCIE"]["Power"].ContainsKey("PCIE1") ? recordValues["PCIE"]["Power"]["PCIE1"] : 0)
                                    + (recordValues["PCIE"]["Power"].ContainsKey("PCIE2") ? recordValues["PCIE"]["Power"]["PCIE2"] : 0)
                                    + (recordValues["PCIE"]["Power"].ContainsKey("PCIE3") ? recordValues["PCIE"]["Power"]["PCIE3"] : 0)
                                    + (recordValues["PCIE"]["Power"].ContainsKey("HPWR") ? recordValues["PCIE"]["Power"]["HPWR"] : 0);

            double total_current = atx_current_sum + eps_current_sum + pcie_current_sum;
            double total_power = atx_power_sum + eps_power_sum + pcie_power_sum;

            string timestamp = "'" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

            // 建立輸出資料字典
            var data = new Dictionary<string, string>
            {
                { "Timestamp", timestamp },
                { "TotalPower(W)", total_power.ToString("F3", CultureInfo.InvariantCulture) },
                { "TotalEPS Power(W)", eps_power_sum.ToString("F3", CultureInfo.InvariantCulture) },
                { "TotalPCIE Power(W)", pcie_power_sum.ToString("F3", CultureInfo.InvariantCulture) },
                { "TotalCurrent(A)", total_current.ToString("F3", CultureInfo.InvariantCulture) },
                { "TotalEPS Current(A)", eps_current_sum.ToString("F3", CultureInfo.InvariantCulture) },
                { "TotalPCIE Current(A)", pcie_current_sum.ToString("F3", CultureInfo.InvariantCulture) },
                { "MB Current(A)", atx_current_sum.ToString("F3", CultureInfo.InvariantCulture) },
                { "ATX 12V Current(A)", recordValues["ATX"]["Current"].ContainsKey("12V") ? recordValues["ATX"]["Current"]["12V"].ToString("F3", CultureInfo.InvariantCulture) : "0.000" },
                { "ATX 5V Current(A)", recordValues["ATX"]["Current"].ContainsKey("5V") ? recordValues["ATX"]["Current"]["5V"].ToString("F3", CultureInfo.InvariantCulture) : "0.000" },
                { "ATX 5VSB Current(A)", recordValues["ATX"]["Current"].ContainsKey("5VSB") ? recordValues["ATX"]["Current"]["5VSB"].ToString("F3", CultureInfo.InvariantCulture) : "0.000" },
                { "ATX 3.3V Current(A)", recordValues["ATX"]["Current"].ContainsKey("3.3V") ? recordValues["ATX"]["Current"]["3.3V"].ToString("F3", CultureInfo.InvariantCulture) : "0.000" },
                { "HPWR Current(A)", recordValues["PCIE"]["Current"].ContainsKey("HPWR") ? recordValues["PCIE"]["Current"]["HPWR"].ToString("F3", CultureInfo.InvariantCulture) : "0.000" },
                { "EPS1 Current(A)", recordValues["EPS"]["Current"].ContainsKey("EPS1") ? recordValues["EPS"]["Current"]["EPS1"].ToString("F3", CultureInfo.InvariantCulture) : "0.000" },
                { "EPS2 Current(A)", recordValues["EPS"]["Current"].ContainsKey("EPS2") ? recordValues["EPS"]["Current"]["EPS2"].ToString("F3", CultureInfo.InvariantCulture) : "0.000" },
                { "PCIE1 Current(A)", recordValues["PCIE"]["Current"].ContainsKey("PCIE1") ? recordValues["PCIE"]["Current"]["PCIE1"].ToString("F3", CultureInfo.InvariantCulture) : "0.000" },
                { "PCIE2 Current(A)", recordValues["PCIE"]["Current"].ContainsKey("PCIE2") ? recordValues["PCIE"]["Current"]["PCIE2"].ToString("F3", CultureInfo.InvariantCulture) : "0.000" },
                { "PCIE3 Current(A)", recordValues["PCIE"]["Current"].ContainsKey("PCIE3") ? recordValues["PCIE"]["Current"]["PCIE3"].ToString("F3", CultureInfo.InvariantCulture) : "0.000" }
            };

            string filename = $"pmd2_maxmin_{DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)}.csv";

            try
            {
                using (var writer = new StreamWriter(filename, false, Encoding.UTF8))
                {
                    writer.WriteLine(string.Join(",", selectedFields));
                    var row = selectedFields.Select(field => data.ContainsKey(field) ? data[field] : "");
                    writer.WriteLine(string.Join(",", row));
                }
                MessageBox.Show($"CSV Max/Min File Exported：{filename}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export CSV Max/Min failed: {ex.Message}");
            }
        }
    }
}
