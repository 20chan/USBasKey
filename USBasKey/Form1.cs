using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.IO;
using System.Management;
using System.Management.Instrumentation;

namespace USBasKey
{
    public partial class Form1 : Form
    {
        Size fullScreen;
        public static string pin = "1234";

        [DllImport("user32.dll")]
        private static extern int SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(int hhk);
        [DllImport("user32.dll")]
        private static extern int CallNextHookEx(int hhk, int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        public struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        private const int WM_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private delegate int LowLevelKeyboardProc(int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam);
        private static int _hookID = 0;
        private static LowLevelKeyboardProc _proc = HookCallback;

        /// <summary>
        /// 여기에 자신의 USB 시리얼 넘버를..
        /// </summary>
        string key = "여기가 USB 시리얼 넘버... 아직 귀찮아서 가져오는건 안만들었음";

        public static void Exit()
        {
            SetTaskManager(true);
            UnhookWindowsHookEx(_hookID);
            Application.Exit();
        }
        public Form1()
        {
            InitializeComponent();

            
            /*
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookID = SetWindowsHookEx(WM_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
            }
            SetTaskManager(false);
            FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.Location = new Point(0, 0);
            this.Size = new Size(10000, 10000);
            */
            if (!File.Exists(Environment.CurrentDirectory + "\\Crypt.dll"))
            {
                StreamWriter sr = new StreamWriter(Environment.CurrentDirectory + "\\Crypt.dll");
                sr.Write(key);
                sr.Flush();
                sr.Dispose();
            }
        }

        ~Form1()
        {
            SetTaskManager(true);
            UnhookWindowsHookEx(_hookID);
        }
        private static int HookCallback(int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam)
        {
            if (nCode >= 0)
            {
                if (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN || wParam == WM_KEYUP || wParam == WM_SYSKEYUP)
                {
                    return 1;
                }
            }
            return CallNextHookEx(0, nCode, wParam, ref lParam);
        }
        protected override void WndProc(ref Message m)
        {
            UInt32 WM_DEVICECHANGE = 0x0219;
            UInt32 DBT_DEVTUP_VOLUME = 0x02;
            UInt32 DBT_DEVICEARRIVAL = 0x8000;
            UInt32 DBT_DEVICEREMOVECOMPLETE = 0x8004;

            if ((m.Msg == WM_DEVICECHANGE) && (m.WParam.ToInt32() == DBT_DEVICEARRIVAL))//디바이스 연결
            {
                //int m_Count = 0;
                int devType = Marshal.ReadInt32(m.LParam, 4);

                if (devType == DBT_DEVTUP_VOLUME)
                {
                    Evented();
                }
            }

            if ((m.Msg == WM_DEVICECHANGE) && (m.WParam.ToInt32() == DBT_DEVICEREMOVECOMPLETE))  //디바이스 연결 해제
            {
                int devType = Marshal.ReadInt32(m.LParam, 4);
                if (devType == DBT_DEVTUP_VOLUME)
                {
                    Evented();
                }
            }

            base.WndProc(ref m);
        }

        public static void SetTaskManager(bool enable)
        {
            RegistryKey objRegistryKey = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Policies\System");
            if (enable && objRegistryKey.GetValue("DisableTaskMgr") != null)
                objRegistryKey.DeleteValue("DisableTaskMgr");
            else
                objRegistryKey.SetValue("DisableTaskMgr", "1");
            objRegistryKey.Close();
        }

        void Evented()
        {
            foreach (string device in System.IO.Directory.GetLogicalDrives())
            {
                System.IO.DriveInfo dr = new System.IO.DriveInfo(device);
                if (dr.DriveType != System.IO.DriveType.Removable) continue;
                if (Check(device))
                    Application.Exit();
            }
        }

        bool Check(string deviceDirect)
        {
            /*
            string fullname = deviceDirect + "Crypt.dll";
            if (!System.IO.File.Exists(fullname))
                return false;
            FileInfo f = new FileInfo(fullname);
            if ((f.Attributes | FileAttributes.Hidden) != f.Attributes)
                return false;

            //File.SetAttributes(deviceDirect, FileAttributes.Normal);
            foreach (string s in File.ReadAllLines(fullname))
            {
                if (s.StartsWith(key)) return true;
            }
            return false;
            */
            
            USBSerialNumber usb = new USBSerialNumber();
            string serial = usb.getSerialNumberFromDriveLetter(deviceDirect.Substring(0, 2));
            if (serial == key)
                return true;
            return false;
        }

    private void Form1_Load(object sender, EventArgs e)
        {
            Evented();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.label2.Text = string.Format("현재 시간 : {0}", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss tt"));
        }

        private void label1_DoubleClick(object sender, EventArgs e)
        {
            Pin p = new Pin();
            p.ShowDialog();
        }
    }

    class USBSerialNumber
    {
        string _serialNumber;
        string _driveLetter;

        public string getSerialNumberFromDriveLetter(string driveLetter)
        {
            this._driveLetter = driveLetter.ToUpper();

            if (!this._driveLetter.Contains(":"))
            {
                this._driveLetter += ":";
            }

            matchDriveLetterWithSerial();


            return this._serialNumber;
        }

        private void matchDriveLetterWithSerial()
        {

            string[] diskArray;
            string driveNumber;
            string driveLetter;

            ManagementObjectSearcher searcher1 = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDiskToPartition");

            
            foreach (ManagementObject dm in searcher1.Get())  // 여기서 에러가 계속 발생
            {
                diskArray = null;
                driveLetter = getValueInQuotes(dm["Dependent"].ToString());
                diskArray = getValueInQuotes(dm["Antecedent"].ToString()).Split(',');
                driveNumber = diskArray[0].Remove(0, 6).Trim();
                if (driveLetter == this._driveLetter)
                {
                    /* This is where we get the drive serial */
                    ManagementObjectSearcher disks = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
                    foreach (ManagementObject disk in disks.Get())
                    {

                        if (disk["Name"].ToString() == ("\\\\.\\PHYSICALDRIVE" + driveNumber) & disk["InterfaceType"].ToString() == "USB")
                        {
                            this._serialNumber = parseSerialFromDeviceID(disk["PNPDeviceID"].ToString());
                        }
                    }
                }
            }
        }

        private string parseSerialFromDeviceID(string deviceId)
        {
            string[] splitDeviceId = deviceId.Split('\\');
            string[] serialArray;
            string serial;
            int arrayLen = splitDeviceId.Length - 1;

            serialArray = splitDeviceId[arrayLen].Split('&');
            serial = serialArray[0];

            return serial;
        }

        private string getValueInQuotes(string inValue)
        {
            string parsedValue = "";

            int posFoundStart = 0;
            int posFoundEnd = 0;

            posFoundStart = inValue.IndexOf("\"");
            posFoundEnd = inValue.IndexOf("\"", posFoundStart + 1);

            parsedValue = inValue.Substring(posFoundStart + 1, (posFoundEnd - posFoundStart) - 1);

            return parsedValue;
        }
    }
}