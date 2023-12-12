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
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd().HasColumnType("bigint AUTO_INCREMENT");
        builder.HasIndex(x => x.TraceId, "idx_trace_id");
        builder.HasIndex(x => x.EventName, "idx_event_name");
        builder.ToTable("span_events");
    }
}
