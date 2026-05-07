public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log);
}
