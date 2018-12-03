using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace KinesisDemo.Services.Aws.Infrastructure
{
    /// <summary>
    /// Setup generic host in lambda to adapt DI, configuration etc.
    /// </summary>
    public abstract class LambdaStartup
    {
        //This is a special Lambda Execution Environment key, used to detect whether in a aws lambda context.
        //https://docs.aws.amazon.com/lambda/latest/dg/current-supported-versions.html
        private const string LambdaTaskRootKey = "LAMBDA_TASK_ROOT";

        public LambdaStartup()
        {
            //build .net core generic host, very like aspnet core WebHost but without HTTP pipeline.
            //SO that we can use the Microsoft.Extensions package(configuration, DI, and logging.) by the same way. 
            //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-2.1
            var host = BuildHost();

            Services = host.Services;
        }

        protected IServiceProvider Services { get; private set; }

        /// <summary>
        /// override this method if we want to configure the Host Configuration.
        /// Include(ApplicationName,EnvironmentName,ContentRootPath,Logging Configration)
        /// </summary>
        protected virtual void ConfigureHostConfiguration(IConfigurationBuilder configurationBuilder)
        {
            configurationBuilder
                .AddInMemoryCollection(new[]
                {//Use Development default
                    new KeyValuePair<string, string>(HostDefaults.EnvironmentKey,EnvironmentName.Development)
                })
                .AddEnvironmentVariables("transactionDemo_");
        }

        /// <summary>
        /// override this method if we want to configure the Applicatoin Configuration.
        /// </summary>
        protected virtual void ConfigureAppConfiguration(HostBuilderContext hostContext, IConfigurationBuilder configurationBuilder)
        {
            var env = hostContext.HostingEnvironment;

            configurationBuilder
                .AddJsonFile("settings.json", optional: true, reloadOnChange: false)
                .AddJsonFile($"settings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables("transactionDemo_"); ;
        }

        /// <summary>
        /// register service to container
        /// </summary>
        /// <param name="hostContext"></param>
        /// <param name="services"></param>
        protected virtual void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
        }

        /// <summary>
        ///  override this method if we want to configure the Logging.
        ///  Default is use cloudwatch for lambda and Console for local 
        /// </summary>
        protected virtual void ConfigureLogging(HostBuilderContext hostContext, ILoggingBuilder logging)
        {
            AWSXRayRecorder.InitializeInstance(hostContext.Configuration);
            AWSSDKHandler.RegisterXRayForAllServices();

            if (IsLambda())
            {
                var loggerOptions = new LambdaLoggerOptions(hostContext.Configuration);
                logging.AddLambdaLogger(loggerOptions);
            }
            else
            {
                logging.AddConsole();
            }
        }


        public bool IsLambda()
        {
            //Copy from XRay SDK source code.
            var lambdaTaskRootKey = Environment.GetEnvironmentVariable(LambdaTaskRootKey);
            return !string.IsNullOrEmpty(lambdaTaskRootKey);
        }

        /// <summary>
        /// this is for IntegrationTests to stop host
        /// </summary>
        public void StopHost()
        {
            var applicationLifetime = Services.GetRequiredService<IApplicationLifetime>();
            applicationLifetime.StopApplication();
        }

        //build the generic host.
        private IHost BuildHost()
        {
            var host = new HostBuilder()
                              .ConfigureHostConfiguration(ConfigureHostConfiguration)
                              .ConfigureAppConfiguration(ConfigureAppConfiguration)
                              .ConfigureServices(ConfigureServices)
                              .ConfigureLogging(ConfigureLogging)
                              .Build();

            if (!IsLambda())
            {// The AWS Lambda execution environment creates a Segment before the Lambda function is invoked, 
             //so a Segment cannot be created inside the Lambda function.
                AWSXRayRecorder.Instance.BeginSegment("segment name"); // generates `TraceId` for you
            }



            //In normal console app, we should call WaitForShutdown to block process to shoutdown and waiting for CTRL+C. These methods will block current thread.
            //AWS lamda not a real console app, The applicationLifetime and hostLifetime is controlled by AWS. So it's more like a library.
            //So We stop and dispose the host when AWS lambda stop the application.
            var applicationLifetime = host.Services.GetRequiredService<IApplicationLifetime>();
            applicationLifetime.ApplicationStopping.Register(() =>
            {
                host.StopAsync().GetAwaiter().GetResult();
                host.Dispose();
                if (!IsLambda())
                    AWSXRayRecorder.Instance.EndSegment();
            });

            host.Start();
            return host;
        }


    }
}
