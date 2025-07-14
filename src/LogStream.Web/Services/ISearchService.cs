using LogStream.Web.Models;

namespace LogStream.Web.Services;

public interface ISearchService
{
    Task<SearchResponse> SearchLogsAsync(SearchRequest request, CancellationToken cancellationToken = default);
    Task<long> GetTotalLogsCountAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetAvailableApplicationsAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetAvailableHostsAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SearchHistogram>> GetSearchHistogramAsync(SearchRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<FieldSuggestion>> GetFieldSuggestionsAsync(string tenantId, string fieldName, CancellationToken cancellationToken = default);
    Task<IEnumerable<QuerySuggestion>> GetQuerySuggestionsAsync(string tenantId, string partialQuery, CancellationToken cancellationToken = default);
    Task SaveSearchAsync(string tenantId, string name, SearchRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<SavedSearch>> GetSavedSearchesAsync(string tenantId, CancellationToken cancellationToken = default);
    Task DeleteSavedSearchAsync(string tenantId, Guid searchId, CancellationToken cancellationToken = default);
    Task<SearchStats> GetSearchStatsAsync(string tenantId, CancellationToken cancellationToken = default);
}