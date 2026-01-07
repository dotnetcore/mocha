// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using LiteDB;

namespace Mocha.Storage.LiteDB;

internal interface ILiteDBCollectionAccessor<T>
{
    ILiteCollection<T> Collection { get; }
}
