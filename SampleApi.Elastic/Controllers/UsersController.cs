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
        private readonly IElasticService _elasticService;

        public UsersController(ILogger<UsersController> logger, IElasticService elasticService)
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
            var result = await _elasticService.AddOrUpdateUserAsync(user);
            return result ? Ok($"User {user.FirstName + user.LastName} added or updated.") : StatusCode(500, "Error adding or update user.");
        }

        [HttpPut("update-users")]
        public async Task<IActionResult> UpdateUsers([FromBody] IEnumerable<User> users, string indexName)
        {
            var result = await _elasticService.AddOrUpdateBulkAsync(users, indexName);
            return result ? Ok($"Users added or updated.") : StatusCode(500, "Error adding or update users.");
        }


        [HttpGet("get-user/{key}")]
        public async Task<IActionResult> GetUser(string key)
        {
            var user = await _elasticService.Get(key);
            return user != null ? Ok(user) : NotFound("User not found.");
        }

        [HttpGet("get-all-users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _elasticService.GetAll();
            return users != null ? Ok(users) : NotFound("No users found.");
        }

        [HttpDelete("delete-user/{key}")]
        public async Task<IActionResult> DeleteUser(string key)
        {
            var result = await _elasticService.RemoveUserAsync(key);
            return result ? Ok($"User {key} deleted.") : NotFound("User not found.");
        }

        [HttpDelete("delete-all-users")]
        public async Task<IActionResult> DeleteAllUsers()
        {
            var result = await _elasticService.RemoveAllUsersAsync();
            return result != null ? Ok($"Deleted {result} users.") : NotFound("No users found.");

        }
    }
}
