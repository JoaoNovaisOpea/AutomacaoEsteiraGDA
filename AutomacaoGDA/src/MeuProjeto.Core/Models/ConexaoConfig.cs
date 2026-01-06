using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MeuProjeto.Core.Models;

public enum DatabaseProvider
{
    SqlServer
}

public class ConexaoConfig : INotifyPropertyChanged
{
    private string _ambiente = string.Empty;
    private string _connectionString = string.Empty;
    private string _urlBaseOriginacao = string.Empty;
    private string _urlLogin = string.Empty;
    private string _urlBase = string.Empty;
    private string _clientId = string.Empty;
    private string _clientSecret = string.Empty;
    private string _cedenteId = string.Empty;
    private string _regionS3Integracao = string.Empty;
    private string _accessKeyIdIntegracao = string.Empty;
    private string _secretAccessKeyIntegracao = string.Empty;
    private string _rootBucketIntegracao = string.Empty;
    private string _urlBaseS3 = string.Empty;
    private DatabaseProvider _provider = DatabaseProvider.SqlServer;
    private bool _isProduction = false;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Ambiente
    {
        get => _ambiente;
        set => SetField(ref _ambiente, value);
    }

    public string ConnectionString
    {
        get => _connectionString;
        set => SetField(ref _connectionString, value);
    }

    public string UrlBaseOriginacao
    {
        get => _urlBaseOriginacao;
        set => SetField(ref _urlBaseOriginacao, value);
    }

    public string UrlLogin
    {
        get => _urlLogin;
        set => SetField(ref _urlLogin, value);
    }

    public string UrlBase
    {
        get => _urlBase;
        set => SetField(ref _urlBase, value);
    }

    public string ClientId
    {
        get => _clientId;
        set => SetField(ref _clientId, value);
    }

    public string ClientSecret
    {
        get => _clientSecret;
        set => SetField(ref _clientSecret, value);
    }

    public string CedenteId
    {
        get => _cedenteId;
        set => SetField(ref _cedenteId, value);
    }

    public string RegionS3Integracao
    {
        get => _regionS3Integracao;
        set => SetField(ref _regionS3Integracao, value);
    }

    public string AccessKeyIDIntegracao
    {
        get => _accessKeyIdIntegracao;
        set => SetField(ref _accessKeyIdIntegracao, value);
    }

    public string SecretAccessKeyIntegracao
    {
        get => _secretAccessKeyIntegracao;
        set => SetField(ref _secretAccessKeyIntegracao, value);
    }

    public string RootBucketIntegracao
    {
        get => _rootBucketIntegracao;
        set => SetField(ref _rootBucketIntegracao, value);
    }

    public string UrlBaseS3
    {
        get => _urlBaseS3;
        set => SetField(ref _urlBaseS3, value);
    }

    [JsonPropertyName("BaseApiUrl")]
    public string? LegacyBaseApiUrl
    {
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(_urlBaseOriginacao))
            {
                _urlBaseOriginacao = value.Trim();
            }
        }
    }

    public DatabaseProvider Provider
    {
        get => _provider;
        set => SetField(ref _provider, value);
    }

    public bool IsProduction
    {
        get => _isProduction;
        set => SetField(ref _isProduction, value);
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
