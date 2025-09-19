using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PMD2
{
    public static class DisplayHelper
    {
        /// <summary>
        /// 建立三行 (Voltage/Current/Power)，多個通道(如"12V","5V","5VSB"...) 的表格 Label
        /// </summary>
        /// <param name="parent">要把控制項放在哪個父容器</param>
        /// <param name="channels">通道名稱陣列</param>
        /// <param name="startX">起始 X</param>
        /// <param name="startY">起始 Y</param>
        /// <returns>dataLabels["Voltage"]["12V"] = label ...</returns>
        public static Dictionary<string, Dictionary<string, Label>>
            CreateInvertedTable(Control parent, string[] channels, int startX, int startY)
        {
            var dataLabels = new Dictionary<string, Dictionary<string, Label>>();
            dataLabels["Voltage"] = new Dictionary<string, Label>();
            dataLabels["Current"] = new Dictionary<string, Label>();
            dataLabels["Power"] = new Dictionary<string, Label>();

            // 放最上面 channel 標題
            int colIndex = 0;
            foreach (var ch in channels)
            {
                Label lblCh = new Label
                {
                    Text = ch,
                    AutoSize = true,
                    Left = startX + 100 + colIndex * 70,
                    Top = startY,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                parent.Controls.Add(lblCh);
                colIndex++;
            }

            // 左側三行: Voltage, Current, Power
            string[] rowTitles = { "Voltage(V)", "Current(A)", "Power(W)" };
            string[] rowKeys = { "Voltage", "Current", "Power" };

            for (int row = 0; row < rowTitles.Length; row++)
            {
                Label lblRow = new Label
                {
                    Text = rowTitles[row],
                    AutoSize = true,
                    Left = startX,//v a w 三行的起始位置
                    Top = startY + (row + 1) * 25,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                parent.Controls.Add(lblRow);

                colIndex = 0;
                foreach (var ch in channels)
                {
                    Label valLabel = new Label
                    {
                        Text = "0.000",
                        AutoSize = true,
                        Left = startX + 100 + colIndex * 70,//v a w value 三行的起始位置
                        Top = startY + (row + 1) * 25,
                        Font = new Font("Consolas", 9, FontStyle.Regular)
                    };
                    parent.Controls.Add(valLabel);

                    dataLabels[rowKeys[row]][ch] = valLabel;
                    colIndex++;
                }
            }
            return dataLabels;
        }
    }
}
