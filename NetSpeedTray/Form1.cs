using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private NotifyIcon notifyIcon1;
        private System.Windows.Forms.Timer timer1;
        private NetworkInterface[] interfaces;
        private Dictionary<string, (long BytesSent, long BytesReceived)> lastValues = new();
        private const string StartupRegKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "NetSpeedTray";

        public Form1()
        {
            InitializeComponent();
            
            // 初始化时就隐藏窗口
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Hide();
            
            // 初始化 NotifyIcon
            notifyIcon1 = new NotifyIcon();
            notifyIcon1.Visible = true;
            
            // 使用嵌入的资源加载图标
            using (var stream = GetType().Assembly.GetManifestResourceStream("NetSpeedTray.Resources.icon.ico"))
            {
                if (stream != null)
                {
                    var icon = new Icon(stream);
                    this.Icon = icon;
                    notifyIcon1.Icon = icon;
                }
            }

            // 初始化网络接口
            interfaces = NetworkInterface.GetAllNetworkInterfaces();

            // 设置定时器
            timer1 = new System.Windows.Forms.Timer();
            timer1.Interval = 1000; // 1秒更新一次
            timer1.Tick += Timer1_Tick;
            timer1.Start();

            // 设置托盘菜单
            notifyIcon1.ContextMenuStrip = new ContextMenuStrip();
            var startupItem = new ToolStripMenuItem("开机启动", null, Startup_Click);
            startupItem.Checked = IsStartupEnabled(); // 检查是否已设置开机启动
            notifyIcon1.ContextMenuStrip.Items.Add(startupItem);
            notifyIcon1.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            notifyIcon1.ContextMenuStrip.Items.Add("退出", null, Exit_Click);
        }

        private bool IsStartupEnabled()
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(StartupRegKey);
            return key?.GetValue(AppName) != null;
        }

        private void Startup_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(StartupRegKey, true);
                if (key != null)
                {
                    if (menuItem.Checked)
                    {
                        // 取消开机启动
                        key.DeleteValue(AppName, false);
                        menuItem.Checked = false;
                    }
                    else
                    {
                        // 设置开机启动
                        string exePath = Application.ExecutablePath;
                        key.SetValue(AppName, exePath);
                        menuItem.Checked = true;
                    }
                }
            }
        }

        private void Timer1_Tick(object? sender, EventArgs e)
        {
            long totalBytesReceived = 0;
            long totalBytesSent = 0;

            foreach (NetworkInterface ni in interfaces)
            {
                if (ni.OperationalStatus == OperationalStatus.Up)
                {
                    var stats = ni.GetIPv4Statistics();
                    
                    if (!lastValues.ContainsKey(ni.Id))
                    {
                        lastValues[ni.Id] = (stats.BytesSent, stats.BytesReceived);
                        continue;
                    }

                    var last = lastValues[ni.Id];
                    var bytesSentSpeed = stats.BytesSent - last.BytesSent;
                    var bytesReceivedSpeed = stats.BytesReceived - last.BytesReceived;

                    totalBytesSent += bytesSentSpeed;
                    totalBytesReceived += bytesReceivedSpeed;

                    lastValues[ni.Id] = (stats.BytesSent, stats.BytesReceived);
                }
            }

            string upSpeed = FormatSpeed(totalBytesSent);
            string downSpeed = FormatSpeed(totalBytesReceived);
            notifyIcon1.Text = $"↑{upSpeed}/s\n↓{downSpeed}/s";
        }

        private string FormatSpeed(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB" };
            int unitIndex = 0;
            double speed = bytes;

            while (speed >= 1024 && unitIndex < units.Length - 1)
            {
                speed /= 1024;
                unitIndex++;
            }

            return $"{speed:F1}{units[unitIndex]}";
        }

        private void Exit_Click(object? sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            base.OnFormClosing(e);
        }

        protected override void SetVisibleCore(bool value)
        {
            // 重写此方法确保窗口初始不可见
            if (!this.IsHandleCreated)
            {
                value = false;
                CreateHandle();
            }
            base.SetVisibleCore(value);
        }
    }
}
