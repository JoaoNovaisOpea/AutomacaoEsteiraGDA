namespace AutomacaoGDA.Core.Interfaces;

public interface IS3Service
{
    Task<string> UploadJsonFileAsync(
        string jsonContent,
        string fileName,
        string folderPath,
        string region,
        string accessKeyId,
        string secretAccessKey,
        string bucketName);
}
