# Delprojekt skjutbana

> Arbetsdokument från projektägaren. Idén: skjutbanor där vapen får användas — inte för att
> döda, utan för att skjuta, vilket är det mest populära i GTA. Instruktör, tavlor, burkar
> och poäng.

## Granskning av utkastet mot det verifierade SHVDNE-API:et

| Anrop i utkastet | Status |
|---|---|
| `Script.Interval = 50` | **Finns inte** i SHVDNE 1.1.0.6 — egenskapen existerar inte på `Script`; `Tick` räcker |
| `World.CreatePed(PedHash, Vector3)` | OK, `Model` har en konstruktor för `PedHash` |
| `World.CreateProp(Model, Vector3, bool, bool)` | OK |
| `instructor.Task.StandStill(-1)` | OK, finns på `TaskInvoker` |
| `Ped.BlockPermanentEvents` | OK |
| `PedHash.SciScientist01SMM` | Namnet finns inte i `PedHash`-enumen — välj en verifierad modell |

Sammanfattning: utkastet är nära nog att byggas, men `Interval`-raden hindrar kompilering och
instruktörens modell måste bytas mot ett namn ur den riktiga enumen.

## Utkastet

Delprojekt skjutbana**


using GTA;**

using GTA.Math;**

using GTA.Native;**

using System;**

using System.Collections.Generic;**


public class ShootingRange : Script**

{**

    private readonly Vector3 center = new Vector3(1710f, 2570f, 45.6f);**

    private const float Radius = 80f;**


    private Ped instructor;**

    private readonly List<Prop> allProps = new List<Prop>();**

    private readonly List<TargetItem> targets = new List<TargetItem>();**


    private int score = 0;**

    private bool built = false;**


    public ShootingRange()**

    {**

        Tick += OnTick;**

        Interval = 50;**

        BuildRange();**

    }**


    private void BuildRange()**

    {**

        if (built) return;**

        built = true;**


        // Instruktör**

        instructor = World.CreatePed(PedHash.SciScientist01SMM, center + new Vector3(2f, 2f, 0f));**

        if (instructor != null && instructor.Exists())**

        {**

            instructor.Task.StandStill(-1);**

            instructor.BlockPermanentEvents = true;**

        }**


        // Tavlor**

        SpawnTarget(**

            "PaperTarget",**

            "prop_target_frame_01",**

            "prop_target_01",**

            center + new Vector3(12f, 0f, 0f),**

            10**

        );**


        SpawnTarget(**

            "PaperTarget2",**

            "prop_target_frame_01",**

            "prop_target_01",**

            center + new Vector3(14f, 0.6f, 0f),**

            10**

        );**


        // Burkar / flaskor**

        SpawnTarget(**

            "Can1",**

            "prop_beer_pissh",**

            "prop_beer_bot_01",**

            center + new Vector3(16f, 2f, 0f),**

            5**

        );**


        SpawnTarget(**

            "Can2",**

            "prop_beer_pissh",**

            "prop_beer_bot_01",**

            center + new Vector3(16.4f, 2f, 0f),**

            5**

        );**


        SpawnTarget(**

            "Bottle1",**

            "prop_cs_beer_bot_01",**

            "prop_beer_bot_01",**

            center + new Vector3(16.8f, 2f, 0f),**

            5**

        );**


        // Flip-down / metallmål (placeholder-modeller som du kan byta till bättre)**

        SpawnTarget(**

            "MetalTarget1",**

            "prop_target_metal",**

            "prop_target_metal",**

            center + new Vector3(20f, -2f, 0f),**

            20**

        );**


        SpawnTarget(**

            "MetalTarget2",**

            "prop_target_metal",**

            "prop_target_metal",**

            center + new Vector3(22f, -2f, 0f),**

            20**

        );**


        UI.Notify("Shooting range built");**

    }**


    private void SpawnTarget(string name, string frameModel, string targetModel, Vector3 pos, int points)**

    {**

        var frameHash = (uint)Game.GenerateHash(frameModel);**

        var targetHash = (uint)Game.GenerateHash(targetModel);**


        Prop frame = World.CreateProp(frameHash, pos, true, true);**

        Prop target = World.CreateProp(targetHash, pos + new Vector3(0f, 0f, 0.8f), true, true);**


        if (frame != null && frame.Exists())**

        {**

            frame.FreezePosition = true;**

            frame.IsPersistent = true;**

            allProps.Add(frame);**

        }**


        if (target != null && target.Exists())**

        {**

            target.FreezePosition = true;**

            target.IsPersistent = true;**

            target.Health = 100;**

            allProps.Add(target);**


            targets.Add(new TargetItem**

            {**

                Name = name,**

                Prop = target,**

                Points = points,**

                InitialPos = target.Position,**

                InitialHealth = target.Health**

            });**

        }**

    }**


