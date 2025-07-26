using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeverageDistributor.Infrastructure.Persistence.Configurations
{
    public abstract class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : class
    {
        public virtual void Configure(EntityTypeBuilder<T> builder)
        {
            // Common configuration for all entities can go here
            builder.Property<DateTime>("CreatedAt")
                .IsRequired();

            builder.Property<DateTime?>("UpdatedAt");

            builder.Property<bool>("IsActive")
                .IsRequired()
                .HasDefaultValue(true);
        }
    }
}
