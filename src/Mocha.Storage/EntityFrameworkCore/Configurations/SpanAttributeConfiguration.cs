// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mocha.Core.Models.Trace;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.EntityFrameworkCore.Configurations;

public class SpanAttributeConfiguration : IEntityTypeConfiguration<EFSpanAttribute>
{
    public void Configure(EntityTypeBuilder<EFSpanAttribute> builder)
    {
        builder.ToTable("span_attribute");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(e => e.TraceId).HasColumnName("trace_id").IsRequired();
        builder.Property(e => e.SpanId).HasColumnName("span_id").IsRequired();
        builder.Property(e => e.Key).HasColumnName("key").IsRequired();
        builder.Property(e => e.ValueType).HasColumnName("value_type").IsRequired();
        builder.Property(e => e.Value).HasColumnName("value").IsRequired();
    }
}
