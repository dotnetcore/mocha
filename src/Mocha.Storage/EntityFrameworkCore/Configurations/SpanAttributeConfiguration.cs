// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.EntityFrameworkCore.Configurations;

public class SpanAttributeConfiguration : IEntityTypeConfiguration<EFSpanAttribute>
{
    public void Configure(EntityTypeBuilder<EFSpanAttribute> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd().HasColumnType("bigint AUTO_INCREMENT");
        builder.HasIndex(x => x.SpanId, "idx_span_id");
        builder.HasIndex(x => x.TraceId, "idx_trace_id");
        builder.HasIndex(x => x.AttributeKey, "idx_attribute_key");
        builder.HasIndex(x => x.AttributeValue, "idx_attribute_value");
        builder.ToTable("span_attributes");
    }
}
