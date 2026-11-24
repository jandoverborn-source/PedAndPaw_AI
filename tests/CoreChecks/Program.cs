using PedAndPaw_AI.Core;

// Console checks for the game-independent core. No test framework, so it runs
// anywhere dotnet runs: exit 0 when everything passes, 1 with a clear list otherwise.

var failures = new List<string>();
var passes = 0;

void Check(string name, Action test)
{
    try
    {
        test();
        passes++;
        Console.WriteLine("PASS " + name);
    }
    catch (Exception ex)
    {
        failures.Add(name + ": " + ex.Message);
        Console.WriteLine("FAIL " + name + ": " + ex.Message);
    }
}

void AssertTrue(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

void AssertEqual<T>(T expected, T actual, string what)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new Exception($"{what}: expected '{expected}', got '{actual}'");
}

void AssertContains(string haystack, string needle, string what)
{
    if (haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0)
        throw new Exception($"{what}: '{needle}' not found in the text");
}

string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "scripts", "PedAndPaw_AI.ini")) &&
            Directory.Exists(Path.Combine(dir.FullName, "src", "Core")))
        {
            return dir.FullName;
        }
        dir = dir.Parent;
    }
    throw new Exception("repository root not found from " + AppContext.BaseDirectory);
}

var root = FindRepoRoot();
var iniPath = Path.Combine(root, "scripts", "PedAndPaw_AI.ini");
var rolesPath = Path.Combine(root, "scripts", "PedAndPaw_AI", "roles");

// 1. Defaults match the documented ini values.
Check("defaults match the ini", () =>
{
    var c = new ModConfig();
    AssertEqual(4.0f, c.InteractionRange, "InteractionRange");
    AssertEqual("gemini-2.5-flash", c.Model, "Model");
    AssertEqual(120, c.MaxTokensPerReply, "MaxTokensPerReply");
    AssertEqual(0, c.NudityPercent, "NudityPercent");
    AssertEqual(50, c.DefaultTrust, "DefaultTrust");
    AssertEqual(6, c.MemoryMessagesPerNpc, "MemoryMessagesPerNpc");
    AssertTrue(c.NpcSelfAwareness, "NpcSelfAwareness should default to true");
    AssertTrue(c.Enabled, "Enabled should default to true");
});

// 2. The real ini parses without warnings and keeps its values.
Check("real ini parses cleanly", () =>
{
    var config = ModConfig.FromIniText(File.ReadAllText(iniPath));
    AssertEqual(0, config.Warnings.Count, "warnings: " + string.Join("; ", config.Warnings));
    AssertEqual(4.0f, config.InteractionRange, "InteractionRange");
    AssertEqual(20, config.MaxRequestsPerMinute, "MaxRequestsPerMinute");
    AssertEqual(500, config.DailyRequestBudget, "DailyRequestBudget");
    AssertEqual("PEDANDPAW_AI_KEY", config.ApiKeyEnvVar, "ApiKeyEnvVar");
    AssertTrue(config.AllowShootingIn.Length == 2, "AllowShootingIn should list two zones");
});

// 3. Unknown keys warn, bad values fall back, nothing throws.
Check("bad ini input warns instead of throwing", () =>
{
    var config = ModConfig.FromIniText("[General]\nEnabled = maybe\nNonsense = 1\n[Limits]\nMaxTokensPerReply = abc\n");
    AssertTrue(config.Warnings.Count >= 2, "expected warnings for the unknown key and the bad values");
    AssertEqual(120, config.MaxTokensPerReply, "bad int falls back to the default");
});

// 4. Every shipped role file parses with the five modes and three reactions.
var roleFiles = Directory.GetFiles(rolesPath, "*.txt", SearchOption.AllDirectories).OrderBy(p => p).ToArray();
Check("three role files are shipped", () => AssertEqual(3, roleFiles.Length, "role file count"));

