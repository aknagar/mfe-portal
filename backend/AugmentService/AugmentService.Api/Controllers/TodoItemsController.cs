using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections;

namespace AugmentService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TodoItemsController : ControllerBase
    {
        SecretClient? _secretClient;

        public TodoItemsController(SecretClient? secretClient)
        {
            _secretClient = secretClient;
        }

        // GET: api/TodoItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> GetTodoItems()
        {
            if (_secretClient == null)
            {
                return BadRequest("SecretClient is not available");
            }
            
            var secret = await _secretClient.GetSecretAsync("AspireTestSecret");
            var list = new List<string>
            {
                secret.Value.Value
            };
            return Ok(list);
        }
    }
}

