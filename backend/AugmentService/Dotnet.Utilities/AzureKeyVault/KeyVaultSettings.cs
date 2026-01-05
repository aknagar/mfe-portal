namespace Dotnet.Utilities.AzureKeyVault;

public class KeyVaultSettings {

 public string? KeyVaultName { get; set; }

public string? AzureADApplicationId { get; set; }

public string? TenantId {get; set;}

public string? ClientSecret {get; set;}

public bool? ReadSecretsFromKeyVault {get; set;}

}