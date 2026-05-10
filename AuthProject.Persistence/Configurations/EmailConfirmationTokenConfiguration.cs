using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class EmailConfirmationTokenConfiguration : IEntityTypeConfiguration<EmailConfirmationToken>
{
    public void Configure(EntityTypeBuilder<EmailConfirmationToken> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token)
               .IsRequired()
               .HasMaxLength(64);

        builder.HasIndex(t => t.Token)
               .IsUnique();

        builder.Property(t => t.ExpiresAt)
               .IsRequired();
    }
}
