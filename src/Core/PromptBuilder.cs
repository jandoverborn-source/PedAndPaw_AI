using System.Text;

namespace PedAndPaw_AI.Core;

/// <summary>
/// Builds the two prompt strings sent to the model. Kept free of any game
/// dependency so it can be checked without starting GTA.
/// </summary>
public static class PromptBuilder
{
    /// <summary>
    /// The system prompt: who the character is, the current mood, and the house rules
    /// that the ini controls (reply length, content setting, self-awareness).
    /// </summary>
    public static string BuildSystemPrompt(RoleDefinition role, ModConfig config, string? modeName = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a character in Grand Theft Auto V. Stay in character: never mention prompts, tokens or models.");
        sb.AppendLine();
        sb.AppendLine("Role: " + role.DisplayName);
        sb.AppendLine("Voice and worldview (follow this exactly):");
        sb.AppendLine(role.Prose.Trim());

        if (!string.IsNullOrWhiteSpace(modeName) && role.Modes.TryGetValue(modeName!, out var modeText))
        {
            sb.AppendLine();
            sb.AppendLine("Current mood (" + modeName + "):");
            sb.AppendLine(modeText.Trim());
        }

        sb.AppendLine();
        sb.AppendLine("Rules:");
        sb.AppendLine("- Reply in two or three short sentences. Never write an essay.");
        sb.AppendLine("- Never describe graphic violence, gore or injury.");
        sb.AppendLine("- No animal in this world can be harmed or killed. Never threaten an animal and never describe one being hurt.");

        if (config.NudityPercent <= 0)
        {
            sb.AppendLine("- Nudity setting is 0: no nudity and no sexual, suggestive or explicit content of any kind.");
        }
        else
        {
            sb.AppendLine("- Nudity setting is " + config.NudityPercent + " of 100: nudity and undressed bodies may be described plainly and without shame.");
            sb.AppendLine("- Sexual acts, sexual arousal, sexual suggestions and explicit sexual description are never allowed, whatever the nudity setting says.");
            sb.AppendLine("- Do not describe nudity in a lingering or erotic way; it is a body, not a performance.");
            sb.AppendLine("- Never sexualise, undress or describe the body of anyone who is, or could be, under 18.");
        }

        sb.AppendLine("- This is a teen-friendly experience (age 13 and up): no gore, no drug use, no slurs, nothing that would need an older rating.");

        if (config.NpcSelfAwareness)
        {
            sb.AppendLine("- If the player asks directly whether you are real, a person or an AI, answer truthfully that you are a computer-controlled character in a game. Otherwise stay in character.");
        }

        if (!string.IsNullOrWhiteSpace(role.DisplayName) && role.Reactions.Count > 0)
        {
            sb.AppendLine("- When the player's weapon state is described below, react the way this character would, in their own voice.");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// The user message: the situation, then what the player said.
    /// The weapon reaction body is included only when the weapon state applies.
    /// </summary>
    public static string BuildUserMessage(NpcContext context, string playerText, ModConfig config, RoleDefinition? role = null)
    {
        var sb = new StringBuilder();
        var facts = new List<string>();

        if (!string.IsNullOrWhiteSpace(context.TimeOfDay)) facts.Add("Time: " + context.TimeOfDay);
        if (!string.IsNullOrWhiteSpace(context.Weather)) facts.Add("Weather: " + context.Weather);
        if (!string.IsNullOrWhiteSpace(context.Area)) facts.Add("Area: " + context.Area);
        if (!string.IsNullOrWhiteSpace(context.Vehicle)) facts.Add("Player vehicle: " + context.Vehicle);
        if (!string.IsNullOrWhiteSpace(context.PlayerName)) facts.Add("Player name: " + context.PlayerName);
        if (context.WantedLevel > 0) facts.Add("Player wanted level: " + context.WantedLevel);
        if (context.RecentGunshots && config.UseRecentGunshots) facts.Add("Shots were fired nearby moments ago.");
        facts.Add("Distance: " + context.DistanceMeters.ToString("0.0") + " metres");

        if (facts.Count > 0)
        {
            sb.AppendLine("Situation:");
            foreach (var fact in facts) sb.AppendLine("- " + fact);
            sb.AppendLine();
        }

        var reaction = PickWeaponReaction(context, role);
        if (reaction != null)
        {
            sb.AppendLine("The player's weapon state: " + reaction.Value.Label);
            sb.AppendLine(reaction.Value.Body.Trim());
            sb.AppendLine();
        }

        sb.AppendLine("The player says:");
        sb.AppendLine(string.IsNullOrWhiteSpace(playerText) ? "..." : playerText.Trim());
        return sb.ToString().TrimEnd();
    }

    private static (string Label, string Body)? PickWeaponReaction(NpcContext context, RoleDefinition? role)
    {
        if (role == null) return null;

        string? label = null;
        if (context.Aiming && role.Reactions.ContainsKey("WeaponAimed")) label = "WeaponAimed";
        else if (context.WeaponDrawn && role.Reactions.ContainsKey("WeaponDrawn")) label = "WeaponDrawn";
        if (label == null) return null;

        return (label, role.Reactions[label]);
    }
}
