using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using System.IO;
using KinesisDemo.Business;
using KinesisDemo.Services.Aws.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Amazon.Lambda.KinesisEvents;
using Microsoft.Extensions.Logging;
using KinesisDemo.Business.Response;
using KinesisDemo.Services.Aws.Model;
using KinesisDemo.Services.StreamMessage;
using Microsoft.Extensions.Hosting;
using KinesisDemo.Services.Repository;
using Amazon.DynamoDBv2;
using Amazon.Kinesis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace KinesisDemo
{
    public class LambdaEntryPoint : LambdaStartup
    {
        readonly ITransactionManager _transactionManager = null;
        readonly ILogger<LambdaEntryPoint> _logger = null;

        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public LambdaEntryPoint()
        {
            _transactionManager = Services.GetRequiredService<ITransactionManager>();
            _logger = Services.GetRequiredService<ILogger<LambdaEntryPoint>>();
        }

        protected override void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            services.AddDefaultAWSOptions(hostContext.Configuration.GetAWSOptions());

            services.AddSingleton<IRepository, DynamoDBRepository>();
            services.AddSingleton<IStreamMessageComsumer, StreamComsumer>();
            services.AddSingleton<IStreamMessageProducer, StreamProducer>();
            services.AddSingleton<ITransactionManager, TransactionManager>();
            services.AddAWSService<IAmazonDynamoDB>();
            services.AddAWSService<IAmazonKinesis>();

            services.Configure<DynamoDBOptions>(hostContext.Configuration.GetSection("DynamoDBOptions"));
            services.Configure<StreamMessageOptions>(hostContext.Configuration.GetSection("StreamMessage"));
            services.Configure<TransactionManagerOptions>(hostContext.Configuration.GetSection("TransactionManager"));
        }

        public async Task<APIGatewayProxyResponse> SaveTransactionToDB(TransactionData request, ILambdaContext context)
        {
            var response = await _transactionManager.SaveTransactionStream(request);

            if (response.ResponseCode != "200")
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400
                };
            }

            return new APIGatewayProxyResponse
            {
                StatusCode = 200
            };
        }
        public async Task<APIGatewayProxyResponse> PublishTransactionToStreamQueuee([FromBody]Stream request, ILambdaContext context)
        {
            MemoryStream memoryStream = new MemoryStream();
            await request.CopyToAsync(memoryStream);
            await _transactionManager.PublishTransactionStreamToKinesis(memoryStream);
            memoryStream.Dispose();
            return new APIGatewayProxyResponse
            {
                StatusCode = 200
            };
        }

        /// <summary>
        /// lambda functin: triggered when kinesis receive stream
        /// </summary>
        /// <param name="kinesisEvent"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task OnKinesisTrigger(KinesisEvent kinesisEvent, ILambdaContext context)
        {
            //var record = kinesisEvent.Records.FirstOrDefault();
            //if (record == null)
            //{
            //    _logger.Log(LogLevel.Error, "record is not found in Kinesis", kinesisEvent);
            //    return;
            //}

            //_logger.Log(LogLevel.Information, "kinesis received the stream and the stream will be saved in db");
            //var response = await _transactionManager.SaveTransactionStream(record.Kinesis.Data);
            //if (response.ResponseCode != "200")
            //{
            //    _logger.Log(LogLevel.Error, "Triggered by kinesis, but failed to save data.", record.Kinesis.Data);
            //}
        }

        public async Task OnCloudWatchTrigger(CloudWatchEvent cloudWatchEvent, ILambdaContext context)
        {
            //var record = (await _transactionManager.GetTransactionStreamFromKinesis(cloudWatchEvent.ShardIterator)).FirstOrDefault();

            //_logger.Log(LogLevel.Information, "the stream will be saved in db");
            //var response = await _transactionManager.SaveTransactionStream(record.Data);
            //if (response.ResponseCode != "200")
            //{
            //    _logger.Log(LogLevel.Error, "Triggered by CloudWatch, but failed to save data.", record.Data);
            //}
        }
    }
}
