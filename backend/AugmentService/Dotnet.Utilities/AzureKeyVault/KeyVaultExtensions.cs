using System;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Dotnet.Utilities.AzureKeyVault;

// https://github.com/Supertext/Supertext.Base/blob/develop/Supertext.Base.Security/Configuration/KeyVaultExtensions.cs
public static class KeyVaultExtensions
{
    /// <summary>
    /// Adds Azure key vault to the configuration for environments Staging and Production.
    /// For Development environments it adds user secrets to the Startup class.
    ///
    /// Configuration in appsettings.json is mandatory as:
    /// "KeyVault": {
    ///     "ReadSecretsFromKeyVault": true,
    ///     "KeyVaultName": "kv-ne-dev",
    ///     "AzureADApplicationId": "11111-111111-111111-111111",
    ///     "ClientSecret": "11111-111111-111111-111111",
    ///     "TenantId": "324234234-2222-4545-BF9F-234324234",
    ///     "AzureADCertThumbprint": "456a7sad6f54fasdf787a9sdf6",
    ///     "CertificateName": "kv-dev-supertext-ch"
    /// },
    /// "IsUsingManagedIdentity": false
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <returns></returns>
    public static IHostBuilder ConfigureKeyVaultAppConfiguration<TStartup>(this IHostBuilder hostBuilder) where TStartup : class
    {
        return hostBuilder.ConfigureAppConfiguration((context, config) =>
                                                     {                                                        
                                                        /*
                                                         if (context.HostingEnvironment.IsDevelopment())
                                                         {
                                                             config.AddUserSecrets<TStartup>();
                                                             return;
                                                         }
                                                         */
                                                         config.ConfigureConfigWithKeyVaultSecrets();
                                                     });
    }

    /// <summary>
    /// Adds secrets from the key vault to the IConfigurationBuilder.
    ///
    /// Configuration in appsettings.json is mandatory as:
    /// "KeyVault": {
    ///     "ReadSecretsFromKeyVault": true,
    ///     "KeyVaultName": "kv-ne-dev",
    /// },
    /// "IsUsingManagedIdentity": true
    /// </summary>
    /// <param name="config"></param>
    public static void ConfigureConfigWithKeyVaultSecrets(this IConfigurationBuilder config)
    {
        var builtConfig = config.Build();
        // var vaultConfigSection = builtConfig.GetSection("KeyVault");
        KeyVaultSettings keyVaultOptions = new();
        builtConfig.GetSection("KeyVault").Bind(keyVaultOptions);
        var options = keyVaultOptions;
        //var vaultUrl = $"https://{vaultConfigSection["KeyVaultName"]}.vault.azure.net/";
        var vaultUrl = $"https://{options.KeyVaultName}.vault.azure.net/";
        
        var isUsingManagedIdentity = builtConfig.GetValue<bool>("IsUsingManagedIdentity");
        
        if (isUsingManagedIdentity)
        {
            var secretClient = new SecretClient(new Uri(vaultUrl),
                                            new DefaultAzureCredential());
            config.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
        }
        else
        {
            // var readSecretsFromKeyVault = vaultConfigSection.GetValue<bool>("ReadSecretsFromKeyVault");
            var readSecretsFromKeyVault = (bool)options.ReadSecretsFromKeyVault;

            if (readSecretsFromKeyVault)
            {
                /*
                var clientId = vaultConfigSection["AzureADApplicationId"];
                var clientSecret = vaultConfigSection["ClientSecret"];
                var tenantId = vaultConfigSection["TenantId"];
                */
                var clientId = options.AzureADApplicationId;                
                var tenantId = options.TenantId;
                var clientSecret = options.ClientSecret;
                var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                var secretClient = new SecretClient(new Uri(vaultUrl), credential);

                config.AddAzureKeyVault(secretClient,
                                        new KeyVaultSecretManager());
            }
        }
    }
}
