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
using System.IO;

namespace USBasKey
{
    public partial class Form1 : Form
    {
        string key = "";
        public Form1()
        {
            InitializeComponent();

            foreach(string s in File.ReadAllLines(Environment.CurrentDirectory + "\\Settings.txt"))
            {
                if (s.StartsWith("\\")) continue;
                key += s;
            }
            
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

        void Evented()
        {
            foreach(string device in System.IO.Directory.GetLogicalDrives())
            {
                System.IO.DriveInfo dr = new System.IO.DriveInfo(device);
                if (dr.DriveType != System.IO.DriveType.Removable) continue;
                if (Check(device))
                    Application.Exit();
            }
        }

        bool Check(string deviceDirect)
        {
            string fullname = deviceDirect + "Crypt.dll";
            if (!System.IO.File.Exists(fullname))
                return false;
            FileInfo f = new FileInfo(fullname);
            if (f.Attributes != FileAttributes.Hidden)
                return false;


            foreach(string s in File.ReadAllLines(deviceDirect))
            {
                if (s.StartsWith(key)) return true;
            }
            return false;
        }
    }
}
