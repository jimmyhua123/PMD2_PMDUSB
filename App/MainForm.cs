using System;
using System.Drawing;
using System.Windows.Forms;
using PMD2_PMDUSB.App.Components;


// 兩邊原始 UI 的命名空間：
// PMD2.MainForm (PMD2 版本主畫面)
// PMD.FormPMD  (PMD-USB 版本主畫面)
using Pmd2MainForm = PMD2.MainForm;
using PmdUsbMainForm = PMD.FormPMD;

namespace PMD2_PMDUSB.App
{
    public sealed class MainForm : Form
    {
        private readonly Panel _leftPane;
        private readonly Panel _hostPanel;
        private readonly DeviceToggle _toggle;
        private readonly Button _btnReload;
        private readonly Components.StatusBar _statusBar;

        private Form _currentChild;
        private bool _switching;

        public MainForm()
        {
            Text = "PMD2 / PMD-USB - Shell";
            MinimumSize = new Size(980, 680);
            StartPosition = FormStartPosition.CenterScreen;

            // 左側控制區
            _leftPane = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = SystemColors.ControlLight
            };

            _toggle = new DeviceToggle
            {
                Dock = DockStyle.Top
            };
            _toggle.SelectedKindChanged += ToggleOnSelectedKindChanged;

            _btnReload = new Button
            {
                Dock = DockStyle.Top,
                Height = 36,
                Text = "Reload current view",
                Margin = new Padding(8)
            };
            _btnReload.Click += (_, __) => ReloadCurrent();

            var info = new Label
            {
                Dock = DockStyle.Top,
                Height = 36,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 8, 0),
                Text = "Description: Switch between PMD2 / PMD-USB on the left side. The previous view will be closed when switching."
            };

            _leftPane.Controls.Add(info);
            _leftPane.Controls.Add(_btnReload);
            _leftPane.Controls.Add(_toggle);

            // 右側承載區
            _hostPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            _statusBar = new Components.StatusBar();
            _statusBar.StatusText = "Ready";

            

            Controls.Add(_hostPanel);
            Controls.Add(_leftPane);
            Controls.Add(_statusBar); // 讓它 Dock 到底部

            // 載入預設（PMD2）
            Load += (_, __) => SwitchTo(_toggle.SelectedKind);
            FormClosing += OnFormClosing;
        }

        private void ToggleOnSelectedKindChanged(object sender, EventArgs e)
        {
            SwitchTo(_toggle.SelectedKind);
        }

        /// <summary>
        /// 重新建立目前視圖（例如視圖內部資源錯位時可用）
        /// </summary>
        private void ReloadCurrent()
        {
            SwitchTo(_toggle.SelectedKind, forceReload: true);
        }

        private void SwitchTo(DeviceKind kind, bool forceReload = false)
        {
            if (_switching) return;
            _switching = true;

            try
            {
                // 若已經是相同型別且非強制重載，直接略過
                if (!forceReload && _currentChild != null)
                {
                    if ((kind == DeviceKind.PMD2 && _currentChild is Pmd2MainForm) ||
                        (kind == DeviceKind.PMD_USB && _currentChild is PmdUsbMainForm))
                    {
                        return;
                    }
                }

                // 關閉舊視圖，避免序列埠等資源佔用
                SafeCloseChild();

                // 建立新視圖（完全不改原始碼）
                Form newChild = (kind == DeviceKind.PMD2)
                    ? (Form)new Pmd2MainForm()
                    : (Form)new PmdUsbMainForm();

                EmbedChild(newChild);

                // 視窗標題顯示目前模式
                this.Text = $"PMD2 / PMD-USB - Shell  [{(kind == DeviceKind.PMD2 ? "PMD2" : "PMD-USB")}]";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Exception occurred during view switch：\n" + ex.Message,
                    "Switch unsuccessful",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _switching = false;
            }


        }

        private void EmbedChild(Form child)
        {
            _currentChild = child;
            child.TopLevel = false;
            child.FormBorderStyle = FormBorderStyle.None;
            child.Dock = DockStyle.Fill;

            _hostPanel.SuspendLayout();
            _hostPanel.Controls.Clear();
            _hostPanel.Controls.Add(child);
            _hostPanel.ResumeLayout();

            child.Show();
        }

        private void SafeCloseChild()
        {
            if (_currentChild == null) return;

            try
            {
                // 優先呼叫 Close 讓原表單有機會釋放資源（例如 SerialPort）
                _currentChild.Close();
                _currentChild.Dispose();
            }
            catch
            {
                // 忽略例外，確保能切換
            }
            finally
            {
                _currentChild = null;
                _hostPanel.Controls.Clear();
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            // 關閉應用前，確實釋放內嵌視圖資源
            SafeCloseChild();
        }
    }
}
