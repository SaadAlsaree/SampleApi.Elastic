using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SampleApi.Elastic.Models;
using SampleApi.Elastic.Services;

namespace SampleApi.Elastic.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {

        private readonly ILogger<UsersController> _logger;
        private readonly IElasticService<User> _elasticService;

        public UsersController(ILogger<UsersController> logger, IElasticService<User> elasticService)
        {
            _logger = logger;
            _elasticService = elasticService;
        }


        [HttpPost("create-index")]
        public async Task<IActionResult> CreateIndex(string indexName)
        {
            await _elasticService.CreateIndexIfNotExistsAsync(indexName);
            return Ok($"Index {indexName} create or already exists.");
        }


        [HttpPost("add-user")]
        public async Task<IActionResult> AddUser([FromBody] User user)
        {
            var result = await _elasticService.IndexDocumentAsync(user);
            return result ? Ok($"User {user.FirstName + user.LastName} added or updated.") : StatusCode(500, "Error adding or update user.");
        }

        [HttpPut("update-users")]
        public async Task<IActionResult> UpdateUsers([FromBody] IEnumerable<User> users, string indexName)
        {
            var result = await _elasticService.BulkIndexAsync(users);
            return result ? Ok($"Users added or updated.") : StatusCode(500, "Error adding or update users.");
        }


        [HttpGet("get-user/{key}")]
        public async Task<IActionResult> GetUser(string key)
        {
            var user = await _elasticService.GetDocumentAsync(key);
            return user != null ? Ok(user) : NotFound("User not found.");
        }

        [HttpGet("get-all-users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _elasticService.GetAllAsync();
            return users != null ? Ok(users) : NotFound("No users found.");
        }

        [HttpDelete("delete-user/{key}")]
        public async Task<IActionResult> DeleteUser(string key)
        {
            var result = await _elasticService.DeleteDocumentAsync(key);
            return result ? Ok($"User {key} deleted.") : NotFound("User not found.");
        }

        [HttpDelete("delete-all-users")]
        public async Task<IActionResult> DeleteAllUsers()
        {
            try
            {
                var allUsers = await _elasticService.GetAllAsync();
                var ids = allUsers.Select(u => u.Id.ToString()).ToList();

                if (!ids.Any())
                    return NotFound("No users found.");

                var result = await _elasticService.BulkDeleteAsync(ids.Cast<string>());
                return result ? Ok($"Deleted {ids.Count} users.") : StatusCode(500, "Error deleting users.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all users");
                return StatusCode(500, "Error deleting all users");
            }
        }
    }
}
