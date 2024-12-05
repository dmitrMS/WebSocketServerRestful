using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ServerRestful;

public partial class WebSocketDbContext : DbContext
{
    public WebSocketDbContext()
    {
    }

    public WebSocketDbContext(DbContextOptions<WebSocketDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<WebSocketClient> WebSocketClients { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=WebSocketDb;Username=postgres;Password=P@ssw0rd");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WebSocketClient>(entity =>
        {
            entity.HasKey(e => e.ClientId).HasName("WebSocketClient_pkey");

            entity.ToTable("WebSocketClient");

            entity.Property(e => e.ClientId).UseIdentityAlwaysColumn();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
