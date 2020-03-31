using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arbitrader
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
}
