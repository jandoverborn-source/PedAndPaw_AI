namespace PedAndPaw_AI.Core;

/// <summary>
/// Mod configuration loaded from PedAndPaw_AI.ini.
/// One public property per ini key with documented defaults.
/// </summary>
public class ModConfig
{
    // General
    public bool Enabled { get; set; } = true;
    public string Language { get; set; } = "sv";
    public string LogLevel { get; set; } = "Info";
    public bool ShowNotifications { get; set; } = true;

    // Keys
    public string InteractKey { get; set; } = "E";
    public string TextMessageKey { get; set; } = "T";
    public string CancelKey { get; set; } = "Escape";

    // Interaction
    public float InteractionRange { get; set; } = 4.0f;
    public bool TalkToCivilians { get; set; } = true;
    public bool TalkToPolice { get; set; } = true;
    public bool TalkToEmergency { get; set; } = true;
    public bool TalkToAnimals { get; set; } = true;
    public bool NpcFacePlayer { get; set; } = true;

    // Context
    public bool UseTimeOfDay { get; set; } = true;
    public bool UseWeather { get; set; } = true;
    public bool UseArea { get; set; } = true;
    public bool UseVehicle { get; set; } = true;
    public bool UseWeaponState { get; set; } = true;
    public bool UseAimingState { get; set; } = true;
    public bool UseWantedLevel { get; set; } = true;
    public bool UseRecentGunshots { get; set; } = true;
    public int GunshotMemorySeconds { get; set; } = 30;

    // Ai
    public string Provider { get; set; } = "gemini";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/openai/";
    public string Model { get; set; } = "gemini-3-flash-preview";
    public string ApiKeyEnvVar { get; set; } = "PEDANDPAW_AI_KEY";
    public int RequestTimeoutSeconds { get; set; } = 30;
    public float Temperature { get; set; } = 0.8f;

    // Limits
    public int MaxTokensPerReply { get; set; } = 120;
    public int MaxReplyCharacters { get; set; } = 240;
    public int NpcCooldownSeconds { get; set; } = 8;
    public int MaxRequestsPerMinute { get; set; } = 20;
    public int MemoryMessagesPerNpc { get; set; } = 6;
    public int MaxConcurrentConversations { get; set; } = 2;
    public int DailyRequestBudget { get; set; } = 500;

    // Behavior
    public int ModeChangeIntervalMinutes { get; set; } = 15;
    public bool DynamicModeSwitching { get; set; } = true;
    public bool AttitudeSystem { get; set; } = true;
    public int DefaultTrust { get; set; } = 50;
    public bool NpcSelfAwareness { get; set; } = true;

    // Weapons
    public bool EnableWeaponReactions { get; set; } = true;
    public string[] AllowShootingIn { get; set; } = new[] { "ShootingRange", "OwnPropertyOutsideCity" };
    public bool BlockFireOutsideAllowedZones { get; set; } = true;
    public bool ReactToIllegalFire { get; set; } = true;

    // Animals
    public bool AnimalsCanBeShot { get; set; } = false;
    public bool AnimalsCanBeKilled { get; set; } = false;
    public bool EnableRat { get; set; } = true;
    public bool EnableDogs { get; set; } = true;
    public bool EnableCat { get; set; } = true;
    public bool EnableCrow { get; set; } = true;
    public bool EnableSeagull { get; set; } = true;
    public bool EnableMountainLion { get; set; } = true;
    public bool EnableFishAndMarine { get; set; } = true;
    public int AnimalQuestIntervalSeconds { get; set; } = 60;

    // Vehicles
    public bool EnableVehicleBorrowing { get; set; } = true;
    public int MinTrustToLendVehicle { get; set; } = 65;

    // Content
    public int NudityPercent { get; set; } = 0;

    // Roles
    public string RolesFolder { get; set; } = "scripts/PedAndPaw_AI/roles";
    public bool ReloadRolesOnStart { get; set; } = true;

    /// <summary>
    /// All warnings collected during parsing (unknown keys, unparsable values, out-of-range values).
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Parse the IniText and return a ModConfig with defaults merged and warnings collected.
    /// Pure (no file I/O) - caller handles file reading.
    /// </summary>
    /// <param name="iniText">The full INI file contents as a string.</param>
    /// <returns>A ModConfig instance populated with parsed values.</returns>
    public static ModConfig FromIniText(string iniText)
    {
        var config = new ModConfig();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(iniText))
        {
            config.Warnings = warnings;
            return config;
        }

