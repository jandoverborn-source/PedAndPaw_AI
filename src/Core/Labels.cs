namespace PedAndPaw_AI.Core;

/// <summary>
/// Fixed mode and reaction label sets matching docs/PROFILES.md.
/// </summary>
public static class Labels
{
    /// <summary>The five mode labels.</summary>
    public static readonly string[] Modes = { "Friendly", "Grumpy", "Scared", "Aggressive", "Philosophical" };

    /// <summary>The three weapon reaction labels.</summary>
    public static readonly string[] Reactions = { "WeaponDrawn", "WeaponAimed", "WeaponFired" };
}