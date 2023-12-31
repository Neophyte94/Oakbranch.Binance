﻿using System;
using Oakbranch.Binance.Utility;

namespace Oakbranch.Binance.Core;

/// <summary>
/// Represents a base URL endpoint for REST API requests.
/// </summary>
public readonly struct BaseEndpoint : IEquatable<BaseEndpoint>
{
    #region Instance props & fields

    public readonly string Url;
    public readonly string Description;
    public readonly NetworkType Type;

    #endregion

    #region Instance constructors

    public BaseEndpoint(NetworkType type, string url, string? description)
    {
        url.ThrowIfNullOrWhitespace();
        if (!url.StartsWith("https://"))
        {
            throw new ArgumentException($"The specified URL \"{url}\" is invalid.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            description = string.Empty;
        }

        Type = type;
        Url = url;
        Description = description;
    }

    #endregion

    #region Instance methods

    public bool Equals(BaseEndpoint other)
    {
        return this == other;
    }

    public override bool Equals(object? obj)
    {
        if (obj is BaseEndpoint other)
        {
            return this == other;
        }
        else
        {
            return false;
        }
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Url, Type);
    }

    public override string ToString()
    {
        return Description != null ? $"{Url} ({Description})" : $"{Url} ({Type})";
    }

    #endregion

    #region Operators

    public static bool operator ==(BaseEndpoint x, BaseEndpoint y)
    {
        return x.Type == y.Type && x.Url == y.Url;
    }

    public static bool operator !=(BaseEndpoint x, BaseEndpoint y)
    {
        return x.Type != y.Type || x.Url != y.Url;
    }

    #endregion
}
