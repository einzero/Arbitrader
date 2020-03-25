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
            var stock1 = comboBox_종목1.SelectedItem as OpenApi.Stock;
            if(stock1 == null)
            {
                return;
            }

            var collection1 = await StockPriceCollection.Get(stock1.Code, dateTimePicker_Begin.Value, dateTimePicker_End.Value);
            Debug.Info("{0}", collection1.Items.Count);

            var stock2 = comboBox_종목2.SelectedItem as OpenApi.Stock;
            if (stock2 == null)
            {
                return;
            }
            var collection2 = await StockPriceCollection.Get(stock2.Code, dateTimePicker_Begin.Value, dateTimePicker_End.Value);

            Debug.Info("{0}", collection2.Items.Count);

            var items1 = collection1.Items;
            var items2 = collection2.Items;


            bool isItem1 = items1[0].Price <= items2[0].Price;
            const long quantity = 2000;

            long sum = 0;
            for(int i = 1; i < items1.Count; ++i)
            {
                var price1 = items1[i].Price;
                var price2 = items2[i].Price;

                var mine = isItem1 ? price1 : price2;
                var other = isItem1 ? price2 : price1;

                var mineValue = mine * quantity;
                var otherValue = other * quantity;

                if (otherValue <= mineValue - mine)
                {
                    long diff = mineValue - otherValue;
                    long count = diff / mine;
                    long profit = count * mine;
                    sum += profit;

                    isItem1 = !isItem1;

                    Debug.Info("Date: {0} - Item1: {1}, Item2: {2}, Profit: {3}", items1[i].Date, price1, price2, profit);
                }
            }

            Debug.Info("Total Profit: {0}", sum);
        }
    }
}

