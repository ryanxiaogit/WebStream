using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinesisDemo.Services.Repository
{
    public class TransactionData
    {
        public string TransactionID { get; set; }
        public int Amount { get; set; }
        public string TransactionType { get; set; }
    }
}
