using System;
using System.Linq;
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

            var etfs = OpenApi.GetETFs();
            foreach(var etf in etfs)
            {
                comboBox_종목1.Items.Add(etf);
                comboBox_종목2.Items.Add(etf);
            }            
        }

        private void BackTestForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private async void button_Test_Click(object sender, EventArgs e)
        {
            var stock = comboBox_종목1.SelectedItem as OpenApi.Stock;
            var date = dateTimePicker_Begin.Value;

            var day = date.ToString("yyyyMMdd");
            var result = await StockPriceCollection.Get(stock.Code, day);

            Debug.Info("{0}", result.Items.Count);

            int x = 5;
        }
    }
}

