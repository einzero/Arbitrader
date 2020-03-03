using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace Arbitrader
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            OpenApi.Init(axKHOpenAPI);

            OpenApi.Connected += OpenApi_Connected;

            dataGridView_Balance.Columns.Add("예수금", "예수금");
            dataGridView_Balance.Columns.Add("D+2추정예수금", "D+2추정예수금");
            dataGridView_Balance.Columns.Add("유가잔고평가액", "유가잔고평가액");
            dataGridView_Balance.Columns.Add("예탁자산평가액", "예탁자산평가액");
            dataGridView_Balance.Columns.Add("총매입금액", "총매입금액");
            dataGridView_Balance.Rows.Add();
        }

        private void OpenApi_Connected(string[] accounts, string userId, string server)
        {
            comboBox_Account.Items.AddRange(accounts);
            comboBox_Account.SelectedIndex = 0;

            label_UserId.Text = userId;
            label_Server.Text = server;

            OpenApi.RegisterAction(5000, UpdateBalances);
            Show();
        }

        private void UpdateBalances()
        {
            OpenApi.UpdateBalances(GetAccount(), delegate (AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent e)
            {
                string 예수금 = OpenApi.GetTrData(e, "예수금");
                string D2추정예수금 = OpenApi.GetTrData(e, "D+2추정예수금");
                string 유가잔고평가액 = OpenApi.GetTrData(e, "유가잔고평가액");
                string 예탁자산평가액 = OpenApi.GetTrData(e, "예탁자산평가액");
                string 총매입금액 = OpenApi.GetTrData(e, "총매입금액");

                var row = dataGridView_Balance.Rows[0];
                row.SetValues(예수금, D2추정예수금, 유가잔고평가액, 예탁자산평가액, 총매입금액);
            });
        }

        private void 백테스터열기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BackTestForm.Instance.Show();
        }

        private void 로그ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LogForm.Instance.Show();
        }

        private void AxKHOpenAPI1_OnReceiveTrData(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
           
        }
     
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            Visible = OpenApi.IsConnected();
        }

        private string GetAccount()
        {
            return (string)comboBox_Account.SelectedItem;
        }     
    }    
}
  