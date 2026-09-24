using System.Data;
using FUNews.DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FUNews.DataAccess.Sequences;

public class SqlSequenceService : ISqlSequenceService
{
    private readonly FUNewsDbContext _context;

    public SqlSequenceService(FUNewsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<short> GetNextAccountIdAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.IsRelational())
        {
            try
            {
                return await ExecuteSequenceScalarAsync<short>("dbo.Seq_AccountID", cancellationToken);
            }
            catch
            {
                // Fallback to Max + 1 if sequence is not present or execution fails
            }
        }

        var max = await _context.SystemAccounts.MaxAsync(a => (short?)a.AccountID, cancellationToken) ?? 0;
        return (short)(max + 1);
    }

    public async Task<int> GetNextTagIdAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.IsRelational())
        {
            try
            {
                return await ExecuteSequenceScalarAsync<int>("dbo.Seq_TagID", cancellationToken);
            }
            catch
            {
                // Fallback
            }
        }

        var max = await _context.Tags.MaxAsync(t => (int?)t.TagID, cancellationToken) ?? 0;
        return max + 1;
    }

    public async Task<string> GetNextNewsArticleIdAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.IsRelational())
        {
            try
            {
                var seqNumber = await ExecuteSequenceScalarAsync<long>("dbo.Seq_NewsArticleID", cancellationToken);
                return $"N{seqNumber}";
            }
            catch
            {
                // Fallback
            }
        }

        var existingIds = await _context.NewsArticles.Select(a => a.NewsArticleID).ToListAsync(cancellationToken);
        long maxNum = 0;
        foreach (var id in existingIds)
        {
            var trimmed = id.TrimStart('N', 'n');
            if (long.TryParse(trimmed, out var n) && n > maxNum)
            {
                maxNum = n;
            }
        }
        return $"N{maxNum + 1}";
    }

    private async Task<T> ExecuteSequenceScalarAsync<T>(string sequenceName, CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();
        var wasClosed = connection.State == ConnectionState.Closed;

        if (wasClosed)
        {
            await _context.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT NEXT VALUE FOR {sequenceName};";
            
            // If there's an ambient transaction associated with DbContext
            var currentTransaction = _context.Database.CurrentTransaction;
            if (currentTransaction != null)
            {
                command.Transaction = currentTransaction.GetDbTransaction();
            }

            var scalarResult = await command.ExecuteScalarAsync(cancellationToken);
            if (scalarResult == null || scalarResult == DBNull.Value)
            {
                throw new InvalidOperationException($"Failed to retrieve next value from sequence {sequenceName}.");
            }

            return (T)Convert.ChangeType(scalarResult, typeof(T));
        }
        finally
        {
            if (wasClosed && _context.Database.CurrentTransaction == null)
            {
                await _context.Database.CloseConnectionAsync();
            }
        }
    }
}
