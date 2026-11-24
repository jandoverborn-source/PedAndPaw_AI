namespace PedAndPaw_AI.Core;

/// <summary>
/// Everything the game knows about the current situation, as seen by the model.
/// A field is left empty when the matching Use* flag in the ini is disabled, so
/// "empty" always means "the player switched this off", never "I guessed".
/// </summary>
public class NpcContext
{
    /// <summary>Role layer of the NPC (from the role file's first line).</summary>
    public string RoleLayer { get; set; } = "";

    /// <summary>Current mode label, for example Friendly.</summary>
    public string ModeName { get; set; } = "";

    /// <summary>Name the game shows for the player, if known.</summary>
    public string PlayerName { get; set; } = "";

    /// <summary>Time of day, for example "18:42" or "evening".</summary>
    public string TimeOfDay { get; set; } = "";

    /// <summary>Weather, for example "Clear" or "Rain".</summary>
    public string Weather { get; set; } = "";

    /// <summary>Area or district name, if the game exposes one.</summary>
    public string Area { get; set; } = "";

    /// <summary>Display name of the vehicle the player is in, if any.</summary>
    public string Vehicle { get; set; } = "";

    /// <summary>The player has a weapon drawn.</summary>
    public bool WeaponDrawn { get; set; }

    /// <summary>The player is aiming.</summary>
    public bool Aiming { get; set; }

    /// <summary>Player wanted level, 0 when clean.</summary>
    public int WantedLevel { get; set; }

    /// <summary>Shots were fired nearby within GunshotMemorySeconds.</summary>
    public bool RecentGunshots { get; set; }

    /// <summary>The NPC is an animal rather than a human.</summary>
    public bool IsAnimal { get; set; }

    /// <summary>Distance from the player in metres.</summary>
    public float DistanceMeters { get; set; }
}
