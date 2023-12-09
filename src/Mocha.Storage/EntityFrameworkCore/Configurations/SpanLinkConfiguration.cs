// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.EntityFrameworkCore.Configurations;

public class SpanLinkConfiguration : IEntityTypeConfiguration<EFSpanLink>
{
    public void Configure(EntityTypeBuilder<EFSpanLink> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd().HasColumnType("bigint AUTO_INCREMENT");
        builder.HasIndex(x => x.SpanId, "idx_span_id");
        builder.HasIndex(x => x.TraceId, "idx_trace_id");
        builder.HasIndex(x => x.LinkedSpanId, "idx_linked_span_id");
        builder.ToTable("span_links");
    }
}
