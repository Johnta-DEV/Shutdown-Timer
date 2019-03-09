using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Shutdown_Timer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }
        
        //импортируем API функцию InitiateSystemShutdown
        [DllImport("advapi32.dll", EntryPoint = "InitiateSystemShutdownEx")]
        static extern int InitiateSystemShutdown(string lpMachineName, string lpMessage, int dwTimeout, bool bForceAppsClosed, bool bRebootAfterShutdown);
        //импортируем API функцию AdjustTokenPrivileges
        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool AdjustTokenPrivileges(IntPtr htok, bool disall,
        ref TokPriv1Luid newst, int len, IntPtr prev, IntPtr relen);
        //импортируем API функцию GetCurrentProcess
        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GetCurrentProcess();
        //импортируем API функцию OpenProcessToken
        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool OpenProcessToken(IntPtr h, int acc, ref IntPtr phtok);
        //импортируем API функцию LookupPrivilegeValue
        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool LookupPrivilegeValue(string host, string name, ref long pluid);
        //импортируем API функцию LockWorkStation
        [DllImport("user32.dll", EntryPoint = "LockWorkStation")]
        static extern bool LockWorkStation();
        //объявляем структуру TokPriv1Luid для работы с привилегиями
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct TokPriv1Luid
        {
            public int Count;
            public long Luid;
            public int Attr;
        }
        //объявляем необходимые, для API функций, константые значения, согласно MSDN
        internal const int SE_PRIVILEGE_ENABLED = 0x00000002;
        internal const int TOKEN_QUERY = 0x00000008;
        internal const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;
        internal const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
        //функция SetPriv для повышения привилегий процесса
        private void SetPriv()
        {
            TokPriv1Luid tkp; //экземпляр структуры TokPriv1Luid 
            IntPtr htok = IntPtr.Zero;
            //открываем "интерфейс" доступа для своего процесса
            if (OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref htok))
            {
                //заполняем поля структуры
                tkp.Count = 1;
                tkp.Attr = SE_PRIVILEGE_ENABLED;
                tkp.Luid = 0;
                //получаем системный идентификатор необходимой нам привилегии
                LookupPrivilegeValue(null, SE_SHUTDOWN_NAME, ref tkp.Luid);
                //повышем привилигеию своему процессу
                AdjustTokenPrivileges(htok, false, ref tkp, 0, IntPtr.Zero, IntPtr.Zero);
            }
        }
        //публичный метод для перезагрузки/выключения машины
        public int ShutDown(bool RebootAfter, bool ForceShutdown)
        {
            SetPriv(); //получаем привилегия
            //вызываем функцию InitiateSystemShutdown, передавая ей необходимые параметры
            return InitiateSystemShutdown(null, null, 0, ForceShutdown, RebootAfter);
        }

        InfoForm infoForm = new InfoForm();
        System.Timers.Timer timer = new System.Timers.Timer();
        ShutdownType type = ShutdownType.Shutdown;
        double timeToTurnOffInSec = 0;
        bool isTimerRunning = false;
        int hours = 0;
        int mins = 0;
        int sec = 0;


        private void MainForm_Load(object sender, EventArgs e)
        {
            timer.AutoReset = true;
            timer.Elapsed += TimerTick;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (isTimerRunning)
            {
                if (type == ShutdownType.Hybernate)
                {
                    type = ShutdownType.None;
                    StopTimer();
                    SleepButton.BackColor = this.BackColor;
                }
                else
                {
                    type = ShutdownType.Hybernate;
                    shutdownButton.BackColor = this.BackColor;
                    ReloadButton.BackColor = this.BackColor;
                    SleepButton.BackColor = SleepButton.FlatAppearance.MouseOverBackColor;
                }
            }
            else
            {
                type = ShutdownType.Hybernate;
                SleepButton.BackColor = SleepButton.FlatAppearance.MouseOverBackColor;
                StartTimer();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (isTimerRunning)
            {
                if (type == ShutdownType.Reload)
                {
                    type = ShutdownType.None;
                    StopTimer();
                    ReloadButton.BackColor = this.BackColor;
                }
                else
                {
                    type = ShutdownType.Reload;
                    shutdownButton.BackColor = this.BackColor;
                    SleepButton.BackColor = this.BackColor;
                    ReloadButton.BackColor = ReloadButton.FlatAppearance.MouseOverBackColor;
                }
            }
            else
            {
                type = ShutdownType.Reload;
                ReloadButton.BackColor = ReloadButton.FlatAppearance.MouseOverBackColor;
                StartTimer();
            }
        }
        private void shutdownButton_Click(object sender, EventArgs e)
        {
            if (isTimerRunning)
            {
                if (type == ShutdownType.Shutdown)
                {
                    type = ShutdownType.None;
                    StopTimer();
                    shutdownButton.BackColor = this.BackColor;
                }
                else
                {
                    type = ShutdownType.Shutdown;
                    SleepButton.BackColor = this.BackColor;
                    ReloadButton.BackColor = this.BackColor;
                    shutdownButton.BackColor = shutdownButton.FlatAppearance.MouseOverBackColor;
                }
            }
            else
            {
                type = ShutdownType.Shutdown;
                shutdownButton.BackColor = shutdownButton.FlatAppearance.MouseOverBackColor;
                StartTimer();
            }
        }
        
        private void richTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (isTimerRunning == false)
            {
                if (e.KeyChar <= 47 || e.KeyChar >= 58)
                {
                    e.Handled = true;
                    return;
                }
                int i = richTextBox1.SelectionStart;
                if (i >= richTextBox1.MaxLength) i = 0;
                if (richTextBox1.Text != "")
                {
                    string txt = richTextBox1.Text;
                    char[] arr = txt.ToCharArray();
                    if (i == 0)
                    {
                        if (e.KeyChar > 46 && e.KeyChar < 51)
                        {
                            arr[i] = e.KeyChar;
                        }
                    }
                    else
                        arr[i] = e.KeyChar;
                    richTextBox1.Text = new string(arr);
                }
                i++;
                richTextBox1.SelectionStart = i;
                if (richTextBox1.SelectionStart >= richTextBox1.MaxLength)
                {
                    ActiveControl = richTextBox2;
                    richTextBox2.SelectionStart = 0;
                }
            }
        }

        private void richTextBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (isTimerRunning == false)
            {
                if (e.KeyChar <= 47 || e.KeyChar >= 58)
                {
                    e.Handled = true;
                    return;
                }
                int i = richTextBox2.SelectionStart;
                if (i >= richTextBox2.MaxLength) i = 0;
                if (richTextBox2.Text != "")
                {
                    string txt = richTextBox2.Text;
                    char[] arr = txt.ToCharArray();
                    if (i == 0)
                    {
                        if (e.KeyChar > 46 && e.KeyChar < 54)
                        {
                            arr[i] = e.KeyChar;
                        }
                    }
                    else
                        arr[i] = e.KeyChar;
                    richTextBox2.Text = new string(arr);
                }
                i++;
                richTextBox2.SelectionStart = i;
                if (richTextBox2.SelectionStart >= richTextBox2.MaxLength)
                {
                    ActiveControl = richTextBox3;
                    richTextBox3.SelectionStart = 0;
                }
            }
        }

        private void richTextBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (isTimerRunning == false)
            {
                if (e.KeyChar <= 47 || e.KeyChar >= 58)
                {
                    e.Handled = true;
                    return;
                }
                int i = richTextBox3.SelectionStart;
                if (i >= richTextBox3.MaxLength) i = 0;
                if (richTextBox3.Text != "")
                {
                    string txt = richTextBox3.Text;
                    char[] arr = txt.ToCharArray();
                    if (i == 0)
                    {
                        if (e.KeyChar > 46 && e.KeyChar < 54)
                        {
                            arr[i] = e.KeyChar;
                        }
                    }
                    else
                        arr[i] = e.KeyChar;
                    richTextBox3.Text = new string(arr);
                }
                i++;
                richTextBox3.SelectionStart = i;
                if (richTextBox3.SelectionStart >= richTextBox3.MaxLength)
                {
                    ActiveControl = richTextBox1;
                    richTextBox1.SelectionStart = 0;
                }
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            int text = 0;
            try
            {
                text = Convert.ToInt32(richTextBox1.Text);
                if (text > 23) text = 23;
                if (text < 0) text = 0;
                hours = text;
                string result = "";
                if (text < 10) result += "0";
                result += text.ToString();
                richTextBox1.Text = result;
            }
            catch
            {
                richTextBox1.Text = hours.ToString();
            }
        }

        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {
            int text = 0;
            try
            {
                text = Convert.ToInt32(richTextBox2.Text);
                if (text > 59) text = 59;
                if (text < 0) text = 0;
                mins = text;
                string result = "";
                if (text < 10) result += "0";
                result += text.ToString();
                richTextBox2.Text = result;
            }
            catch
            {
                richTextBox2.Text = mins.ToString();
            }
        }

        private void richTextBox3_TextChanged(object sender, EventArgs e)
        {
            int text = 0;
            try
            {
                text = Convert.ToInt32(richTextBox3.Text);
                if (text > 59) text = 59;
                if (text < 0) text = 0;
                sec = text;
                string result = "";
                if (text < 10) result += "0";
                result += text.ToString();
                richTextBox3.Text = result;
            }
            catch
            {
                richTextBox3.Text = sec.ToString();
            }
        }

        //Сервисные методы

        private void StartTimer()
        {
            if (isTimerRunning == false)
            {
                isTimerRunning = true;
                LockUI(true);
                timeToTurnOffInSec = sec + (mins * 60) + (hours * 3600);
                timer.Interval = 1000;
                timer.Start();
            }
        }
        private void StopTimer()
        {
            if (isTimerRunning)
            {
                isTimerRunning = false;
                LockUI(false);
                this.Text = "Таймер выключения";
                timer.Stop();
            }
        }
        private void TimerTick(object source, EventArgs e)
        {
            BeginInvoke(new Action(() => DecreaseTime()));
            BeginInvoke(new Action(() => UpdateUI()));
        }
        private void OnTimerEnd()
        {
            timer.Stop();
            isTimerRunning = false;
            LockUI(false);
            ReloadButton.BackColor = this.BackColor;
            SleepButton.BackColor = this.BackColor;
            shutdownButton.BackColor = this.BackColor;
            infoForm.Hide();

            switch (type)
            {
                case ShutdownType.None:
                    break;
                case ShutdownType.Shutdown:
                    ShutDown(false, true);
                    break;
                case ShutdownType.Reload:
                    ShutDown(true, true);
                    break;
                case ShutdownType.Hybernate:
                    Application.SetSuspendState(PowerState.Hibernate, true, false);
                    break;
                default:
                    break;
            }
            //Application.Exit();
        }
        private void DecreaseTime()
        {
            sec--;
            if (hours <= 0 && mins <= 0 && sec <= 0)
            {
                hours = 0;
                mins = 0;
                sec = 0;
                OnTimerEnd();
                return;
            }
            if (sec < 1)
            {
                sec = 59;
                mins--;
                if (mins < 1 && hours > 0)
                {
                    mins = 59;
                    hours--;
                }
            }
        }
        private void UpdateUI()
        {
            richTextBox1.Text = hours.ToString();
            richTextBox2.Text = mins.ToString();
            richTextBox3.Text = sec.ToString();
            this.Text = richTextBox1.Text + ":" + richTextBox2.Text + ":" + richTextBox3.Text + " Таймер выключения";
        }
        private void LockUI(bool LockState)
        {
            if (LockState)
            {
                richTextBox1.ReadOnly = true;
                richTextBox2.ReadOnly = true;
                richTextBox3.ReadOnly = true;
            }
            else
            {
                richTextBox1.ReadOnly = false;
                richTextBox2.ReadOnly = false;
                richTextBox3.ReadOnly = false;
            }
        }

        private void InfoButton_Click(object sender, EventArgs e)
        {
            if (infoForm.Visible == false)
                infoForm.Show();
        }

        private void MainForm_Click(object sender, EventArgs e)
        {
            if (infoForm.Visible == true)
                infoForm.Hide();
        }
    }

    public enum ShutdownType
    {
        None, Shutdown, Reload, Hybernate
    }
}
