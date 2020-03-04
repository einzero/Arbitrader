using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Arbitrader
{
    public static class OpenApi
    {
        public class Stock
        {
            public string Code;
            public string Name;

            public override string ToString()
            {
                return Name;
            }
        };

        public delegate void TrCallback(AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent e);

        public static event Action<string[], string, string> Connected;

        private static AxKHOpenAPILib.AxKHOpenAPI _api;
        private static bool _loginSucceed;

        private static Timer _timer = new Timer();
        private static DateTime _beginTime;
        private static List<TimerAction> _actions = new List<TimerAction>();

        private static readonly Dictionary<string, TrCallback> _trs = new Dictionary<string, TrCallback>();
        private static readonly List<Stock> _etfs = new List<Stock>();

        private class TimerAction
        {
            // in ms
            public int Interval;
            public Action Action;

            public DateTime LastTime;
        }

        public static void Init(AxKHOpenAPILib.AxKHOpenAPI api)
        {
            _api = api;

            _api.OnEventConnect += AxKHOpenAPI1_OnEventConnect;
            _api.OnReceiveTrData += AxKHOpenAPI1_OnReceiveTrData;
            _api.CommConnect();

            _timer.Tick += Timer_Tick;
            _timer.Interval = 200;
            _timer.Start();
            _beginTime = Time();
        }

        public static bool IsConnected()
        {
            return _api.GetConnectState() == 1;
        }

        public static void RegisterAction(int interval, Action action)
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

        public static string GetTrData(AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent e, string sName)
        {
            var data = _api.GetCommData(e.sTrCode, e.sRecordName, 0, sName);
            return data.Trim();
        }

        public static void UpdateBalances(string account, TrCallback callback)
        {
            SetInputValue("계좌번호", account);
            SetInputValue("비밀번호", "");
            SetInputValue("상장폐지조회구분", "1");
            SetInputValue("비밀번호입력매체구분", "00");
            CommRqData("OPW00004", callback);
        }

        public static IEnumerable<Stock> GetETFs()
        {
            return _etfs;
        }

        private static void AxKHOpenAPI1_OnEventConnect(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnEventConnectEvent e)
        {
            if (e.nErrCode != 0)
            {
                Application.Exit();
            }

            _loginSucceed = true;

            string acclist = _api.GetLoginInfo("ACCLIST");
            string[] accounts = acclist.Trim().Split(';').Where(x => x.Length > 0).ToArray();

            var userId = _api.GetLoginInfo("USER_ID");
            var server = _api.GetLoginInfo("GetServerGubun") == "0" ? "모의" : "실제";

            if (Connected != null)
            {
                Connected(accounts, userId, server);
            }

            // fill etfs
            var list = _api.GetCodeListByMarket("8").Trim().Split(';');
            foreach(var code in list)
            {
                if(string.IsNullOrEmpty(code))
                {
                    continue;
                }

                _etfs.Add(new Stock
                {
                    Code = code,
                    Name = _api.GetMasterCodeName(code)
                });
            }
        }

        private static void AxKHOpenAPI1_OnReceiveTrData(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            if (_trs.TryGetValue(e.sTrCode, out TrCallback callback))
            {
                _trs.Remove(e.sTrCode);
                callback(e);
            }
        }

        private static void Timer_Tick(object sender, EventArgs e)
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
            foreach (var item in _actions)
            {
                TimeSpan span = now - item.LastTime;
                if (span.TotalMilliseconds >= item.Interval)
                {
                    item.LastTime = now;
                    item.Action();
                }
            }
        }

        private static bool CheckDisconnected()
        {
            TimeSpan span = TimeElapsed();
            bool notConnectedYet = span.TotalSeconds > 20 && !_loginSucceed;
            bool disconnected = _loginSucceed && !IsConnected();
            return notConnectedYet || disconnected;
        }

        private static TimeSpan TimeElapsed()
        {
            return Time() - _beginTime;
        }

        private static DateTime Time()
        {
            return DateTime.Now;
        }

        private static void SetInputValue(string sID, string sValue)
        {
            _api.SetInputValue(sID, sValue);
        }

        private static void CommRqData(string sTrCode, TrCallback callback)
        {
            if (_trs.ContainsKey(sTrCode))
            {
                Debug.Warning("Previous {0} is not finished yet. Request will be ignored", sTrCode);
                return;
            }

            _api.CommRqData("RQName", sTrCode, 0, "화면번호");
            _trs[sTrCode] = callback;
        }
    }
}
