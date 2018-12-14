using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KinesisDemo.Business;
using KinesisDemo.Services.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionManagerController : ControllerBase
    {

        readonly ITransactionManager _transactionManager = null;
        readonly ILogger<TransactionManagerController> _logger = null;

        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public TransactionManagerController(
            ITransactionManager transactionManager,
            ILogger<TransactionManagerController> logger
            )
        {
            _transactionManager = transactionManager;
            _logger = logger;
        }

        [HttpPost]
        [Route("/savetransaction")]
        public async Task<string> SaveTransactionToDB(TransactionData transaction)
        {
            var start = DateTime.Now;
            var response = await _transactionManager.SaveTransactionStream(transaction);
            var timespan = DateTime.Now - start;
            System.IO.File.AppendAllText("C:/Document/Report/Normal.csv", $"{timespan.TotalSeconds.ToString("0.0000000000000000")},{response.Message},{transaction.TransactionID}\r");
            return response.Message;
        }

        [HttpPost]
        [Route("/savetransaction/stream")]
        public async Task<string> SaveRawStreamToDb()
        {
            var start = DateTime.Now;
            var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms);
            var response = await _transactionManager.SaveTransactionStream(ms);
            var timespan = DateTime.Now - start;
            System.IO.File.AppendAllText("C:/Document/Report/Stream.csv", $"{timespan.TotalSeconds.ToString("0.0000000000000000")},{response.Message},Stream\r");
            return response.Message;
        }

        [HttpPost]
        [Route("/PublishTransactionToStreamQueuee")]
        public async Task<string> PublishTransactionToStreamQueuee()
        {
            MemoryStream memoryStream = new MemoryStream();
            await Request.Body.CopyToAsync(memoryStream);
            var response = await _transactionManager.PublishTransactionStreamToKinesis(memoryStream);
            memoryStream.Dispose();
            return response.Message;
        }

        [HttpPost]
        [Route("/SaveDataFromStream/{shardId}/{sequenceNumber}")]
        public async Task<string> SaveStreamFromKinesis(string shardId, string sequenceNumber)
        {
            var record = (await _transactionManager.GetTransactionStreamFromKinesis(shardId, sequenceNumber)).FirstOrDefault();

            _logger.Log(LogLevel.Information, "the stream will be saved in db");
            var response = await _transactionManager.SaveTransactionStream(record.Data);
            if (response.ResponseCode != "200")
            {
                _logger.Log(LogLevel.Error, "Triggered by CloudWatch, but failed to save data.", record.Data);
            }

            return response.Message + " " + response.ResponseCode;
        }

        [HttpPost]
        [Route("/SaveTransactionToDBThroughStreamQueue")]
        public async Task<string> SaveTransactionToDBThroughStreamQueue()
        {
            var start = DateTime.Now;
            MemoryStream memoryStream = new MemoryStream();
            await Request.Body.CopyToAsync(memoryStream);
            var response = await _transactionManager.PublishTransactionStreamToKinesis(memoryStream);
            memoryStream.Dispose();
            var result = await SaveStreamFromKinesis(response.ShardId, response.SequenceNumber);
            var timespan = DateTime.Now - start;

            System.IO.File.AppendAllText("C:/Document/Report/StreamQueue.csv", $"{timespan.TotalSeconds.ToString("0.0000000000000000")},{result},{response.ShardId}\r");

            return result;
        }
    }
}
