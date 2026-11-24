namespace PedAndPaw_AI.Core;

/// <summary>
/// Parser and validator for the locked role-file grammar in docs/PROFILES.md.
/// Behaviour must stay identical to tools/validate_roles.py; tests/CoreChecks
/// cross-checks this class against the shipped role files.
/// </summary>
public static class RoleParser
{
    /// <summary>Maximum size of a role file in bytes.</summary>
    public const int MaxBytes = 8 * 1024;

    private const string RolePrefix = "Role layer,";
    private const string ModesHeader = "=== MODES ===";
    private const string ReactionsHeader = "=== WEAPON REACTIONS ===";

    /// <summary>
    /// Parse one role file. Returns true only when the file satisfies the grammar.
    /// Every error message starts with "sourceName:line:" so a user can jump straight to it.
    /// </summary>
    public static bool TryParse(string? text, string sourceName, out RoleDefinition role, out IReadOnlyList<string> errors)
    {
        var errs = new List<string>();
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
            return false;
        }

        if (text.StartsWith("\uFEFF", StringComparison.Ordinal))
            errs.Add(sourceName + ":0: UTF-8 BOM present");

        var byteCount = System.Text.Encoding.UTF8.GetByteCount(text);
        if (byteCount > MaxBytes)
            errs.Add(sourceName + ":0: file exceeds 8 KB limit (" + byteCount + " bytes)");

        if (errs.Count > 0)
        {
            errors = errs;
            return false;
        }

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        // ----- line 1: Role layer, <Name>: -----
        var first = lines[0].Trim();
        if (!first.StartsWith(RolePrefix, StringComparison.Ordinal))
        {
            errs.Add(sourceName + ":1: line 1 must be 'Role layer, <Display Name>:'");
            errors = errs;
            return false;
        }

        var afterPrefix = first.Substring(RolePrefix.Length);
        if (!afterPrefix.EndsWith(":", StringComparison.Ordinal))
        {
            errs.Add(sourceName + ":1: role-layer line must end with ':'");
            errors = errs;
            return false;
        }

        var displayName = afterPrefix.Substring(0, afterPrefix.Length - 1).Trim();
        if (displayName.Length < 1 || displayName.Length > 40)
            errs.Add(sourceName + ":1: display name must be 1-40 characters (got " + displayName.Length + ")");
        if (displayName.IndexOf(':') >= 0)
            errs.Add(sourceName + ":1: display name must not contain a colon");
        role.DisplayName = displayName;

        // ----- locate the two section headers, in order -----
        var modesIndex = -1;
        var reactionsIndex = -1;

        for (var i = 1; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal))
                continue;

            if (modesIndex < 0)
            {
                if (trimmed == ModesHeader) modesIndex = i;
                else if (trimmed == ReactionsHeader)
                    errs.Add(sourceName + ":" + (i + 1) + ": '=== WEAPON REACTIONS ===' appears before '=== MODES ==='");
                continue;
            }

            if (reactionsIndex < 0)
            {
                if (trimmed == ReactionsHeader) reactionsIndex = i;
                else if (trimmed == ModesHeader)
                    errs.Add(sourceName + ":" + (i + 1) + ": duplicate '=== MODES ===' header");
                continue;
            }

            if (trimmed == ModesHeader || trimmed == ReactionsHeader)
                errs.Add(sourceName + ":" + (i + 1) + ": duplicate section header '" + trimmed + "'");
        }

        if (modesIndex < 0)
            errs.Add(sourceName + ":1: missing section header '=== MODES ==='");
        if (reactionsIndex < 0)
            errs.Add(sourceName + ":1: missing section header '=== WEAPON REACTIONS ==='");

        // ----- prose between line 1 and the first header -----
        if (modesIndex >= 0)
        {
            var prose = new List<string>();
            for (var i = 1; i < modesIndex; i++)
            {
                var trimmed = lines[i].Trim();
                if (trimmed.Length == 0) continue;
                if (trimmed.StartsWith("#", StringComparison.Ordinal)) continue;
                if (trimmed.StartsWith("===", StringComparison.Ordinal)) continue;
                prose.Add(trimmed);
            }

            if (prose.Count == 0)
                errs.Add(sourceName + ":2: missing prose before the first section header");
            else
                role.Prose = string.Join("\n", prose);
        }

        // ----- the two label sections -----
        if (modesIndex >= 0)
        {
            var end = reactionsIndex >= 0 ? reactionsIndex : lines.Length;
            var blocks = ParseBlocks(lines, modesIndex + 1, end, sourceName, errs);
            ValidateLabelSet(blocks, Labels.Modes, "mode", sourceName, errs);
            role.Modes = blocks;
        }

        if (reactionsIndex >= 0)
        {
            var blocks = ParseBlocks(lines, reactionsIndex + 1, lines.Length, sourceName, errs);
            ValidateLabelSet(blocks, Labels.Reactions, "reaction", sourceName, errs);
            role.Reactions = blocks;
        }

        if (errs.Count > 0)
        {
            errors = errs;
            return false;
        }

        errors = Array.Empty<string>();
        return true;
    }

    /// <summary>Parse "[Label] body..." blocks between two line indexes.</summary>
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

            if (currentLabel != null && trimmed.Length > 0)
                body.Add(trimmed);
        }

        Flush();
        return blocks;
    }

    /// <summary>Compare parsed labels with the fixed set, case-insensitively, reporting canonical names.</summary>
    private static void ValidateLabelSet(Dictionary<string, string> blocks, string[] expected, string kind, string sourceName, List<string> errs)
    {
        foreach (var actual in blocks.Keys)
        {
            var known = false;
            foreach (var wanted in expected)
                if (string.Equals(wanted, actual, StringComparison.OrdinalIgnoreCase)) known = true;
            if (!known)
                errs.Add(sourceName + ":unknown: unknown " + kind + " label '" + actual + "'");
        }

        foreach (var wanted in expected)
        {
            if (!blocks.ContainsKey(wanted))
                errs.Add(sourceName + ":unknown: missing " + kind + " label '" + wanted + "'");
        }
    }

    /// <summary>
    /// Parse every .txt under a directory (recursively). A rejected file is reported
    /// through <paramref name="onError"/>; it never stops the rest from loading.
    /// </summary>
    public static Dictionary<string, RoleDefinition> LoadDirectory(string directory, Action<string, IReadOnlyList<string>> onError)
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

            if (TryParse(text, file, out var role, out var errors))
                loaded[role.DisplayName] = role;
            else
                onError(file, errors);
        }

        return loaded;
    }
}
