using System.Linq.Expressions;
using Elastic.Clients.Elasticsearch.QueryDsl;
using SampleApi.Elastic.Models;

namespace SampleApi.Elastic.Services
{
    public interface IElasticService<T> where T : class
    {
        // Index management
        Task CreateIndexIfNotExistsAsync(string indexName);
        Task DeleteIndexAsync(string indexName);
        Task<bool> IndexExistsAsync(string indexName);

        // Document operations
        Task<bool> IndexDocumentAsync(T document, string? id = null);
        Task<bool> UpdateDocumentAsync(string id, T document);
        Task<bool> DeleteDocumentAsync(string id);
        Task<T?> GetDocumentAsync(string id);

        // Bulk operations
        Task<bool> BulkIndexAsync(IEnumerable<T> documents);
        Task<bool> BulkUpdateAsync(IEnumerable<(string id, T document)> documents);
        Task<bool> BulkDeleteAsync(IEnumerable<string> ids);

        // Basic search
        Task<IReadOnlyCollection<T>> SearchAsync(string query, int size = 10, int from = 0);
        Task<IReadOnlyCollection<T>> SearchByFieldAsync(string field, string value, int size = 10);
        Task<IReadOnlyCollection<T>> GetAllAsync(int size = 1000, int from = 0);

        // Advanced search
        Task<IReadOnlyCollection<T>> SearchAsync(Func<QueryDescriptor<T>, QueryDescriptor<T>> queryBuilder, int size = 10);
        Task<IReadOnlyCollection<T>> SearchAsync(Expression<Func<T, bool>> predicate, int size = 10);

        // Aggregations
        Task<long> CountAsync(Func<QueryDescriptor<T>, QueryDescriptor<T>>? queryBuilder = null);

        // Advanced operations
        Task<bool> RefreshIndexAsync(string indexName);
        Task<IReadOnlyCollection<T>> MultiMatchAsync(string query, string[] fields, int size = 10);
        Task<IReadOnlyCollection<T>> FuzzySearchAsync(string field, string value, int fuzziness = 1, int size = 10);
        Task<IReadOnlyCollection<T>> PrefixSearchAsync(string field, string prefix, int size = 10);
        Task<IReadOnlyCollection<T>> RangeQueryAsync<TValue>(string field, TValue? gt = default, TValue? lt = default, bool includeUpper = false, bool includeLower = false, int size = 10);
    }
}
