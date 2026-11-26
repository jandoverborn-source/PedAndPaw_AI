namespace PedAndPaw_AI.Core;

/// <summary>
/// Turns exact game values into the coarse buckets a character may know about.
/// The model never receives a clock time: a small model that is told "18:42" will
/// happily invent "it is 11 am", but a bucket cannot contradict the game.
/// </summary>
public static class ContextBuckets
{
    /// <summary>morning, noon, afternoon, evening or night.</summary>
    public static string TimeOfDay(int hour)
    {
        if (hour < 5) return "night";
        if (hour < 11) return "morning";
        if (hour < 14) return "noon";
        if (hour < 18) return "afternoon";
        if (hour < 22) return "evening";
        return "night";
    }

    /// <summary>daylight, dusk or darkness.</summary>
    public static string Light(int hour)
    {
        if (hour >= 7 && hour < 18) return "daylight";
        if (hour == 5 || hour == 6 || hour == 18 || hour == 19) return "dusk";
        return "darkness";
    }

    /// <summary>clear, cloudy, rain or fog, from a GTA weather name.</summary>
    public static string Weather(string? gtaWeather)
    {
        if (string.IsNullOrWhiteSpace(gtaWeather)) return "";
        var name = gtaWeather!.Trim().ToLowerInvariant();

        if (name.Contains("fog") || name.Contains("smog")) return "fog";
        if (name.Contains("rain") || name.Contains("thunder") || name.Contains("snow") ||
            name.Contains("clearing") || name.Contains("blizzard")) return "rain";
        if (name.Contains("cloud") || name.Contains("overcast") || name.Contains("neutral")) return "cloudy";
        return "clear";
    }

    /// <summary>
    /// The professional mode is meant to be repeatable, so it gets a low temperature
    /// no matter what the ini says (the review asked for roughly 0.2 to 0.3).
    /// </summary>
    public static float TemperatureFor(string? modeName, float configured)
    {
        if (string.Equals(modeName, "Professional", StringComparison.OrdinalIgnoreCase)) return 0.25f;
        return configured;
    }
}

/// <summary>The seeded personality of one individual, drawn from a role's trait pools.</summary>
public class TraitSelection
{
    public string Name { get; set; } = "";
    public string Species { get; set; } = "";
    public string Trait1 { get; set; } = "";
    public string Trait2 { get; set; } = "";
    public string Trait3 { get; set; } = "";
    public string Quirk { get; set; } = "";

    /// <summary>
    /// Placeholder values ready for PromptBuilder. Empty values get a neutral
    /// substitute, so a role file that uses {NAME} before it has any trait pools
    /// never sends the model an empty gap like "You are , a ...".
    /// </summary>
    public Dictionary<string, string> ToPlaceholders()
    {
        var name = string.IsNullOrWhiteSpace(Name) ? Species : Name;
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["NAME"] = string.IsNullOrWhiteSpace(name) ? "someone" : name,
            ["SPECIES"] = string.IsNullOrWhiteSpace(Species) ? "a local" : Species,
            ["TRAIT_1"] = Neutral(Trait1, "ordinary in most things"),
            ["TRAIT_2"] = Neutral(Trait2, "keeps to themselves"),
            ["TRAIT_3"] = Neutral(Trait3, "notices the small things"),
            ["QUIRK"] = Neutral(Quirk, "no particular quirk")
        };
    }

    private static string Neutral(string value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value;
}

/// <summary>
/// Chooses 3 core traits, 0 or 1 quirk and 1 name from a role's pools. The seed is the
/// ped's handle, so the same character is the same person for a whole session.
/// </summary>
public static class TraitSelector
{
    public static TraitSelection Select(RoleDefinition role, int seed)
    {
        var selection = new TraitSelection
        {
            Species = role.DisplayName,
            Name = role.DisplayName
        };

        var random = new Random(seed * 397 ^ role.DisplayName.GetHashCode());

        if (role.Names.Count > 0)
            selection.Name = role.Names[random.Next(role.Names.Count)];

        selection.Trait1 = Pick(role.CoreTraits, random, 0);
        selection.Trait2 = Pick(role.CoreTraits, random, 1);
        selection.Trait3 = Pick(role.CoreTraits, random, 2);

        // Quirks are optional in the grammar and "0 or 1" in the design: half get one.
        if (role.Quirks.Count > 0 && random.Next(2) == 0)
            selection.Quirk = role.Quirks[random.Next(role.Quirks.Count)];

        return selection;
    }

    /// <summary>Distinct trait where possible, a repeat when the pool is short.</summary>
    private static string Pick(IReadOnlyList<string> pool, Random random, int index)
    {
        if (pool.Count == 0) return "";
        if (index < pool.Count)
        {
            // Walk the pool from a random offset so the three picks stay distinct.
            return pool[(random.Next(pool.Count) + index) % pool.Count];
        }
        return pool[random.Next(pool.Count)];
    }
}
