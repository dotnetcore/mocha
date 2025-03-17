// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Mocha.Storage.EntityFrameworkCore.Metadata.Models;

namespace Mocha.Storage.EntityFrameworkCore.Metadata;

public class MochaMetadataContext(DbContextOptions<MochaMetadataContext> options) : DbContext(options)
{
    public DbSet<EFMetricMetadata> MetricMetadata => Set<EFMetricMetadata>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EFMetricMetadata>(entity =>
        {
            entity.ToTable("metric_metadata");
            entity.HasKey(e => e.Metric);
            entity.Property(e => e.Metric).HasColumnName("metric").IsRequired();
            entity.Property(e => e.ServiceName).HasColumnName("service_name").IsRequired();
            entity.Property(e => e.Type).HasColumnName("type").IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").IsRequired();
            entity.Property(e => e.Unit).HasColumnName("unit").IsRequired();
        });
    }
}
