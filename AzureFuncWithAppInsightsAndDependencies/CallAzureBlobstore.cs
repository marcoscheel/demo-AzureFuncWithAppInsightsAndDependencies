using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureFuncWithAppInsightsAndDependencies
{
    public static class CallAzureBlobstore
    {
        [FunctionName("CallAzureBlobstore")]
        public static async void Run([TimerTrigger("0 */5 * * * *", RunOnStartup = true)]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddEnvironmentVariables();
            var cfg = configBuilder.Build();

            var storageAccount = CloudStorageAccount.Parse(cfg["AzureWebJobsStorage"]);

            var blobClient = storageAccount.CreateCloudBlobClient();

            var containerName = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmm");

            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            if (!await container.ExistsAsync()) //DEP Call 1
            {
                await container.CreateIfNotExistsAsync(); //DEP Call 2
            }

            var iterations = 100;
            for (int i = 0; i < iterations; i++) //Each iteration will add 2 DEP calls
            {
                var blockBlob = container.GetBlockBlobReference($"{i.ToString("000")}.txt");
                if (!await blockBlob.ExistsAsync()) //This check is only here to get an extra DEP call
                {
                    await blockBlob.UploadTextAsync("These are not the droids you are looking for!");
                    log.LogTrace($"{i} of {iterations} iterations");

                }
            }//with 100 iterations we did 200 DEP calls

            //Each function run should have 202 DEP calls
            log.LogInformation($"C# Timer trigger function finished with {iterations} iterations to this conatiner {containerName}");
        }
    }
}
