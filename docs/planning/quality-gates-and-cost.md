# Quality gates and cost

> Working document, originally Swedish. Converted to Markdown so it lives with the project.

## Kvalitetsgrindar

- Bygget måste gå igenom. Kör `dotnet build` och får inte gå vidare om det fallerar. Om det fallerar två gånger i rad eskalerar den till molnmodellen. 

- Separera ren logik från spelberoende kod. Providerklienten, konfigurationsläsningen och kontextlogiken kan ligga i ett bibliotek utan SHVDNE-beroenden. Då kan du testa dem med en falsk HTTP-handler utan att starta GTA. 

- GitHub Actions validerar att DLL:en byggs och att `scripts/` är komplett. Det är er sista kontroll innan release. 

- Lokalt bygge kräver Windows med .NET SDK och referenssammansättningar för net48. Om din agent kör på en annan maskin, låt den bara skriva kod och låt CI bygga. 

## Kostnaden

- När möjligt använd dsh-agent-teams. 

- Cacha FreeLLMAPI-svar för återkommande frågor, t.ex. API-signaturer och mallar. 

- Byt leverantör vid rate limit i stället för att vänta, och håll en lista över vilka gratismodeller som räcker för vilken uppgift. 

- Nycklar i miljövariabler, aldrig i `.ini` eller i repot. Lägg till en tydlig regel i `AGENTS.md` att agenten aldrig skriver nycklar i filer. 

Lokala modeller som är svaga på verktygsanrop och på att hålla flera filer konsekventa. Låt därför den lokala agenten föreslå ändringar medan en tydlig kontrollsteg, till exempel `dotnet build` och en diff-granskning, avgör vad som faktiskt landar i repot.
