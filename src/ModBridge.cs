using System;
using System.IO;
using GTA;

namespace PedAndPaw_AI
{
    /// <summary>Dialogue states a ped can be in, as published to plugins.</summary>
    public enum PedState
    {
        Idle = 0,
        AwaitingPlayer = 1,
        QueryingAI = 2,
        Speaking = 3,
        Fleeing = 4,
        Aggressive = 5
    }

    /// <summary>Raised when the player starts a conversation with a ped.</summary>
    public delegate void DialogueStartedHandler(Ped ped, string roleLayer);

    /// <summary>Raised when a reply has arrived from the AI provider.</summary>
    public delegate void AiResponseReceivedHandler(Ped ped, string response);

    /// <summary>Raised when a ped's dialogue state changes.</summary>
    public delegate void PedStateChangedHandler(Ped ped, PedState oldState, PedState newState);

    /// <summary>
    /// The plugin surface documented in docs/PLUGINS.md. Other SHVDNE scripts
    /// subscribe here; a throwing subscriber never breaks the mod.
    /// </summary>
    public static class ModBridge
    {
        /// <summary>Version of this event surface. Bumped on any signature change.</summary>
        public const string ApiVersion = "0.3.0";

        public static event DialogueStartedHandler OnDialogueStarted;
        public static event AiResponseReceivedHandler OnAiResponseReceived;
        public static event PedStateChangedHandler OnPedStateChanged;

        internal static void RaiseDialogueStarted(Ped ped, string roleLayer)
        {
            SafeInvoke(() => OnDialogueStarted?.Invoke(ped, roleLayer), "OnDialogueStarted");
        }

        internal static void RaiseAiResponseReceived(Ped ped, string response)
        {
            SafeInvoke(() => OnAiResponseReceived?.Invoke(ped, response), "OnAiResponseReceived");
        }

        internal static void RaisePedStateChanged(Ped ped, PedState oldState, PedState newState)
        {
            if (oldState == newState) return;
            SafeInvoke(() => OnPedStateChanged?.Invoke(ped, oldState, newState), "OnPedStateChanged");
        }

        private static void SafeInvoke(Action action, string eventName)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Log.Warn("A subscriber of " + eventName + " threw: " + ex.GetType().Name + ": " + ex.Message);
            }
        }
    }

    /// <summary>
    /// Tiny file logger. SHVDNE has no logger of its own, and a mod that fails
    /// silently is impossible to debug in game. Writes next to the ini.
    /// </summary>
    internal static class Log
    {
        private static readonly object Gate = new object();
        private static string _path;

        internal static void Init(string directory)
        {
            _path = Path.Combine(directory ?? ".", "PedAndPaw_AI.log");
        }

        internal static void Info(string message) { Write("INFO", message); }
        internal static void Warn(string message) { Write("WARN", message); }
        internal static void Error(string message) { Write("ERROR", message); }

        private static void Write(string level, string message)
        {
            var line = "[" + DateTime.Now.ToString("HH:mm:ss") + "] [" + level + "] " + message;
            try
            {
                if (_path == null) return;
                var info = new FileInfo(_path);
                if (info.Exists && info.Length > 1024 * 1024)
                    File.WriteAllText(_path, "[" + DateTime.Now.ToString("HH:mm:ss") + "] [INFO] Log rotated at 1 MB" + Environment.NewLine);
                File.AppendAllText(_path, line + Environment.NewLine);
            }
            catch
            {
                // Never let logging break gameplay.
            }
        }
    }
}
