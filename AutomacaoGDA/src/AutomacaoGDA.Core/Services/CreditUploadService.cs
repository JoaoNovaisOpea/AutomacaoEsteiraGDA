using System.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AutomacaoGDA.Core.Interfaces;

namespace AutomacaoGDA.Core.Services;

public class CreditUploadService : ICreditUploadService
{
    private static readonly HttpClient HttpClient = new();
    private readonly IDatabaseService _databaseService;
    private readonly ILogService _logService;

    public CreditUploadService(IDatabaseService databaseService, ILogService logService)
    {
        _databaseService = databaseService;
        _logService = logService;
    }

    public async Task<OAuthTokenResponse> AuthenticateAsync(string urlLogin, string clientId, string clientSecret)
    {
        var url = $"{urlLogin.TrimEnd('/')}/oauth/token";
        _logService.Log($"Iniciando autenticação OAuth em: {url}");
        _logService.Log($"ClientId: {clientId}");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var formData = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "grant_type", "client_credentials" },
                { "scope", "openid" }
            };

            request.Content = new FormUrlEncodedContent(formData);

            using var response = await HttpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logService.LogHttp("POST", url, (int)response.StatusCode,
                $"grant_type=client_credentials&client_id={clientId}&scope=openid",
                responseBody);

            response.EnsureSuccessStatusCode();

            var jsonDoc = JsonDocument.Parse(responseBody);
            var root = jsonDoc.RootElement;

            _logService.Log("Autenticação OAuth concluída com sucesso");

            return new OAuthTokenResponse(
                root.GetProperty("access_token").GetString() ?? string.Empty,
                root.GetProperty("token_type").GetString() ?? string.Empty,
                root.GetProperty("expires_in").GetInt32()
            );
        }
        catch (Exception ex)
        {
            _logService.LogError($"Erro na autenticação OAuth", ex);
            throw;
        }
    }

    public async Task<AssignmentResponse> CreateAssignmentAsync(
        string urlBase,
        string bearerToken,
        Guid fundingId,
        string assignorId,
        string externalId)
    {
        var url = $"{urlBase.TrimEnd('/')}/assignments";
        _logService.Log($"Criando cessão em: {url}");
        _logService.Log($"FundingId: {fundingId}, AssignorId: {assignorId}, ExternalId: {externalId}");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var token = NormalizeBearerToken(bearerToken);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                _logService.Log($"Authorization header: Bearer {token.Substring(0, Math.Min(10, token.Length))}...");
            }
            else
            {
                _logService.LogError("AVISO: Bearer token está vazio!");
            }

            var requestBody = new
            {
                fundingId = fundingId,
                assignorId = assignorId,
                externalId = externalId
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            using var response = await HttpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logService.LogHttp("POST", url, (int)response.StatusCode, jsonContent, responseBody);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = $"HTTP {(int)response.StatusCode}";
                try
                {
                    var errorDoc = JsonDocument.Parse(responseBody);
                    if (errorDoc.RootElement.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
                    {
                        var firstError = errors[0];
                        if (firstError.TryGetProperty("message", out var message))
                        {
                            errorMessage = message.GetString() ?? errorMessage;
                        }
                    }
                }
                catch { }

                throw new HttpRequestException($"{errorMessage} (Status Code: {(int)response.StatusCode})");
            }

            var jsonDoc = JsonDocument.Parse(responseBody);
            var root = jsonDoc.RootElement;

            var assignmentId = Guid.Parse(root.GetProperty("id").GetString() ?? Guid.Empty.ToString());
            _logService.Log($"Cessão criada com sucesso. ID: {assignmentId}");

            return new AssignmentResponse(
                assignmentId,
                root.GetProperty("status").GetString() ?? string.Empty,
                root.GetProperty("statusDescription").GetString() ?? string.Empty
            );
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logService.LogError($"Erro ao criar cessão", ex);
            throw;
        }
    }

    public async Task<string> ReplaceAssignmentIdInJsonAsync(string jsonContent, Guid newAssignmentId)
    {
        return await Task.Run(() =>
        {
            var jsonNode = JsonNode.Parse(jsonContent);
            if (jsonNode == null)
            {
                throw new InvalidOperationException("Failed to parse JSON content.");
            }

            ReplaceAssignmentIdInNode(jsonNode, newAssignmentId);

            return jsonNode.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        });
    }

    public async Task TriggerWorkerAsync(string urlBase, string bearerToken, string fileUrl, string workerType, string id)
    {
        var url = $"{urlBase.TrimEnd('/')}/aatestworkers";
        _logService.Log($"Disparando worker: {workerType} para ID: {id}");
        if (!string.IsNullOrWhiteSpace(fileUrl))
        {
            _logService.Log($"FileUrl: {fileUrl}");
        }

        const int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

                var token = NormalizeBearerToken(bearerToken);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    if (attempt == 1)
                    {
                        _logService.Log($"Authorization header: Bearer {token.Substring(0, Math.Min(10, token.Length))}...");
                    }
                }

                using var form = new MultipartFormDataContent();

                if (!string.IsNullOrWhiteSpace(fileUrl))
                {
                    form.Add(new StringContent(fileUrl), "FileUrl");
                }

                form.Add(new StringContent(workerType), "WorkerType");
                form.Add(new StringContent(id), "Id");

                request.Content = form;

                using var response = await HttpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                _logService.LogHttp("POST", url, (int)response.StatusCode,
                    $"WorkerType={workerType}, Id={id}, FileUrl={fileUrl}",
                    responseBody);

                response.EnsureSuccessStatusCode();
                _logService.Log($"Worker {workerType} disparado com sucesso");
                return;
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                var delayMs = attempt * 2000;
                _logService.LogError($"Tentativa {attempt}/{maxRetries} falhou. Aguardando {delayMs}ms antes de tentar novamente", ex);
                await Task.Delay(delayMs);
            }
            catch (Exception ex)
            {
                _logService.LogError($"Erro ao disparar worker {workerType}", ex);
                throw;
            }
        }
    }

    public async Task<List<string>> GetBatchIdsAsync(string ambiente, Guid assignmentId)
    {
        var sql = $"SELECT * FROM AcquisitionBatch WHERE AcquisitionId = '{assignmentId}' AND DeletedOn IS NULL ORDER BY UpdatedOn";

        _logService.Log($"Executando consulta SQL no ambiente '{ambiente}'");
        _logService.Log($"SQL: {sql}");

        var table = await _databaseService.ExecutarConsulta(sql, ambiente);

        _logService.Log($"Consulta retornou {table.Rows.Count} linha(s)");
        _logService.Log($"Colunas da tabela: {string.Join(", ", table.Columns.Cast<DataColumn>().Select(c => $"{c.ColumnName} ({c.DataType.Name})"))}");

        var batchIds = new List<string>();
        foreach (DataRow row in table.Rows)
        {
            // Log all column values for debugging
            _logService.Log("=== Valores de todas as colunas ===");
            foreach (DataColumn col in table.Columns)
            {
                var value = row[col.ColumnName];
                var isDbNull = value == DBNull.Value;
                _logService.Log($"  {col.ColumnName}: '{(isDbNull ? "NULL" : value)}' ({col.DataType.Name})");
            }

            // Get the integer Id column (primary key) for batch processing
            if (table.Columns.Contains("Id"))
            {
                var idValue = row["Id"];
                if (idValue != null && idValue != DBNull.Value)
                {
                    var batchId = idValue.ToString();
                    batchIds.Add(batchId);
                    _logService.Log($"Batch ID (inteiro) encontrado: {batchId}");
                }
                else
                {
                    _logService.LogError("Coluna Id é null ou DBNull");
                }
            }
            else
            {
                _logService.LogError("Coluna Id não encontrada na tabela");
            }
        }

        _logService.Log($"Total de batch IDs válidos: {batchIds.Count}");
        return batchIds;
    }

    public async Task CloseAssignmentAsync(string urlOriginacao, string bearerToken, Guid assignmentId, int batchCount)
    {
        _logService.Log($"Fechando cessão. AssignmentId: {assignmentId}, BatchCount: {batchCount}");

        try
        {
            var eventData = new
            {
                AggregateType = "Assignment",
                AggregateId = assignmentId.ToString(),
                AssignmentId = assignmentId.ToString(),
                BatchCount = batchCount
            };

            var jsonString = JsonSerializer.Serialize(eventData);
            var jsonEncoded = Uri.EscapeDataString(jsonString);

            var url = $"{urlOriginacao.TrimEnd('/')}/aatestworkers/events?Json={jsonEncoded}&EventType=CLOSE_PACKAGE";
            _logService.Log($"URL completa: {url}");

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            var token = NormalizeBearerToken(bearerToken);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                _logService.Log($"Authorization header: Bearer {token.Substring(0, Math.Min(10, token.Length))}...");
            }

            request.Content = new StringContent(string.Empty);

            using var response = await HttpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logService.LogHttp("POST", url, (int)response.StatusCode,
                jsonString,
                responseBody);

            response.EnsureSuccessStatusCode();
            _logService.Log("Cessão fechada com sucesso");
        }
        catch (Exception ex)
        {
            _logService.LogError($"Erro ao fechar cessão", ex);
            throw;
        }
    }

    private static void ReplaceAssignmentIdInNode(JsonNode node, Guid newAssignmentId)
    {
        if (node is JsonObject obj)
        {
            foreach (var kvp in obj.ToList())
            {
                if (kvp.Key.Equals("AssignmentId", StringComparison.OrdinalIgnoreCase))
                {
                    obj[kvp.Key] = newAssignmentId.ToString();
                }
                else if (kvp.Value != null)
                {
                    ReplaceAssignmentIdInNode(kvp.Value, newAssignmentId);
                }
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item != null)
                {
                    ReplaceAssignmentIdInNode(item, newAssignmentId);
                }
            }
        }
    }

    private static string NormalizeBearerToken(string bearerToken)
    {
        var token = bearerToken?.Trim() ?? string.Empty;
        const string bearerPrefix = "Bearer ";

        if (token.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            token = token.Substring(bearerPrefix.Length).Trim();
        }

        return token;
    }
}
