using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Arbitrader
{
    public class StockPrice
    {
        public string Date;
        public int StartPrice;
        public int HighPrice;
        public int LowPrice;
        public int Price;
    }

    public class StockPriceCollection
    {
        private TaskCompletionSource<StockPriceCollection> _source = new TaskCompletionSource<StockPriceCollection>();
        private string _code;
        private string _date;

        public List<StockPrice> Items = new List<StockPrice>();

        public static async Task<StockPriceCollection> Get(string code, string date)
        {
            var collection = new StockPriceCollection();
            await Task.Factory.StartNew(delegate
            {
                collection.Request(code, date, collection);
            }, TaskCreationOptions.AttachedToParent);
               
            return await Task.FromResult(collection._source.Task.Result);
        }

        private void Request(string code, string date, StockPriceCollection collection, int seq = 0)
        {
            _code = code;
            _date = date;

            OpenApi.SetInputValue("종목코드", code);
            OpenApi.SetInputValue("기준일자", date);
            OpenApi.SetInputValue("수정주가구분", "0");
            OpenApi.CommRqData("opt10081", collection.PriceCallback, seq);
        }

        private void PriceCallback(AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            int count = OpenApi.GetRepeatCnt(e);
            for(int i = 0; i < count; ++i)
            {
                string date = OpenApi.GetTrData(e, "일자", i);
                string startPrice = OpenApi.GetTrData(e, "시가", i);
                string highPrice = OpenApi.GetTrData(e, "고가", i);
                string lowPrice = OpenApi.GetTrData(e, "저가", i);
                string price = OpenApi.GetTrData(e, "현재가", i);

                var stock = new StockPrice();
                stock.Date = date;
                int.TryParse(startPrice, out stock.StartPrice);
                int.TryParse(highPrice, out stock.HighPrice);
                int.TryParse(lowPrice, out stock.LowPrice);
                int.TryParse(price, out stock.Price);

                Items.Add(stock);
            }

            Debug.Info("Price Received({0}): {1}", _code, Items.Count);

            int seq;
            int.TryParse(e.sPrevNext, out seq);
            if (seq != 0)
            {
                Thread.Sleep(300);
                Request(_code, _date, this, seq);
            }
            else
            {
                _source.SetResult(this);
            }
        }
    }
}
