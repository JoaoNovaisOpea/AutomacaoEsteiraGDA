namespace AutomacaoGDA.Core.Interfaces;

public record OAuthTokenResponse(string AccessToken, string TokenType, int ExpiresIn);

public record AssignmentResponse(Guid Id, string Status, string StatusDescription);

public interface ICreditUploadService
{
    Task<OAuthTokenResponse> AuthenticateAsync(string urlLogin, string clientId, string clientSecret);

    Task<AssignmentResponse> CreateAssignmentAsync(string urlBase, string bearerToken, Guid fundingId, string assignorId, string externalId);

    Task<string> ReplaceAssignmentIdInJsonAsync(string jsonContent, Guid newAssignmentId);

    Task TriggerWorkerAsync(string urlBase, string bearerToken, string fileUrl, string workerType, string id);

    Task<List<string>> GetBatchIdsAsync(string ambiente, Guid assignmentId);

    Task CloseAssignmentAsync(string urlOriginacao, string bearerToken, Guid assignmentId, int batchCount);
}
