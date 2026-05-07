using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Event)
               .HasConversion<string>()
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(a => a.Email)
               .HasMaxLength(256);

        builder.Property(a => a.IpAddress)
               .HasMaxLength(45);

        builder.Property(a => a.DeviceInfo)
               .HasMaxLength(256);

        builder.Property(a => a.Details)
               .HasMaxLength(500);

        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.Event);
    }
}
