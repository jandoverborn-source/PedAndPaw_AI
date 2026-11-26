using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GTA;
using GtaScreen = GTA.UI.Screen;
using GtaNotification = GTA.UI.Notification;
using PedAndPaw_AI.Core;

namespace PedAndPaw_AI
{
    /// <summary>
    /// The mod's entry point. SHVDNE instantiates a Script subclass named Main
    /// from scripts/PedAndPaw_AI.dll. Tick stays cheap and never awaits:
    /// replies arrive on a worker thread and are queued back to the game thread.
    /// </summary>
    public class Main : Script
    {
        private readonly ModConfig _config;
        private readonly CharacterManager _characters;
        private readonly RateLimiter _limiter;
        private readonly IAiProvider _provider;
        private readonly ConcurrentQueue<Action> _gameThreadQueue = new ConcurrentQueue<Action>();
        private readonly Keys _interactKey;
        private readonly Keys _textKey;
        private readonly Keys _cancelKey;

        // SHVDNE's World.GetNearbyPeds calls Array.ConvertAll on the model filter without a null
        // check, so null throws ArgumentNullException. An empty array means "any model".
        private static readonly Model[] NoModelFilter = new Model[0];

        private DateTime _lastGunshotUtc = DateTime.MinValue;
        private DateTime _lastShotWarnUtc = DateTime.MinValue;
        private int _activePedHandle;
        private PedState _activeState = PedState.Idle;

        public Main()
        {
            var baseDirectory = BaseDirectory ?? AppDomain.CurrentDomain.BaseDirectory;
            Log.Init(baseDirectory);

            var iniPath = Path.Combine(baseDirectory, "PedAndPaw_AI.ini");
            _config = ModConfig.Load(iniPath);
            foreach (var warning in _config.Warnings) Log.Warn("Config: " + warning);

            _characters = new CharacterManager(_config, baseDirectory);
            _limiter = new RateLimiter(_config);
            _provider = CreateProvider(_config, baseDirectory);

            _interactKey = ParseKey(_config.InteractKey, Keys.E);
            _textKey = ParseKey(_config.TextMessageKey, Keys.T);
            _cancelKey = ParseKey(_config.CancelKey, Keys.Escape);

            Tick += OnTick;
            KeyDown += OnKeyDown;

            var cloudProvider = !string.Equals(_config.Provider, "ollama", StringComparison.OrdinalIgnoreCase);
            if (cloudProvider && _provider is OpenAiCompatibleClient)
            {
                var notice = "PAPAI: cloud provider in use (" + _config.Provider + "). " +
                             "What you type is sent to that provider; a local model keeps it on this machine.";
                Log.Warn(notice);
                GtaScreen.ShowSubtitle(notice, 6000);
            }

            if (_config.NudityPercent > 0)
            {
                var notice = "PedAndPaw_AI: nudity is enabled (" + _config.NudityPercent +
                             "%). Sexual content stays disabled.";
                Log.Warn(notice);
                GtaScreen.ShowSubtitle(notice, 6000);
            }

            Log.Info("PedAndPaw_AI " + ModBridge.ApiVersion + " loaded. enabled=" + _config.Enabled +
                     ", roles=" + _characters.Roles.Count +
                     ", provider=" + _provider.GetType().Name +
                     ", keys=" + _interactKey + "/" + _textKey + "/" + _cancelKey +
                     ", ini=" + iniPath);
        }

        /// <summary>
        /// Use the real client when a key is available (or the provider needs none),
        /// otherwise stay on the placeholder so the game never fails without a key.
        /// </summary>
        private static IAiProvider CreateProvider(ModConfig config, string baseDirectory)
        {
            var fromEnvironment = new OpenAiCompatibleClient(config);
            if (fromEnvironment.HasCredentials)
            {
                Log.Info("AI provider: " + config.Provider + " / " + config.Model + " via " + config.BaseUrl +
                         " (key from the environment variable " + config.ApiKeyEnvVar + ")");
                return fromEnvironment;
            }
            fromEnvironment.Dispose();

            // Proton does not always hand the launcher's environment to the game, so a
            // local key file next to the ini is the reliable route. It is never committed.
            var keyPath = Path.Combine(baseDirectory, "PedAndPaw_AI.key");
            var fileKey = ReadKeyFile(keyPath);
            if (!string.IsNullOrWhiteSpace(fileKey))
            {
                var fromFile = new OpenAiCompatibleClient(config, fileKey);
                Log.Info("AI provider: " + config.Provider + " / " + config.Model + " via " + config.BaseUrl +
                         " (key from the local file " + keyPath + ")");
                return fromFile;
            }

            Log.Warn("No API key found. Looked in the environment variable '" + config.ApiKeyEnvVar +
                     "' and in '" + keyPath + "'. Provider '" + config.Provider +
                     "' is not ollama, so the placeholder provider is used.");
            return new NullAiProvider();
        }

