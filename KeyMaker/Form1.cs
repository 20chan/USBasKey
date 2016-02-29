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
                string key = "";
                foreach (string s in File.ReadAllLines(Environment.CurrentDirectory + "\\Settings.txt"))
                {
                    if (s.StartsWith("//")) continue;
                    key += s;
                }
                if (File.Exists(directory + "\\Crypt.dll"))
                {
                    File.SetAttributes(directory + "\\Crypt.dll", FileAttributes.Normal);
                }
                StreamWriter sr = new StreamWriter(directory + "\\Crypt.dll");
                sr.Write(key);
                sr.Flush();
                sr.Dispose();

                File.SetAttributes(directory + "\\Crypt.dll", FileAttributes.Hidden);
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
    }
}
