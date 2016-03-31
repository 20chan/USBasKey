using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Management;

namespace KeyMaker
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Evented();
        }

        public void MakeCryptFile(string directory)
        {
            try
            {
                StreamReader sr = new StreamReader("password.dat");
                string text = sr.ReadToEnd();
                sr.Dispose();

                StreamWriter sw = new StreamWriter("password.dat");

                USBSerialNumber usb = new USBSerialNumber();
                string serial = usb.getSerialNumberFromDriveLetter(directory.Substring(0, 2));

                sw.Write(text + Environment.NewLine + serial);
                sw.Flush();
                sw.Dispose();
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        public List<DriveInfo> USBDrives()
        {
            List<DriveInfo> usbs = new List<DriveInfo>();
            foreach (string device in System.IO.Directory.GetLogicalDrives())
            {
                System.IO.DriveInfo dr = new System.IO.DriveInfo(device);
                if (dr.DriveType != System.IO.DriveType.Removable) continue;
                usbs.Add(dr);
            }
            return usbs;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(this.listView1.SelectedItems.Count == 0)
            {
                MessageBox.Show("선택하신 드라이브가 없습니다!");
                return;
            }
            MakeCryptFile(this.listView1.SelectedItems[0].SubItems[1].Text);
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
                    System.Threading.Thread t = new System.Threading.Thread(Evented);
                    t.Start();
                }
            }

            if ((m.Msg == WM_DEVICECHANGE) && (m.WParam.ToInt32() == DBT_DEVICEREMOVECOMPLETE))  //디바이스 연결 해제
            {
                int devType = Marshal.ReadInt32(m.LParam, 4);
                if (devType == DBT_DEVTUP_VOLUME)
                {
                    System.Threading.Thread t = new System.Threading.Thread(Evented);
                    t.Start();
                }
            }

            base.WndProc(ref m);
        }

        private void Evented()
        {
            List<DriveInfo> Usbs = USBDrives();
            foreach(DriveInfo d in Usbs)
            {
                ListViewItem i = new ListViewItem(d.VolumeLabel);
                i.SubItems.Add(d.RootDirectory.ToString());
                this.listView1.Items.Add(i);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Evented();
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