    private void OnTick(object sender, EventArgs e)**

    {**

        if (!Game.Player.Character.Exists()) return;**


        if (Game.Player.Character.Position.DistanceTo(center) > Radius)**

        {**

            Cleanup();**

            return;**

        }**


        UpdateTargets();**

        UI.ShowSubtitle($"Range score: {score}", 1);**

    }**


    private void UpdateTargets()**

    {**

        for (int i = targets.Count - 1; i >= 0; i--)**

        {**

            var t = targets\[i\];**

            if (t.Prop == null || !t.Prop.Exists())**

            {**

                targets.RemoveAt(i);**

                continue;**

            }**


            // Träff-/skadedetektion:**

            // 1) hälsa minskar**

            // 2) modellen har flyttats/knuffats**

            // 3) den är död eller förstörd**

            bool damaged = t.Prop.Health < t.InitialHealth;**

            bool moved = t.Prop.Position.DistanceTo(t.InitialPos) > 0.35f;**

            bool destroyed = t.Prop.Health <= 0 || t.Prop.IsDead;**


            if (damaged || moved || destroyed)**

            {**

                score += t.Points;**

                UI.Notify($"{t.Name} hit +{t.Points}");**


                if (t.Prop.Exists())**

                    t.Prop.Delete();**


                targets.RemoveAt(i);**

            }**

        }**

    }**


    private void Cleanup()**

    {**

        foreach (var p in allProps)**

        {**

            if (p != null && p.Exists())**

                p.Delete();**

        }**


        if (instructor != null && instructor.Exists())**

            instructor.Delete();**


        allProps.Clear();**

        targets.Clear();**

        score = 0;**

        built = false;**

    }**


    private class TargetItem**

    {**

        public string Name;**

        public Prop Prop;**

        public int Points;**

        public Vector3 InitialPos;**

        public int InitialHealth;**

    }**

}**

  


Viktigt:**


    prop_target_01, prop_target_metal, prop_beer_bot_01 osv. kan behöva justeras beroende på vilken modell som faktiskt finns och laddas i din version.**

    FreezePosition gör målen stabila.**

    Hit-detektionen här är praktisk, inte perfekt: den upptäcker skada/flytt/förstörelse.**


Bättre nästa steg skulle vara att:**


    skapa svagare collider-zoner för tavlor**

    använda raycast från kulan eller HasBeenDamagedByWeapon**

    ge varje mål olika “reaktion”:**

        tavla = poäng direkt**

        burk/flaska = poäng + liten animation**

        metallmål = flip-down/fold animation eller rotation**

## Rättelse från projektägaren (2026-11-26): använd spelets egna skjutbanor

Det finns **tre befintliga inomhusskjutbanor i GTA V** med färdiga props: tavlor, ställ,
diskar, belysning och väggar. Vi ska **inte** bygga en egen bana från grunden och inte
hitta på koordinater eller propnamn.

Planen är därför:

1. **Använd de befintliga banorna.** Vi behöver deras positioner och vilka props som hör
   till dem. Koordinaterna verifieras i spelet av ägaren — vi gissar aldrig.
2. **Bygg inget som redan finns.** Bara det som saknas: AI-instruktören, poängräkningen och
   kopplingen till moddens samtalsmotor.
3. **Props listas, inte påhittas.** Propnamn hämtas ur spelets egen lista över existerande
   objekt (`PedHash`/`ObjectHash`-enumen i API-dumpen), aldrig ur minnet.

### Vad som behövs från ägaren innan bygget

| Uppgift | Varför |
|---|---|
| Position för de tre banorna | Vi måste veta var de ligger, inte gissa |
| Vilka av banornas props som räknas som träffobjekt | Poängen ska räknas på rätt saker |
| Om spelaren får ta med egna vapen eller får lånevapen | Påverkar hur banan startar |
| Om instruktören ska stå på en fast plats eller följa spelaren | Påverkar AI-rollens läge |

Tills listan finns är skjutbanan **planerad, inte påbörjad** — och det står så i roadmapen.

## Fakta från gta.fandom.com (hämtat 2026-11-26 via wikins API)

