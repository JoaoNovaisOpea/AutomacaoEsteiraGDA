namespace AutomacaoGDA.Core.Models;

public class OperationInfo
{
    public Guid Id { get; set; }
    public string FundName { get; set; } = string.Empty;
    public string? Status { get; set; }

    public string DisplayName => string.IsNullOrWhiteSpace(FundName) ? Id.ToString() : FundName;
}
