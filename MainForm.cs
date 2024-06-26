using System.ComponentModel;
using System.Runtime.InteropServices;

namespace GC_Clicker
{
    public partial class MainForm : Form
    {
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;

        private const int HOTKEY_ID = 1;
        private const uint MOD_NONE = 0x0000;
        private const uint VK_F9 = 0x78; // F9 key for start/stop

        private bool checkBlueStacksBeforeStart = true;
        private bool isClicking = false;
        private bool stopAtTime = false;
        private bool randomDelay = false;
        private bool randomPositions = false;
        private bool temporaryPause = false;
        private DateTime endTime;
        private int delay, earnPerTap, energyLimit, elapsedPauseTime, x = 1120, y = 500;
        private int pauseAfter, resumeAfter, clickTimer;
        private Random random;

        private BackgroundWorker worker;

        public MainForm()
        {
            InitializeComponent();
            ReadSettings();
            RegisterHotKey(this.Handle, HOTKEY_ID, MOD_NONE, VK_F9);
            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            random = new Random();
        }

        private void Worker_DoWork(object? sender, DoWorkEventArgs e)
        {
            DateTime lastPauseTime = DateTime.Now;
            elapsedPauseTime = 0;
            isClicking = true;

            while (!worker.CancellationPending)
            {
                if (checkBlueStacksBeforeStart && !BlueStacksController.BringBlueStacksToFront())
                {
                    MessageBox.Show("'BlueStacks App Player' is not running!");
                    break;
                }

                if (stopAtTime && DateTime.Now >= endTime)
                    break;
                int clickCount = 0;
                while (!worker.CancellationPending)
                {
                    int rX = random.Next(x - 20, x + 20);
                    int rY = random.Next(y - 10, y + 10);
                    if (randomPositions)
                        PerformClick(rX, rY);
                    else
                        PerformClick(x, y);

                    var delayVal = delay;
                    if (randomDelay)
                        delayVal = random.Next(1, delay);
                    Thread.Sleep(delayVal);

                    clickCount++;

                    if (temporaryPause)
                    {
                        elapsedPauseTime += delayVal;
                        worker.ReportProgress((int)(elapsedPauseTime * 100 / (double)pauseAfter));

                        if (elapsedPauseTime >= pauseAfter)
                        {
                            elapsedPauseTime = 0;
                            break;
                        }
                    }
                }
                int tickCount = 0;
                worker.ReportProgress(0);
                while (!worker.CancellationPending)
                {
                    Thread.Sleep(1000);
                    tickCount++;
                    worker.ReportProgress((int)(tickCount * 1000 * 100 / (double)resumeAfter));
                    if (tickCount * 1000 >= resumeAfter)
                        break;
                }
            }
        }

        private void Worker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            if (!pbMain.Visible)
            {
                pbMain.Visible = true;
                TaskbarProgress.SetState(this.Handle, TaskbarProgress.TaskbarStates.Indeterminate);
            }
            if (e.ProgressPercentage >= 0 && e.ProgressPercentage <= pbMain.Maximum)
            {
                pbMain.Value = e.ProgressPercentage;
                TaskbarProgress.SetValue(this.Handle, e.ProgressPercentage, 100);
            }
        }

