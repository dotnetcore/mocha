// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mocha.Core.Extensions;

namespace Mocha.Query.JsonConverters;

public class JsonDescriptionEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException("This converter is write-only");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (value.TryGetDescription(out var description))
        {
            writer.WriteStringValue(description);
            return;
        }

        throw new InvalidOperationException(
            $"Description not found for enum value {value}, please add a DescriptionAttribute to the enum value");
    }
}
