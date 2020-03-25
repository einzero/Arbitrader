using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Arbitrader
{
    public class StockPrice
    {
        public DateTime Date;
        public long StartPrice;
        public long HighPrice;
        public long LowPrice;
        public long Price;
    }

    public class StockPriceCollection
    {
        private TaskCompletionSource<StockPriceCollection> _source = new TaskCompletionSource<StockPriceCollection>();
        private string _code;
        private DateTime _begin;
        private DateTime _end;

        public List<StockPrice> Items = new List<StockPrice>();

        public static Task<StockPriceCollection> Get(string code, DateTime begin, DateTime end)
        {
            var collection = new StockPriceCollection();            
            collection.Request(code, 
                new DateTime(begin.Year, begin.Month, begin.Day), 
                new DateTime(end.Year, end.Month, end.Day), 
                collection);

            return collection._source.Task;
        }

        private void Request(string code, DateTime begin, DateTime end, StockPriceCollection collection, int seq = 0)
        {
            _code = code;
            _begin = begin;
            _end = end;

            OpenApi.SetInputValue("종목코드", code);
            OpenApi.SetInputValue("기준일자", end.ToString("yyyyMMdd"));
            OpenApi.SetInputValue("수정주가구분", "0");
            OpenApi.CommRqData("opt10081", collection.PriceCallback, seq);
        }

        private void PriceCallback(AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            bool continued = true;

            int count = OpenApi.GetRepeatCnt(e);
            for(int i = 0; i < count; ++i)
            {
                string date = OpenApi.GetTrData(e, "일자", i);
                string startPrice = OpenApi.GetTrData(e, "시가", i);
                string highPrice = OpenApi.GetTrData(e, "고가", i);
                string lowPrice = OpenApi.GetTrData(e, "저가", i);
                string price = OpenApi.GetTrData(e, "현재가", i);

                var stock = new StockPrice();

                DateTime.TryParseExact(date, "yyyyMMdd", CultureInfo.CurrentCulture, DateTimeStyles.None, out stock.Date);
                long.TryParse(startPrice, out stock.StartPrice);
                long.TryParse(highPrice, out stock.HighPrice);
                long.TryParse(lowPrice, out stock.LowPrice);
                long.TryParse(price, out stock.Price);

                if(stock.Date < _begin)
                {
                    continued = false;
                    break;
                }

                Items.Add(stock);
            }

            int seq;
            int.TryParse(e.sPrevNext, out seq);
            if (seq != 0 && continued)
            {
                Thread.Sleep(300);
                Request(_code, _begin, _end, this, seq);
            }
            else
            {
                Items.Reverse();
                _source.SetResult(this);
            }
        }
    }
}
