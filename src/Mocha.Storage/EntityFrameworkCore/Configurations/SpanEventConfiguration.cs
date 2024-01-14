// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.EntityFrameworkCore.Configurations;

public class SpanEventConfiguration : IEntityTypeConfiguration<EFSpanEvent>
{
    public void Configure(EntityTypeBuilder<EFSpanEvent> builder)
    {
        builder.ToTable("span_event");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(e => e.TraceId).HasColumnName("trace_id").IsRequired();
        builder.Property(e => e.SpanId).HasColumnName("span_id").IsRequired();
        builder.Property(e => e.Index).HasColumnName("index").IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").IsRequired();
        builder.Property(e => e.TimestampUnixNano).HasColumnName("timestamp_unix_nano").IsRequired();
    }
}
