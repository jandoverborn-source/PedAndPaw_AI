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

            // Route by model name where it is reliable: PedHash is an enum of hashes, so
            // casting the model hash back to it gives a stable name such as "Cop01SMY".
            var modelName = ModelName(ped);

            if (isAnimal)
            {
                var wanted = AnimalRoleForModel(modelName);
                var animal = FindRole(wanted) ?? AnyRole(isAnimal: true) ?? AnyRole(isAnimal: false);
                roleLayer = animal.DisplayName;
                return animal;
            }

            var wantedHuman = HumanRoleForModel(modelName);
            var human = FindRole(wantedHuman) ?? AnyRole(isAnimal: false) ?? AnyRole(isAnimal: true);
            roleLayer = human.DisplayName;
            return human;
        }

        /// <summary>Name of the ped's model, or an empty string when it cannot be read.</summary>
        private static string ModelName(Ped ped)
        {
            try
            {
                return ((GTA.PedHash)ped.Model.Hash).ToString();
            }
            catch
            {
                return "";
            }
        }

        private static string HumanRoleForModel(string modelName)
        {
            if (modelName.IndexOf("Cop", StringComparison.OrdinalIgnoreCase) >= 0 ||
                modelName.IndexOf("Sheriff", StringComparison.OrdinalIgnoreCase) >= 0 ||
                modelName.IndexOf("Security", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Police";

            return "Civilian";
        }

        private static string AnimalRoleForModel(string modelName)
        {
            if (modelName.IndexOf("Cat", StringComparison.OrdinalIgnoreCase) >= 0) return "Street Cat";
            if (modelName.IndexOf("MountainLion", StringComparison.OrdinalIgnoreCase) >= 0) return "Mountain Lion";
            return "";
        }

        private RoleDefinition FindRole(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return null;
            RoleDefinition role;
            return _roles.TryGetValue(displayName, out role) ? role : null;
        }

        /// <summary>Deterministic fallback when no model-specific role exists yet.</summary>
        private RoleDefinition AnyRole(bool isAnimal)
        {
            var pool = _roles.Values
                .Where(role => IsAnimalRole(role) == isAnimal)
                .OrderBy(role => role.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return pool.Count > 0 ? pool[0] : null;
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
                // ClearAll stops the ped walking away mid-sentence; TurnTo makes the
                // conversation feel aimed at the player. Skipped in combat, where
                // interfering with the ped's task would be dangerous and rude.
                if (ped.IsOnFoot && !ped.IsInCombat)
                {
                    ped.Task.ClearAll();
                    ped.Task.TurnTo(player, 3000);
                }
                else
                {
                    ped.Task.TurnTo(player, 2000);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Could not turn the ped towards the player: " + ex.Message);
            }
        }
    }
}
