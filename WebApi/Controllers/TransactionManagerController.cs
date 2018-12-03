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
            var response = await _transactionManager.SaveTransactionStream(transaction);

            return response.Message;

        }

        [HttpPost]
        [Route("/savetransaction/stream")]
        public async Task<string> SaveRawStreamToDb()
        {
            var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms);
            var response = await _transactionManager.SaveTransactionStream(ms);

            return response.Message;

        }

        [HttpPost]
        [Route("/publishtransaction")]
        public async Task<string> PublishTransactionToStreamQueuee()
        {
            MemoryStream memoryStream = new MemoryStream();
            await Request.Body.CopyToAsync(memoryStream);
            var response = await _transactionManager.PublishTransactionStreamToKinesis(memoryStream);
            memoryStream.Dispose();
            return response.Message;
        }
    }
}
