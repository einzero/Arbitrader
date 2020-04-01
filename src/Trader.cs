using AxKHOpenAPILib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Arbitrader
{
    class Trader
    {
        public event Action<EPhase> Processed;
        public enum EPhase
        {
            Begin,
            OrderConfirm,
            Balance,            
            Price,
            Order,     
        }

        public EPhase Phase
        {
            get;
            private set;
        }

        public string Error
        {
            get;
            private set;
        }

        private class StockInfo
        {
            public StockInfo(Stock stock)
            {
                Stock = stock;
            }

            public Stock Stock;
            public int Quantity;
            public AskingPrice AskingPrice;
        }

        private class Order
        {
            public Stock Stock;
            public int Quantity;
        }

        private readonly string _account;
        private readonly int _quantity;
        private readonly float _margin;
        private readonly StockInfo[] _targets;
        private readonly StockInfo _inverse;
        private readonly List<StockInfo> _stockInfos = new List<StockInfo>();

        private bool _wait;
        private readonly List<Order> _buyOrders = new List<Order>();
        private readonly List<Order> _sellOrders = new List<Order>();
        private bool _priceCollected;

        public Trader(string account, int quantity, float margin, Stock target1, Stock target2, Stock inverse)
        {
            _account = account;
            _quantity = quantity;
            _margin = margin;
            _targets = new StockInfo[2];
            _targets[0] = new StockInfo(target1);
            _targets[1] = new StockInfo(target2);
            _inverse = new StockInfo(inverse);
            
            _stockInfos.AddRange(_targets);
            _stockInfos.Add(_inverse);

            Phase = EPhase.Begin;
        }

        public void SetAskingPrice(string code, AskingPrice price)
        {
            foreach(var balance in _stockInfos)
            {
                if(balance.Stock.Code == code)
                {
                    balance.AskingPrice = price;                    
                }
            }

            if(Phase == EPhase.Price)
            {
                ProcessPhase();
            }            
        }

        public void Process()
        {
            if(Phase == EPhase.Begin)
            {
                MoveState(EPhase.OrderConfirm);
            }

            ProcessPhase();

            if (Processed != null)
            {
                Processed(Phase);
            }
        }

        private void MoveState(EPhase phase)
        {

            if (phase != Phase)
            {
                Debug.Info("Phase: " + phase.ToString());
            }

            Phase = phase;
            ProcessPhase();
        }

        private void ProcessPhase()
        {
            if (_wait || !OpenApi.IsTradeable())
            {
                return;
            }

            switch (Phase)
            {
                case EPhase.OrderConfirm:
                    OnOrderConfirm();
                    break;
                case EPhase.Balance:
                    OnBalance();
                    break;
                case EPhase.Price:
                    OnPrice();
                    break;
                case EPhase.Order:
                    OnOrder();
                    break;
            }
        }

        private void OnOrderConfirm()
        {            
            _wait = true;

            OpenApi.SetInputValue("계좌번호", _account);
            OpenApi.SetInputValue("전체종목구분", "0");
            OpenApi.SetInputValue("매매구분", "0");
            OpenApi.SetInputValue("체결구분", "1");
            OpenApi.CommRqData("실시간미체결요청", "opt10075", delegate(_DKHOpenAPIEvents_OnReceiveTrDataEvent e)
            {
                int count = OpenApi.GetRepeatCnt(e);
                bool hasRemain = false;
                for(int i = 0; i < count; ++i)
                {
                    var name = OpenApi.GetTrData(e, "종목명", i);
                    var remain = OpenApi.GetTrData(e, "미체결수량", i).ToInt();
                    if(remain > 0)
                    {
                        hasRemain = true;
                        break;
                    }
                }

                _wait = false;                        
                MoveState(hasRemain ? EPhase.Begin : EPhase.Balance);
            });            
        }

        private void OnBalance()
        {
            _wait = true;

            OpenApi.UpdateBalances(_account, delegate (AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent e)
            {
                int count = OpenApi.GetRepeatCnt(e);
                for (int i = 0; i < count; ++i)
                {
                    string 종목명 = OpenApi.GetTrData(e, "종목명", i);
                    string 보유수량 = OpenApi.GetTrData(e, "보유수량", i);
                    foreach(var stock in _stockInfos)
                    {
                        if(stock.Stock.ToString() == 종목명)
                        {
                            int quantity;
                            if (int.TryParse(보유수량, out quantity))
                            {
                                stock.Quantity = quantity;
                            }
                            else
                            {
                                // something wrong;
                                Error = "보유수량 오류: " + 보유수량;
                                return;
                            }
                            break;
                        }
                    }
                }

                _wait = false;
                MoveState(EPhase.Price);
            });
        }

        private void OnPrice()
        {
            var now = DateTime.Now;
            foreach(var info in _stockInfos)
            {
                if(info.AskingPrice == null)
                {
                    return;
                }

                TimeSpan span = now - info.AskingPrice.Time;
                if(Math.Abs(span.TotalSeconds) > 1)
                {
                    return;
                }
            }
            
            if(!_priceCollected)
            {
                Debug.Warn("Price Colleted!");
                _priceCollected = true;
            }
            
            // 모든 가격 정보들이 들어왔으므로 뭘 할지 결정
            // 싼걸 사야 함
            int index = _targets[0].AskingPrice.Sell[0].Price < _targets[1].AskingPrice.Sell[0].Price ? 0 : 1;
            var target = _targets[index];
            var other = _targets[(index + 1) % 2];
            
            // 내꺼 사기만 하면 되는 케이스
            if(other.Quantity == 0)
            {
                if (target.Quantity < _quantity)
                {
                    _buyOrders.Add(new Order
                    {
                        Stock = target.Stock,
                        Quantity = _quantity - target.Quantity
                    });
                }
            }
            else
            {
                // 일단 판거만큼만 산다.
                // 최대 주문 크기를 제한 
                var quantity = Math.Min(other.Quantity, _quantity);
                double sellPrice = CalculatePrice(quantity, other.AskingPrice.Buy);
                double buyPrice = CalculatePrice(quantity, target.AskingPrice.Sell);                
                if(sellPrice == -1 || buyPrice == -1)
                {
                    MoveState(EPhase.Begin);
                    return;
                }

                Debug.Info("Price Inverse: Sell({0}) - {1} / Buy({2}) - {3}", other.Stock, sellPrice, target.Stock, buyPrice);
                if (sellPrice >= buyPrice * _margin)
                {
                    _sellOrders.Add(new Order
                    {
                        Stock = other.Stock,
                        Quantity = quantity
                    });

                    _buyOrders.Add(new Order
                    {
                        Stock = target.Stock,
                        Quantity = quantity
                    });
                }
            }

            CheckInverse(target);

            if(_buyOrders.Count > 0 || _sellOrders.Count > 0)
            {
                MoveState(EPhase.Order);
            }
        }

        private int CalculatePrice(int quantity, List<Asking> askings)
        {
            int sum = 0;
            foreach(var asking in askings)
            {
                int num = Math.Min(quantity, asking.Quantity);
                quantity -= num;
                sum += asking.Price * num;
                if(quantity <= 0)
                {
                    return sum;
                }
            }

            return -1;
        }

        private void CheckInverse(StockInfo target)
        {
            var totalPrice = _quantity * target.AskingPrice.Buy[0].Price;
            var totalInversePrice = _inverse.AskingPrice.Buy[0].Price * _inverse.Quantity;

            var priceDiff = Math.Abs(totalPrice - totalInversePrice);
            float ratio = (float)priceDiff / totalPrice;

            // 20% 이상 차이나면 리밸런싱
            if (ratio >= 0.2f)
            {
                // 사야할 인버스의 수
                var inverseQuantity = totalPrice / _inverse.AskingPrice.Buy[0].Price;
                var diff = Math.Abs(inverseQuantity - _inverse.Quantity);
                var order = new Order
                {
                    Stock = _inverse.Stock,
                    Quantity = diff
                };
                if (inverseQuantity > _inverse.Quantity)
                {
                    _buyOrders.Add(order);
                }
                else
                {
                    _sellOrders.Add(order);
                }
            }
        }

        private void OnOrder()
        {
            for (int i = _sellOrders.Count - 1; i >= 0; --i)
            {
                var order = _sellOrders[i];
                var result = OpenApi.Sell(_account, order.Stock.Code, order.Quantity);
                Debug.Warn("Sell: {0}, {1}, Result: {2}", order.Stock, order.Quantity, result);
            }

            for (int i = _buyOrders.Count - 1; i >= 0; --i)
            {
                var order = _buyOrders[i];
                var result = OpenApi.Buy(_account, order.Stock.Code, order.Quantity);
                Debug.Warn("Buy: {0}, {1}, Result: {2}", order.Stock, order.Quantity, result);
            }

            _sellOrders.Clear();
            _buyOrders.Clear();
            MoveState(EPhase.OrderConfirm);
        }
    }
}

