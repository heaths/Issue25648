using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Issue25648.Controllers
{
    [ApiController]
    [Route("secrets")]
    public class SecretsController : ControllerBase
    {
        private readonly SecretClient _secretClient;

        public SecretsController(SecretClient secretClient)
        {
            _secretClient = secretClient;
        }

        // GET: <SecretsController>
        [HttpGet]
        public async IAsyncEnumerable<Secret> Get()
        {
            await foreach (SecretProperties secret in _secretClient.GetPropertiesOfSecretsAsync())
            {
                yield return new Secret(secret.Name)
                {
                    Version = secret.Version,
                };
            }
        }

        // GET <SecretsController>/{name}
        [HttpGet("{name}")]
        public async Task<ActionResult<Secret>> Get(string name)
        {
            try
            {
                KeyVaultSecret secret = await _secretClient.GetSecretAsync(name);
                return Ok(new Secret(secret.Name)
                {
                    Version = secret.Properties.Version,
                    Value = secret.Value,
                });
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return NotFound();
            }
        }
    }
}
