using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using LowLevelHooks.Keyboard;

namespace Chupul
{
    public class Chups : Form
    {
        public static State State { get; set; }

        public static Config Configuration { get; set; }

        private Form _lockWindow;

        public Chups()
        {
        }

        private KeyboardHook kHook;
        private ContextMenuStrip mnu;
        private ToolStripMenuItem exitToolStripMenuItem;
        private string _cache;

        public void Init()
        {
            _cache = "";
            State = State.WaitForIdle;
            Configuration = new Config()
            {
                LockFullScreen = false,
                MessageToChupul = "Chupul",
                PatternToNoChupul = "111",
                SecondsToChups = 10,
                RefreshInterval = 50,
            };
        }

        private void KHook_KeyEvent(object sender, KeyboardHookEventArgs e)
        {
            if (State == State.Locked)
            {
                _cache = _cache + e.KeyString;
                if (_cache.Length > 10)
                {
                    _cache = _cache.Remove(0, 1);
                }

                if (_cache.Contains(Configuration.PatternToNoChupul))
                {
                    UnLock();
                }

                if (_cache.Length > Configuration.PatternToNoChupul.Length + 1)
                {
                    ShowAntiChupul();
                }
            }
        }

        private void ShowAntiChupul()
        {
            if (_lockWindow != null)
            {
                var thisExe = System.Reflection.Assembly.GetExecutingAssembly();
                Stream file = thisExe.GetManifestResourceStream("Chupul.antichupul.gif");
                if (file != null)
                {
                    Image imgAntiChupul = Image.FromStream(file);
                    PictureBox pBox = new PictureBox();
                    pBox.Width = imgAntiChupul.Width;
                    pBox.Height = imgAntiChupul.Height;
                    pBox.Top = (Screen.PrimaryScreen.Bounds.Height/2) - (pBox.Height/2);
                    pBox.Left = (Screen.PrimaryScreen.Bounds.Width/2) - (pBox.Width/2);
                    pBox.Image = imgAntiChupul;
                    pBox.Visible = true;
                    _lockWindow.Controls.Add(pBox);
                }
            }
        }

        private async Task LockAsync()
        {
            await Task.Run(() =>
            {
                if (InvokeRequired)
                {
                    this.Invoke(new Action(Lock));
                    return;
                }
            });
        }

        private DateTime _lockedOnTime;
        private string _prevImagePath;
        private void Lock()
        {
            _lockedOnTime = DateTime.Now;

            if (_lockWindow == null)
            {
                _lockWindow = new Form();
            }

            //if (Configuration.LockFullScreen)
            //{
            //    _lockWindow.TopMost = true;
            //    _lockWindow.FormBorderStyle = FormBorderStyle.None;
            //    _lockWindow.WindowState = FormWindowState.Maximized;
            //}

            _prevImagePath = GetScreenShot();

            // Get rect for all screens.
            Rectangle r = new Rectangle();
            foreach (Screen s in Screen.AllScreens)
            {
                //if (!Equals(s, Screen.PrimaryScreen)) // Blackout only the secondary screens
                //    r = Rectangle.Union(r, s.Bounds);

                r = Rectangle.Union(r, s.Bounds);
            }
            _lockWindow.Top = r.Top;
            _lockWindow.Left = r.Left;
            _lockWindow.Width = r.Width;
            _lockWindow.Height = r.Height;
            _lockWindow.TopMost = true;
            _lockWindow.FormBorderStyle = FormBorderStyle.None;
            _lockWindow.BackgroundImage = Image.FromFile(_prevImagePath);
            _lockWindow.Show();
            _lockWindow.Shown += (sender, args) =>
            {
                _lockWindow.Cursor = Cursors.Default;
            };


            kHook = new KeyboardHook();
            kHook.Hook();
            kHook.KeyEvent += KHook_KeyEvent;

            State = State.Locked;
        }

