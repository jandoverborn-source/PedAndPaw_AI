using System.Text.RegularExpressions;

namespace PedAndPaw_AI.Core;

/// <summary>
/// Parser and validator for the role-file grammar (v1.1), documented in docs/PROFILES.md.
/// Must stay identical to tools/validate_roles.py; tests/CoreChecks cross-checks both.
///
/// v1.1 adds: an optional === TRAITS === section with [CORE], [QUIRK] and [NAMES]
/// blocks, the optional [Professional] mode, a 64 KB size limit, and placeholder
/// checking. Unknown placeholders are errors; thin trait lists and adjective-like
/// core traits are warnings, because a young species file is allowed to grow.
/// </summary>
public static class RoleParser
{
    /// <summary>Maximum size of a role file in bytes.</summary>
    public const int MaxBytes = 64 * 1024;

    /// <summary>Trait lists below these counts get a warning, not an error.</summary>
    public const int RecommendedCoreTraits = 10;
    public const int RecommendedQuirks = 8;
    public const int RecommendedNames = 10;

    public static readonly string[] KnownPlaceholders =
    {
        "NAME", "SPECIES", "TRAIT_1", "TRAIT_2", "TRAIT_3",
        "QUIRK", "TIME_OF_DAY", "LIGHT", "WEATHER", "AREA"
    };

    private const string RolePrefix = "Role layer,";
    private const string ModesHeader = "=== MODES ===";
    private const string ReactionsHeader = "=== WEAPON REACTIONS ===";
    private const string TraitsHeader = "=== TRAITS ===";

    private static readonly string[] AdjectiveHints =
    {
        "curious", "lazy", "happy", "sad", "angry", "friendly", "kind", "smart",
        "grumpy", "playful", "shy", "brave", "timid", "proud", "warm"
    };

    private static readonly string[] ForbiddenTopicWords =
    {
        "explicit sex", "sexual act", "rape", "porn", "nigger", "faggot",
        "drug deal", "cocaine", "heroin", "meth", "murder", "torture"
    };

    /// <summary>Parse with errors only.</summary>
    public static bool TryParse(string? text, string sourceName, out RoleDefinition role, out IReadOnlyList<string> errors)
        => TryParse(text, sourceName, out role, out errors, out _);

    /// <summary>
    /// Parse one role file. Errors make the file unusable; warnings are advice and the
    /// file still loads.
    /// </summary>
    public static bool TryParse(string? text, string sourceName, out RoleDefinition role,
        out IReadOnlyList<string> errors, out IReadOnlyList<string> warnings)
    {
        var errs = new List<string>();
        var warns = new List<string>();
        role = new RoleDefinition
        {
            SourceName = sourceName,
            Modes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            Reactions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };

        if (text == null)
        {
            errs.Add(sourceName + ":0: no content");
            errors = errs;
            warnings = warns;
            return false;
        }

        if (text.StartsWith("\uFEFF", StringComparison.Ordinal))
            errs.Add(sourceName + ":0: UTF-8 BOM present");

        var byteCount = System.Text.Encoding.UTF8.GetByteCount(text);
        if (byteCount > MaxBytes)
            errs.Add(sourceName + ":0: file exceeds 64 KB limit (" + byteCount + " bytes)");

        if (errs.Count > 0)
        {
            errors = errs;
            warnings = warns;
            return false;
        }

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        // ----- line 1 -----
        var first = lines[0].Trim();
        if (!first.StartsWith(RolePrefix, StringComparison.Ordinal))
        {
            errs.Add(sourceName + ":1: line 1 must be 'Role layer, <Display Name>:'");
            errors = errs;
            warnings = warns;
            return false;
        }

        var afterPrefix = first.Substring(RolePrefix.Length);
        if (!afterPrefix.EndsWith(":", StringComparison.Ordinal))
        {
            errs.Add(sourceName + ":1: role-layer line must end with ':'");
            errors = errs;
            warnings = warns;
            return false;
        }

        var displayName = afterPrefix.Substring(0, afterPrefix.Length - 1).Trim();
        if (displayName.Length < 1 || displayName.Length > 40)
            errs.Add(sourceName + ":1: display name must be 1-40 characters (got " + displayName.Length + ")");
        if (displayName.IndexOf(':') >= 0)
            errs.Add(sourceName + ":1: display name must not contain a colon");
        role.DisplayName = displayName;

        // ----- section headers, in order: MODES, WEAPON REACTIONS, [TRAITS] -----
        var modesIndex = -1;
        var reactionsIndex = -1;
        var traitsIndex = -1;

        for (var i = 1; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal)) continue;
            if (!trimmed.StartsWith("===", StringComparison.Ordinal)) continue;

