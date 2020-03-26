using System;
using System.Linq;
using System.Windows.Forms;

namespace Arbitrader
{
    public partial class BackTestForm : Form
    {
        private static int[] Intervals =
        {
            1,
            3,
            5,
            10,
            15,
            30,
            45,
            60            
        };

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
                comboBox_Stock1.Items.Add(etf);
                comboBox_Stock2.Items.Add(etf);
                comboBox_Stock3.Items.Add(etf);
            }
            comboBox_Stock1.SelectedItem = etfs.FirstOrDefault(x => x.Name == "KODEX 200");
            comboBox_Stock2.SelectedItem = etfs.FirstOrDefault(x => x.Name == "TIGER 200");
            comboBox_Stock3.SelectedItem = etfs.FirstOrDefault(x => x.Name == "KBSTAR 200");

            foreach (var interval in Intervals)
            {
                comboBox_Interval.Items.Add(interval);
            }
            comboBox_Interval.SelectedIndex = 0;
            comboBox_Interval.Enabled = false;
        }

        private void BackTestForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private class IntProgress : IProgress<int>
        {
            public IntProgress(ProgressBar bar, int target)
            {
                _bar = bar;
                _bar.Minimum = 0;
                _bar.Maximum = 100;
                _bar.Value = 0;
                _target = target;
            }

            public void Report(int value)
            {
                int percent = _target > 0 ? value * 100 / _target : 100;
                _bar.Value = percent;
            }

            ProgressBar _bar;
            int _target;
        }

        private async void button_Test_Click(object sender, EventArgs e)
        {
            var stock1 = comboBox_Stock1.SelectedItem as OpenApi.Stock;
            var stock2 = comboBox_Stock2.SelectedItem as OpenApi.Stock;
            if (stock1 == null || stock2 == null) return;

            string interval = checkBox_UseMinute.Checked ? comboBox_Interval.SelectedItem.ToString() : "";
            int gap;
            int.TryParse(interval, out gap);

            var begin = dateTimePicker_Begin.Value;
            var end = dateTimePicker_End.Value;

            int target = CalculateTarget(interval, begin, end);
            var progress = new IntProgress(progressBar, target);

            var collection1 = await StockPriceCollection.Get(stock1.Code, begin, end, interval, progress);
            progressBar.Value = 0;
            Debug.Info("{0}", collection1.Items.Count);

            var collection2 = await StockPriceCollection.Get(stock2.Code, begin, end, interval, progress);
            Debug.Info("{0}", collection2.Items.Count);
            progressBar.Value = 100;

            var items1 = collection1.Items;
            var items2 = collection2.Items;
            if (items1.Count <= 0 || items2.Count <= 0)
            {
                Debug.Warn("No data");
                return;
            }

            int i = 0;
            int j = 0;
            while (items1[i].Time != items2[j].Time)
            {
                if (items1[i].Time < items2[j].Time)
                {
                    i++;
                }
                else
                {
                    j++;
                }
            }

            bool isItem1 = items1[i].Price <= items2[j].Price;

            long quantity;
            long.TryParse(textBox_Quantity.Text, out quantity);

            i++;
            j++;

            long sum = 0;
            while (i < items1.Count && j < items2.Count)
            {
                var item1 = items1[i];
                var item2 = items2[j];
                if (item1.Time != item2.Time)
                {
                    if (item1.Time < item2.Time)
                    {
                        i++;
                    }
                    else
                    {
                        j++;
                    }
                    continue;
                }

                i++;
                j++;

                if (item1.Time.Hour == 9 && item1.Time.Minute < 5) continue;
                if (item1.Time.Hour >= 15) continue;

                var price1 = item1.Price;
                var price2 = item2.Price;

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

                    Debug.Info("Date: {0} - Item1: {1}, Item2: {2}, Profit: {3}", item1.Time, price1, price2, profit);
                }
            }

            Debug.Info("Total Profit: {0}", sum);
        }

        private static int CalculateTarget(string interval, DateTime begin, DateTime end)
        {
            int gap;
            int.TryParse(interval, out gap);

            int target = 0;
            for (; begin <= end; begin = begin.AddDays(1))
            {
                if(begin.DayOfWeek == DayOfWeek.Sunday ||
                   begin.DayOfWeek == DayOfWeek.Saturday)
                {
                    continue;
                }

                if (gap == 0)
                {
                    target++;
                }
                else
                {
                    target += 390 / gap;
                }
            }

            return target;
            
        }

        private void checkBox_UseMinute_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_Interval.Enabled = checkBox_UseMinute.Checked;
        }
    }
}

