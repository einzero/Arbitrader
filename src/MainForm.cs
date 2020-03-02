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
            OpenAPI.OnEventConnect += AxKHOpenAPI1_OnEventConnect;
            OpenAPI.OnReceiveTrData += AxKHOpenAPI1_OnReceiveTrData;
            OpenAPI.CommConnect();

            _timer.Tick += Timer_Tick;
            _timer.Interval = 200;
            _timer.Start();

            _beginTime = Time();

            dataGridView_Balance.Columns.Add("예수금", "예수금");
            dataGridView_Balance.Columns.Add("D+2추정예수금", "D+2추정예수금");
            dataGridView_Balance.Columns.Add("유가잔고평가액", "유가잔고평가액");
            dataGridView_Balance.Columns.Add("예탁자산평가액", "예탁자산평가액");
            dataGridView_Balance.Columns.Add("총매입금액", "총매입금액");
            dataGridView_Balance.Rows.Add();
        }
   
        private void AxKHOpenAPI1_OnEventConnect(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnEventConnectEvent e)
        {
            if (e.nErrCode != 0)
            {
                Application.Exit();
            }

            _loginSucceed = true;

            string acclist = OpenAPI.GetLoginInfo("ACCLIST");
            string[] accounts = acclist.Trim().Split(';').Where(x => x.Length > 0).ToArray();
            comboBox_Account.Items.AddRange(accounts);
            comboBox_Account.SelectedIndex = 0;

            label_UserId.Text = OpenAPI.GetLoginInfo("USER_ID");
            label_Server.Text = OpenAPI.GetLoginInfo("GetServerGubun") == "1" ? "모의 투자" : "실서버";

            RegisterAction(5000, UpdateBalances);
            Show();
        }

        private void 백테스터열기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var form = new BackTestForm(OpenAPI);
            form.Show();
        }

        private void 로그ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LogForm.Instance.Show();
        }

        private void AxKHOpenAPI1_OnReceiveTrData(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            string 예수금 = GetTrData(e, "예수금");
            string D2추정예수금 = GetTrData(e, "D+2추정예수금");
            string 유가잔고평가액 = GetTrData(e, "유가잔고평가액");
            string 예탁자산평가액 = GetTrData(e, "예탁자산평가액");
            string 총매입금액 = GetTrData(e, "총매입금액");

            var row = dataGridView_Balance.Rows[0];
            row.SetValues(예수금, D2추정예수금, 유가잔고평가액, 예탁자산평가액, 총매입금액);
        }
        
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (CheckDisconnected())
            {
                _timer.Stop();
                MessageBox.Show("접속이 종료되었습니다.\n 프로그램을 종료합니다.");
                Application.Exit();
                return;
            }

            if (!IsConnected())
            {
                return;
            }

            DateTime now = Time();
            foreach(var item in _actions)
            {
                TimeSpan span = now - item.LastTime;
                if(span.TotalMilliseconds >= item.Interval)
                {
                    item.LastTime = now;
                    item.Action();
                }
            }
        }

        private bool CheckDisconnected()
        {
            TimeSpan span = TimeElapsed();
            bool notConnectedYet = span.TotalSeconds > 20 && !_loginSucceed;
            bool disconnected = _loginSucceed && !IsConnected();
            return notConnectedYet || disconnected;
        }

        private void UpdateBalances()
        {
            SetInputValue("계좌번호", GetAccount());
            SetInputValue("비밀번호", "");
            SetInputValue("상장폐지조회구분", "1");
            SetInputValue("비밀번호입력매체구분", "00");
            CommRqData("OPW00004");
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            Visible = IsConnected();
        }

        private string GetAccount()
        {
            return (string)comboBox_Account.SelectedItem;
        }

        private bool IsConnected()
        {
            return OpenAPI.GetConnectState() == 1;
        }

        private TimeSpan TimeElapsed()
        {
            return Time() - _beginTime;
        }

        private DateTime Time()
        {
            return DateTime.Now;
        }

        private void SetInputValue(string sID, string sValue)
        {
            OpenAPI.SetInputValue(sID, sValue);
        }

        private void CommRqData(string sTrCode)
        {
            OpenAPI.CommRqData("RQName", sTrCode, 0, "화면번호");
        }

        private string GetTrData(AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent e, string sName)
        {
            var data = OpenAPI.GetCommData(e.sTrCode, e.sRecordName, 0, sName);
            return data.Trim();
        }

        private void RegisterAction(int interval, Action action)
        {
            if (_actions.Any(x => x.Action == action))
            {
                return;
            }

            var item = new TimerAction();
            item.Interval = interval;
            item.Action = action;
            item.LastTime = Time();
            _actions.Add(item);
        }

        private bool _loginSucceed;
        private Timer _timer = new Timer();
        private DateTime _beginTime;
        private List<TimerAction> _actions = new List<TimerAction>();

        private class TimerAction
        {
            // in ms
            public int Interval;
            public Action Action;

            public DateTime LastTime;
        }
    }    
}
  