foreach (var file in roleFiles)
{
    Check("role file parses: " + Path.GetFileName(file), () =>
    {
        var ok = RoleParser.TryParse(File.ReadAllText(file), Path.GetFileName(file), out var role, out var errors);
        AssertTrue(ok, "parse errors: " + string.Join("; ", errors));
        AssertEqual(5, role.Modes.Count, "mode count");
        AssertEqual(3, role.Reactions.Count, "reaction count");
        AssertTrue(!string.IsNullOrWhiteSpace(role.Prose), "prose must not be empty");
    });
}

// 5. The reject cases from the locked grammar.
var policeText = File.ReadAllText(Path.Combine(rolesPath, "humans", "police.txt"));
Check("display names survive parsing", () =>
{
    var ok = RoleParser.TryParse(policeText, "police.txt", out var role, out _);
    AssertTrue(ok, "police.txt should parse");
    AssertEqual("Police", role.DisplayName, "display name");
});

void ExpectReject(string label, string text)
{
    Check("rejects " + label, () =>
    {
        var ok = RoleParser.TryParse(text, "bad.txt", out _, out var errors);
        AssertTrue(!ok, "expected a parse error");
        AssertTrue(errors.Count > 0, "expected at least one error message");
    });
}

ExpectReject("missing prose", "Role layer, Test:\n=== MODES ===\n[Friendly] a\n[Grumpy] b\n[Scared] c\n[Aggressive] d\n[Philosophical] e\n=== WEAPON REACTIONS ===\n[WeaponDrawn] a\n[WeaponAimed] b\n[WeaponFired] c\n");
ExpectReject("comment on line 1", "# comment\nRole layer, Test:\nprose\n=== MODES ===\n[Friendly] a\n[Grumpy] b\n[Scared] c\n[Aggressive] d\n[Philosophical] e\n=== WEAPON REACTIONS ===\n[WeaponDrawn] a\n[WeaponAimed] b\n[WeaponFired] c\n");
ExpectReject("wrong header order", "Role layer, Test:\nprose\n=== WEAPON REACTIONS ===\n[WeaponDrawn] a\n[WeaponAimed] b\n[WeaponFired] c\n=== MODES ===\n[Friendly] a\n[Grumpy] b\n[Scared] c\n[Aggressive] d\n[Philosophical] e\n");
ExpectReject("unknown mode label", "Role layer, Test:\nprose\n=== MODES ===\n[Friendly] a\n[Grumpy] b\n[Scared] c\n[Aggressive] d\n[Philosophical] e\n[Unknown] f\n=== WEAPON REACTIONS ===\n[WeaponDrawn] a\n[WeaponAimed] b\n[WeaponFired] c\n");
ExpectReject("missing reaction label", "Role layer, Test:\nprose\n=== MODES ===\n[Friendly] a\n[Grumpy] b\n[Scared] c\n[Aggressive] d\n[Philosophical] e\n=== WEAPON REACTIONS ===\n[WeaponDrawn] a\n[WeaponAimed] b\n");
ExpectReject("duplicate mode label", "Role layer, Test:\nprose\n=== MODES ===\n[Friendly] a\n[Friendly] b\n[Grumpy] b\n[Scared] c\n[Aggressive] d\n[Philosophical] e\n=== WEAPON REACTIONS ===\n[WeaponDrawn] a\n[WeaponAimed] b\n[WeaponFired] c\n");
ExpectReject("empty body", "Role layer, Test:\nprose\n=== MODES ===\n[Friendly]\n[Grumpy] b\n[Scared] c\n[Aggressive] d\n[Philosophical] e\n=== WEAPON REACTIONS ===\n[WeaponDrawn] a\n[WeaponAimed] b\n[WeaponFired] c\n");
ExpectReject("colon in the display name", "Role layer, Bad:Name:\nprose\n=== MODES ===\n[Friendly] a\n[Grumpy] b\n[Scared] c\n[Aggressive] d\n[Philosophical] e\n=== WEAPON REACTIONS ===\n[WeaponDrawn] a\n[WeaponAimed] b\n[WeaponFired] c\n");
ExpectReject("a file over 8 KB", "Role layer, Test:\n" + new string('x', 8200) + "\n=== MODES ===\n[Friendly] a\n[Grumpy] b\n[Scared] c\n[Aggressive] d\n[Philosophical] e\n=== WEAPON REACTIONS ===\n[WeaponDrawn] a\n[WeaponAimed] b\n[WeaponFired] c\n");