        /// <summary>First non-empty line of the file, or null. The file is optional.</summary>
        private static string ReadKeyFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                foreach (var line in File.ReadAllLines(path))
                {
                    var trimmed = line.Trim();
                    if (trimmed.Length == 0) continue;
                    if (trimmed.StartsWith("#", StringComparison.Ordinal)) continue;
                    return trimmed;
                }
                return null;
            }
            catch (Exception ex)
            {
                Log.Warn("Could not read the key file " + path + ": " + ex.Message);
                return null;
            }
        }

        private static Keys ParseKey(string text, Keys fallback)
        {
            Keys parsed;
            if (!string.IsNullOrWhiteSpace(text) && Enum.TryParse(text.Trim(), true, out parsed)) return parsed;
            Log.Warn("Could not read key '" + text + "', using " + fallback + ".");
            return fallback;
        }

        private void OnTick(object sender, EventArgs e)
        {
            Action action;
            while (_gameThreadQueue.TryDequeue(out action))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Log.Error("Queued game-thread action failed: " + ex.Message);
                }
            }

            TrackShots();
        }

        /// <summary>Remember gunfire when the player or a nearby ped fires.</summary>
        private void TrackShots()
        {
            if (!_config.UseRecentGunshots) return;
            if (Game.FrameCount % 15 != 0) return;
            try
            {
                var player = Game.Player.Character;
                if (player != null && player.IsShooting)
                {
                    _lastGunshotUtc = DateTime.UtcNow;
                    return;
                }

                var peds = World.GetNearbyPeds(player, 60f, NoModelFilter);
                if (peds == null) return;
                foreach (var ped in peds)
                {
                    if (ped != null && ped.Exists() && ped.IsShooting)
                    {
                        _lastGunshotUtc = DateTime.UtcNow;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                if ((DateTime.UtcNow - _lastShotWarnUtc).TotalSeconds > 60)
                {
                    _lastShotWarnUtc = DateTime.UtcNow;
                    Log.Warn("Shot tracking failed: " + ex.Message);
                }
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!_config.Enabled) return;

            try
            {
                if (e.KeyCode == _interactKey) StartConversation(false);
                else if (e.KeyCode == _textKey) StartConversation(true);
                else if (e.KeyCode == _cancelKey) Dismiss();
            }
            catch (Exception ex)
            {
                // A keypress must never kill the script tick.
                Log.Error("Key handling failed: " + ex.GetType().Name + ": " + ex.Message);
                GtaScreen.ShowSubtitle("[PedAndPaw_AI] " + ex.GetType().Name + ": " + ex.Message, 3000);
            }
        }

        private void StartConversation(bool asTextMessage)
        {
            var player = Game.Player.Character;
            if (player == null) return;

            var ped = FindNearestPed(player);
            if (ped == null)
            {
                GtaScreen.ShowSubtitle("Nobody close enough to talk to.", 2000);
                return;
            }

            if (!asTextMessage && _activePedHandle == ped.Handle)
            {
                Dismiss();
                return;
            }

            if (asTextMessage && !ped.IsHuman)
            {
                GtaScreen.ShowSubtitle("You cannot send a text message to an animal.", 2000);
                return;
            }

            // The text box blocks the player, so do not open one in the middle of a fight.
            if (ped.IsInCombat || ped.IsFleeing)
            {
                GtaScreen.ShowSubtitle("(This one is in no mood to talk.)", 2000);
                return;
            }

            if (Game.Player.Wanted.WantedLevel > 0)
            {
                GtaScreen.ShowSubtitle("(Not while the police are looking for you.)", 2500);
                return;
            }

            BeginTalking(ped);
        }

        private Ped FindNearestPed(Ped player)
        {
            Ped best = null;
            var bestDistance = float.MaxValue;

            var radius = Math.Max(1f, _config.InteractionRange);
            var peds = World.GetNearbyPeds(player, radius, NoModelFilter);

            // Belt and braces: if the native filter returned nothing, fall back to a
            // distance filter over every ped. Cheap on a keypress and never null.
            if (peds == null || peds.Length == 0)
            {
                var all = World.GetAllPeds(NoModelFilter);
                if (all == null) return null;
                peds = Array.FindAll(all, candidate =>
                    candidate != null && candidate.Exists() &&
                    player.Position.DistanceTo(candidate.Position) <= radius);
            }

            if (peds == null) return null;

            foreach (var ped in peds)
            {
                if (ped == null || !ped.Exists() || !ped.IsAlive) continue;
                if (ped.Handle == player.Handle) continue;

                var isAnimal = !ped.IsHuman;
                if (isAnimal && !_config.TalkToAnimals) continue;
                if (!isAnimal && !_config.TalkToCivilians) continue;

                var distance = player.Position.DistanceTo(ped.Position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = ped;
                }
            }

            return best;
        }

        private void BeginTalking(Ped ped)
        {
            string roleLayer;
            var role = _characters.PickRole(ped, out roleLayer);
            if (role == null)
            {
                GtaScreen.ShowSubtitle("No role file loaded; check scripts/PedAndPaw_AI/roles.", 4000);
                return;
            }

            var playerNow = Game.Player.Character;
            var weaponDrawn = playerNow != null && playerNow.Weapons.Current.Hash != WeaponHash.Unarmed;
            var aiming = Game.Player.IsAiming;
            var shotsFresh = (DateTime.UtcNow - _lastGunshotUtc).TotalSeconds <= _config.GunshotMemorySeconds;
            var mode = _characters.StartMode(role, ped);
            if (string.Equals(mode, "Professional", StringComparison.OrdinalIgnoreCase) == false)
                mode = _characters.ReactToWeaponState(ped, weaponDrawn, aiming, shotsFresh);
            var npcKey = (ped.IsHuman ? "human-" : "animal-") + ped.Handle;

            if (!_limiter.TryAcquire(npcKey, DateTime.UtcNow))
            {
                GtaScreen.ShowSubtitle("(" + role.DisplayName + " needs a moment: " + _limiter.LastDenyReason + ")", 2500);
                return;
            }

            _characters.FacePlayer(ped, Game.Player.Character);
            _activePedHandle = ped.Handle;
            SetState(ped, PedState.AwaitingPlayer);
            ModBridge.RaiseDialogueStarted(ped, roleLayer);
            Log.Info("Dialogue started with ped " + ped.Handle + " as " + roleLayer + " (" + mode + ")");

            string input;
            var player = Game.Player.Character;
            var wasTargetable = player != null && player.CanBeTargetted;
            try
            {
                // Typing blocks the player; do not let them be shot or stabbed meanwhile.
                if (player != null) player.CanBeTargetted = false;
                input = Game.GetUserInput("");
            }
            catch (Exception ex)
            {
                Log.Error("GetUserInput failed: " + ex.Message);
                SetState(ped, PedState.Idle);
                return;
            }
            finally
            {
                if (player != null && wasTargetable) player.CanBeTargetted = true;
            }

            Log.Info("GetUserInput returned " + (input == null ? "null" : input.Length + " characters"));

            if (string.IsNullOrWhiteSpace(input))
            {
                Log.Info("Input cancelled or empty for ped " + ped.Handle);
                GtaScreen.ShowSubtitle("(" + role.DisplayName + " waits. Nothing was said.)", 2000);
                SetState(ped, PedState.Idle);
                return;
            }

            Log.Info("Player said to ped " + ped.Handle + ": " + Shorten(input));

            var context = ContextScanner.Scan(ped, _config, role, mode, _lastGunshotUtc, DateTime.UtcNow);
            context = context ?? new NpcContext { RoleLayer = role.DisplayName, ModeName = mode };

            var traits = _characters.SelectTraits(role, ped);
            var placeholders = traits.ToPlaceholders();
            placeholders["TIME_OF_DAY"] = context.TimeOfDay;
            placeholders["LIGHT"] = context.Light;
            placeholders["WEATHER"] = context.Weather;
            placeholders["AREA"] = context.Area;

            Log.Info("Traits for ped " + ped.Handle + ": " + traits.Name + " - " +
                     traits.Trait1 + ", " + traits.Trait2 + ", " + traits.Trait3 +
                     (string.IsNullOrEmpty(traits.Quirk) ? "" : " | quirk: " + traits.Quirk));

            var request = new AiRequest
            {
                SystemPrompt = PromptBuilder.BuildSystemPrompt(role, _config, mode, placeholders),
                UserMessage = PromptBuilder.BuildUserMessage(context, input, _config, role),
                MaxTokens = _config.MaxTokensPerReply,
                Temperature = ContextBuckets.TemperatureFor(mode, _config.Temperature),
                NpcKey = npcKey
            };

            SetState(ped, PedState.QueryingAI);
            RequestReply(ped, role, request);
        }

        /// <summary>Run the provider off the game thread and queue the result back.</summary>
        private void RequestReply(Ped ped, RoleDefinition role, AiRequest request)
        {
            var cancellation = new CancellationTokenSource();
            Task.Run(async () =>
            {
                string reply = null;
                try
                {
                    reply = await _provider.GetReplyAsync(request, cancellation.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Log.Error("Provider failed: " + ex.GetType().Name + ": " + ex.Message);
                }

                var capturedPed = ped;
                var capturedRole = role;
                _gameThreadQueue.Enqueue(() => ShowReply(capturedPed, capturedRole, reply));
            });
        }

        private void ShowReply(Ped ped, RoleDefinition role, string reply)
        {
            if (string.IsNullOrWhiteSpace(reply))
            {
                var client = _provider as OpenAiCompatibleClient;
                if (client != null && !string.IsNullOrEmpty(client.LastError))
                    Log.Warn("Provider error: " + client.LastError);
                GtaScreen.ShowSubtitle("(" + role.DisplayName + " did not answer)", 2000);
                SetState(ped, PedState.Idle);
                return;
            }

            var text = reply.Trim();
            if (_config.MaxReplyCharacters > 0 && text.Length > _config.MaxReplyCharacters)
                text = text.Substring(0, _config.MaxReplyCharacters).TrimEnd() + "...";

            Log.Info("Reply to ped " + (ped != null ? ped.Handle.ToString() : "?") + " (" + role.DisplayName + "): " + Shorten(text));
            GtaScreen.ShowSubtitle(text, 6000);
            if (_config.ShowNotifications) GtaNotification.PostTicker(text, false, true);

            ModBridge.RaiseAiResponseReceived(ped, text);
            SetState(ped, PedState.Speaking);
            SetState(ped, PedState.Idle);
        }

        private static string Shorten(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            var single = value.Replace("\r", " ").Replace("\n", " ").Trim();
            return single.Length <= 80 ? single : single.Substring(0, 80) + "...";
        }

        private void Dismiss()
        {
            if (_activePedHandle == 0) return;

            Ped active = null;
            var peds = World.GetNearbyPeds(Game.Player.Character, 20f, NoModelFilter);
            if (peds != null)
            {
                foreach (var ped in peds)
                {
                    if (ped != null && ped.Exists() && ped.Handle == _activePedHandle)
                    {
                        active = ped;
                        break;
                    }
                }
            }

            if (active != null) SetState(active, PedState.Idle);
            _activeState = PedState.Idle;
            _activePedHandle = 0;
            GtaScreen.ShowSubtitle("Conversation ended.", 1500);
        }

        private void SetState(Ped ped, PedState newState)
        {
            var oldState = _activeState;
            _activeState = newState;

            if (ped == null || !ped.Exists()) return;
            ModBridge.RaisePedStateChanged(ped, oldState, newState);
        }
    }
}
