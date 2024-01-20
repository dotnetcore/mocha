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
        builder.ToTable("span");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(e => e.TraceId).HasColumnName("trace_id").IsRequired();
        builder.Property(e => e.SpanId).HasColumnName("span_id").IsRequired();
        builder.Property(e => e.SpanName).HasColumnName("span_name").IsRequired();
        builder.Property(e => e.ParentSpanId).HasColumnName("parent_span_id");
        builder.Property(e => e.StartTimeUnixNano).HasColumnName("start_time_unix_nano").IsRequired();
        builder.Property(e => e.EndTimeUnixNano).HasColumnName("end_time_unix_nano").IsRequired();
        builder.Property(e => e.DurationNanoseconds).HasColumnName("duration_nanoseconds").IsRequired();
        builder.Property(e => e.StatusCode).HasColumnName("status_code");
        builder.Property(e => e.StatusMessage).HasColumnName("status_message");
        builder.Property(e => e.SpanKind).HasColumnName("span_kind").IsRequired();
        builder.Property(e => e.ServiceName).HasColumnName("service_name").IsRequired();
        builder.Property(e => e.ServiceInstanceId).HasColumnName("service_instance_id").IsRequired();
        builder.Property(e => e.TraceFlags).HasColumnName("trace_flags").IsRequired();
        builder.Property(e => e.TraceState).HasColumnName("trace_state").IsRequired();
    }
}
