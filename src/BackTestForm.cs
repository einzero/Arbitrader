using System;
using System.Windows.Forms;

namespace Arbitrader
{
    public partial class BackTestForm : Form
    {
        private static BackTestForm _instance;
        public static BackTestForm Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BackTestForm();
                    _instance.Hide();
                }

                return _instance;
            }
        }

        public BackTestForm()
        {
            InitializeComponent();

            //var list = OpenApi.GetCodeListByMarket("8").Trim().Split(';');
        }

        private void BackTestForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }
    }
}
