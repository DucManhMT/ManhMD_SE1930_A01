using FUNews.BusinessLogic.DTOs;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Validator;

namespace ManhMD_SE1930_A01_BE.OData;

public static class ODataQueryHelper
{
    public static ODataResponse<T> ApplyOData<T>(
        IQueryable<T> query,
        ODataQueryOptions<T> queryOptions,
        int maxTop = 100,
        int defaultPageSize = 10)
    {
        var validationSettings = new ODataValidationSettings
        {
            MaxTop = maxTop,
            AllowedQueryOptions = AllowedQueryOptions.Filter | AllowedQueryOptions.OrderBy | AllowedQueryOptions.Top | AllowedQueryOptions.Skip | AllowedQueryOptions.Count,
            AllowedFunctions = AllowedFunctions.AllFunctions & ~AllowedFunctions.All & ~AllowedFunctions.Any
        };

        queryOptions.Validate(validationSettings);

        long? count = null;
        if (queryOptions.Count != null && queryOptions.Count.Value)
        {
            var filteredForCount = queryOptions.Filter != null
                ? (IQueryable<T>)queryOptions.Filter.ApplyTo(query, new ODataQuerySettings())
                : query;

            count = filteredForCount.LongCount();
        }

        var querySettings = new ODataQuerySettings
        {
            PageSize = queryOptions.Top != null ? null : defaultPageSize
        };

        var finalQuery = (IQueryable<T>)queryOptions.ApplyTo(query, querySettings);
        var items = finalQuery.ToList();

        return new ODataResponse<T>
        {
            Count = count,
            Value = items
        };
    }
}
