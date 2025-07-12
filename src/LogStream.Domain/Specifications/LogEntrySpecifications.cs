using System.Linq.Expressions;
using LogStream.Domain.Entities;
using LogStream.Domain.ValueObjects;

namespace LogStream.Domain.Specifications;

public class LogEntriesByTenantSpecification : BaseSpecification<LogEntry>
{
    public LogEntriesByTenantSpecification(TenantId tenantId)
    {
        Criteria = log => log.TenantId == tenantId;
        ApplyOrderByDescending(log => log.Timestamp);
    }
}

public class LogEntriesByTenantAndDateRangeSpecification : BaseSpecification<LogEntry>
{
    public LogEntriesByTenantAndDateRangeSpecification(TenantId tenantId, DateTime startDate, DateTime endDate)
    {
        Criteria = log => log.TenantId == tenantId && 
                         log.Timestamp >= startDate && 
                         log.Timestamp <= endDate;
        ApplyOrderByDescending(log => log.Timestamp);
    }
}

public class LogEntriesByTenantAndLevelSpecification : BaseSpecification<LogEntry>
{
    public LogEntriesByTenantAndLevelSpecification(TenantId tenantId, LogLevel level)
    {
        Criteria = log => log.TenantId == tenantId && log.Level == level;
        ApplyOrderByDescending(log => log.Timestamp);
    }
}

public class UnprocessedLogEntriesSpecification : BaseSpecification<LogEntry>
{
    public UnprocessedLogEntriesSpecification(int takeCount = 1000)
    {
        Criteria = log => !log.IsProcessed;
        ApplyOrderBy(log => log.CreatedAt);
        Take = takeCount;
    }
}

public class LogEntriesForArchivalSpecification : BaseSpecification<LogEntry>
{
    public LogEntriesForArchivalSpecification(DateTime beforeDate, int takeCount = 1000)
    {
        Criteria = log => !log.IsArchived && log.CreatedAt < beforeDate;
        ApplyOrderBy(log => log.CreatedAt);
        Take = takeCount;
    }
}

public class LogEntriesSearchSpecification : BaseSpecification<LogEntry>
{
    public LogEntriesSearchSpecification(
        TenantId tenantId,
        string? keyword = null,
        LogLevel? level = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? application = null,
        string? environment = null)
    {
        Expression<Func<LogEntry, bool>> criteria = log => log.TenantId == tenantId;

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            criteria = CombineExpressions(criteria, log => 
                log.Message.Content.Contains(keyword) ||
                (log.Exception != null && log.Exception.Contains(keyword)) ||
                (log.Tags != null && log.Tags.Contains(keyword)));
        }

        if (level != null)
        {
            criteria = CombineExpressions(criteria, log => log.Level == level);
        }

        if (startDate.HasValue)
        {
            criteria = CombineExpressions(criteria, log => log.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            criteria = CombineExpressions(criteria, log => log.Timestamp <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(application))
        {
            criteria = CombineExpressions(criteria, log => log.Source.Application == application);
        }

        if (!string.IsNullOrWhiteSpace(environment))
        {
            criteria = CombineExpressions(criteria, log => log.Source.Environment == environment);
        }

        Criteria = criteria;
        ApplyOrderByDescending(log => log.Timestamp);
    }

    private static Expression<Func<LogEntry, bool>> CombineExpressions(
        Expression<Func<LogEntry, bool>> expr1,
        Expression<Func<LogEntry, bool>> expr2)
    {
        var parameter = Expression.Parameter(typeof(LogEntry));
        var left = new ParameterReplacerVisitor(parameter).Visit(expr1.Body);
        var right = new ParameterReplacerVisitor(parameter).Visit(expr2.Body);
        var body = Expression.AndAlso(left!, right!);
        return Expression.Lambda<Func<LogEntry, bool>>(body, parameter);
    }
}

internal class ParameterReplacerVisitor : ExpressionVisitor
{
    private readonly ParameterExpression _parameter;

    public ParameterReplacerVisitor(ParameterExpression parameter)
    {
        _parameter = parameter;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return base.VisitParameter(_parameter);
    }
}