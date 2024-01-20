// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mocha.Core.Models.Trace;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.EntityFrameworkCore.Configurations;

public class SpanLinkConfiguration : IEntityTypeConfiguration<EFSpanLink>
{
    public void Configure(EntityTypeBuilder<EFSpanLink> builder)
    {
        builder.ToTable("span_link");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(e => e.TraceId).HasColumnName("trace_id").IsRequired();
        builder.Property(e => e.SpanId).HasColumnName("span_id").IsRequired();
        builder.Property(e => e.Index).HasColumnName("index").IsRequired();
        builder.Property(e => e.LinkedTraceId).HasColumnName("linked_trace_id").IsRequired();
        builder.Property(e => e.LinkedSpanId).HasColumnName("linked_span_id").IsRequired();
        builder.Property(e => e.LinkedTraceState).HasColumnName("linked_trace_state").IsRequired();
        builder.Property(e => e.LinkedTraceFlags).HasColumnName("linked_trace_flags").IsRequired();
    }
}
