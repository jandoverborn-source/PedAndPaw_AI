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

  


