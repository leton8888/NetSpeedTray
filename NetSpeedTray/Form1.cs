using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private NotifyIcon trayIcon = null!;
        private System.Windows.Forms.Timer updateTimer = null!;
        private NetworkInterface[] interfaces = null!;
        private long lastBytesReceived = 0;
        private long lastBytesSent = 0;
        private DateTime lastCheckTime;

        public Form1()
        {
            InitializeComponent();
            InitializeTrayIcon();
            InitializeNetworkMonitoring();

            // 窗体启动时最小化到托盘
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
        }

        private void InitializeTrayIcon()
        {

            trayIcon = new NotifyIcon();
            trayIcon.Icon = SystemIcons.Application; // 可以替换成自己的图标
            trayIcon.Visible = true;

            // 创建右键菜单
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("显示", null, ShowForm);
            menu.Items.Add("退出", null, ExitApplication);
            trayIcon.ContextMenuStrip = menu;

            // 双击托盘图标显示窗体
            trayIcon.DoubleClick += (s, e) => ShowForm(s, e);
        }

        private void InitializeNetworkMonitoring()
        {
            // 获取所有网络接口
            interfaces = NetworkInterface.GetAllNetworkInterfaces();
            lastCheckTime = DateTime.Now;

            // 初始化定时器，每秒更新一次
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 1000;
            updateTimer.Tick += UpdateNetworkSpeed;
            updateTimer.Start();
        }

        private void UpdateNetworkSpeed(object? sender, EventArgs e)
        {
            long currentBytesReceived = 0;
            long currentBytesSent = 0;

            foreach (NetworkInterface ni in interfaces)
            {
                if (ni.OperationalStatus == OperationalStatus.Up)
                {
                    IPv4InterfaceStatistics stats = ni.GetIPv4Statistics();
                    currentBytesReceived += stats.BytesReceived;
                    currentBytesSent += stats.BytesSent;
                }
            }

            TimeSpan timeDiff = DateTime.Now - lastCheckTime;
            double seconds = timeDiff.TotalSeconds;

            // 添加更严格的除零保护
            if (seconds >= 0.001) // 确保至少有 1 毫秒的时间差
            {
                // 使用 double 进行计算，避免提前转换为 long
                double bytesReceivedPerSec = (currentBytesReceived - lastBytesReceived) / seconds;
                double bytesSentPerSec = (currentBytesSent - lastBytesSent) / seconds;

                // 更新托盘图标提示文本
                string downloadSpeed = FormatSpeed(bytesReceivedPerSec);
                string uploadSpeed = FormatSpeed(bytesSentPerSec);
                trayIcon.Text = $"↓ {downloadSpeed}\n↑ {uploadSpeed}";
            }

            // 更新上次的值
            lastBytesReceived = currentBytesReceived;
            lastBytesSent = currentBytesSent;
            lastCheckTime = DateTime.Now;
        }

        private string FormatSpeed(double bytesPerSec)
        {
            // 转换为 bits per second (1 byte = 8 bits)
            double bitsPerSec = bytesPerSec * 8;

            // 定义单位转换阈值
            const double GB = 1000 * 1000 * 1000;
            const double MB = 1000 * 1000;
            const double KB = 1000;

            string formattedSpeed;
            if (bitsPerSec >= GB)
            {
                formattedSpeed = $"{(bitsPerSec / GB):F2} Gbps";
            }
            else if (bitsPerSec >= MB)
            {
                formattedSpeed = $"{(bitsPerSec / MB):F2} Mbps";
            }
            else if (bitsPerSec >= KB)
            {
                formattedSpeed = $"{(bitsPerSec / KB):F2} Kbps";
            }
            else
            {
                formattedSpeed = $"{bitsPerSec:F0} bps";
            }

            return formattedSpeed;
        }

        private void ShowForm(object? sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }

        private void ExitApplication(object? sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
            }
            base.OnFormClosing(e);
        }
    }
}
