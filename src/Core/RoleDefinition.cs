namespace PedAndPaw_AI.Core;

/// <summary>
/// A parsed role definition from a role .txt file.
/// </summary>
public class RoleDefinition
{
    /// <summary>The display name (text after "Role layer," and before ":").</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>Free prose text from line 1 after the role-layer line.</summary>
    public string Prose { get; set; } = "";

    /// <summary>Modes dictionary: label -> body text.</summary>
    public IReadOnlyDictionary<string, string> Modes { get; set; } = new Dictionary<string, string>();

    /// <summary>Reactions dictionary: label -> body text.</summary>
    public IReadOnlyDictionary<string, string> Reactions { get; set; } = new Dictionary<string, string>();

    /// <summary>Source name of the role file (usually the filename).</summary>
    public string SourceName { get; set; } = "";
}