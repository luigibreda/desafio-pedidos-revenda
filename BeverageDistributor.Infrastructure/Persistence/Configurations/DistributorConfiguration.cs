using BeverageDistributor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeverageDistributor.Infrastructure.Persistence.Configurations
{
    public class DistributorConfiguration : BaseEntityConfiguration<Distributor>
    {
        public override void Configure(EntityTypeBuilder<Distributor> builder)
        {
            base.Configure(builder);

            builder.ToTable("Distributors");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Cnpj)
                .IsRequired()
                .HasMaxLength(14);

            builder.HasIndex(d => d.Cnpj)
                .IsUnique();

            builder.Property(d => d.CompanyName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(d => d.TradingName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(d => d.Email)
                .IsRequired()
                .HasMaxLength(100);

            // Configure owned entity types
            ConfigureOwnedPhoneNumbers(builder);
            ConfigureOwnedContactNames(builder);
            ConfigureOwnedAddresses(builder);
        }

        private static void ConfigureOwnedPhoneNumbers(EntityTypeBuilder<Distributor> builder)
        {
            builder.OwnsMany(d => d.PhoneNumbers, phoneNumber =>
            {
                phoneNumber.ToTable("DistributorPhoneNumbers");
                phoneNumber.WithOwner().HasForeignKey("DistributorId");
                phoneNumber.Property<Guid>("Id").ValueGeneratedOnAdd();
                phoneNumber.HasKey("Id");
                
                phoneNumber.Property(pn => pn.Number)
                    .IsRequired()
                    .HasMaxLength(20);
                
                phoneNumber.Property(pn => pn.IsMain)
                    .IsRequired()
                    .HasDefaultValue(false);
            });
        }

        private static void ConfigureOwnedContactNames(EntityTypeBuilder<Distributor> builder)
        {
            builder.OwnsMany(d => d.ContactNames, contactName =>
            {
                contactName.ToTable("DistributorContactNames");
                contactName.WithOwner().HasForeignKey("DistributorId");
                contactName.Property<Guid>("Id").ValueGeneratedOnAdd();
                contactName.HasKey("Id");
                
                contactName.Property(cn => cn.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                
                contactName.Property(cn => cn.IsPrimary)
                    .IsRequired()
                    .HasDefaultValue(false);
                
                // Add a unique constraint to ensure only one primary contact per distributor
                contactName.HasIndex("DistributorId", "IsPrimary")
                    .HasFilter("IsPrimary = true")
                    .IsUnique()
                    .HasFilter(null); // Remove the filter to allow multiple non-primary contacts
            });
        }

        private static void ConfigureOwnedAddresses(EntityTypeBuilder<Distributor> builder)
        {
            builder.OwnsMany(d => d.Addresses, address =>
            {
                address.ToTable("DistributorAddresses");
                address.WithOwner().HasForeignKey("DistributorId");
                address.Property<Guid>("Id").ValueGeneratedOnAdd();
                address.HasKey("Id");
                
                address.Property(a => a.Street)
                    .IsRequired()
                    .HasMaxLength(200);
                
                address.Property(a => a.Number)
                    .IsRequired()
                    .HasMaxLength(20);
                
                address.Property(a => a.Complement)
                    .HasMaxLength(200);
                
                address.Property(a => a.Neighborhood)
                    .IsRequired()
                    .HasMaxLength(100);
                
                address.Property(a => a.City)
                    .IsRequired()
                    .HasMaxLength(100);
                
                address.Property(a => a.State)
                    .IsRequired()
                    .HasMaxLength(50);
                
                address.Property(a => a.Country)
                    .IsRequired()
                    .HasMaxLength(100);
                
                address.Property(a => a.PostalCode)
                    .IsRequired()
                    .HasMaxLength(20);
                
                address.Property(a => a.IsMain)
                    .IsRequired()
                    .HasDefaultValue(false);
                
                // Add a unique constraint to ensure only one main address per distributor
                address.HasIndex("DistributorId", "IsMain")
                    .HasFilter("IsMain = true")
                    .IsUnique()
                    .HasFilter(null); // Remove the filter to allow multiple non-main addresses
            });
        }
    }
}
