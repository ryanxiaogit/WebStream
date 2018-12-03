using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Kinesis;
using KinesisDemo.Business;
using KinesisDemo.Services.Repository;
using KinesisDemo.Services.StreamMessage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddSingleton<IRepository, DynamoDBRepository>();
            services.AddSingleton<IStreamMessageComsumer, StreamComsumer>();
            services.AddSingleton<IStreamMessageProducer, StreamProducer>();
            services.AddSingleton<ITransactionManager, TransactionManager>();
            services.AddAWSService<IAmazonDynamoDB>();
            services.AddAWSService<IAmazonKinesis>();

            
            services.Configure<DynamoDBOptions>(Configuration.GetSection("DynamoDBOptions"));
            services.Configure<StreamMessageOptions>(Configuration.GetSection("StreamMessage"));
            services.Configure<TransactionManagerOptions>(Configuration.GetSection("TransactionManager"));

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
            
        }
    }
}
