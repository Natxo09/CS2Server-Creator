using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CS2ServerCreator
{
    public partial class About : Form
    {
        public About()
        {
            InitializeComponent();
        }

        private void labelGitHub_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Natxo09");
        }
    }
}
