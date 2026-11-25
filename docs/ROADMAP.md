# Roadmap till 1.0

Målet är inte att bli klar snabbt, utan att 1.0 ska vara något man kan rekommendera utan att
skämmas. Ordningen nedan är vald så att varje steg gör nästa steg billigare.

## Var vi står (gjort)

- Repo, CI som bygger mot pinnad SHVDNE 1.1.0.6, MIT-licens
- Låst rollfilmsformat med validator (5 lägen, 3 vapenreaktioner) och fyra rollfiler
- `src/Core`: config, rollparser, promptbygge, rate limiter — testbart utan spel (32 kontroller)
- Spelets halva: Tick-loop, E/T/Escape, närmaste ped, kontextscanning, roller per modell,
  plugins-event, loggning
- AI-provider mot valfritt OpenAI-kompatibelt API; verifierat live mot Gemini och lokal Ollama
- Rollsäkerhet: polis/civilist separerade, samtal vägras i strid, spelaren kan inte skadas medan
  textrutan har fokus
- Granskningsmapp med spårbara beslut (`docs/reviews/`)
- Menyhopp via tredjeparts-ASI (dokumenterad i `docs/TESTING.md`)

## 1. Traits — individer i stället för arter

Rollfiler blir **mallar**: en valfri `=== TRAITS ===`-sektion med `core:`, `quirk:` och `names:`,
plus platshållarna `{NAME}`, `{TRAIT_1..3}`, `{QUIRK}` i prosan. Koden väljer 2–3 core och 0–1
quirk, fröat på ped-handle, så samma katt beter sig likadant hela sessionen.

*Klart när:* validatorn (v1.1) har testfall för både giltiga och ogiltiga traits-sektioner;
`CharacterManager` fyller platshållarna; `CoreChecks` bevisar att två olika ped-id:n får olika
egenskaper och att samma id får samma.

## 2. Skjutbanan som skyltfönster

Egen `Script`-klass i samma DLL: tavlor och burkar på en verifierad plats, poäng, och en
**AI-instruktör** som kommenterar din träffbild. Ingen blod, inga skadade — bara skytte.

*Klart när:* banan byggs på plats utan att krascha, poäng räknas, instruktören svarar i roll,
och `AllowShootingIn`-zonen äntligen har en riktig plats att peka på.

## 3. Innehållspaket

Små, tydliga paket som kan installeras var för sig:

- **Strand:** badgäster, surfare, joggare (13+-säkert)
- **Bilar:** taxichaufför, mekaniker, parkeringsvakt, bilträff
- **Skjutbana:** instruktör, tävlingsskytt, åskådare
- **Djur:** de sju hundraserna, kråka, mås, råtta, fisk

*Klart när:* varje paket är ren text, validerat, och beskrivet med en rad i README.

## 4. Tillgänglighet och release

- Engelskt README som primär, kinesisk översättning (den här mappen) — svenska behövs inte, alla svenskar läser engelska
- 15-sekunders GIF högst upp: katt, puma, polis
- Release med färdig ZIP: `scripts/` + DLL, inget byggsteg för användaren
- Publicering på GTA5-Mods och Nexus med rubriken "free with a local model, no API key"

## Vad 1.0 kräver

| Krav | Varför |
|---|---|
| 30 minuters spel utan undantag i loggen | Det är vad en recensent gör först |
| Fungerar utan API-nyckel (lokal modell) | Annars kostar varje mening pengar |
| Minst 20 rollfiler, varav 5 djur | Tillgången är det som får folk att stanna |
| Inga hemligheter i repot, inga nycklar i filer | Förtroende |
| 13+-policyn håller i praktiken, inte bara i prompten | Föräldrar ska kunna lita på märkningen |
| Dokumentation som räcker för att skriva en egen roll | Innehållet ska växa utan oss |

## Inte i 1.0

Röst (TTS/STT), multiplayer-medvetenhet, minne mellan spelsessioner, automatisk
översättning av rollfiler. Alla är möjliga senare; ingen av dem behövs för att konceptet
ska fungera.
