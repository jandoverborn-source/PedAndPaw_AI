# AGENTS.md: instruktioner för agenten i PedAndPaw_AI

## Projekt
PedAndPaw_AI är en MIT-licensierad ScriptHookVDotNet Enhanced-mod för GTA V (C#, .NET Framework 4.8).
 Vi skall ligga inom "Rekommenderad ålder: 13+" 
Källkoden ligger i src/. Det som ska kopieras till spelet ligger i scripts/.

## Regler
- Ändra endast filer i den uppgift du fått. Skapa inga nya filer utanför mappstrukturen nedan.
- Arbeta med diffar. Skriv inte om hela filer om det inte krävs.
- Skriv aldrig API-nycklar i kod, .ini eller dokumentation. Nycklar läses från miljövariabler.
- Gör ett commit per avslutad uppgift på en egen gren.
- Gissa aldrig SHVDNE-API-signaturer. Om du är osäker, markera det och eskalera.
- Rollfiler är vanliga textfiler (.txt), inte JSON. Innehållet är användarens egen beskrivning av personligheten.
- Skriv kodkommentarer, ini-kommentarer och dokumentation på engelska. Repo:t är öppen källkod.

## Mappstruktur
- src/: alla .cs-filer och PedAndPaw_AI.csproj (net48, bygger till scripts/)
- scripts/: PedAndPaw_AI.ini och PedAndPaw_AI/roles/ (en .txt-fil per roll eller djur, i animals/ och humans/)
- docs/: PROFILES.md, PLUGINS.md, TESTING.md och docs/planning/ (arbetsdokument och underlag)
- .github/workflows/build.yml

## Modellroller
- Planering och svår logik (Main.cs, OpenAiCompatibleClient.cs, ModBridge.cs): använd stark modell via FreeLLMAPI.
- Mönsterkod (ContextScanner.cs, CharacterManager.cs): använd lokal kodmodell.
- Profiler, ini och dokumentation: använd liten lokal modell.
- Eskalera till stark modell efter två misslyckade byggen i rad.

## Kvalitetsgrindar
1. Bygget ska gå igenom (dotnet build) innan du committar. Kör agenten på en maskin utan .NET SDK, verifiera struktur och CI-flöde i stället och skriv i uppgiftsrapporten att bygget inte kunde köras lokalt.
2. Logik som inte kräver spelet ska kunna testas utan GTA. Håll den separat från SHVDNE-beroende kod.
3. Ändra inte CI-konfigurationen utan att fråga.

## Kostnad
- Använd dhs-agent-team om och när det funkar 
- Cacha svar från molnmodeller för återkommande frågor.
- Vid rate limit: byt leverantör i stället för att vänta.
- Håll kontexten liten: läs bara de filer som uppgiften kräver.
