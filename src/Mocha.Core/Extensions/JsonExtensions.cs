// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.


using System.Text.Json;

namespace Mocha.Core.Extensions;

public static class JsonSerializationExtensions
{
    public static T? FromJson<T>(this string json)
    {
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public static string ToJson<T>(this T obj)
    {
        return JsonSerializer.Serialize(obj,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}