            if (trimmed == ModesHeader)
            {
                if (modesIndex >= 0) errs.Add(sourceName + ":" + (i + 1) + ": duplicate '=== MODES ===' header");
                else if (reactionsIndex >= 0 || traitsIndex >= 0) errs.Add(sourceName + ":" + (i + 1) + ": '=== MODES ===' must come first");
                else modesIndex = i;
            }
            else if (trimmed == ReactionsHeader)
            {
                if (reactionsIndex >= 0) errs.Add(sourceName + ":" + (i + 1) + ": duplicate '=== WEAPON REACTIONS ===' header");
                else if (modesIndex < 0) errs.Add(sourceName + ":" + (i + 1) + ": '=== WEAPON REACTIONS ===' appears before '=== MODES ==='");
                else if (traitsIndex >= 0) errs.Add(sourceName + ":" + (i + 1) + ": '=== WEAPON REACTIONS ===' must come before '=== TRAITS ==='");
                else reactionsIndex = i;
            }
            else if (trimmed == TraitsHeader)
            {
                if (traitsIndex >= 0) errs.Add(sourceName + ":" + (i + 1) + ": duplicate '=== TRAITS ===' header");
                else if (reactionsIndex < 0) errs.Add(sourceName + ":" + (i + 1) + ": '=== TRAITS ===' must come last, after the weapon reactions");
                else traitsIndex = i;
            }
            else
            {
                errs.Add(sourceName + ":" + (i + 1) + ": unknown section header '" + trimmed + "'");
            }
        }

        if (modesIndex < 0) errs.Add(sourceName + ":1: missing section header '=== MODES ==='");
        if (reactionsIndex < 0) errs.Add(sourceName + ":1: missing section header '=== WEAPON REACTIONS ==='");

        // ----- prose -----
        if (modesIndex >= 0)
        {
            var prose = new List<string>();
            for (var i = 1; i < modesIndex; i++)
            {
                var trimmed = lines[i].Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal)) continue;
                prose.Add(trimmed);
            }

            if (prose.Count == 0) errs.Add(sourceName + ":2: missing prose before the first section header");
            else role.Prose = string.Join("\n", prose);
        }

        // ----- modes: five required, Professional optional -----
        if (modesIndex >= 0)
        {
            var end = reactionsIndex >= 0 ? reactionsIndex : (traitsIndex >= 0 ? traitsIndex : lines.Length);
            var blocks = ParseBlocks(lines, modesIndex + 1, end, sourceName, errs);
            var allowed = Labels.Modes.Concat(new[] { "Professional" }).ToArray();
            foreach (var actual in blocks.Keys)
            {
                if (!Array.Exists(allowed, a => string.Equals(a, actual, StringComparison.OrdinalIgnoreCase)))
                    errs.Add(sourceName + ":unknown: unknown mode label '" + actual + "'");
            }
            foreach (var required in Labels.Modes)
            {
                if (!blocks.ContainsKey(required)) errs.Add(sourceName + ":unknown: missing mode label '" + required + "'");
            }
            role.Modes = blocks;
        }

        // ----- reactions: three required -----
        if (reactionsIndex >= 0)
        {
            var end = traitsIndex >= 0 ? traitsIndex : lines.Length;
            var blocks = ParseBlocks(lines, reactionsIndex + 1, end, sourceName, errs);
            foreach (var actual in blocks.Keys)
            {
                if (!Array.Exists(Labels.Reactions, a => string.Equals(a, actual, StringComparison.OrdinalIgnoreCase)))
                    errs.Add(sourceName + ":unknown: unknown reaction label '" + actual + "'");
            }
            foreach (var required in Labels.Reactions)
            {
                if (!blocks.ContainsKey(required)) errs.Add(sourceName + ":unknown: missing reaction label '" + required + "'");
            }
            role.Reactions = blocks;
        }

        // ----- traits (optional) -----
        if (traitsIndex >= 0)
        {
            ParseTraits(lines, traitsIndex + 1, sourceName, role, errs, warns);
        }

        CheckPlaceholders(role, sourceName, errs);
        CheckContent(role, sourceName, warns);

        errors = errs;
        warnings = warns;
        return errs.Count == 0;
    }

    private static Dictionary<string, string> ParseBlocks(string[] lines, int start, int end, string sourceName, List<string> errs)
    {
        var blocks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? currentLabel = null;
        var body = new List<string>();
        var labelLine = 0;

        void Flush()
        {
            if (currentLabel == null) return;
            var bodyText = string.Join("\n", body).Trim();
            if (bodyText.Length == 0)
                errs.Add(sourceName + ":" + labelLine + ": label '" + currentLabel + "' has empty body");
            else
                blocks[currentLabel] = bodyText;
            currentLabel = null;
            body.Clear();
        }

        for (var i = start; i < end; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith("#", StringComparison.Ordinal)) continue;

            if (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.IndexOf(']') > 1)
            {
                Flush();
                var close = trimmed.IndexOf(']');
                currentLabel = trimmed.Substring(1, close - 1).Trim();
                labelLine = i + 1;
                var rest = trimmed.Substring(close + 1).Trim();
                if (rest.Length > 0) body.Add(rest);
                if (blocks.ContainsKey(currentLabel))
                    errs.Add(sourceName + ":" + labelLine + ": duplicate label '" + currentLabel + "'");
                continue;
            }

            if (currentLabel != null && trimmed.Length > 0) body.Add(trimmed);
        }

        Flush();
        return blocks;
    }

    private static void ParseTraits(string[] lines, int start, string sourceName, RoleDefinition role,
        List<string> errs, List<string> warns)
    {
        var current = "";
        var items = new List<string>();
        var blockLine = 0;

        void Flush()
        {
            if (current.Length == 0) return;

            var cleaned = items.Where(item => item.Trim().Length > 0).Select(item => item.Trim()).ToList();
            switch (current.ToUpperInvariant())
            {
                case "CORE":
                    role.CoreTraits = cleaned;
                    if (cleaned.Count > 0 && cleaned.Count < RecommendedCoreTraits)
                        warns.Add(sourceName + ":" + blockLine + ": only " + cleaned.Count + " core traits; " + RecommendedCoreTraits + " recommended");
                    foreach (var trait in cleaned)
                    {
                        var lower = trait.ToLowerInvariant();
                        if (Array.Exists(AdjectiveHints, a => lower == a || lower.StartsWith(a + " ")))
                            warns.Add(sourceName + ":" + blockLine + ": core trait '" + trait + "' is an adjective; describe a behaviour instead");
                    }
                    break;
                case "QUIRK":
                    role.Quirks = cleaned;
                    if (cleaned.Count > 0 && cleaned.Count < RecommendedQuirks)
                        warns.Add(sourceName + ":" + blockLine + ": only " + cleaned.Count + " quirks; " + RecommendedQuirks + " recommended");
                    break;
                case "NAMES":
                    role.Names = cleaned;
                    if (cleaned.Count > 0 && cleaned.Count < RecommendedNames)
                        warns.Add(sourceName + ":" + blockLine + ": only " + cleaned.Count + " names; " + RecommendedNames + " recommended");
                    break;
                default:
                    errs.Add(sourceName + ":" + blockLine + ": unknown traits block '[" + current + "]'; use [CORE], [QUIRK] or [NAMES]");
                    break;
            }

            current = "";
            items.Clear();
        }

        for (var i = start; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith("#", StringComparison.Ordinal)) continue;

            if (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.IndexOf(']') > 1)
            {
                Flush();
                var close = trimmed.IndexOf(']');
                current = trimmed.Substring(1, close - 1).Trim();
                blockLine = i + 1;
                continue;
            }

            if (current.Length > 0 && trimmed.Length > 0) items.Add(trimmed);
        }

        Flush();
    }

    private static void CheckPlaceholders(RoleDefinition role, string sourceName, List<string> errs)
    {
        var texts = new List<string> { role.Prose };
        foreach (var mode in role.Modes.Values) texts.Add(mode);
        foreach (var reaction in role.Reactions.Values) texts.Add(reaction);

        foreach (var text in texts)
        {
            foreach (Match match in Regex.Matches(text ?? "", @"\{([A-Za-z0-9_]+)\}"))
            {
                var name = match.Groups[1].Value;
                if (!Array.Exists(KnownPlaceholders, p => string.Equals(p, name, StringComparison.OrdinalIgnoreCase)))
                    errs.Add(sourceName + ":unknown placeholder '{" + name + "}'; known placeholders are " +
                             string.Join(", ", KnownPlaceholders.Select(p => "{" + p + "}")));
            }
        }
    }

    private static void CheckContent(RoleDefinition role, string sourceName, List<string> warns)
    {
        var all = new List<string> { role.Prose };
        all.AddRange(role.Modes.Values);
        all.AddRange(role.Reactions.Values);
        all.AddRange(role.CoreTraits);
        all.AddRange(role.Quirks);
        all.AddRange(role.Names);

        foreach (var text in all)
        {
            if (string.IsNullOrEmpty(text)) continue;
            var lower = text.ToLowerInvariant();
            foreach (var word in ForbiddenTopicWords)
            {
                if (Regex.IsMatch(lower, @"(?<![a-z])" + Regex.Escape(word) + @"(?![a-z])"))
                    warns.Add(sourceName + ":content: '" + word + "' appears in the role file; the 13+ policy should keep this out of character text");
            }
        }
    }

    /// <summary>Parse every .txt under a directory (recursively), reporting both lists.</summary>
    public static Dictionary<string, RoleDefinition> LoadDirectory(string directory,
        Action<string, IReadOnlyList<string>> onError, Action<string>? onWarning = null)
    {
        var loaded = new Dictionary<string, RoleDefinition>(StringComparer.OrdinalIgnoreCase);
        if (!System.IO.Directory.Exists(directory)) return loaded;

        foreach (var file in System.IO.Directory.GetFiles(directory, "*.txt", System.IO.SearchOption.AllDirectories))
        {
            string text;
            try
            {
                text = System.IO.File.ReadAllText(file);
            }
            catch (Exception ex)
            {
                onError(file, new[] { file + ":0: cannot read: " + ex.Message });
                continue;
            }

            if (TryParse(text, file, out var role, out var errors, out var warnings))
            {
                loaded[role.DisplayName] = role;
                if (onWarning != null) foreach (var warning in warnings) onWarning(warning);
            }
            else
            {
                onError(file, errors);
            }
        }

        return loaded;
    }
}
