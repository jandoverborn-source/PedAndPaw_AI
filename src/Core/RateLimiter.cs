namespace PedAndPaw_AI.Core;

/// <summary>
/// Enforces the three limits from the ini that protect the player's API budget:
/// a per-NPC cooldown, a global requests-per-minute cap, and a daily budget.
/// The clock is passed in, so the behaviour can be tested without waiting.
/// </summary>
public sealed class RateLimiter
{
    private readonly int _cooldownSeconds;
    private readonly int _maxPerMinute;
    private readonly int _dailyBudget;

    private readonly Dictionary<string, DateTime> _lastCallPerNpc = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<DateTime> _callsInWindow = new();
    private DateTime _budgetDay = DateTime.MinValue;
    private int _callsToday;

    public RateLimiter(ModConfig config)
        : this(config.NpcCooldownSeconds, config.MaxRequestsPerMinute, config.DailyRequestBudget)
    {
    }

    public RateLimiter(int cooldownSeconds, int maxPerMinute, int dailyBudget)
    {
        _cooldownSeconds = Math.Max(0, cooldownSeconds);
        _maxPerMinute = Math.Max(0, maxPerMinute);
        _dailyBudget = Math.Max(0, dailyBudget);
    }

    /// <summary>Why the last call was refused, for logging. Null when it was allowed.</summary>
    public string? LastDenyReason { get; private set; }

    /// <summary>
    /// True when a call for this NPC may be made now. A successful call is recorded,
    /// so calling this twice in a row inside the cooldown returns false the second time.
    /// </summary>
    public bool TryAcquire(string npcKey, DateTime now)
    {
        LastDenyReason = null;

        if (_cooldownSeconds > 0 && _lastCallPerNpc.TryGetValue(npcKey, out var last))
        {
            var elapsed = (now - last).TotalSeconds;
            if (elapsed < _cooldownSeconds)
            {
                LastDenyReason = $"NPC cooldown ({elapsed:0.0}s of {_cooldownSeconds}s)";
                return false;
            }
        }

        while (_callsInWindow.Count > 0 && (now - _callsInWindow.Peek()).TotalSeconds >= 60)
        {
            _callsInWindow.Dequeue();
        }

        if (_maxPerMinute > 0 && _callsInWindow.Count >= _maxPerMinute)
        {
            LastDenyReason = $"per-minute cap ({_maxPerMinute})";
            return false;
        }

        if (_budgetDay != now.Date)
        {
            _budgetDay = now.Date;
            _callsToday = 0;
        }

        if (_dailyBudget > 0 && _callsToday >= _dailyBudget)
        {
            LastDenyReason = $"daily budget ({_dailyBudget})";
            return false;
        }

        _lastCallPerNpc[npcKey] = now;
        _callsInWindow.Enqueue(now);
        _callsToday++;
        return true;
    }

    /// <summary>Calls made today, for the startup log.</summary>
    public int CallsToday => _callsToday;
}
