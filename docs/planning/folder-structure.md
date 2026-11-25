# Folder structure

> Working document, originally Swedish. Converted to Markdown so it lives with the project.

## Mappstruktur


PedAndPaw_AI/

├── README.md

├── LICENSE                     \# MIT

├── .gitignore

├── PedAndPaw_AI.sln             \# Valfri, för utvecklare

├── src/

│   ├── PedAndPaw_AI.csproj      \# Bygger till scripts/ (net48)

│   ├── Main.cs

│   ├── ContextScanner.cs

│   ├── CharacterManager.cs

│   ├── IAiProvider.cs

│   ├── OpenAiCompatibleClient.cs

│   ├── IAudioHook.cs

│   └── ModBridge.cs

├── scripts/                    \# Det som kopieras till GTA V-mappen

│   ├── PedAndPaw_AI.ini

│   └── PedAndPaw_AI/

│       └── roles/              \# en .txt-fil per roll eller djur, läses rekursivt

│           ├── animals/

│           │   ├── gatukatt.txt

│           │   └── puma.txt

│           └── humans/

│               └── police.txt

├── docs/

│   ├── PROFILES.md             \# Hur man skriver egna profiler

│   └── PLUGINS.md              \# Hur man kopplar på ljud och andra moddar

└── .github/

    └── workflows/

        └── build.yml           \# Bygger DLL vid release
        
Mappstruktur, README-upplägg,
Jag tar steg 1 nu. Ett viktigt förtydligande först: ScriptHookVDotNet laddar kompilerade .dll-filer från scripts/, inte råa .cs-filer. Den vanliga lösningen är att hålla källkoden i src/, bygga den till scripts/PedAndPaw_AI.dll och publicera den färdiga DLL:en i releaser. Användarna slipper då Visual Studio, men det behövs ändå ett litet byggsteg för dig som utvecklare. Strukturen nedan utgår från det.

Mappen scripts/ är placerad i repots rot så att användare kan kopiera innehållet direkt till sin GTA V-installation. Rollfilerna ligger under PedAndPaw_AI/roles/ så att de inte blandas ihop med DLL-filen. Rollfiler är vanliga textfiler (.txt) som användaren själv skriver, en per roll eller djur, med personligheten i egna ord. JSON används inte.
