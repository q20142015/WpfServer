using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfServer
{
    internal class Data
    {
        [Key]
        public string ProductCode { get; set; }
        public int Amount { get; set; }

        public Data(string productCode, int amount)
        {
            ProductCode = productCode;
            Amount = amount;
        }
    }
}