        private void Worker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            isClicking = false;
            btnStartStop.Text = "Start";
            gbClick.Enabled = true;
            gbTimer.Enabled = true;
            gbTempPause.Enabled = true;
            pbMain.Visible = false;
            TaskbarProgress.SetState(this.Handle, TaskbarProgress.TaskbarStates.Normal);
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            if (!isClicking)
            {
                SaveSettings();
                if (checkBlueStacksBeforeStart && !BlueStacksController.BringBlueStacksToFront())
                {
                    MessageBox.Show("'BlueStacks App Player' is not running!");
                    return;
                }
                isClicking = true;
                gbClick.Enabled = false;
                gbTimer.Enabled = false;
                gbTempPause.Enabled = false;
                btnStartStop.Text = "Stop";
                worker.RunWorkerAsync();
            }
            else
            {
                worker.CancelAsync();
            }
        }

        private void PerformClick(int x, int y)
        {
            SetCursorPos(x, y);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_ID)
                {
                    if (isClicking)
                    {
                        StartButton_Click(btnStartStop, new EventArgs());
                    }
                    else
                    {
                        StartButton_Click(btnStartStop, new EventArgs());
                    }
                }
            }
            base.WndProc(ref m);
        }

        private void ReadFromUI()
        {
            randomDelay = chRandomDelay.Checked;
            int.TryParse(txtDelay.Text, out delay);
            randomPositions = chRandomPosition.Checked;
            stopAtTime = chStopAtTime.Checked;
            checkBlueStacksBeforeStart = chCheckBlueStacks.Checked;
            endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, endTimePicker.Value.Hour, endTimePicker.Value.Minute, endTimePicker.Value.Second);
            temporaryPause = chTempPause.Checked;
            int.TryParse(txtEanPerTap.Text, out earnPerTap);
            int.TryParse(txtEnergyLimit.Text, out energyLimit);
            int.TryParse(txtPauseAfter.Text, out pauseAfter);
            int.TryParse(txtResumeAfter.Text, out resumeAfter);
            int.TryParse(txtX.Text, out x);
            int.TryParse(txtY.Text, out y);
        }

        private void UpdateUI()
        {
            chRandomDelay.Checked = randomDelay;
            txtDelay.Text = delay.ToString();
            chRandomPosition.Checked = randomPositions;
            chStopAtTime.Checked = stopAtTime;
            endTimePicker.Value = endTime;
            chTempPause.Checked = temporaryPause;
            txtEanPerTap.Text = earnPerTap.ToString();
            txtEnergyLimit.Text = energyLimit.ToString();
            txtPauseAfter.Text = pauseAfter.ToString();
            txtResumeAfter.Text = resumeAfter.ToString();
            txtX.Text = x.ToString();
            txtY.Text = y.ToString();
            chCheckBlueStacks.Checked = checkBlueStacksBeforeStart;
        }

        private void ReadSettings()
        {
            randomDelay = Properties.Settings.Default.RandomDelay;
            delay = Properties.Settings.Default.ClickDelay;
            randomPositions = Properties.Settings.Default.RandomPosition;
            x = Properties.Settings.Default.X;
            y = Properties.Settings.Default.Y;
            stopAtTime = Properties.Settings.Default.StopAtTime;
            endTime = Properties.Settings.Default.EndTime;
            temporaryPause = Properties.Settings.Default.TemporaryPause;
            earnPerTap = Properties.Settings.Default.EarnPerTap;
            energyLimit = Properties.Settings.Default.EnergyLimit;
            pauseAfter = Properties.Settings.Default.PauseAfter;
            resumeAfter = Properties.Settings.Default.ResumeAfter;
            checkBlueStacksBeforeStart = Properties.Settings.Default.CheckBlueStackAppPlayer;

            UpdateUI();
        }

        private void SaveSettings()
        {
            ReadFromUI();
            Properties.Settings.Default.RandomDelay = randomDelay;
            Properties.Settings.Default.ClickDelay = delay;
            Properties.Settings.Default.RandomPosition = randomPositions;
            Properties.Settings.Default.X = x;
            Properties.Settings.Default.Y = y;
            Properties.Settings.Default.StopAtTime = stopAtTime;
            Properties.Settings.Default.EndTime = endTime;
            Properties.Settings.Default.TemporaryPause = temporaryPause;
            Properties.Settings.Default.EarnPerTap = earnPerTap;
            Properties.Settings.Default.EnergyLimit = energyLimit;
            Properties.Settings.Default.PauseAfter = pauseAfter;
            Properties.Settings.Default.ResumeAfter = resumeAfter;
            Properties.Settings.Default.CheckBlueStackAppPlayer = checkBlueStacksBeforeStart;
            Properties.Settings.Default.Save();
        }

        private void CalculateTiming()
        {
            int el = 1000; //Energy Limit
            int.TryParse(txtEnergyLimit.Text, out el);
            el = el < 1000 ? 1000 : el;
            int ept = 1; //Earn Per Tap
            int.TryParse(txtEanPerTap.Text, out ept);
            ept = ept < 1 ? 1 : ept;
            int clickDelay = 500;
            int.TryParse(txtDelay.Text, out clickDelay);
            clickDelay = clickDelay < 10 ? 10 : clickDelay;
            int delayDiv = chRandomDelay.Checked ? 2 : 1;
            clickDelay = chRandomDelay.Checked ? (int)Math.Round(clickDelay * (3 / 4.0f)) : clickDelay;
            txtResumeAfter.Text = (el / 3 * 1000).ToString();
            int clickDuration = el / ept * clickDelay;
            txtPauseAfter.Text = (clickDuration + (clickDuration / 10)).ToString();
        }

        private void txtEanPerTap_TextChanged(object sender, EventArgs e)
        {
            CalculateTiming();
        }

        private void txtEnergyLimit_TextChanged(object sender, EventArgs e)
        {
            CalculateTiming();
        }

        private void txtDelay_TextChanged(object sender, EventArgs e)
        {
            CalculateTiming();
        }

        private void chRandomDelay_CheckedChanged(object sender, EventArgs e)
        {
            CalculateTiming();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isClicking)
                worker.CancelAsync();
            UnregisterHotKey(this.Handle, HOTKEY_ID);
            SaveSettings();
        }
    }
}
