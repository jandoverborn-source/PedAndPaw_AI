# INI specification (annotated)

> Working document, originally Swedish. Converted to Markdown so it lives with the project.

; =============================================================

; PedAndPaw\_AI – konfiguration

; Ändringar kräver att spelet startas om, om inget annat anges.

; Kommentarer börjar med semikolon.

; =============================================================


\[General\]

; Aktivera eller stäng av hela modden

Enabled = true

; Språk för systemmeddelanden och undertexter (sv / en)

Language = sv

; Loggnivå: None, Error, Info, Debug

LogLevel = Info

; Visa notiser i spelet när ett svar kommer

ShowNotifications = true


\[Keys\]

; Interagera med närmaste NPC/djur, eller avfärda NPC:n

InteractKey = E

; Skicka textmeddelande till närliggande civila, polis och räddningstjänst

TextMessageKey = T

; Stäng av textrutan utan att skicka

CancelKey = Escape


\[Interaction\]

; Max avstånd (meter) för att kunna prata med en NPC eller ett djur

InteractionRange = 4.0

; Vilka typer som kan pratas med

TalkToCivilians = true

TalkToPolice = true

TalkToEmergency = true

TalkToAnimals = true

; Om NPC:n ska vända sig mot spelaren när samtal startar

NpcFacePlayer = true


\[Context\]

; Läs av omgivningen och skicka med det till AI:n

UseTimeOfDay = true

UseWeather = true

UseArea = true

UseVehicle = true

; Vapen- och hotstatus (tillval)

UseWeaponState = true

UseAimingState = true

UseWantedLevel = true

UseRecentGunshots = true

; Hur länge (sekunder) efter skott som räknas som "nyligen"

GunshotMemorySeconds = 30


\[Ai\]

; Standardleverantör: Gemini (OpenAI-kompatibelt endpoint)

; Alternativ: ollama, openrouter, groq, custom

Provider = gemini

; Base URL för OpenAI-kompatibel chat completions

BaseUrl = https://generativelanguage.googleapis.com/v1beta/openai/

; Modellnamn för vald leverantör

Model = gemini-2.5-flash

; Namnet på miljövariabeln som innehåller API-nyckeln (lägg inte nyckeln här)

ApiKeyEnvVar = PEDANDPAW\_AI\_KEY

; Timeout för HTTP-anrop (sekunder)

RequestTimeoutSeconds = 15

; Temperatur för svarens variation (0.0–1.5)

Temperature = 0.8


\[Limits\]

; Spar API-tokens: korta svar och begränsningar

MaxTokensPerReply = 120

; Max antal tecken i ett svar som visas i spelet

MaxReplyCharacters = 240

; Minsta tid (sekunder) mellan två AI-anrop från samma NPC

NpcCooldownSeconds = 8

; Max antal AI-anrop per minut totalt

MaxRequestsPerMinute = 20

; Max antal tidigare meddelanden som sparas per NPC i minnet

MemoryMessagesPerNpc = 6

; Max antal NPC:er som kan prata samtidigt

MaxConcurrentConversations = 2

; Stoppa AI-anrop när månadens eller dagens budget är slut (antal anrop)

DailyRequestBudget = 500


\[Behavior\]

; Hur ofta humör (mode) slumpas om för en NPC (minuter), 0 = aldrig

ModeChangeIntervalMinutes = 15

; Låt händelser ändra humöret (t.ex. spelaren drar vapen)

DynamicModeSwitching = true

; Attityd påverkas av spelarens handlingar

AttitudeSystem = true

; Startvärde för förtroende (0–100)

DefaultTrust = 50


\[Weapons\]

; Reaktioner på vapen i dialogen (EnableWeaponReactions)

EnableWeaponReactions = true

; Skjutning endast på tillåtna platser

; Tillåtna zoner: ShootingRange, OwnPropertyOutsideCity

AllowShootingIn = ShootingRange, OwnPropertyOutsideCity

; Om skjutning är blockerad utanför tillåtna zoner

BlockFireOutsideAllowedZones = true

; NPC:er reagerar när spelaren skjuter på fel plats

ReactToIllegalFire = true


\[Animals\]

; Djur kan prata men kan aldrig skadas eller dödas

AnimalsCanBeShot = false

AnimalsCanBeKilled = false

; Aktivera eller stäng av enskilda djur

EnableRat = true

EnableDogs = true

EnableCat = true

EnableCrow = true

EnableSeagull = true

EnableMountainLion = true

EnableFishAndMarine = true

; Hur mycket djur får fråga efter sådant de vill ha (sekunder mellan frågor)

AnimalQuestIntervalSeconds = 60


\[Vehicles\]

; Låt spelaren låna fordon av NPC:er

EnableVehicleBorrowing = true

; Krav på attityd (0–100) för att NPC:n ska gå med på att låna ut fordon

MinTrustToLendVehicle = 65


\[Content\]

; Innehållsreglage för nakenhet i AI:ns svar (0–100 procent)

; Standard är 0. Modden ska respektera värdet i all dialog.

NudityPercent = 0


\[Roles\]

; Mapp med redigerbara rollfiler (en textfil per roll/djur)

RolesFolder = scripts/PedAndPaw\_AI/roles

; Ladda om rollfiler vid start av spelet

ReloadRolesOnStart = true




Några saker att kontrollera:

- Standardvärdet för `Model` är ett förslag. Kontrollera vilka modellnamn som är aktuella hos Google när du sätter upp API-nyckeln. 

- `AllowShootingIn` och `BlockFireOutsideAllowedZones` behöver stöd i `ContextScanner.cs`, så att zonerna kan identifieras. 

- `DailyRequestBudget` är ett skydd mot att API-kvoten tar slut i förtid. Justera värdet efter din egen gräns.
