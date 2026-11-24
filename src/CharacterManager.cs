using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GTA;
using PedAndPaw_AI.Core;

namespace PedAndPaw_AI
{
    /// <summary>
    /// Loads the role files and decides which role and mood a given ped gets.
    /// </summary>
    public sealed class CharacterManager
    {
        private readonly ModConfig _config;
        private readonly string _baseDirectory;
        private readonly Dictionary<string, RoleDefinition> _roles =
            new Dictionary<string, RoleDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, string> _modeByPed = new Dictionary<int, string>();

        public CharacterManager(ModConfig config, string baseDirectory)
        {
            _config = config;
            _baseDirectory = baseDirectory ?? ".";
            LoadRoles();
        }

        public IReadOnlyDictionary<string, RoleDefinition> Roles { get { return _roles; } }

        /// <summary>Where the roles were actually found.</summary>
        public string ResolvedRolesFolder { get; private set; }

        private void LoadRoles()
        {
            var folder = string.IsNullOrWhiteSpace(_config.RolesFolder) ? "scripts/PedAndPaw_AI/roles" : _config.RolesFolder;

            // The ini value counts from the game root ("scripts/..."), BaseDirectory is
            // the scripts folder, so try both roots before giving up.
            var candidates = new List<string>();
            if (Path.IsPathRooted(folder))
            {
                candidates.Add(folder);
            }
            else
            {
                candidates.Add(Path.Combine(_baseDirectory, folder));
                var parent = Directory.GetParent(_baseDirectory);
                if (parent != null) candidates.Add(Path.Combine(parent.FullName, folder));
            }

            ResolvedRolesFolder = candidates.FirstOrDefault(Directory.Exists);
            if (ResolvedRolesFolder == null)
            {
                Log.Warn("No role folder found. Tried: " + string.Join(" | ", candidates.ToArray()));
                ResolvedRolesFolder = candidates[0];
                return;
            }

            var loaded = RoleParser.LoadDirectory(ResolvedRolesFolder, (file, errors) =>
            {
                foreach (var error in errors) Log.Warn("Role file rejected: " + error);
            });

            foreach (var pair in loaded) _roles[pair.Key] = pair.Value;

            Log.Info("Roles loaded from " + ResolvedRolesFolder + ": " + _roles.Count +
                     (_roles.Count > 0 ? " (" + string.Join(", ", _roles.Keys.ToArray()) + ")" : ""));
        }

        /// <summary>
        /// Pick a role for this ped. Humans draw from the roles under humans/,
        /// animals from animals/, deterministically per ped, so the same person
        /// keeps the same personality while you play.
        /// Model-specific mapping (cop, paramedic, puma) needs verified model hashes
        /// and is deliberately not guessed here.
        /// </summary>
        public RoleDefinition PickRole(Ped ped, out string roleLayer)
        {
            roleLayer = "";
            if (_roles.Count == 0) return null;

            var isAnimal = !ped.IsHuman;
            var pool = _roles.Values
                .Where(role => IsAnimalRole(role) == isAnimal)
                .OrderBy(role => role.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (pool.Count == 0) pool = _roles.Values.OrderBy(role => role.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();

            var chosen = pool[Math.Abs(ped.Handle) % pool.Count];
            roleLayer = chosen.DisplayName;
            return chosen;
        }

        private static bool IsAnimalRole(RoleDefinition role)
        {
            return role.SourceName != null &&
                   role.SourceName.IndexOf("animals", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>The current mood of a ped, chosen once and remembered.</summary>
        public string EnsureMode(Ped ped)
        {
            string mode;
            if (_modeByPed.TryGetValue(ped.Handle, out mode)) return mode;

            var modes = Labels.Modes;
            mode = modes[Math.Abs(ped.Handle) % modes.Length];
            _modeByPed[ped.Handle] = mode;
            return mode;
        }

        /// <summary>
        /// Switch a ped's mood because of a game event (weapon drawn, aimed at,
        /// gunfire), but only when the ini allows dynamic mode switching.
        /// </summary>
        public string ReactToWeaponState(Ped ped, bool weaponDrawn, bool aiming, bool recentGunshots)
        {
            var current = EnsureMode(ped);
            if (!_config.DynamicModeSwitching) return current;

            var target = current;
            if (recentGunshots || aiming) target = "Scared";
            else if (weaponDrawn) target = "Grumpy";

            if (target != current)
            {
                _modeByPed[ped.Handle] = target;
                Log.Info("Ped " + ped.Handle + " switched mode " + current + " -> " + target);
            }

            return target;
        }

        /// <summary>Turn the ped towards the player when a conversation starts.</summary>
        public void FacePlayer(Ped ped, Ped player)
        {
            if (!_config.NpcFacePlayer || ped == null || !ped.IsAlive || player == null) return;
            try
            {
                ped.Task.TurnTo(player, 2000);
            }
            catch (Exception ex)
            {
                Log.Warn("Could not turn the ped towards the player: " + ex.Message);
            }
        }
    }
}
