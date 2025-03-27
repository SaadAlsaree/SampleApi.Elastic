using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Options;
using SampleApi.Elastic.Configuration;
using System.Linq.Expressions;

namespace SampleApi.Elastic.Services
{
    public class ElasticService<T> : IElasticService<T> where T : class
    {
        private readonly ElasticsearchClient _client;
        private readonly ElasticSettings _elasticSettings;

        public ElasticService(IOptions<ElasticSettings> optionsMonitor)
        {
            _elasticSettings = optionsMonitor.Value;

            var settings = new ElasticsearchClientSettings(new Uri(_elasticSettings.Url))
                //.Authentication(_elasticSettings.Username, _elasticSettings.Password)
                .DefaultIndex(_elasticSettings.DefaultIndex);
            _client = new ElasticsearchClient(settings);
        }

        public async Task<bool> BulkDeleteAsync(IEnumerable<string> ids)
        {
            var bulkRequest = new BulkRequestDescriptor();
            foreach (var id in ids)
            {
                bulkRequest.Delete<T>(d => d.Index(_elasticSettings.DefaultIndex).Id(id));
            }

            var response = await _client.BulkAsync(bulkRequest);
            return response.IsValidResponse;
        }

        public async Task<bool> BulkIndexAsync(IEnumerable<T> documents)
        {
            var response = await _client.BulkAsync(b => b
                .Index(_elasticSettings.DefaultIndex)
                .IndexMany(documents));

            return response.IsValidResponse;
        }

        public async Task<bool> BulkUpdateAsync(IEnumerable<(string id, T document)> documents)
        {
            var bulkRequest = new BulkRequestDescriptor();
            foreach (var (id, document) in documents)
            {
                bulkRequest.Update<T, T>(u => u
                    .Index(_elasticSettings.DefaultIndex)
                    .Id(id)
                    .Doc(document)
                    .DocAsUpsert(true));
            }

            var response = await _client.BulkAsync(bulkRequest);
            return response.IsValidResponse;
        }

        public async Task<long> CountAsync(Func<QueryDescriptor<T>, QueryDescriptor<T>>? queryBuilder = null)
        {
            if (queryBuilder != null)
            {
                var queryDescriptor = new QueryDescriptor<T>();
                var builtQuery = queryBuilder(queryDescriptor);
                var response = await _client.CountAsync<T>(c => c.Query(builtQuery));
                return response.IsValidResponse ? response.Count : 0;
            }
            else
            {
                var response = await _client.CountAsync<T>();
                return response.IsValidResponse ? response.Count : 0;
            }
        }

        public async Task CreateIndexIfNotExistsAsync(string indexName)
        {
            var existsResponse = await _client.Indices.ExistsAsync(indexName);
            if (!existsResponse.Exists)
            {
                await _client.Indices.CreateAsync(indexName);
            }
        }

        public async Task<bool> DeleteDocumentAsync(string id)
        {
            var response = await _client.DeleteAsync<T>(id, d => d.Index(_elasticSettings.DefaultIndex));
            return response.IsValidResponse;
        }

        public async Task DeleteIndexAsync(string indexName)
        {
            await _client.Indices.DeleteAsync(indexName);
        }

        public async Task<IReadOnlyCollection<T>> FuzzySearchAsync(string field, string value, int fuzziness = 1, int size = 10)
        {
            var response = await _client.SearchAsync<T>(s => s
                .Index(_elasticSettings.DefaultIndex)
                .Size(size)
                .Query(q => q
                    .Fuzzy(f => f
                        .Field(field!)
                        .Value(value)
                        .Fuzziness(new Fuzziness(fuzziness))
                    )
                )
            );

            return response.IsValidResponse ? response.Documents : new List<T>();
        }

        public async Task<IReadOnlyCollection<T>> GetAllAsync(int size = 1000, int from = 0)
        {
            var response = await _client.SearchAsync<T>(s => s
                .Index(_elasticSettings.DefaultIndex)
                .Size(size)
                .From(from)
                
            );

            return response.IsValidResponse ? response.Documents : new List<T>();
        }

        public async Task<T?> GetDocumentAsync(string id)
        {
            var response = await _client.GetAsync<T>(id, g => g.Index(_elasticSettings.DefaultIndex));
            return response.IsValidResponse && response.Found ? response.Source : null;
        }

        public async Task<bool> IndexDocumentAsync(T document, string? id = null)
        {
            var request = new IndexRequestDescriptor<T>(document);
            request.Index(_elasticSettings.DefaultIndex);

            if (!string.IsNullOrEmpty(id))
            {
                request.Id(id);
            }

            var response = await _client.IndexAsync(request);
            return response.IsValidResponse;
        }

