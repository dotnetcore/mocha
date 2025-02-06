// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Mocha.Core.Extensions;

public static class EnumExtensions
{
    private static readonly ConcurrentDictionary<Type, Dictionary<Enum, string>> _enumDescriptions = new();

    public static bool TryGetDescription(this Enum value, [NotNullWhen(true)] out string? description)
    {
        var type = value.GetType();
        var descriptions = _enumDescriptions.GetOrAdd(type, _ =>
        {
            var enumDescriptions = new Dictionary<Enum, string>();

            foreach (var enumValue in Enum.GetValues(type))
            {
                var name = Enum.GetName(type, enumValue)!;
                var field = type.GetField(name)!;
                var attribute = (DescriptionAttribute?)field.GetCustomAttribute(typeof(DescriptionAttribute));
                enumDescriptions[(Enum)enumValue] = attribute?.Description ?? name;
            }

            return enumDescriptions;
        });

        return descriptions.TryGetValue(value, out description);
    }

    public static string GetDescription(this Enum value)
    {
        if (value.TryGetDescription(out var description))
        {
            return description;
        }

        throw new InvalidOperationException(
            $"Description not found for enum value {value}, please add a DescriptionAttribute to the enum value");
    }
}
