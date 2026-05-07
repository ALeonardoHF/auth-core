public class AuditLog
{
    public Guid Id { get; private set; }
    public AuditLogEvent Event { get; private set; }
    public Guid? UserId { get; private set; }
    public string? Email { get; private set; }
    public string? IpAddress { get; private set; }
    public string? DeviceInfo { get; private set; }
    public string? Details { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        AuditLogEvent auditEvent,
        Guid? userId = null,
        string? email = null,
        string? ipAddress = null,
        string? deviceInfo = null,
        string? details = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            Event = auditEvent,
            UserId = userId,
            Email = email,
            IpAddress = ipAddress,
            DeviceInfo = deviceInfo,
            Details = details,
            CreatedAt = DateTime.UtcNow
        };
    }
}
