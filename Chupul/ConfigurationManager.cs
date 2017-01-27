using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chupul
{
    public partial class ConfigurationManager : Chups
    {

        public ConfigurationManager()
        {
            InitializeComponent();

            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
        }

        private void ConfigurationManager_Shown(object sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                Init();
                await WaitForLockAsync();
            });
        }
    }
}