        public async Task<bool> IndexExistsAsync(string indexName)
        {
            var response = await _client.Indices.ExistsAsync(indexName);
            return response.Exists;
        }

        public async Task<IReadOnlyCollection<T>> MultiMatchAsync(string query, string[] fields, int size = 10)
        {
            var response = await _client.SearchAsync<T>(s => s
                .Index(_elasticSettings.DefaultIndex)
                .Size(size)
                .Query(q => q
                    .MultiMatch(mm => mm
                        .Query(query)
                        .Fields(fields)
                    )
                )
            );

            return response.IsValidResponse ? response.Documents : new List<T>();
        }

        public async Task<IReadOnlyCollection<T>> PrefixSearchAsync(string field, string prefix, int size = 10)
        {
            var response = await _client.SearchAsync<T>(s => s
                .Index(_elasticSettings.DefaultIndex)
                .Size(size)
                .Query(q => q
                    .Prefix(p => p
                        .Field(field!)
                        .Value(prefix)
                    )
                )
            );

            return response.IsValidResponse ? response.Documents : new List<T>();
        }

        public async Task<IReadOnlyCollection<T>> RangeQueryAsync<TValue>(string field, TValue? gt = default, TValue? lt = default, bool includeUpper = false, bool includeLower = false, int size = 10)
        {
            var rangeQuery = new TermRangeQuery(field!)
            {
                Gt = gt is not null ? Convert.ToString(gt) : null,
                Gte = includeLower && gt is not null ? Convert.ToString(gt) : null,
                Lt = lt is not null ? Convert.ToString(lt) : null,
                Lte = includeUpper && lt is not null ? Convert.ToString(lt) : null
            };

            var response = await _client.SearchAsync<T>(s => s
                .Index(_elasticSettings.DefaultIndex)
                .Size(size)
                .Query(q => q.Range(rangeQuery))
            );

            return response.IsValidResponse ? response.Documents : new List<T>();
        }

        public async Task<bool> RefreshIndexAsync(string indexName)
        {
            var response = await _client.Indices.RefreshAsync(indexName);
            return response.IsValidResponse;
        }

        public async Task<IReadOnlyCollection<T>> SearchAsync(string query, int size = 10, int from = 0)
        {
            var response = await _client.SearchAsync<T>(s => s
                .Index(_elasticSettings.DefaultIndex)
                .Size(size)
                .From(from)
                .Query(q => q
                    .QueryString(qs => qs
                        .Query(query)
                    )
                )
            );

            return response.IsValidResponse ? response.Documents : new List<T>();
        }

        public async Task<IReadOnlyCollection<T>> SearchAsync(Func<QueryDescriptor<T>, QueryDescriptor<T>> queryBuilder, int size = 10)
        {
            var queryDescriptor = new QueryDescriptor<T>();
            var builtQuery = queryBuilder(queryDescriptor);

            var response = await _client.SearchAsync<T>(s => s
                .Index(_elasticSettings.DefaultIndex)
                .Size(size)
                .Query(builtQuery)
            );

            return response.IsValidResponse ? response.Documents : new List<T>();
        }

        public async Task<IReadOnlyCollection<T>> SearchAsync(Expression<Func<T, bool>> predicate, int size = 10)
        {
            // Convert expression to a query string
            var queryString = predicate.ToString().Replace("AndAlso", "AND").Replace("OrElse", "OR");

            var response = await _client.SearchAsync<T>(s => s
                .Index(_elasticSettings.DefaultIndex)
                .Size(size)
                .Query(q => q
                    .QueryString(qs => qs
                        .Query(queryString)
                    )
                )
            );

            return response.IsValidResponse ? response.Documents : new List<T>();
        }

        public async Task<IReadOnlyCollection<T>> SearchByFieldAsync(string field, string value, int size = 10)
        {
            var response = await _client.SearchAsync<T>(s => s
                .Index(_elasticSettings.DefaultIndex)
                .Size(size)
                .Query(q => q
                    .Match(m => m
                        .Field(field!)
                        .Query(value)
                    )
                )
            );

            return response.IsValidResponse ? response.Documents : new List<T>();
        }

        public async Task<bool> UpdateDocumentAsync(string id, T document)
        {
            var response = await _client.UpdateAsync<T, T>(id, u => u
                .Index(_elasticSettings.DefaultIndex)
                .Doc(document)
                .DocAsUpsert(true)
            );

            return response.IsValidResponse;
        }
    }
}
