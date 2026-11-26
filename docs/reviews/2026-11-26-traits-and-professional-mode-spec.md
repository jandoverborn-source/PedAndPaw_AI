# Granskning/specifikation: validator, kontextfält och professionellt läge — 2026-11-26

| | |
|---|---|
| **Underlag** | `npc-and-animal-prompt-template.odt` (projektägarens mall + tre preciseringspunkter) |
| **Datum** | 2026-11-26 |
| **Form** | Word-dokument, 97 rader; texten nedan är besluten, inte en avskrift |

Granskningen preciserar tre saker innan traits byggs: vad validatorn ska kontrollera, hur
kontextfält ska levereras, och att yrkes-NPC:er behöver ett sjätte läge.

## Beslut och status

| # | Punkt | Beslut | Status |
|---|---|---|---|
| 1 | Validatorns kontroller | Införs i både `tools/validate_roles.py` och `RoleParser` med samma regler | `open` |
| 1a | Sektionsrubriker `=== TRAITS ===` med `[CORE]`, `[QUIRK]`, `[NAMES]` | Antas. Hakparenteser i samma stil som `[Friendly]`, en post per rad | `open` |
| 1b | Minst 10 core, 8 quirk, 10 namn per art | Antas som **varning**, inte fel: en art kan vara ung och växa. Kravet ligger i planeringen, inte i hård grind | `open` (avvikelse) |
| 1c | Core-egenskap ska vara ett beteende, inte ett adjektiv | Antas som varning med en lista på vanliga adjektiv (nyfiken, lat, glad, arg, snäll) | `open` |
| 1d | Alla platshållare finns | **Ändrat:** okänd platshållare är fel; att kräva närvaro av alla platshållare vore att bryta mot befintliga rollfiler som saknar mall. Kända platshållare: `{NAME}`, `{SPECIES}`, `{TRAIT_1..3}`, `{QUIRK}`, `{TIME_OF_DAY}`, `{LIGHT}`, `{WEATHER}`, `{AREA}` | `open` (avvikelse) |
| 1e | Maxstorlek 64 KB | Antas (höjt från 8 KB; traits-listorna tar plats) | `open` |
| 1f | Ordlista mot förbjudet innehåll | Antas som **varning**, inte fel: en rollfil säger ofta "beskriv aldrig blod", och en hård regel skulle fälla den texten | `open` (avvikelse) |
| 2 | `{TIME_OF_DAY}` morning/noon/afternoon/evening/night, `{LIGHT}` daylight/dusk/darkness, `{WEATHER}` clear/cloudy/rain/fog, `{AREA}` från spelet | Antas. Buckets beräknas i Core så de går att testa utan spel. **Modellen får aldrig ett exakt klockslag** | `open` |
| 2a | Uppdateras högst var 30:e sekund | Antas: kontexten cachas och räknas om tidigast var 30:e sekund | `open` |
| 3 | Sjätte läget `[Professional]` för yrkes-NPC:er | Antas som **valfritt läge** utöver de fem obligatoriska. Yrkesroller får starta i Professional | `open` |
| 3a | Temperatur 0,2–0,3 och fast seed per NPC i professionellt läge | Temperatur kan sättas per läge. Fast `seed` stöds inte av alla leverantörer; skickas bara när leverantören är känd för att stödja det. Markeras som begränsning | `open` (begränsad) |
| 3b | Instruktören får bara spelnära vapenkunskap | Antas som promptregel: säkerhet, hantering på banan, spelets vapenfunktioner — aldrig tillverkning, modifiering eller optimering av riktiga vapen | `open` |

## Vad det här betyder för formatet

En rollfil får två nya, valfria delar:

```
=== TRAITS ===
[CORE]
jagar fjärilar
gillar lådor
kräsen med mat
[QUIRK]
tror att spelaren är ett möbelstycke
[NAMES]
Silvia
Mrr
Tass
```

och, för människor med yrke, ett sjätte läge:

```
[Professional]
Du talar lugnt och sakligt. En fråga i taget. Regler anges med skäl.
```

## Uppföljning

Implementeras i den här ordningen: parser och validator först (så att mallar kan skrivas och
kontrolleras), sedan kontextbuckets och platshållarsubstitution i prompten, därefter
`CharacterManager`-fröning och Professional som startläge.
