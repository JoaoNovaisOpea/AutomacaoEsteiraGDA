using System.Text;
using MeuProjeto.Core.Interfaces;

namespace MeuProjeto.Core.Services;

public class LogService : ILogService
{
    private readonly string _logFilePath;
    private readonly object _lockObject = new();

    public LogService()
    {
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataFolder, "AutomacaoGDA");
        Directory.CreateDirectory(appFolder);

        var timestamp = DateTime.Now.ToString("yyyyMMdd");
        _logFilePath = Path.Combine(appFolder, $"upload_log_{timestamp}.txt");
    }

    public void Log(string message)
    {
        WriteToFile($"[INFO] {message}");
    }

    public void LogError(string message, Exception? ex = null)
    {
        var errorMessage = new StringBuilder();
        errorMessage.AppendLine($"[ERROR] {message}");

        if (ex != null)
        {
            errorMessage.AppendLine($"  Exception: {ex.GetType().Name}");
            errorMessage.AppendLine($"  Message: {ex.Message}");
            errorMessage.AppendLine($"  StackTrace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                errorMessage.AppendLine($"  InnerException: {ex.InnerException.Message}");
            }
        }

        WriteToFile(errorMessage.ToString());
    }

    public void LogHttp(string method, string url, int? statusCode, string? requestBody = null, string? responseBody = null)
    {
        var httpLog = new StringBuilder();
        httpLog.AppendLine($"[HTTP] {method} {MaskSensitiveUrl(url)}");

        if (!string.IsNullOrWhiteSpace(requestBody))
        {
            httpLog.AppendLine($"  Request Body: {MaskSensitiveData(requestBody)}");
        }

        if (statusCode.HasValue)
        {
            httpLog.AppendLine($"  Status Code: {statusCode}");
        }

        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            var truncatedResponse = responseBody.Length > 500
                ? responseBody.Substring(0, 500) + "... (truncated)"
                : responseBody;
            httpLog.AppendLine($"  Response: {MaskSensitiveData(truncatedResponse)}");
        }

        WriteToFile(httpLog.ToString());
    }

    public string GetLogFilePath() => _logFilePath;

    public void ClearLog()
    {
        lock (_lockObject)
        {
            try
            {
                if (File.Exists(_logFilePath))
                {
                    File.WriteAllText(_logFilePath, string.Empty);
                }
            }
            catch
            {
                // Falha silenciosa para não interromper o fluxo
            }
        }
    }

    private void WriteToFile(string message)
    {
        lock (_lockObject)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] {message}{Environment.NewLine}";
                File.AppendAllText(_logFilePath, logEntry);
            }
            catch
            {
                // Falha silenciosa para não interromper o fluxo
            }
        }
    }

    private string MaskSensitiveUrl(string url)
    {
        // Mascara tokens e secrets em URLs
        if (url.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("secret", StringComparison.OrdinalIgnoreCase))
        {
            return url.Length > 50 ? url.Substring(0, 50) + "... (masked)" : url;
        }
        return url;
    }

    private string MaskSensitiveData(string data)
    {
        // Mascara campos sensíveis em JSON/XML
        var masked = data;

        // Mascara tokens
        if (masked.Contains("access_token", StringComparison.OrdinalIgnoreCase))
        {
            masked = System.Text.RegularExpressions.Regex.Replace(
                masked,
                @"""access_token""\s*:\s*""([^""]+)""",
                @"""access_token"":""***MASKED***""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // Mascara client_secret
        if (masked.Contains("client_secret", StringComparison.OrdinalIgnoreCase))
        {
            masked = System.Text.RegularExpressions.Regex.Replace(
                masked,
                @"""client_secret""\s*:\s*""([^""]+)""",
                @"""client_secret"":""***MASKED***""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return masked;
    }
}
