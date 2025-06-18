using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace WildfireAgents.Services
{
    public class KeyVaultService
    {
        private readonly SecretClient _secretClient;

        public KeyVaultService(string keyVaultUrl)
        {
            _secretClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
        }

        public async Task<string> GetSecretAsync(string secretName)
        {
            KeyVaultSecret secret = await _secretClient.GetSecretAsync(secretName);
            return secret.Value;
        }
    }
}