Källa: https://gta.fandom.com/wiki/Ammu-Nation_Shooting_Ranges

- **Antal banor i GTA V/Online: två**, båda i Ammu-Nation-butiker — **Cypress Flats** och
  **Pillbox Hill**. Projektägaren nämnde tre; det behöver redas ut (tredje bana i Enhanced,
  i en Online-uppdatering, eller en annan typ av bana).
- **Inträde: $14.**
- **Kunderna på banan talar koreanska** (mandarin i PC-versionen). Bra karaktärsdetalj för
  en "range patron"-rollfil.
- **Varningsskyltarna** är färdigt samtalsmaterial för en skjutledare: hörsel- och
  ögonskydd, inget ätande eller drickande, patronhylsor i hinken, handtvätt efter bly,
  ammunitionstyp (endast full metal jacket), och den bästa raden: *"Never point a loaded or
  unloaded firearm at anyone on these premises."*
- **Poängskala per vapentyp** med brons, silver och guld. Exempel:

| Vapentyp | Brons | Silver | Guld |
|---|---|---|---|
| Handeldvapen (statiska mål) | 1 400 | 2 000 | 2 800 |
| Handeldvapen (rörliga mål) | 650 | 3 000 | 15 000 |
| Kulsprutepistol (statiska, vändbara) | 4 000 | 6 000 | 12 000 |
| Automatkarbin (statiska, tre träffar) | 2 500 | 3 550 | 7 550 |
| Hagelgevär (statiska, en träff) | 22 | 32 | 46 |
| Lätt kulspruta (fjärrmål som vänder) | 5 500 | 12 000 | 40 000 |
| Tungan vapen (minigun, multiplikator) | 10 000 | 25 000 | 50 000 |
| Bonus (railgun) | 20 | 22 | 24 |

- **Belöning:** alla brons ger 10 % rabatt i Ammu-Nation.

## Vad det betyder för vår bana

- De befintliga banorna är **inomhus**. Projektägaren vill ha en **utomhusbana med
  personal**. Två etapper föreslås, för att första versionen ska bli klar:
  1. **Etapp 1 — personal på en befintlig bana.** Skjutledare och lageransvarig som
     AI-NPC:er inne på Cypress Flats eller Pillbox Hill. Inga nya props, ingen
     ombyggnad: bara roller, regler och samtal. Spelarens skytte sker med spelets egen
     bana och poäng.
  2. **Etapp 2 — utomhusbanan.** Vi placerar props i öppen värld. Där återanvänder vi
     propmodeller från inomhusbanorna, men namn och positioner måste verifieras — aldrig
     gissas.
- **Poängnivåer** kan återanvändas rakt av: brons/silver/guld ovan ger en färdig skala för
  vår egen poängräkning i etapp 2.
- **Varningsskyltarna** blir skjutledarens röst. De är spelets egna ord, vilket gör rollen
  trovärdig utan att vi hittar på.

## Personal — projektägarens mål

| Roll | Uppgift i banan |
|---|---|
| **Skjutledare** | Säkerhet, briefing före skytte, regler med skäl, poäng, avvisa osäkert beteende |
| **Lageransvarig** | Vapenförråd, utlämning och inlämning, ammunition, inventarie |

## Öppna frågor som ägaren beslutar

1. Vilka är de tre banorna du menar, och är det samma två som wikin listar plus en?
2. Ska etapp 1 köras på en befintlig bana, eller vill du hoppa direkt till utomhusbanan?
3. Aktiviteter: fri skjutning, tidsutmaning, säkerhetskurs, eller allt tre?
4. Ska lageransvarig faktiskt låna ut och ta emot vapen (ett riktigt inventarie), eller bara
   samtala om det?
5. Ska skjutledaren kunna stoppa spelaren vid osäker hantering — och i så fall hur?
6. Inträdesavgift som i spelet ($14), eller gratis?
7. Får spelaren skjuta med vilka vapen som helst, eller bara de som banan tillhandahåller?

## Arbetsfördelning

| Vem | Bidrar med |
|---|---|
| Projektägaren | Upplevelsen: vilka aktiviteter, vilka personer, vilka regler som känns rätt, samt positioner och verifiering i spelet |
| Agenten | Verifierbara fakta (wiki, `PedHash`/`ObjectHash`-enumen, API-dumpen), rollfiler, kod, tester och dokumentation |

## Status

**Planerad, inte påbörjad.** Ingen kod, inga koordinater och inga propnamn skrivs förrän
projektägaren sagt till.
