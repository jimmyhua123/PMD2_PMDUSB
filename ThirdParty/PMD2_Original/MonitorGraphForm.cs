using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PMD2
{
    public partial class MonitorGraphForm : Form
    {
        // 定義左右兩邊的通道
        // 左邊通道：Total, 12V, 5V, 3.3V, 5VSB（Total 為新增，置於12V上方）
        private string[] leftChannels = { "Total", "12V", "5V", "3.3V", "5VSB" };
        // 右邊通道：EPS1, EPS2, PCIE1, PCIE2, PCIE3, HPWR
        private string[] rightChannels = { "EPS1", "EPS2", "PCIE1", "PCIE2", "PCIE3", "HPWR" };

        // 用字典儲存各通道的圖表（每個通道皆包含 Voltage、Current、Power）
        private Dictionary<string, MonitorGraph> voltageGraphs = new Dictionary<string, MonitorGraph>();
        private Dictionary<string, MonitorGraph> currentGraphs = new Dictionary<string, MonitorGraph>();
        private Dictionary<string, MonitorGraph> powerGraphs = new Dictionary<string, MonitorGraph>();

        public MonitorGraphForm()
        {
            InitializeComponent();
            this.Text = "All Channels Graphs (Voltage/Current/Power)";

            // 調整視窗大小
            this.Width = 1300;
            this.Height = 750;

            // 建立所有圖表
            BuildAllGraphs();
        }

        private void BuildAllGraphs()
        {
            // 圖表參數設定
            int graphWidth = 200, graphHeight = 100; // 每個圖表的大小
            int colGap = 10, rowGap = 10;

            // 建立左邊通道圖表（Total, 12V, 5V, 3.3V, 5VSB）
            int startXLeft = 10, startYLeft = 10;
            for (int i = 0; i < leftChannels.Length; i++)
            {
                string ch = leftChannels[i];
                int posY = startYLeft + i * (graphHeight + rowGap);

                // Voltage 圖表
                int posX_voltage = startXLeft;
                var gv = new MonitorGraph(
                    id: i,
                    desc: $"{ch} Voltage",
                    suffix: "V",
                    value_format: "F2",
                    pos_x: posX_voltage,
                    pos_y: posY,
                    width: graphWidth,
                    height: graphHeight,
                    border: true,
                    parent_desc: "PMD2"
                );
                this.Controls.Add(gv);
                voltageGraphs[ch] = gv;

                // Current 圖表
                int posX_current = posX_voltage + graphWidth + colGap;
                var gc = new MonitorGraph(
                    id: i + 100,
                    desc: $"{ch} Current",
                    suffix: "A",
                    value_format: "F2",
                    pos_x: posX_current,
                    pos_y: posY,
                    width: graphWidth,
                    height: graphHeight,
                    border: true,
                    parent_desc: "PMD2"
                );
                this.Controls.Add(gc);
                currentGraphs[ch] = gc;

                // Power 圖表
                int posX_power = posX_current + graphWidth + colGap;
                var gp = new MonitorGraph(
                    id: i + 200,
                    desc: $"{ch} Power",
                    suffix: "W",
                    value_format: "F2",
                    pos_x: posX_power,
                    pos_y: posY,
                    width: graphWidth,
                    height: graphHeight,
                    border: true,
                    parent_desc: "PMD2"
                );
                this.Controls.Add(gp);
                powerGraphs[ch] = gp;
            }

            // 建立右邊通道圖表（EPS1, EPS2, PCIE1, PCIE2, PCIE3, HPWR）
            // 設定右邊區域的起始位置，與左邊保留一定間距
            int startXRight = startXLeft + 3 * (graphWidth + colGap) ;
            int startYRight = 10;
            for (int i = 0; i < rightChannels.Length; i++)
            {
                string ch = rightChannels[i];
                int posY = startYRight + i * (graphHeight + rowGap);

                // Voltage 圖表
                int posX_voltage = startXRight;
                var gv = new MonitorGraph(
                    id: leftChannels.Length + i,
                    desc: $"{ch} Voltage",
                    suffix: "V",
                    value_format: "F2",
                    pos_x: posX_voltage,
                    pos_y: posY,
                    width: graphWidth,
                    height: graphHeight,
                    border: true,
                    parent_desc: "PMD2"
                );
                this.Controls.Add(gv);
                voltageGraphs[ch] = gv;

                // Current 圖表
                int posX_current = posX_voltage + graphWidth + colGap;
                var gc = new MonitorGraph(
                    id: leftChannels.Length + i + 100,
                    desc: $"{ch} Current",
                    suffix: "A",
                    value_format: "F2",
                    pos_x: posX_current,
                    pos_y: posY,
                    width: graphWidth,
                    height: graphHeight,
                    border: true,
                    parent_desc: "PMD2"
                );
                this.Controls.Add(gc);
                currentGraphs[ch] = gc;

                // Power 圖表
                int posX_power = posX_current + graphWidth + colGap;
                var gp = new MonitorGraph(
                    id: leftChannels.Length + i + 200,
                    desc: $"{ch} Power",
                    suffix: "W",
                    value_format: "F2",
                    pos_x: posX_power,
                    pos_y: posY,
                    width: graphWidth,
                    height: graphHeight,
                    border: true,
                    parent_desc: "PMD2"
                );
                this.Controls.Add(gp);
                powerGraphs[ch] = gp;
            }
        }

        // 更新指定通道的 Voltage、Current、Power 數值
        // 例如: AddValue("12V", 12.2, 1.5, 18.3) 會更新 12V 對應的圖表數值
        // 同理，您也可以呼叫 AddValue("Total", ...) 來更新 Total 的數值
        public void AddValue(string ch, double voltage, double current, double power)
        {
            if (voltageGraphs.ContainsKey(ch))
                voltageGraphs[ch].AddValue(voltage);
            if (currentGraphs.ContainsKey(ch))
                currentGraphs[ch].AddValue(current);
            if (powerGraphs.ContainsKey(ch))
                powerGraphs[ch].AddValue(power);
        }
    }
}
