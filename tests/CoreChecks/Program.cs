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
    AssertEqual("gemini-3-flash-preview", c.Model, "Model");
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
Check("the shipped role files are present and named", () =>
{
    AssertTrue(roleFiles.Length >= 4, "expected at least 4 role files, got " + roleFiles.Length);
    var names = roleFiles.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
    foreach (var expected in new[] { "police", "civilian", "street_cat", "mountain_lion" })
        AssertTrue(names.Contains(expected), "missing role file: " + expected);
});

foreach (var file in roleFiles)
{
    Check("role file parses: " + Path.GetFileName(file), () =>
    {
        var ok = RoleParser.TryParse(File.ReadAllText(file), Path.GetFileName(file), out var role, out var errors);
        AssertTrue(ok, "parse errors: " + string.Join("; ", errors));
        AssertTrue(role.Modes.Count >= 5 && role.Modes.Count <= 6,
            "mode count should be 5, plus the optional Professional, got " + role.Modes.Count);
        foreach (var required in Labels.Modes)
            AssertTrue(role.Modes.ContainsKey(required), "missing mode " + required);
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
ExpectReject("a file over 64 KB", "Role layer, Test:\n" + new string('x', 66000) + "\n=== MODES ===\n[Friendly] a\n[Grumpy] b\n[Scared] c\n[Aggressive] d\n[Philosophical] e\n=== WEAPON REACTIONS ===\n[WeaponDrawn] a\n[WeaponAimed] b\n[WeaponFired] c\n");

// 6. Prompt building follows the ini switches.
RoleParser.TryParse(policeText, "police.txt", out var police, out _);
var strictConfig = ModConfig.FromIniText(File.ReadAllText(iniPath));

Check("prompt forbids nudity and sex at NudityPercent 0", () =>
{
    var prompt = PromptBuilder.BuildSystemPrompt(police, strictConfig, "Friendly");
    AssertContains(prompt, "no nudity", "nudity rule at 0");
    AssertContains(prompt, "no sexual", "sexual content rule");
    AssertContains(prompt, "Friendly", "mode section");
    AssertContains(prompt, "harm", "animal rule");
});

Check("prompt allows nudity but never sex above 0", () =>
{
    var nudityConfig = ModConfig.FromIniText(File.ReadAllText(iniPath));
    nudityConfig.NudityPercent = 40;
    var prompt = PromptBuilder.BuildSystemPrompt(police, nudityConfig, "Friendly");
    AssertContains(prompt, "nudity and undressed bodies may be described", "nudity allowed above 0");
    AssertContains(prompt, "never allowed", "sex still forbidden");
    AssertContains(prompt, "13", "teen rating rule");
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

// 7b. Grammar v1.1: traits, the optional Professional mode, placeholders and warnings.
const string v11 = "Role layer, Test Ped:\nA short line.\n=== MODES ===\n[Friendly]\nf\n[Grumpy]\ng\n[Scared]\ns\n[Aggressive]\na\n[Philosophical]\np\n[Professional]\ncalm\n=== WEAPON REACTIONS ===\n[WeaponDrawn]\nd\n[WeaponAimed]\nm\n[WeaponFired]\nf\n=== TRAITS ===\n[CORE]\nchases butterflies\nsleeps in boxes\nbegs for food\n[QUIRK]\nbelieves the player is furniture\n[NAMES]\nSilvia\nMrr\n";

Check("v1.1 parses traits and the professional mode", () =>
{
    var ok = RoleParser.TryParse(v11, "v11.txt", out var role, out var errors, out _);
    AssertTrue(ok, "errors: " + string.Join("; ", errors));
    AssertEqual(6, role.Modes.Count, "mode count");
    AssertTrue(role.Modes.ContainsKey("Professional"), "Professional mode");
    AssertEqual(3, role.CoreTraits.Count, "core traits");
    AssertEqual(1, role.Quirks.Count, "quirks");
    AssertEqual(2, role.Names.Count, "names");
});

Check("v1.1 rejects an unknown placeholder", () =>
{
    var text = v11.Replace("A short line.", "Hello {PLAYER_X}.");
    AssertTrue(!RoleParser.TryParse(text, "bad.txt", out _, out var errors), "expected an error");
    AssertTrue(errors.Count > 0, "expected a message");
});

Check("v1.1 rejects an unknown traits block and traits before modes", () =>
{
    var badBlock = v11.Replace("[QUIRK]", "[WHATEVER]");
    AssertTrue(!RoleParser.TryParse(badBlock, "bad.txt", out _, out var e1), "unknown block should fail");
    AssertTrue(e1.Count > 0, "expected a message");

    var badOrder = "Role layer, Test:\nprose\n=== TRAITS ===\n[CORE]\nx\n=== MODES ===\n[Friendly]\nt\n[Grumpy]\nt\n[Scared]\nt\n[Aggressive]\nt\n[Philosophical]\nt\n=== WEAPON REACTIONS ===\n[WeaponDrawn]\nt\n[WeaponAimed]\nt\n[WeaponFired]\nt\n";
    AssertTrue(!RoleParser.TryParse(badOrder, "bad.txt", out _, out var e2), "traits first should fail");
    AssertTrue(e2.Count > 0, "expected a message");
});

Check("v1.1 warns about adjectives and thin lists instead of failing", () =>
{
    var thin = v11.Replace("[CORE]\nchases butterflies\nsleeps in boxes\nbegs for food\n", "[CORE]\ncurious\n");
    var ok = RoleParser.TryParse(thin, "thin.txt", out _, out var errors, out var warnings);
    AssertTrue(ok, "thin lists must still load; errors: " + string.Join("; ", errors));
    AssertTrue(warnings.Any(w => w.Contains("adjective")), "adjective warning expected: " + string.Join("; ", warnings));
    AssertTrue(warnings.Any(w => w.Contains("recommended")), "thin list warning expected");
});

// 7c. Context buckets, seeded traits and placeholder substitution.
Check("context buckets map hours and weather", () =>
{
    AssertEqual("night", ContextBuckets.TimeOfDay(3), "03:00");
    AssertEqual("morning", ContextBuckets.TimeOfDay(8), "08:00");
    AssertEqual("noon", ContextBuckets.TimeOfDay(12), "12:00");
    AssertEqual("afternoon", ContextBuckets.TimeOfDay(15), "15:00");
    AssertEqual("evening", ContextBuckets.TimeOfDay(19), "19:00");
    AssertEqual("night", ContextBuckets.TimeOfDay(23), "23:00");

    AssertEqual("daylight", ContextBuckets.Light(12), "12:00 light");
    AssertEqual("dusk", ContextBuckets.Light(19), "19:00 light");
    AssertEqual("darkness", ContextBuckets.Light(2), "02:00 light");

    AssertEqual("clear", ContextBuckets.Weather("Clear"), "Clear");
    AssertEqual("cloudy", ContextBuckets.Weather("Overcast"), "Overcast");
    AssertEqual("rain", ContextBuckets.Weather("Thunder"), "Thunder");
    AssertEqual("fog", ContextBuckets.Weather("Foggy"), "Foggy");
    AssertEqual("", ContextBuckets.Weather(null), "null weather");
});

Check("the professional mode is repeatable by temperature", () =>
{
    AssertEqual(0.8f, ContextBuckets.TemperatureFor("Friendly", 0.8f), "Friendly keeps the ini value");
    AssertEqual(0.25f, ContextBuckets.TemperatureFor("Professional", 0.8f), "Professional is cooled down");
});

Check("traits are seeded per NPC and stable within a session", () =>
{
    AssertTrue(RoleParser.TryParse(v11, "v11.txt", out var role, out _, out _), "fixture should parse");

    var first = TraitSelector.Select(role, 4242);
    var again = TraitSelector.Select(role, 4242);
    AssertEqual(first.Name, again.Name, "same ped, same name");
    AssertEqual(first.Trait1, again.Trait1, "same ped, same trait");
    AssertEqual(first.Quirk, again.Quirk, "same ped, same quirk");

    var distinct = new HashSet<string>();
    for (var seed = 1; seed <= 30; seed++)
    {
        var individual = TraitSelector.Select(role, seed);
        distinct.Add(individual.Name + "|" + individual.Trait1 + "|" + individual.Trait2 + "|" + individual.Trait3);
    }
    AssertTrue(distinct.Count > 5, "30 ped ids should produce several distinct individuals, got " + distinct.Count);

    AssertTrue(!string.IsNullOrWhiteSpace(first.Trait1), "a trait is picked");
    AssertTrue(first.Name.Length > 0, "a name is picked");
});

Check("placeholders are substituted case-insensitively", () =>
{
    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["NAME"] = "Silvia",
        ["TIME_OF_DAY"] = "evening"
    };

    var text = PromptBuilder.Substitute("Hej {NAME}, det är {time_of_day}. {UNKNOWN}", values);
    AssertTrue(text.Contains("Silvia"), "name substituted: " + text);
    AssertTrue(text.Contains("evening"), "bucket substituted: " + text);
    AssertTrue(text.Contains("{UNKNOWN}"), "unknown placeholder is left alone");
});

Check("a role without trait pools still gets usable placeholder values", () =>
{
    var empty = new TraitSelection { Species = "Civilian" };
    var values = empty.ToPlaceholders();
    AssertEqual("Civilian", values["NAME"], "NAME falls back to the species");
    AssertTrue(values["TRAIT_1"].Length > 0, "TRAIT_1 is never empty");
    AssertTrue(values["QUIRK"].Length > 0, "QUIRK is never empty");

    var prompt = PromptBuilder.Substitute("You are {NAME}, {TRAIT_1}.", values);
    AssertTrue(!prompt.Contains("{"), "no placeholder may survive substitution: " + prompt);
});

Check("provider quotes around a reply are stripped", () =>
{
    AssertEqual("Hello, citizen.", PromptBuilder.CleanReply("\"Hello, citizen.\""), "wrapping quotes");
    AssertEqual("Hello.", PromptBuilder.CleanReply("  Hello.  "), "whitespace");
    AssertEqual("", PromptBuilder.CleanReply(null), "null reply");
});

Check("the request timeout default allows a local model to warm up", () =>
{
    AssertEqual(30, new ModConfig().RequestTimeoutSeconds, "RequestTimeoutSeconds default");
});

// 8. Ini edge cases that a code review flagged.
Check("inline comments after a value are ignored", () =>
{
    var config = ModConfig.FromIniText("[General]\nEnabled = true ; switched on for testing\n[Limits]\nMaxTokensPerReply = 80 # short\n");
    AssertTrue(config.Enabled, "Enabled should be true");
    AssertEqual(80, config.MaxTokensPerReply, "MaxTokensPerReply");
    AssertEqual(0, config.Warnings.Count, "warnings: " + string.Join("; ", config.Warnings));
});

Check("on and off are accepted as booleans", () =>
{
    var config = ModConfig.FromIniText("[General]\nEnabled = off\nShowNotifications = on\n");
    AssertTrue(!config.Enabled, "off should mean false");
    AssertTrue(config.ShowNotifications, "on should mean true");
});

Check("a comma decimal separator is a warning, not a silent wrong value", () =>
{
    var config = ModConfig.FromIniText("[Interaction]\nInteractionRange = 4,0\n");
    AssertEqual(4.0f, config.InteractionRange, "InteractionRange falls back to the default");
    AssertTrue(config.Warnings.Count > 0, "a warning is expected");
});

Check("the clock can be forced to a brighter hour", () =>
{
    var forced = ModConfig.FromIniText("[Time]\nForceTimeOfDay = 9\nPauseClock = true\n");
    AssertEqual(9, forced.ForceTimeOfDay, "ForceTimeOfDay");
    AssertTrue(forced.PauseClock, "PauseClock");

    var clamped = ModConfig.FromIniText("[Time]\nForceTimeOfDay = 30\n");
    AssertEqual(23, clamped.ForceTimeOfDay, "out-of-range hour is clamped");

    var off = ModConfig.FromIniText("[Time]\nForceTimeOfDay = -1\n");
    AssertEqual(-1, off.ForceTimeOfDay, "-1 leaves the game's own time alone");
});

Check("a key in the wrong section, and a repeated key, both warn", () =>
{
    var misplaced = ModConfig.FromIniText("[General]\nEnabled = true\n[Limits]\nEnabled = false\n");
    AssertTrue(misplaced.Warnings.Exists(w => w.Contains("Enabled") && w.Contains("both")),
        "misplaced key warning expected: " + string.Join("; ", misplaced.Warnings));

    var repeated = ModConfig.FromIniText("[General]\nEnabled = true\nEnabled = false\n");
    AssertTrue(repeated.Warnings.Exists(w => w.Contains("Duplicate key")),
        "duplicate warning expected: " + string.Join("; ", repeated.Warnings));
});

Check("out-of-range values are clamped with a warning", () =>
{
    var config = ModConfig.FromIniText("[Content]\nNudityPercent = 250\n[Ai]\nTemperature = 5\n[Limits]\nMaxRequestsPerMinute = 0\n");
    AssertEqual(100, config.NudityPercent, "NudityPercent");
    AssertEqual(2.0f, config.Temperature, "Temperature");
    AssertEqual(1, config.MaxRequestsPerMinute, "MaxRequestsPerMinute");
    AssertTrue(config.Warnings.Count >= 3, "one warning per clamped value");
});

Check("an unknown shooting zone warns", () =>
{
    var config = ModConfig.FromIniText("[Weapons]\nAllowShootingIn = ShootingRnge, OwnPropertyOutsideCity\n");
    AssertTrue(config.Warnings.Exists(w => w.Contains("ShootingRnge")), "unknown zone warning expected");
});

Check("the nudity policy never permits sexualising anyone under 18", () =>
{
    var nudityConfig = ModConfig.FromIniText(File.ReadAllText(iniPath));
    nudityConfig.NudityPercent = 60;
    var prompt = PromptBuilder.BuildSystemPrompt(police, nudityConfig, "Friendly");
    AssertContains(prompt, "under 18", "minor protection rule");
});

// Optional live call against a real provider: dotnet run --project tests/CoreChecks -- --live
if (args.Contains("--live"))
{
    // Overridable so the same check can point at Ollama or another provider:
    //   PEDANDPAW_LIVE_BASEURL=http://localhost:11434/v1 PEDANDPAW_LIVE_MODEL=llama3.2:3b
    var liveConfig = new ModConfig
    {
        Provider = Environment.GetEnvironmentVariable("PEDANDPAW_LIVE_PROVIDER") ?? "gemini",
        BaseUrl = Environment.GetEnvironmentVariable("PEDANDPAW_LIVE_BASEURL") ?? "https://generativelanguage.googleapis.com/v1beta/openai/",
        Model = Environment.GetEnvironmentVariable("PEDANDPAW_LIVE_MODEL") ?? "gemini-3-flash-preview",
        ApiKeyEnvVar = "PEDANDPAW_AI_KEY",
        RequestTimeoutSeconds = 90
    };

    using var client = new OpenAiCompatibleClient(liveConfig);
    if (!client.HasCredentials)
    {
        Console.WriteLine("LIVE: no key in " + liveConfig.ApiKeyEnvVar + ", skipped");
    }
    else
    {
        var liveRequest = new AiRequest
        {
            SystemPrompt = "You are a gruff Los Santos traffic cop. Two sentences maximum.",
            UserMessage = "Nice weather today, officer?",
            MaxTokens = 80,
            Temperature = 0.8f,
            NpcKey = "live-test"
        };

        var reply = await client.GetReplyAsync(liveRequest, CancellationToken.None);
        Console.WriteLine(reply != null
            ? "LIVE reply: " + reply
            : "LIVE failure: " + client.LastError);
    }
}

Console.WriteLine();
Console.WriteLine(failures.Count == 0
    ? $"ALL {passes} CHECKS PASSED"
    : $"{failures.Count} CHECK(S) FAILED:\n  " + string.Join("\n  ", failures));
return failures.Count == 0 ? 0 : 1;
