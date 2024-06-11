using Acceptor.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Acceptor.Data;

public class AcceptorContext : DbContext
{
    public AcceptorContext(DbContextOptions<AcceptorContext> options) : base(options)
    {
    }

    public DbSet<Order> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configuração usando Fluent API
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(p => p.OrderId);
            entity.Property(e => e.OrderId)
                  .ValueGeneratedOnAdd();

            entity.Property(p => p.Symbol)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(p => p.Side)
                  .IsRequired();

            entity.Property(p => p.Quantity)
                  .IsRequired();

            entity.Property(p => p.Price)
                  .IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }


}
