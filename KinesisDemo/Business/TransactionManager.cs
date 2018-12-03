using Amazon.Lambda.Core;
using Amazon.Lambda.KinesisEvents;
using KinesisDemo.Business.Response;
using KinesisDemo.Services.Repository;
using KinesisDemo.Services.StreamMessage;
using KinesisDemo.Services.Aws.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace KinesisDemo.Business
{
    public interface ITransactionManager
    {
        Task<SaveTransactionResponse> PublishTransactionStreamToKinesis(MemoryStream msTransaction);
        Task<SaveTransactionResponse> SaveTransactionStream(TransactionData transaction);
        Task<SaveTransactionResponse> SaveTransactionStream(MemoryStream msTransaction);
        Task<List<DataRecord>> GetTransactionStreamFromKinesis();
    }

    public class TransactionManager : ITransactionManager
    {
        readonly IStreamMessageProducer _streamProducer = null;
        readonly IRepository _repository = null;
        readonly TransactionManagerOptions _transactionManagerOptions = null;
        readonly ILogger<TransactionManager> _logger = null;
        readonly IStreamMessageComsumer _streamComsumer = null;
        public TransactionManager(
            IStreamMessageProducer streamProducer,
            IRepository repository,
            IOptions<TransactionManagerOptions> options,
            ILogger<TransactionManager> logger,
            IStreamMessageComsumer streamMessageComsumer)
        {
            _streamProducer = streamProducer;
            _repository = repository;
            _transactionManagerOptions = options.Value;
            _logger = logger;
            _streamComsumer = streamMessageComsumer;
        }

        public async Task<SaveTransactionResponse> SaveTransactionStream(TransactionData transaction)
        {
            var response = new SaveTransactionResponse
            {
                Message = "Save successfully",
                ResponseCode = "200"
            };

            try
            {
                await _repository.SaveTransaction(transaction);
                _logger.Log(LogLevel.Information, "object saved in DynamoDB", transaction);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex.Message);
                response.Message = "Save is failed, Please check log for the details";
                response.ResponseCode = "500";
            }
            return response;
        }
        public async Task<SaveTransactionResponse> SaveTransactionStream(MemoryStream msTransaction)
        {
            msTransaction.Position = 0;
            var response = new SaveTransactionResponse
            {
                Message = "Save successfully",
                ResponseCode = "200"
            };

            try
            {
                var transaction = DeserializeToObject<TransactionData>(msTransaction);
                msTransaction.Dispose();
                await _repository.SaveTransaction(transaction);
                _logger.Log(LogLevel.Information, "object saved in DynamoDB", transaction);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex.Message);
                response.Message = "Save is failed, Please check log for the details";
                response.ResponseCode = "500";
            }
            return response;
        }
        public async Task<SaveTransactionResponse> PublishTransactionStreamToKinesis(MemoryStream msTransaction)
        {
            msTransaction.Position = 0;
            var response = new SaveTransactionResponse
            {
                Message = "Save successfully",
                ResponseCode = "200"
            };
            var transaction = DeserializeToObject<TransactionData>(msTransaction);
            try
            {
                await _streamProducer.PublishStream(_transactionManagerOptions.Topic, msTransaction);
                msTransaction.Dispose();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex.Message);
                response.Message = "Save is failed, Please check log for the details";
                response.ResponseCode = "500";
            }
            return response;
        }
        private T DeserializeToObject<T>(Stream stream) where T : class
        {
            var obj = default(T);
            var json = string.Empty;
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                stream.CopyTo(ms);
                ms.Position = 0;
                json = Encoding.Default.GetString(ms.ToArray());
            }
            using (var sr = new StringReader(json))
            using (var jr = new JsonTextReader(sr))
            {
                var js = new JsonSerializer();
                obj = js.Deserialize<T>(jr);
            }
            return obj;
        }
        public async Task<List<DataRecord>> GetTransactionStreamFromKinesis()
        {
            var records = await _streamComsumer.GetStream(_transactionManagerOptions.Topic);
            if (records?.Count == 0)
            {
                _logger.Log(LogLevel.Error, $"record is not found in Kinesis, Topic {_transactionManagerOptions.Topic}");
            }

            _logger.Log(LogLevel.Information, "kinesis received the stream and the stream will be saved in db");

            return records;
        }
    }
}
