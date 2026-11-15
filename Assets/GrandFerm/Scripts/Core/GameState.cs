using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrandFerm
{
    [Serializable] public class PlotSave
    {
        public int state;          // 0 untilled, 1 plowed, 2 growing, 3 ready
        public string crop = "";
        public float progress;     // 0..1
        public bool wet;
        public int waterCount;
    }

    [Serializable] public class BuildingSave
    {
        public bool owned;
        public int feed;           // production cycles left
        public float progress;     // seconds into current cycle
        public int ready;          // products waiting
    }

    [Serializable] public class SaveData
    {
        public int version = 1;
        public int money = 100;
        public int xp;
        public int level = 1;
        public int lang = 0;
        public string selected = "wheat";
        public List<string> invKeys = new List<string>();
        public List<int> invVals = new List<int>();
        public PlotSave[] plots;
        public BuildingSave[] buildings;
        public long lastTicks;
    }

    /// <summary>All game rules. No Unity scene objects here, so it is easy to test.</summary>
    public class GameState
    {
        public const int GridSize = 6;
        public const int PlotCount = GridSize * GridSize;
        const float MaxOfflineSeconds = 8f * 3600f;
        const string PrefKey = "grandferm_save_v1";

        public static GameState I;
        public SaveData D;
        public event Action Changed;
        public event Action<string> Toast;
        readonly Dictionary<string, int> inv = new Dictionary<string, int>();

        public GameState() { I = this; }

        // ---------- save / load ----------
        public void Load()
        {
            SaveData d = null;
            try
            {
                if (PlayerPrefs.HasKey(PrefKey)) d = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(PrefKey));
            }
            catch (Exception e) { Debug.LogWarning("Save load failed: " + e.Message); }
            if (d == null) { NewGame(false); return; }
            D = d;
            Normalize();
            L.Current = (Lang)Mathf.Clamp(D.lang, 0, 2);
            double elapsed = (new DateTime(DateTime.UtcNow.Ticks) - new DateTime(Math.Max(D.lastTicks, 0))).TotalSeconds;
            if (D.lastTicks > 0 && elapsed > 1) Tick((float)Math.Min(elapsed, MaxOfflineSeconds));
        }

        public void NewGame(bool keepLang)
        {
            int lang = keepLang ? (int)L.Current : 0;
            D = new SaveData { lang = lang };
            L.Current = (Lang)lang;
            Normalize();
            Save();
            Raise();
            Say(L.T("welcome"));
        }

        void Normalize()
        {
            if (D.plots == null || D.plots.Length != PlotCount)
            {
                D.plots = new PlotSave[PlotCount];
            }
            for (int i = 0; i < PlotCount; i++) if (D.plots[i] == null) D.plots[i] = new PlotSave();
            if (D.buildings == null || D.buildings.Length != Defs.Buildings.Length)
            {
                var nb = new BuildingSave[Defs.Buildings.Length];
                for (int i = 0; i < nb.Length; i++) nb[i] = (D.buildings != null && i < D.buildings.Length && D.buildings[i] != null) ? D.buildings[i] : new BuildingSave();
                D.buildings = nb;
            }
            inv.Clear();
            for (int i = 0; i < D.invKeys.Count && i < D.invVals.Count; i++) inv[D.invKeys[i]] = D.invVals[i];
            if (Defs.Crop(D.selected) == null) D.selected = "wheat";
        }

        public void Save()
        {
            D.invKeys.Clear(); D.invVals.Clear();
            foreach (var kv in inv) { D.invKeys.Add(kv.Key); D.invVals.Add(kv.Value); }
            D.lang = (int)L.Current;
            D.lastTicks = DateTime.UtcNow.Ticks;
            try { PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(D)); PlayerPrefs.Save(); }
            catch (Exception e) { Debug.LogWarning("Save failed: " + e.Message); }
        }

        // ---------- helpers ----------
        public int Inv(string id) { int v; return inv.TryGetValue(id, out v) ? v : 0; }
        void AddInv(string id, int n) { inv[id] = Inv(id) + n; }
        void Raise() { if (Changed != null) Changed(); }
        void Say(string s) { if (Toast != null) Toast(s); }

        public void AddXp(int n)
        {
            D.xp += n;
            while (D.xp >= Defs.XpForNext(D.level))
            {
                D.xp -= Defs.XpForNext(D.level);
                D.level++;
                Say(L.T("levelUp", D.level));
            }
        }

        public void SelectCrop(string id)
        {
            var c = Defs.Crop(id);
            if (c == null) return;
            if (D.level < c.unlockLevel) { Say(L.T("lvlNeeded", c.unlockLevel)); return; }
            D.selected = id;
            Raise();
        }

        // ---------- simulation ----------
        public void Tick(float dt)
        {
            foreach (var p in D.plots)
            {
                if (p.state != 2) continue;
                var c = Defs.Crop(p.crop);
                if (c == null) { p.state = 1; continue; }
                if (!p.wet) continue;
                p.progress += dt / c.growSeconds;
                if (p.waterCount == 1 && p.progress >= 0.5f) { p.progress = 0.5f; p.wet = false; }
                else if (p.progress >= 1f) { p.progress = 1f; p.state = 3; p.wet = false; }
            }
            for (int i = 0; i < D.buildings.Length; i++)
            {
                var b = D.buildings[i]; var def = Defs.Buildings[i];
                if (!b.owned || b.feed <= 0 || b.ready >= 10) continue;
                b.progress += dt;
                while (b.progress >= def.cycleSeconds && b.feed > 0 && b.ready < 10)
                {
                    b.progress -= def.cycleSeconds;
                    b.feed--;
                    b.ready += def.animals;
                }
                if (b.feed <= 0) b.progress = 0;
            }
        }

        // ---------- player actions ----------
        public void TapPlot(int i)
        {
            var p = D.plots[i];
            switch (p.state)
            {
                case 0:
                    p.state = 1; AddXp(1); Say(L.T("plowed"));
                    break;
                case 1:
                {
                    var c = Defs.Crop(D.selected);
                    if (D.level < c.unlockLevel) { Say(L.T("lvlNeeded", c.unlockLevel)); return; }
                    if (D.money < c.seedCost) { Say(L.T("noMoney")); return; }
                    D.money -= c.seedCost;
                    p.state = 2; p.crop = c.id; p.progress = 0; p.wet = false; p.waterCount = 0;
                    Say(L.T("planted"));
                    break;
                }
                case 2:
                    if (!p.wet)
                    {
                        p.wet = true; p.waterCount++; AddXp(1);
                        Say(L.T("watered"));
                    }
                    else Say(L.T("growing", Mathf.RoundToInt(p.progress * 100f)));
                    break;
                case 3:
                {
                    var c = Defs.Crop(p.crop);
                    AddInv(c.id, c.yield);
                    AddXp(c.xp);
                    Say(L.T("harvested", c.yield, L.T(c.id)));
                    p.state = 1; p.crop = ""; p.progress = 0; p.wet = false; p.waterCount = 0;
                    break;
                }
            }
            Raise();
        }

        public void TapBuilding(int i)
        {
            var b = D.buildings[i]; var def = Defs.Buildings[i];
            if (!b.owned) return;
            if (b.ready > 0)
            {
                AddInv(def.product, b.ready);
                AddXp(def.xp);
                Say(L.T("collected", b.ready, L.T(def.product)));
                b.ready = 0;
            }
            else if (b.feed <= 0)
            {
                if (D.money < def.feedCost) { Say(L.T("noMoney")); return; }
                D.money -= def.feedCost;
                b.feed = def.feedCycles; b.progress = 0;
                Say(L.T("fed"));
            }
            else Say(L.T("producing"));
            Raise();
        }

        public bool BuyBuilding(int i)
        {
            var b = D.buildings[i]; var def = Defs.Buildings[i];
            if (b.owned) return false;
            if (D.level < def.unlockLevel) { Say(L.T("lvlNeeded", def.unlockLevel)); return false; }
            if (D.money < def.cost) { Say(L.T("noMoney")); return false; }
            D.money -= def.cost;
            b.owned = true; b.feed = def.feedCycles; b.progress = 0; b.ready = 0;
            AddXp(10);
            Say(L.T("bought"));
            Save(); Raise();
            return true;
        }

        public int SellItem(string id)
        {
            int n = Inv(id);
            if (n <= 0) return 0;
            int gold = n * Defs.Price(id);
            inv[id] = 0;
            D.money += gold;
            Raise();
            return gold;
        }

        public void SellAll()
        {
            int total = 0;
            foreach (var id in Defs.AllItems()) total += SellItem(id);
            Say(total > 0 ? L.T("sold", total) : L.T("nothing"));
            Save();
        }

        public void SellOne(string id)
        {
            int g = SellItem(id);
            Say(g > 0 ? L.T("sold", g) : L.T("nothing"));
            Save();
        }

        public void ToggleLang() { L.Next(); D.lang = (int)L.Current; Raise(); }
    }
}
