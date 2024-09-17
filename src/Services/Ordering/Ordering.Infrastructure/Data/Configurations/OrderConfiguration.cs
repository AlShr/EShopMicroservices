using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Domain.Enums;
using Ordering.Domain.Models;
using Ordering.Domain.ValueObjects;

namespace Ordering.Infrastructure.Data.Configurations
{
  public class OrderConfiguration : IEntityTypeConfiguration<Order>
  {
    public void Configure(EntityTypeBuilder<Order> builder)
    {
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Id).HasConversion(orderId => orderId.Value, dbId => OrderId.Of(dbId));

      builder.HasOne<Customer>()
        .WithMany()
        .HasForeignKey(x => x.CustomerId)
        .IsRequired();

      builder.HasMany(x => x.OrderItems)
        .WithOne()
        .HasForeignKey(x => x.OrderId);

      builder.ComplexProperty(x => x.OrderName, y =>
      {
        y.Property(z => z.Value)
          .HasColumnName(nameof(Order.OrderName))
          .HasMaxLength(100)
          .IsRequired();
      });

      builder.ComplexProperty(x => x.ShippingAddress, y =>
      {
        y.Property(z => z.FirstName)
          .HasMaxLength(50)
          .IsRequired();

        y.Property(z => z.LastName)
          .HasMaxLength(50)
          .IsRequired();

        y.Property(z => z.EmailAddress)
          .HasMaxLength(50);

        y.Property(z => z.AddressLine)
          .HasMaxLength(180)
          .IsRequired();

        y.Property(z => z.Country)
          .HasMaxLength(50);

        y.Property(z => z.State)
          .HasMaxLength(50);

        y.Property(z => z.ZipCode)
          .HasMaxLength(5)
          .IsRequired();
      });

      builder.ComplexProperty(x => x.BillingAddress, y =>
      {
        y.Property(z => z.FirstName)
          .HasMaxLength(50)
          .IsRequired();

        y.Property(z => z.LastName)
          .HasMaxLength(50)
          .IsRequired();

        y.Property(z => z.EmailAddress)
          .HasMaxLength(50);

        y.Property(z => z.AddressLine)
          .HasMaxLength(180)
          .IsRequired();

        y.Property(z => z.Country)
          .HasMaxLength(50);

        y.Property(z => z.State)
          .HasMaxLength(50);

        y.Property(z => z.ZipCode)
          .HasMaxLength(5)
          .IsRequired();
      });

      builder.ComplexProperty(x => x.Payment, y =>
      {
        y.Property(z => z.CartName)
          .HasMaxLength(50);

        y.Property(z => z.CartNumber)
          .HasMaxLength(24)
          .IsRequired();

        y.Property(z => z.Expiration)
          .HasMaxLength(10);

        y.Property(z => z.Cvv)
          .HasMaxLength(3);

        y.Property(z => z.PaymentMethod);
      });

      builder.Property(x => x.Status)
        .HasDefaultValue(OrderStatus.Draft)
        .HasConversion(y => y.ToString(), dbStatus => (OrderStatus)Enum.Parse(typeof(OrderStatus), dbStatus));

      builder.Property(x => x.TotalPrice);
    }
  }
}