// 6. Prompt building follows the ini switches.
RoleParser.TryParse(policeText, "police.txt", out var police, out _);
var strictConfig = ModConfig.FromIniText(File.ReadAllText(iniPath));

Check("prompt forbids sexual content at NudityPercent 0", () =>
{
    var prompt = PromptBuilder.BuildSystemPrompt(police, strictConfig, "Friendly");
    AssertContains(prompt, "no sexual", "content rule");
    AssertContains(prompt, "Friendly", "mode section");
    AssertContains(prompt, "harm", "animal rule");
});

Check("prompt admits being an NPC when NpcSelfAwareness is on", () =>
{
    var prompt = PromptBuilder.BuildSystemPrompt(police, strictConfig, "Friendly");
    AssertContains(prompt, "computer-controlled", "self awareness rule");
});

Check("weapon reaction appears only when the weapon state applies", () =>
{
    var armed = new NpcContext { RoleLayer = "Police", ModeName = "Friendly", WeaponDrawn = true, Aiming = true, DistanceMeters = 2.5f };
    var message = PromptBuilder.BuildUserMessage(armed, "Evening, officer.", strictConfig, police);
    AssertContains(message, "WeaponAimed", "the aimed label");
    AssertContains(message, police.Reactions["WeaponAimed"], "the reaction body");

    var calm = new NpcContext { RoleLayer = "Police", ModeName = "Friendly", DistanceMeters = 3.0f };
    var calmMessage = PromptBuilder.BuildUserMessage(calm, "Evening, officer.", strictConfig, police);
    AssertTrue(calmMessage.IndexOf("WeaponAimed", StringComparison.OrdinalIgnoreCase) < 0,
        "no weapon reaction should appear when no weapon is out");
});

// 7. The rate limiter enforces all three limits.
Check("per-NPC cooldown", () =>
{
    var limiter = new RateLimiter(8, 20, 500);
    var t0 = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    AssertTrue(limiter.TryAcquire("ped-1", t0), "first call should pass");
    AssertTrue(!limiter.TryAcquire("ped-1", t0.AddSeconds(5)), "second call inside the cooldown must be refused");
    AssertTrue(limiter.TryAcquire("ped-2", t0.AddSeconds(5)), "a different NPC is not affected");
    AssertTrue(limiter.TryAcquire("ped-1", t0.AddSeconds(9)), "after the cooldown the call passes again");
});

Check("per-minute cap", () =>
{
    var limiter = new RateLimiter(0, 2, 0);
    var t0 = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    AssertTrue(limiter.TryAcquire("a", t0), "call 1");
    AssertTrue(limiter.TryAcquire("b", t0.AddSeconds(1)), "call 2");
    AssertTrue(!limiter.TryAcquire("c", t0.AddSeconds(2)), "call 3 must be refused by the cap");
    AssertTrue(limiter.TryAcquire("c", t0.AddSeconds(61)), "after a minute the cap frees up");
});

Check("daily budget", () =>
{
    var limiter = new RateLimiter(0, 0, 2);
    var t0 = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    AssertTrue(limiter.TryAcquire("a", t0), "call 1");
    AssertTrue(limiter.TryAcquire("b", t0.AddSeconds(1)), "call 2");
    AssertTrue(!limiter.TryAcquire("c", t0.AddSeconds(2)), "call 3 must be refused by the budget");
    AssertTrue(limiter.TryAcquire("c", t0.AddDays(1)), "the budget resets the next day");
});

Console.WriteLine();
Console.WriteLine(failures.Count == 0
    ? $"ALL {passes} CHECKS PASSED"
    : $"{failures.Count} CHECK(S) FAILED:\n  " + string.Join("\n  ", failures));
return failures.Count == 0 ? 0 : 1;