        private void UnLock()
        {
            _cache = "";

            // Unlock the keyboard hooks.
            kHook.KeyEvent -= KHook_KeyEvent;
            kHook.Dispose();
            kHook = null;

            // Dispose lock window.
            _lockWindow.BackgroundImage.Dispose();
            _lockWindow.BackgroundImage = null;
            _lockWindow.Close();
            _lockWindow.Dispose();
            _lockWindow = null;
            
            // Delete the previous camo file.
            if (File.Exists(_prevImagePath))
                File.Delete(_prevImagePath);

            // Wait once again for idle time.
            State = State.WaitForIdle;
        }

        public async Task WaitForLockAsync()
        {
            await Task.Run(async () =>
            {
                while (State != State.Disabled)
                {
                    //Thread.Sleep(Configuration.RefreshInterval);
                    Thread.Sleep(50);
                    switch (State)
                    {
                        case State.WaitForIdle:
                            if (IdleTimeFinder.GetIdleTime().TotalSeconds > Configuration.SecondsToChups)
                                await LockAsync();

                            break;
                        case State.Locked:

                            break;
                    }
                }
            });
        }

        private string GetScreenShot()
        {
            string fileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".csv";
            Bitmap screenshot = new Bitmap(SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height, PixelFormat.Format32bppArgb);
            Graphics screenGraph = Graphics.FromImage(screenshot);
            screenGraph.CopyFromScreen(SystemInformation.VirtualScreen.X, SystemInformation.VirtualScreen.Y, 0, 0, SystemInformation.VirtualScreen.Size, CopyPixelOperation.SourceCopy);

            screenshot.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
            return fileName;
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.notifyIco = new System.Windows.Forms.NotifyIcon(this.components);
            this.mnu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnu.SuspendLayout();
            this.SuspendLayout();
            // 
            // notifyIco
            // 
            this.notifyIco.ContextMenuStrip = this.mnu;
            this.notifyIco.Text = "notifyIcon1";
            this.notifyIco.Visible = true;
            // 
            // mnu
            // 
            this.mnu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.mnu.Name = "mnu";
            this.mnu.Size = new System.Drawing.Size(153, 48);
            this.mnu.Opening += new System.ComponentModel.CancelEventHandler(this.mnu_Opening);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.exitToolStripMenuItem.Text = "&Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // Chups
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "Chups";
            this.mnu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private NotifyIcon notifyIco;
        private System.ComponentModel.IContainer components;

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void mnu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
    }

    public enum State
    {
        WaitForIdle,
        Locked,
        Unlocked,
        Disabled
    }

    public class Config
    {
        public int SecondsToChups { get; set; }

        public string MessageToChupul { get; set; }

        public string PatternToNoChupul { get; set; }

        public bool LockFullScreen { get; set; }

        public int RefreshInterval { get; set; }

        public string AntiChupulImage { get; set; }
    }

    public struct LastInputInfo
    {
        public uint cbSize;

        public uint dwTime;
    }

    /// <summary>
    /// Helps to find the idle time, (in milliseconds) spent since the last user input
    /// </summary>
    public class IdleTimeFinder
    {
        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LastInputInfo plii);

        [DllImport("Kernel32.dll")]
        private static extern uint GetLastError();

        /// <summary>
        /// Get idle timespan.
        /// </summary>
        /// <returns>The timespan PC is idle.</returns>
        public static TimeSpan GetIdleTime()
        {
            LastInputInfo lastInPut = new LastInputInfo();
            lastInPut.cbSize = (uint) System.Runtime.InteropServices.Marshal.SizeOf(lastInPut);
            GetLastInputInfo(ref lastInPut);

            uint ticks = (uint) Environment.TickCount - lastInPut.dwTime;
            TimeSpan timespent = TimeSpan.FromMilliseconds(ticks);
            return timespent;
        }

        /// <summary>
        /// Get the Last input time in milliseconds
        /// </summary>
        /// <returns></returns>
        public static long GetLastInputTime()
        {
            LastInputInfo lastInPut = new LastInputInfo();
            lastInPut.cbSize = (uint) System.Runtime.InteropServices.Marshal.SizeOf(lastInPut);
            if (!GetLastInputInfo(ref lastInPut))
            {
                throw new Exception(GetLastError().ToString());
            }
            return lastInPut.dwTime;
        }
    }
}
