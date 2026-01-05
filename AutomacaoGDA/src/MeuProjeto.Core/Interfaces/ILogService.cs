namespace MeuProjeto.Core.Interfaces;

public interface ILogService
{
    void Log(string message);
    void LogError(string message, Exception? ex = null);
    void LogHttp(string method, string url, int? statusCode, string? requestBody = null, string? responseBody = null);
    string GetLogFilePath();
    void ClearLog();
}
