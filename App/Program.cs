using System;
using System.Windows.Forms;
using PMD2;

namespace PMD2_PMDUSB.App
{
    internal static class Program
    {
        /// <summary>
        /// 應用程式進入點（殼層）
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
