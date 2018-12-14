using System;
using System.Collections.Generic;
using System.Text;

namespace KinesisDemo.Business.Response
{
    public class PublishTransactionResponse
    {
        public string ShardId { get; set; }
        public string SequenceNumber { get; set; }
        public string Message { get; set; }
        public string ResponseCode { get; set; }
    }
}
