using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinesisDemo.Services.Repository
{
    public interface IRepository
    {
        Task SaveTransaction(TransactionData trans);
        Task DeleteTransaction(string transactionId);
        Task BatchSaveTransaction(IEnumerable<TransactionData> trans);
    }

    public class DynamoDBRepository : IRepository
    {
        readonly DynamoDBContext _amazonDynamoDB = null;
        readonly IOptions<DynamoDBOptions> _options = null;

        public DynamoDBRepository(IAmazonDynamoDB amazonDynamoDB, IOptions<DynamoDBOptions> options)
        {
            _amazonDynamoDB = new DynamoDBContext(amazonDynamoDB);
            _options = options;
        }

        public async Task DeleteTransaction(string transactionId)
        {
            await _amazonDynamoDB.DeleteAsync(transactionId);
        }

        public async Task BatchSaveTransaction(IEnumerable<TransactionData> trans)
        {
            var writer = _amazonDynamoDB.CreateBatchWrite<TransactionData>();


            foreach (var tran in trans)
            {
                writer.AddPutItem(tran);
            }

            await writer.ExecuteAsync();
        }

        public async Task SaveTransaction(TransactionData trans)
        {
            await _amazonDynamoDB.SaveAsync(trans);
        }
    }
}
