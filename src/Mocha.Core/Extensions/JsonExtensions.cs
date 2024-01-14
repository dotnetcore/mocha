// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Mocha.Core.Extensions;

public static class JsonSerializationExtensions
{
    public static T? FromJson<T>(this string json)
    {
        return JsonConvert.DeserializeObject<T>(json);
    }

    public static string ToJson<T>(this T obj)
    {
        return JsonConvert.SerializeObject(obj);
    }
}
