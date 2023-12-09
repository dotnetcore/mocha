// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.EntityFrameworkCore.Configurations;

public class SpanConfiguration : IEntityTypeConfiguration<EFSpan>
{
    public void Configure(EntityTypeBuilder<EFSpan> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd().HasColumnType("bigint AUTO_INCREMENT");
        builder.HasIndex(x => x.SpanId, "idx_span_id");
        builder.HasIndex(x => x.TraceId, "idx_trace_id");
        builder.HasMany(config => config.SpanAttributes)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(config => config.SpanEvents)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(config => config.SpanLinks)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
        builder.ToTable("spans");
    }
}
