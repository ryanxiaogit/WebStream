using Amazon.Kinesis;
using KinesisDemo.Business.Response;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KinesisDemo.Services.StreamMessage
{
    public interface IStreamMessageProducer
    {
        Task<PublishTransactionResponse> PublishStream(string topic, Stream data);
    }

    public class StreamProducer : IStreamMessageProducer
    {
        readonly IAmazonKinesis _kinesis = null;
        readonly StreamMessageOptions _options = null;

        public StreamProducer(IAmazonKinesis amazonKinesis, IOptions<StreamMessageOptions> options)
        {
            _kinesis = amazonKinesis;
            _options = options.Value;
        }

        public async Task<PublishTransactionResponse> PublishStream(string topic, Stream data)
        {
            var response = new PublishTransactionResponse();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                await data.CopyToAsync(memoryStream);

                var putRecordResponse = await _kinesis.PutRecordAsync(new Amazon.Kinesis.Model.PutRecordRequest
                {
                    Data = memoryStream,
                    PartitionKey = topic,
                    StreamName = _options.StreamName
                });

                response.ShardId = putRecordResponse.ShardId;
                response.SequenceNumber = putRecordResponse.SequenceNumber;
            }

            return response;
        }
    }
}
