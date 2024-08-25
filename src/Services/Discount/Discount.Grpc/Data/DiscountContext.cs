﻿using Discount.Grpc.Models;
using Microsoft.EntityFrameworkCore;

namespace Discount.Grpc.Data
{
  public class DiscountContext : DbContext
  {
    public DiscountContext(DbContextOptions<DiscountContext> options) : base(options)
    {
    }

    public DbSet<Coupon> Coupons { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Coupon>().HasData(
        new Coupon { Id = 1, ProductName = "IPhone 13", Description = "IPhone Discount", Amount = 5 },
        new Coupon { Id = 2, ProductName = "IPhone 15", Description = "IPhone Discount", Amount = 15 });
    }
  }
}
