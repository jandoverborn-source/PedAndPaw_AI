namespace PedAndPaw_AI.Core;

/// <summary>
/// A parsed role definition from a role .txt file (grammar v1.1).
/// </summary>
public class RoleDefinition
{
    /// <summary>The display name (text after "Role layer," and before ":").</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>Free prose from below line 1 up to the first section header.</summary>
    public string Prose { get; set; } = "";

    /// <summary>Modes dictionary: label -> body text. Five required, Professional optional.</summary>
    public IReadOnlyDictionary<string, string> Modes { get; set; } = new Dictionary<string, string>();

    /// <summary>Reactions dictionary: label -> body text.</summary>
    public IReadOnlyDictionary<string, string> Reactions { get; set; } = new Dictionary<string, string>();

    /// <summary>Optional core traits, one behaviour per entry ("chases butterflies").</summary>
    public IReadOnlyList<string> CoreTraits { get; set; } = new List<string>();

    /// <summary>Optional quirks, one per entry ("believes the player is furniture").</summary>
    public IReadOnlyList<string> Quirks { get; set; } = new List<string>();

    /// <summary>Optional names this character can be given.</summary>
    public IReadOnlyList<string> Names { get; set; } = new List<string>();

    /// <summary>True when the file carries a traits section at all.</summary>
    public bool HasTraits => CoreTraits.Count > 0 || Quirks.Count > 0 || Names.Count > 0;

    /// <summary>Source name of the role file (usually the filename).</summary>
    public string SourceName { get; set; } = "";
}