        var lines = iniText.Split('\n');
        var currentSection = "";
        var seenKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            // Skip empty lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#"))
                continue;

            // Section header
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentSection = line.Substring(1, line.Length - 2).Trim();
                continue;
            }

            // Key = Value
            int eqIdx = line.IndexOf('=');
            if (eqIdx > 0)
            {
                string key = line.Substring(0, eqIdx).Trim();
                string value = StripInlineComment(line.Substring(eqIdx + 1)).Trim();

                string? firstSection;
                if (seenKeys.TryGetValue(key, out firstSection))
                {
                    if (string.Equals(firstSection, currentSection, StringComparison.OrdinalIgnoreCase))
                        warnings.Add($"Duplicate key '{key}' in section [{currentSection}] (line {i + 1}); the last value wins.");
                    else
                        warnings.Add($"Key '{key}' appears in both [{firstSection}] and [{currentSection}] (line {i + 1}); only the last value is used.");
                }
                else
                {
                    seenKeys[key] = currentSection;
                }

                // Case-insensitive key matching
                string lowerKey = key.ToLowerInvariant();

                try
                {
                    switch (lowerKey)
                    {
                        // General
                        case "enabled": config.Enabled = ParseBool(value, config.Enabled, warnings, $"General: enabled (line {i + 1})"); break;
                        case "language": config.Language = value; break;
                        case "loglevel": config.LogLevel = value; break;
                        case "shownotifications": config.ShowNotifications = ParseBool(value, config.ShowNotifications, warnings, $"General: shownotifications (line {i + 1})"); break;

                        // Keys
                        case "interactkey": config.InteractKey = value; break;
                        case "textmessagekey": config.TextMessageKey = value; break;
                        case "cancelkey": config.CancelKey = value; break;

                        // Interaction
                        case "interactionrange": config.InteractionRange = ParseFloat(value, config.InteractionRange, warnings, $"Interaction: interactionrange (line {i + 1})"); break;
                        case "talktocivilians": config.TalkToCivilians = ParseBool(value, config.TalkToCivilians, warnings, $"Interaction: talktocivilians (line {i + 1})"); break;
                        case "talktopolice": config.TalkToPolice = ParseBool(value, config.TalkToPolice, warnings, $"Interaction: talktopolice (line {i + 1})"); break;
                        case "talktoemergency": config.TalkToEmergency = ParseBool(value, config.TalkToEmergency, warnings, $"Interaction: talktoemergency (line {i + 1})"); break;
                        case "talktoanimals": config.TalkToAnimals = ParseBool(value, config.TalkToAnimals, warnings, $"Interaction: talktoanimals (line {i + 1})"); break;
                        case "npcfaceplayer": config.NpcFacePlayer = ParseBool(value, config.NpcFacePlayer, warnings, $"Interaction: npcfaceplayer (line {i + 1})"); break;

                        // Context
                        case "usetimeofday": config.UseTimeOfDay = ParseBool(value, config.UseTimeOfDay, warnings, $"Context: usetimeofday (line {i + 1})"); break;
                        case "useweather": config.UseWeather = ParseBool(value, config.UseWeather, warnings, $"Context: useweather (line {i + 1})"); break;
                        case "usearea": config.UseArea = ParseBool(value, config.UseArea, warnings, $"Context: usearea (line {i + 1})"); break;
                        case "usevehicle": config.UseVehicle = ParseBool(value, config.UseVehicle, warnings, $"Context: usevehicle (line {i + 1})"); break;
                        case "useweaponstate": config.UseWeaponState = ParseBool(value, config.UseWeaponState, warnings, $"Context: useweaponstate (line {i + 1})"); break;
                        case "useaimingstate": config.UseAimingState = ParseBool(value, config.UseAimingState, warnings, $"Context: useaimingstate (line {i + 1})"); break;
                        case "usewantedlevel": config.UseWantedLevel = ParseBool(value, config.UseWantedLevel, warnings, $"Context: usewantedlevel (line {i + 1})"); break;
                        case "userecentgunshots": config.UseRecentGunshots = ParseBool(value, config.UseRecentGunshots, warnings, $"Context: userecentgunshots (line {i + 1})"); break;
                        case "gunshotmemoryseconds": config.GunshotMemorySeconds = ParseInt(value, config.GunshotMemorySeconds, warnings, $"Context: gunshotmemoryseconds (line {i + 1})"); break;

                        // Ai
                        case "provider": config.Provider = value; break;
                        case "baseurl": config.BaseUrl = value; break;
                        case "model": config.Model = value; break;
                        case "apikeyenvvar": config.ApiKeyEnvVar = value; break;
                        case "requesttimeoutseconds": config.RequestTimeoutSeconds = ParseInt(value, config.RequestTimeoutSeconds, warnings, $"Ai: requesttimeoutseconds (line {i + 1})"); break;
                        case "temperature": config.Temperature = ParseFloat(value, config.Temperature, warnings, $"Ai: temperature (line {i + 1})"); break;

                        // Limits
                        case "maxtokensperreply": config.MaxTokensPerReply = ParseInt(value, config.MaxTokensPerReply, warnings, $"Limits: maxtokensperreply (line {i + 1})"); break;
                        case "maxreplycharacters": config.MaxReplyCharacters = ParseInt(value, config.MaxReplyCharacters, warnings, $"Limits: maxreplycharacters (line {i + 1})"); break;
                        case "npccooldownseconds": config.NpcCooldownSeconds = ParseInt(value, config.NpcCooldownSeconds, warnings, $"Limits: npccooldownseconds (line {i + 1})"); break;
                        case "maxrequestsperminute": config.MaxRequestsPerMinute = ParseInt(value, config.MaxRequestsPerMinute, warnings, $"Limits: maxrequestsperminute (line {i + 1})"); break;
                        case "memorymessagespernpc": config.MemoryMessagesPerNpc = ParseInt(value, config.MemoryMessagesPerNpc, warnings, $"Limits: memorymessagespernpc (line {i + 1})"); break;
                        case "maxconcurrentconversations": config.MaxConcurrentConversations = ParseInt(value, config.MaxConcurrentConversations, warnings, $"Limits: maxconcurrentconversations (line {i + 1})"); break;
                        case "dailyrequestbudget": config.DailyRequestBudget = ParseInt(value, config.DailyRequestBudget, warnings, $"Limits: dailyrequestbudget (line {i + 1})"); break;

                        // Behavior
                        case "modechangeintervalminutes": config.ModeChangeIntervalMinutes = ParseInt(value, config.ModeChangeIntervalMinutes, warnings, $"Behavior: modechangeintervalminutes (line {i + 1})"); break;
                        case "dynamicmodeswitching": config.DynamicModeSwitching = ParseBool(value, config.DynamicModeSwitching, warnings, $"Behavior: dynamicmodeswitching (line {i + 1})"); break;
                        case "attitudesystem": config.AttitudeSystem = ParseBool(value, config.AttitudeSystem, warnings, $"Behavior: attitudesystem (line {i + 1})"); break;
                        case "defaulttrust": config.DefaultTrust = ParseInt(value, config.DefaultTrust, warnings, $"Behavior: defaulttrust (line {i + 1})"); break;
                        case "npcselfawareness": config.NpcSelfAwareness = ParseBool(value, config.NpcSelfAwareness, warnings, $"Behavior: npcselfawareness (line {i + 1})"); break;

                        // Weapons
                        case "enableweaponreactions": config.EnableWeaponReactions = ParseBool(value, config.EnableWeaponReactions, warnings, $"Weapons: enableweaponreactions (line {i + 1})"); break;
                        case "allowshootingin": config.AllowShootingIn = value.Split(',').Select(part => part.Trim()).Where(part => part.Length > 0).ToArray(); break;
                        case "blockfireoutsideallowedzones": config.BlockFireOutsideAllowedZones = ParseBool(value, config.BlockFireOutsideAllowedZones, warnings, $"Weapons: blockfireoutsideallowedzones (line {i + 1})"); break;
                        case "reacttoillegalfire": config.ReactToIllegalFire = ParseBool(value, config.ReactToIllegalFire, warnings, $"Weapons: reacttoillegalfire (line {i + 1})"); break;

                        // Animals
                        case "animalscanbeshot": config.AnimalsCanBeShot = ParseBool(value, config.AnimalsCanBeShot, warnings, $"Animals: animalscanbeshot (line {i + 1})"); break;
                        case "animalscanbekilled": config.AnimalsCanBeKilled = ParseBool(value, config.AnimalsCanBeKilled, warnings, $"Animals: animalscanbekilled (line {i + 1})"); break;
                        case "enablecat": config.EnableCat = ParseBool(value, config.EnableCat, warnings, $"Animals: enablecat (line {i + 1})"); break;
                        case "enablecrow": config.EnableCrow = ParseBool(value, config.EnableCrow, warnings, $"Animals: enablecrow (line {i + 1})"); break;
                        case "enablefishandmarine": config.EnableFishAndMarine = ParseBool(value, config.EnableFishAndMarine, warnings, $"Animals: enablefishandmarine (line {i + 1})"); break;
                        case "enabledogs": config.EnableDogs = ParseBool(value, config.EnableDogs, warnings, $"Animals: enabledogs (line {i + 1})"); break;
                        case "enablerat": config.EnableRat = ParseBool(value, config.EnableRat, warnings, $"Animals: enablerat (line {i + 1})"); break;
                        case "enableseagull": config.EnableSeagull = ParseBool(value, config.EnableSeagull, warnings, $"Animals: enableseagull (line {i + 1})"); break;
                        case "enablemountainlion": config.EnableMountainLion = ParseBool(value, config.EnableMountainLion, warnings, $"Animals: enablemountainlion (line {i + 1})"); break;
                        case "animalquestintervalseconds": config.AnimalQuestIntervalSeconds = ParseInt(value, config.AnimalQuestIntervalSeconds, warnings, $"Animals: animalquestintervalseconds (line {i + 1})"); break;

                        // Vehicles
                        case "enablevehicleborrowing": config.EnableVehicleBorrowing = ParseBool(value, config.EnableVehicleBorrowing, warnings, $"Vehicles: enablevehicleborrowing (line {i + 1})"); break;
                        case "mintrusttolendvehicle": config.MinTrustToLendVehicle = ParseInt(value, config.MinTrustToLendVehicle, warnings, $"Vehicles: mintrusttolendvehicle (line {i + 1})"); break;

                        // Content
                        case "nuditypercent": config.NudityPercent = ParseInt(value, config.NudityPercent, warnings, $"Content: nuditypercent (line {i + 1})"); break;

                        // Roles
                        case "rolesfolder": config.RolesFolder = value; break;
                        case "reloadrolesonstart": config.ReloadRolesOnStart = ParseBool(value, config.ReloadRolesOnStart, warnings, $"Roles: reloadrolesonstart (line {i + 1})"); break;

                        default:
                            warnings.Add($"Unknown key '{key}' (line {i + 1})");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"Failed to parse '{key}' = '{value}' (line {i + 1}): {ex.Message}");
                }
            }
        }

        ClampValues(config, warnings);

        config.Warnings = warnings;
        return config;
    }

    /// <summary>Strip a trailing ';' or '#' comment that is not inside quotes.</summary>
    private static string StripInlineComment(string value)
    {
        var inQuotes = false;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c == '"') inQuotes = !inQuotes;
            else if (!inQuotes && (c == ';' || c == '#')) return value.Substring(0, i);
        }
        return value;
    }

    private static void Clamp(int value, int min, int max, string name, Action<int> set, List<string> warnings)
    {
        if (value < min || value > max)
        {
            var clamped = Math.Min(max, Math.Max(min, value));
            warnings.Add($"{name} = {value} is outside {min}..{max}; using {clamped}.");
            set(clamped);
        }
    }

    private static void ClampFloat(float value, float min, float max, string name, Action<float> set, List<string> warnings)
    {
        if (value < min || value > max)
        {
            var clamped = Math.Min(max, Math.Max(min, value));
            warnings.Add($"{name} = {value} is outside {min}..{max}; using {clamped}.");
            set(clamped);
        }
    }

    /// <summary>
    /// Replace values that would break the mod with safe ones, and say so.
    /// A zero request timeout or a zero request budget is a silent kill switch.
    /// </summary>
    private static void ClampValues(ModConfig config, List<string> warnings)
    {
        Clamp(config.NudityPercent, 0, 100, "NudityPercent", v => config.NudityPercent = v, warnings);
        ClampFloat(config.Temperature, 0f, 2f, "Temperature", v => config.Temperature = v, warnings);
        ClampFloat(config.InteractionRange, 0.5f, 50f, "InteractionRange", v => config.InteractionRange = v, warnings);
        Clamp(config.RequestTimeoutSeconds, 1, 300, "RequestTimeoutSeconds", v => config.RequestTimeoutSeconds = v, warnings);
        Clamp(config.MaxTokensPerReply, 1, 4096, "MaxTokensPerReply", v => config.MaxTokensPerReply = v, warnings);
        Clamp(config.MaxReplyCharacters, 1, 2000, "MaxReplyCharacters", v => config.MaxReplyCharacters = v, warnings);
        Clamp(config.MaxRequestsPerMinute, 1, 600, "MaxRequestsPerMinute", v => config.MaxRequestsPerMinute = v, warnings);
        Clamp(config.DailyRequestBudget, 1, 100000, "DailyRequestBudget", v => config.DailyRequestBudget = v, warnings);
        Clamp(config.MaxConcurrentConversations, 1, 50, "MaxConcurrentConversations", v => config.MaxConcurrentConversations = v, warnings);
        Clamp(config.NpcCooldownSeconds, 0, 3600, "NpcCooldownSeconds", v => config.NpcCooldownSeconds = v, warnings);
        Clamp(config.MemoryMessagesPerNpc, 0, 100, "MemoryMessagesPerNpc", v => config.MemoryMessagesPerNpc = v, warnings);
        Clamp(config.GunshotMemorySeconds, 0, 3600, "GunshotMemorySeconds", v => config.GunshotMemorySeconds = v, warnings);
        Clamp(config.DefaultTrust, 0, 100, "DefaultTrust", v => config.DefaultTrust = v, warnings);
        Clamp(config.MinTrustToLendVehicle, 0, 100, "MinTrustToLendVehicle", v => config.MinTrustToLendVehicle = v, warnings);
        Clamp(config.ModeChangeIntervalMinutes, 0, 1440, "ModeChangeIntervalMinutes", v => config.ModeChangeIntervalMinutes = v, warnings);

        var knownZones = new[] { "shootingrange", "ownpropertyoutsidecity" };
        foreach (var zone in config.AllowShootingIn)
        {
            if (Array.FindIndex(knownZones, z => string.Equals(z, zone.Trim(), StringComparison.OrdinalIgnoreCase)) < 0)
                warnings.Add($"Unknown zone '{zone}' in AllowShootingIn; known zones are ShootingRange and OwnPropertyOutsideCity.");
        }
    }

    /// <summary>
    /// Load a ModConfig from a file path. Returns defaults if file is missing, with a warning.
    /// </summary>
    /// <param name="path">Path to the INI file.</param>
    /// <returns>A ModConfig instance.</returns>
    public static ModConfig Load(string path)
    {
        var config = new ModConfig();

        if (!File.Exists(path))
        {
            config.Warnings?.Add($"Config file not found: {path}, using defaults");
            return config;
        }

        try
        {
            string text = File.ReadAllText(path);
            config = FromIniText(text);
        }
        catch (Exception ex)
        {
            config.Warnings?.Add($"Failed to read config file {path}: {ex.Message}");
        }

        return config;
    }

    /// <summary>Parse a boolean INI value.</summary>
    private static bool ParseBool(string value, bool fallback, List<string> warnings, string context)
    {
        string lower = value.Trim().ToLowerInvariant();
        switch (lower)
        {
            case "true":
            case "yes":
            case "on":
            case "1":
                return true;
            case "false":
            case "no":
            case "off":
            case "0":
                return false;
            default:
                warnings.Add($"Cannot parse boolean value '{value}' in {context}");
                return fallback;
        }
    }

    /// <summary>Parse an integer INI value.</summary>
    private static int ParseInt(string value, int fallback, List<string> warnings, string context)
    {
        if (int.TryParse(value, out int result))
            return result;
        warnings.Add($"Cannot parse integer value '{value}' in {context}");
        return fallback;
    }

    /// <summary>Parse a float INI value.</summary>
    private static float ParseFloat(string value, float fallback, List<string> warnings, string context)
    {
        if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float result))
            return result;
        warnings.Add($"Cannot parse float value '{value}' in {context}");
        return fallback;
    }
}