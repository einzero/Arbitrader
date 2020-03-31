using System;
using System.Collections.Generic;

namespace Arbitrader
{
    class Asking
    {
        public int Price;
        public int Quantity;
    };

    class AskingPrice
    {
        public AskingPrice(DateTime time)
        {
            Time = time;
        }

        public DateTime Time;
        public List<Asking> Sell = new List<Asking>();
        public List<Asking> Buy = new List<Asking>();
    }
}
