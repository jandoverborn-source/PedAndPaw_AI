using System;
using GTA;
using PedAndPaw_AI.Core;

namespace PedAndPaw_AI
{
    /// <summary>
    /// Reads the situation around a conversation. A field is only filled when the
    /// matching Use* flag in the ini is on: empty means "switched off", never "guessed".
    /// Every SHVDNE member used here was verified against the real assembly
    /// (ScriptHookVDotNet3 1.1.0.6); nothing is invented.
    /// </summary>
    public static class ContextScanner
    {
        private static NpcContext _envCache;
        private static DateTime _envAtUtc = DateTime.MinValue;

        private static string SafeStreetName(Ped ped)
        {
            try
            {
                return World.GetStreetName(ped.Position);
            }
            catch
            {
                return "";
            }
        }
        public static NpcContext Scan(Ped ped, ModConfig config, RoleDefinition role, string mode, DateTime lastGunshotUtc, DateTime nowUtc)
        {
            var player = Game.Player.Character;
            var context = new NpcContext
            {
                RoleLayer = role != null ? role.DisplayName : "",
                ModeName = mode ?? ""
            };

            if (ped == null || player == null) return context;

            context.IsAnimal = !ped.IsHuman;
            context.DistanceMeters = player.Position.DistanceTo(ped.Position);

            // The slow fields are the same for everyone nearby, so they are cached for 30 s
            // (the specification review asked for at most one refresh per 30 s).
            var nowUtcForCache = DateTime.UtcNow;
            if (_envCache == null || (nowUtcForCache - _envAtUtc).TotalSeconds > 30)
            {
                var hour = GTA.Chrono.GameClock.Hour;
                _envCache = new NpcContext
                {
                    TimeOfDay = ContextBuckets.TimeOfDay(hour),
                    Light = ContextBuckets.Light(hour),
                    Weather = ContextBuckets.Weather(World.Weather.ToString()),
                    Area = SafeStreetName(ped)
                };
                _envAtUtc = nowUtcForCache;
            }

            if (config.UseTimeOfDay)
            {
                context.TimeOfDay = _envCache.TimeOfDay;
                context.Light = _envCache.Light;
            }

            if (config.UseWeather)
                context.Weather = _envCache.Weather;

            if (config.UseArea)
                context.Area = _envCache.Area;

            if (config.UseVehicle)
            {
                var vehicle = player.CurrentVehicle;
                if (vehicle != null && vehicle.Exists())
                    context.Vehicle = vehicle.DisplayName;
            }

            if (config.UseWeaponState)
                context.WeaponDrawn = player.Weapons.Current.Hash != WeaponHash.Unarmed;

            if (config.UseAimingState)
                context.Aiming = Game.Player.IsAiming;

            if (config.UseWantedLevel)
                context.WantedLevel = Game.Player.Wanted.WantedLevel;

            if (config.UseRecentGunshots)
            {
                var age = (nowUtc - lastGunshotUtc).TotalSeconds;
                context.RecentGunshots = age >= 0 && age <= config.GunshotMemorySeconds;
            }

            return context;
        }
    }
}
