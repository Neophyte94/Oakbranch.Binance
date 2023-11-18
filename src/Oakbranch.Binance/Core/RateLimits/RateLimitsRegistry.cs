using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Oakbranch.Binance.Abstractions;

namespace Oakbranch.Binance.Core.RateLimits;

/// <summary>
/// A simple thread-safe implementation of the <see cref="IRateLimitsRegistry"/> interface.
/// </summary>
public sealed class RateLimitsRegistry : IRateLimitsRegistry
{
    #region Nested types

    private sealed class LimitNode
    {
        public readonly LimitCounter Current;

        private LimitNode? _next;
        public LimitNode? Next
        {
            get
            {
                return _next;
            }
            set
            {
                if (value != null)
                {
                    if (value == this)
                    {
                        throw new ArgumentException("The specified next node is the current node itself.");
                    }
                    if (value.Current == Current)
                    {
                        throw new ArgumentException("The next node cannot point to the same instance as the current node.");
                    }
                    if (value.Next == this)
                    {
                        throw new ArgumentException("The next node of the specified next node points to the current node.");
                    }
                }

                _next = value;
            }
        }

        public LimitNode(LimitCounter current)
        {
            Current = current ?? throw new ArgumentNullException(nameof(current));
        }
    }

    #endregion

    #region Instance members

    private readonly Dictionary<int, LimitCounter> _idToLimitDict;
    private readonly Dictionary<int, LimitNode> _dimensionToLimitsDict;

    #endregion

    #region Instance indexers

    public RateLimitInfo this[int id]
    {
        get
        {
            if (_idToLimitDict.TryGetValue(id, out LimitCounter? limit))
            {
                return new RateLimitInfo(limit.DimensionId, limit.ResetInterval, limit.Limit, limit.Usage, limit.Name);
            }
            else
            {
                throw new KeyNotFoundException($"No rate limit with the ID {id} was found.");
            }
        }
    }

    #endregion

    #region Instance constructors

    public RateLimitsRegistry(int limitCapacity = 32)
    {
        if (limitCapacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limitCapacity));
        }

        _idToLimitDict = new Dictionary<int, LimitCounter>(limitCapacity);
        _dimensionToLimitsDict = new Dictionary<int, LimitNode>(limitCapacity);
    }

    #endregion

    #region Instance methods

    public bool TryRegisterLimit(int id, RateLimitInfo limitParams)
    {
        if (limitParams.IsUndefined)
        {
            throw new ArgumentException($"The specified limit parameters instance represents the undefined value.");
        }

        lock (_idToLimitDict)
        {
            if (_idToLimitDict.ContainsKey(id))
            {
                return false;
            }

            LimitCounter limit = new LimitCounter(
                id, limitParams.DimensionId, limitParams.Limit, limitParams.Interval,
                limitParams.Usage, limitParams.Name);

            _idToLimitDict.Add(id, limit);
            if (_dimensionToLimitsDict.TryGetValue(limit.DimensionId, out LimitNode? node))
            {
                while (node.Next != null)
                {
                    node = node.Next;
                }
                node.Next = new LimitNode(limit);
            }
            else
            {
                node = new LimitNode(limit);
                _dimensionToLimitsDict.Add(limit.DimensionId, node);
            }

            return true;
        }
    }

    public bool ContainsLimit(int id)
    {
        return _idToLimitDict.ContainsKey(id);
    }

    public void ModifyLimit(int id, uint newLimit)
    {
        if (_idToLimitDict.TryGetValue(id, out LimitCounter? limit))
        {
            limit.Limit = newLimit;
        }
        else
        {
            throw new KeyNotFoundException($"No rate limit with the ID {id} was found.");
        }
    }

    public bool TestUsage(IReadOnlyList<QueryWeight> weights, out int violatedLimitId)
    {
        int wCount = weights.Count;
        for (int wIdx = 0; wIdx != wCount;)
        {
            QueryWeight w = weights[wIdx++];
            if (_dimensionToLimitsDict.TryGetValue(w.DimensionId, out LimitNode? node))
            {
                while (node != null)
                {
                    if (!node.Current.TestUsage(w.Amount))
                    {
                        violatedLimitId = node.Current.Id;
                        return false;
                    }
                    node = node.Next;
                }
            }
            else
            {
                throw new KeyNotFoundException(
                    $"No limit has been registered that targets a weight dimension {w.DimensionId}.");
            }
        }

        violatedLimitId = 0;
        return true;
    }

    public void IncrementUsage(IReadOnlyList<QueryWeight> weights, DateTime timestamp)
    {
        int wCount = weights.Count;
        for (int wIdx = 0; wIdx != wCount;)
        {
            QueryWeight w = weights[wIdx++];
            if (_dimensionToLimitsDict.TryGetValue(w.DimensionId, out LimitNode? node))
            {
                while (node != null)
                {
                    node.Current.AddUsage(w.Amount, timestamp);
                    node = node.Next;
                }
            }
            else
            {
                throw new KeyNotFoundException(
                    $"No limit has been registered that targets a weight dimension {w.DimensionId}.");
            }
        }
    }

    public void UpdateUsage(int id, uint usage, DateTime timestamp)
    {
        if (_idToLimitDict.TryGetValue(id, out LimitCounter? limit))
        {
            limit.SetUsage(usage, timestamp);
        }
        else
        {
            throw new KeyNotFoundException($"No rate limit with the ID {id} was found.");
        }
    }

#if DEBUG
    internal void LogCurrentUsage(ILogger logger)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("The current rate limits usage:");

        foreach (LimitCounter counter in _idToLimitDict.Values)
        {
            sb.AppendLine($"{counter.Name}: {counter.Usage} / {counter.Limit}");
        }

        logger.Log(LogLevel.Debug, "API rate limits", sb.ToString());
    }
#endif

    #endregion
}