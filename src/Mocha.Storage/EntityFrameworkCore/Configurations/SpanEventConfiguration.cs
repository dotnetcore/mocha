// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.EntityFrameworkCore.Configurations;

public class SpanEventConfiguration : IEntityTypeConfiguration<SpanEvent>
{
    public void Configure(EntityTypeBuilder<SpanEvent> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd().HasColumnType("bigint AUTO_INCREMENT");
        builder.HasIndex(x => x.TraceId, "trace_id_index");
        builder.HasIndex(x => x.EventName, "event_name_index");
        builder.ToTable("span_events");
    }
}
