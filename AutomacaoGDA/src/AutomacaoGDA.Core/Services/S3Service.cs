using System.Text;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using AutomacaoGDA.Core.Interfaces;

namespace AutomacaoGDA.Core.Services;

public class S3Service : IS3Service
{
    public async Task<string> UploadJsonFileAsync(
        string jsonContent,
        string fileName,
        string folderPath,
        string region,
        string accessKeyId,
        string secretAccessKey,
        string bucketName)
    {
        var credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
        var regionEndpoint = RegionEndpoint.GetBySystemName(region);
        var config = new AmazonS3Config { RegionEndpoint = regionEndpoint };

        using var s3Client = new AmazonS3Client(credentials, config);
        using var fileTransferUtility = new TransferUtility(s3Client);

        var key = $"{folderPath.TrimEnd('/')}/{fileName}";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));
        await fileTransferUtility.UploadAsync(stream, bucketName, key);

        return $"https://{bucketName}.s3.{region}.amazonaws.com/{key}";
    }
}
