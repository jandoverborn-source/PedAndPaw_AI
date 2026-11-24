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

            if (config.UseTimeOfDay)
                context.TimeOfDay = GTA.Chrono.GameClock.Hour.ToString("00") + ":" + GTA.Chrono.GameClock.Minute.ToString("00");

            if (config.UseWeather)
                context.Weather = World.Weather.ToString();

            if (config.UseArea)
            {
                try
                {
                    context.Area = World.GetStreetName(ped.Position);
                }
                catch
                {
                    context.Area = "";
                }
            }

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
