using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KinesisDemo.Services.StreamMessage
{
    public class DataRecord
    {
        public DateTime ApproximateArrivalTimestamp { get; set; }
        public MemoryStream Data { get; set; }

        public string SequenceNumber { get; set; }
    }
}
