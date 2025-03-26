using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Options;
using SampleApi.Elastic.Configuration;
using SampleApi.Elastic.Models;

namespace SampleApi.Elastic.Services
{
    public class ElasticService : IElasticService
    {

        private readonly ElasticsearchClient _client;
        private readonly ElasticSettings _elasticSettings;

        public ElasticService( IOptions<ElasticSettings> optionsMonitor)
        {
            _elasticSettings = optionsMonitor.Value;

            var settings = new ElasticsearchClientSettings(new Uri(_elasticSettings.Url))
                //.Authentication(_elasticSettings.Username, _elasticSettings.Password)
                .DefaultIndex(_elasticSettings.DefaultIndex);
            _client =new ElasticsearchClient(settings);

        }

        public async Task<bool> AddOrUpdateBulkAsync(IEnumerable<User> users, string indexName)
        {
            var response = await _client.BulkAsync(b => b.Index(_elasticSettings.DefaultIndex).UpdateMany(users, (ud, u)=> ud.Doc(u).DocAsUpsert(true)));

            return response.IsValidResponse;
        }

        public async Task<bool> AddOrUpdateUserAsync(User user)
        {
            var response = await _client.IndexAsync<User>(user, idx => idx.Index(_elasticSettings.DefaultIndex).OpType(OpType.Index));

            return response.IsValidResponse;
        }

        public async Task CreateIndexIfNotExistsAsync(string indexName)
        {
            var existsResponse = await _client.Indices.ExistsAsync(indexName);
            if (!existsResponse.Exists)
            {
                await _client.Indices.CreateAsync(indexName);
            }
        }

        public async Task<User> Get(string key)
        {
            var response = await _client.GetAsync<User>(key, idx => idx.Index(_elasticSettings.DefaultIndex));

            return response.Source!;
        }

        public async Task<List<User>?> GetAll()
        {
            var response = await _client.SearchAsync<User>(s => s.Index(_elasticSettings.DefaultIndex));

            return response.IsValidResponse ? response.Documents.ToList() : default;
        }

        public async Task<long?> RemoveAllUsersAsync()
        {
           var response = await _client.DeleteByQueryAsync<User>(d => d.Indices(_elasticSettings.DefaultIndex));

            return response.IsValidResponse ? response.Deleted : default;
        }

        public async Task<bool> RemoveUserAsync(string key)
        {
            var response = await _client.DeleteAsync<User>(key, idx => idx.Index(_elasticSettings.DefaultIndex));

            return response.IsValidResponse ;
        }
    }
}
