using System;
using System.Windows.Forms;

namespace Generations_Archive_Editor
{
    partial class PaddingDialog : Form
    {
        public PaddingDialog(int padding)
        {
            InitializeComponent();
            numericUpDown1.Value = padding;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}