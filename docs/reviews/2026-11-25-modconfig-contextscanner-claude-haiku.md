# Granskning: ModConfig / ContextScanner-underlag — 2026-11-25

| | |
|---|---|
| **Granskare** | Claude Haiku 5.5 (externa genomläsning, beställd av projektägaren) |
| **Datum** | 2026-11-25 |
| **Underlag** | `src/Core/ModConfig.cs` i två delar, plus `src/ContextScanner.cs` |
| **Mottagare** | Kaptenen (agenten) |
| **Form** | Kalkylark, 19 rader; överfört till den här filen |

Ägarens instruktion: detaljerna behöver inte redovisas för honom — de ska hanteras.
Filen finns för att besluten ska gå att granska i efterhand.

## Fynd och status

| # | Allvarlighet | Plats | Problem | Åtgärd | Status |
|---|---|---|---|---|---|
| 1 | Hög | `ParseFloat` | Kulturberoende float-parsning: `4.0` kan bli fel på svensk Windows, och `NumberStyles.Any` tillåter tusentalsavgränsare så att `4,0` kan bli 40 | Parsar nu med `CultureInfo.InvariantCulture` och `NumberStyles.Float` | `fixed` |
| 2 | Hög | `NudityPercent` | Ingen klampning; 250 och −5 gick igenom | Klampas till 0–100 med varning | `fixed` |
| 3 | Hög | Hela konfigurationen | Inga gränsvärden validerades; noll timeouts och noll budgetar blir tysta dödsknappar | `ClampValues` klämmer 15 värden (timeout, tokens, budgetar, intervall, förtroende) med varning per korrigering | `fixed` |
| 4 | Hög | `PromptBuilder` / nakenhet | Nakenhetsreglaget måste vara en kontrollpunkt i koden, inte bara ett fält — särskilt för karaktärer som kan vara minderåriga | Egen, ovillkorlig promptregel: ingen sexualisering av någon som är eller kan vara under 18, och ett synligt meddelande i spelet när nakenhet slås på | `fixed` |
| 5 | Medel | Kommentarer | `Enabled = true ; kommentar` gav "Cannot parse boolean"; bara hela rader med `;` räknades som kommentar | `StripInlineComment` klipper `;` och `#` utanför citattecken | `fixed` |
| 6 | Medel | `currentSection` | Sektionen sparades men användes inte; samma nyckel i två sektioner skrev tyst över | Nyckel→sektion-karta: varnar både för dubblett i samma sektion och för nyckel i fel sektion | `fixed` |
| 7 | Medel | Varningstexter | Stavfel i kontextsträngarna: `npcfaceseplayer`, `usewaponstate`, `npccelfawareness` | Rättade | `fixed` |
| 8 | Medel | `AllowShootingIn` | Zonnamn validerades inte; en stavfel blockerar tyst skjutning överallt | Okända zoner ger nu varning med de kända namnen | `fixed` |
| 9 | Medel | `InteractKey` m.fl. | Tangenter måste mappas mot `Keys`, ogiltigt värde ska varna och falla tillbaka | Hanteras i `Main.ParseKey`: varning + fallback till E/T/Escape | `fixed` (i `Main`, inte i Core) |
| 10 | Låg | `ParseBool` | Accepterade inte `on`/`off` | Tillagda | `fixed` |
| 11 | Låg | Standardvärden | Påstående att ini-planen hade `MaxTokensPerReply = 150` och `ModeChangeIntervalMinutes = 10` | Stämmer inte: både `docs/planning/ini-spec.md` och levererad ini säger 120 respektive 15 | `not-applicable` |
| 12 | Hög | Filhuvuden | "Inga using-satser" | Core-projektet har `ImplicitUsings`; `ContextScanner.cs` har explicita usings och bygger med noll varningar | `not-applicable` |
| 13 | Låg | `catch` i `FromIniText` | Fångar nästan inget; kan ge falsk trygghet | Behålls som skydd; kommentaren säger nu att parsningen inte kastar | `fixed` |
| 14 | Medel | Modellnamn | `gemini-3-flash-preview` är en preview-modell som byts ofta | Verifierat live mot Google; namnbytet är dokumenterat i `docs/TESTING.md` | `deferred` (bevakat) |

## Inte åtgärdat, med skäl

- **Skjutning utanför tillåtna zoner blockeras inte i kod ännu.** `AllowShootingIn`,
  `BlockFireOutsideAllowedZones` och `ReactToIllegalFire` läses och valideras, men ingen
  kod hindrar avtryck. Zonerna kräver spelkoordinater (skjutbanor, egen tomt utanför
  staden) som måste verifieras mot spelet — det arbetet är inte gjort. Står kvar som
  `open` i planeringen, och ini-kommentaren påstår inget annat.
- **Roll per modell** är fortfarande inte kopplad: alla människor får samma roll eftersom
  bara en mänsklig rollfil finns. Modell→roll-mappning ligger i nästa steg.

## Vad granskningen inte täckte

Granskaren efterfrågade `ContextScanner.cs` och de delar av `Main.cs` som anropar
`ModConfig` samt zon- och vapenkontrollen. Det underlaget har vi inte skickat än.
Nästa granskning bör därför täcka:

1. `ContextScanner.cs` — att varje `Use*`-flagga respekteras och att inga värden gissas.
2. `Main.cs` — att `Tick` aldrig blockerar och att trådarna möts i kön.
3. Zon- och vapenspärren när den byggs.

## Verifiering

```
PATH=$HOME/.dotnet:$PATH dotnet run --project tests/CoreChecks -c Release
→ ALL 31 CHECKS PASSED
```

Nya kontroller som kom ur den här granskningen: inline-kommentarer, `on`/`off`,
kommatecken som decimaltecken, dubblettnycklar och nyckel i fel sektion, klampning av
fyra värden, okänd skjutzon, samt minor-skyddet i prompten.
