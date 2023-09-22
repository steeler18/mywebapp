using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string testKeyVault()
{
    SecretClientOptions options = new SecretClientOptions()
    {
        Retry =
            {
                Delay= TimeSpan.FromSeconds(2),
                MaxDelay = TimeSpan.FromSeconds(16),
                MaxRetries = 5,
                Mode = RetryMode.Exponential
            }
    };

    var cred = new DefaultAzureCredential();

    var client = new SecretClient(new Uri("https://ruixkeyvault2.vault.azure.net/"), cred, options);

    Console.WriteLine("I love Hailey and Caity");

    KeyVaultSecret secret = client.GetSecret("mySecret");

    string secretValue = secret.Value;

    return secretValue;
}


async Task<string> testBlobStorage()
{
    TokenCredential tokenCredential = new DefaultAzureCredential();

    BlobServiceClient serviceClient = new BlobServiceClient(new Uri("https://ruibinstorageaccount.blob.core.windows.net"), tokenCredential);

    UserDelegationKey userDelegationKey =
        await serviceClient.GetUserDelegationKeyAsync(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1));

    BlobContainerClient containerClient = serviceClient.GetBlobContainerClient("temp");
    var blobName = "2023-04-13 15.58.00.mp4";
    BlobClient blobClient = containerClient.GetBlobClient(blobName);
    //blobClient.SetTags(new Dictionary<string, string>() { { "ben", "1234" } });

    BlobSasBuilder sasBuilder = new BlobSasBuilder()
    {
        BlobContainerName = blobClient.BlobContainerName,
        BlobName = blobClient.Name,
        Resource = "b",
        StartsOn = DateTimeOffset.UtcNow,
        ExpiresOn = DateTimeOffset.UtcNow.AddDays(1)
    };

    // Specify the necessary permissions
    sasBuilder.SetPermissions(BlobSasPermissions.Read | BlobSasPermissions.Write | BlobSasPermissions.All);

    // Add the SAS token to the blob URI
    BlobUriBuilder uriBuilder = new BlobUriBuilder(blobClient.Uri)
    {
        // Specify the user delegation key
        Sas = sasBuilder.ToSasQueryParameters(
            userDelegationKey,
            blobClient.GetParentBlobContainerClient()
            .GetParentBlobServiceClient().AccountName)
    };

    BlobClient blobClientSAS = new BlobClient(uriBuilder.ToUri());
    //blobClientSAS.SetTags(new Dictionary<string, string>() { { "ben", "1234" } });

    return uriBuilder.ToUri().AbsoluteUri;

    // BlobContainerClient containerClient = serviceClient.GetBlobContainerClient("temp");

    // var blobName = "abcefg";
    // BlobClient blobClient = containerClient.GetBlobClient(blobName);

    // var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read | BlobSasPermissions.Write, DateTimeOffset.UtcNow.AddHours(2));

    // return sasUri.AbsoluteUri;

    // string containerName = "container-" + Guid.NewGuid();

    // try
    // {
    //     // Create the container
    //     BlobContainerClient container = await serviceClient.CreateBlobContainerAsync(containerName);

    //     if (await container.ExistsAsync())
    //     {
    //         Console.WriteLine("Created container {0}", container.Name);
    //         return "good";
    //     }
    // }
    // catch (RequestFailedException e)
    // {
    //     Console.WriteLine("HTTP error code {0}: {1}",
    //                         e.Status, e.ErrorCode);
    //     Console.WriteLine(e.Message);
    // }

    // return "bad";
}


//var s = await testBlobStorage();
var s = testKeyVault();

app.MapGet("/", () => s);

app.Run();
