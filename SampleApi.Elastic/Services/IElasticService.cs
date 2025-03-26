using SampleApi.Elastic.Models;

namespace SampleApi.Elastic.Services
{
    public interface IElasticService
    {
        // Create index
        Task CreateIndexIfNotExistsAsync(string indexName);

        // add or update User
        Task<bool> AddOrUpdateUserAsync(User user);

        // add or update user bulk
        Task<bool> AddOrUpdateBulkAsync(IEnumerable<User> users, string indexName);


        // Get User
        Task<User> Get(string key);

        // Get All Users
        Task<List<User>?> GetAll();

        // Remove User
        Task<bool> RemoveUserAsync(string key);

        // Remove All Users
        Task<long?> RemoveAllUsersAsync();
    }
}
