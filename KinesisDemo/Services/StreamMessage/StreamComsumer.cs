using Amazon.DynamoDBv2.Model;
using Amazon.Kinesis;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KinesisDemo.Services.StreamMessage
{
    public interface IStreamMessageComsumer
    {
        Task<List<DataRecord>> GetStream(string shardId, string startingSequenceNumber, int limit = 10000);
    }
    public class StreamComsumer : IStreamMessageComsumer
    {
        readonly IAmazonKinesis _kinesis = null;
        readonly StreamMessageOptions _options = null;
        public StreamComsumer(IAmazonKinesis amazonKinesis, IOptions<StreamMessageOptions> options)
        {
            _kinesis = amazonKinesis;
            _options = options.Value;
        }

        public async Task<List<DataRecord>> GetStream(string shardId, string startingSequenceNumber, int limit = 10000)
        {
            var siResponse = await _kinesis.GetShardIteratorAsync(new Amazon.Kinesis.Model.GetShardIteratorRequest
            {
                ShardId = shardId,
                ShardIteratorType = ShardIteratorType.AT_SEQUENCE_NUMBER,
                StartingSequenceNumber = startingSequenceNumber,
                StreamName = _options.StreamName
            });

            var response = await _kinesis.GetRecordsAsync(new Amazon.Kinesis.Model.GetRecordsRequest
            {
                ShardIterator = siResponse.ShardIterator,
                Limit = limit
            });

            var records = response.Records.Select(record =>
                new DataRecord
                {
                    ApproximateArrivalTimestamp = record.ApproximateArrivalTimestamp,
                    Data = record.Data,
                    SequenceNumber = record.SequenceNumber
                }
            ).ToList();

            return records;
        }
    }
}
