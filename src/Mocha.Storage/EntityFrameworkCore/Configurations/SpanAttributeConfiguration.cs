using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.EntityFrameworkCore.Configurations;

public class SpanAttributeConfiguration : IEntityTypeConfiguration<SpanAttribute>
{
    public void Configure(EntityTypeBuilder<SpanAttribute> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd().HasColumnType("bigint AUTO_INCREMENT");
        builder.HasIndex(x => x.SpanId, "span_id_index");
        builder.HasIndex(x => x.TraceId, "trace_id_index");
        builder.HasIndex(x => x.AttributeKey, "attribute_key_index");
        builder.HasIndex(x => x.AttributeValue, "attribute_value_index");
        builder.ToTable("span_attributes");
    }
}
