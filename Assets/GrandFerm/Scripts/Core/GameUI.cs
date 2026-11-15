using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GrandFerm
{
    /// <summary>Whole UI built from code (uGUI): HUD, seed bar, market and shop panels, toasts.</summary>
    public class GameUI : MonoBehaviour
    {
        public static GameUI Instance;

        static readonly Vector2 Z = Vector2.zero, O = Vector2.one;
        static readonly Color Panel = new Color(0.12f, 0.17f, 0.13f, 0.97f);
        static readonly Color Gold = new Color(1f, 0.85f, 0.25f);
        static readonly Color Green = new Color(0.25f, 0.6f, 0.32f);
        static readonly Color Grey = new Color(0.35f, 0.38f, 0.36f);

        GameState gs; Font font;
        GameObject canvasGo; RectTransform root;
        Text moneyTx, levelTx, toastTx; RectTransform xpFill; CanvasGroup toastGroup; float toastT;
        readonly List<SeedBtn> seeds = new List<SeedBtn>();
        GameObject modal; RectTransform modalContent; Action<RectTransform> modalFill;
        bool resetArmed;

        class SeedBtn { public string id; public Image bg; public GameObject lockGo; }

        public void Build(GameState state)
        {
            Instance = this; gs = state;
            font = Font.CreateDynamicFontFromOSFont("Arial", 32);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            gs.Changed += OnChanged;
            gs.Toast += ShowToast;
            BuildCanvas();
            ShowToast(L.T("welcome"));
        }

        void OnDestroy() { if (gs != null) { gs.Changed -= OnChanged; gs.Toast -= ShowToast; } }

        // ---------------- layout helpers ----------------
        RectTransform R(string n, Transform p, Vector2 amin, Vector2 amax, Vector2 omin, Vector2 omax)
        {
            var go = new GameObject(n, typeof(RectTransform));
            var r = go.GetComponent<RectTransform>();
            r.SetParent(p, false);
            r.anchorMin = amin; r.anchorMax = amax; r.offsetMin = omin; r.offsetMax = omax;
            return r;
        }

        Image Im(RectTransform r, Color c, Sprite s = null)
        {
            var i = r.gameObject.AddComponent<Image>();
            i.color = c; i.sprite = s; i.preserveAspect = s != null;
            return i;
        }

        Text Tx(Transform p, string s, int size, TextAnchor a, Color c, Vector2 amin, Vector2 amax, Vector2 omin, Vector2 omax)
        {
            var r = R("Text", p, amin, amax, omin, omax);
            var t = r.gameObject.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.alignment = a; t.color = c; t.text = s;
            t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        Button Bt(RectTransform r, Color c, UnityAction onClick)
        {
            var i = Im(r, c);
            var b = r.gameObject.AddComponent<Button>();
            b.targetGraphic = i; b.onClick.AddListener(onClick);
            return b;
        }

        Text Center(Transform p, string s, int size, Color c)
        {
            return Tx(p, s, size, TextAnchor.MiddleCenter, c, Z, O, Z, Z);
        }

        // ---------------- canvas ----------------
        void BuildCanvas()
        {
            canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var sc = canvasGo.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1280, 720); sc.matchWidthOrHeight = 0.5f;
            root = canvasGo.GetComponent<RectTransform>();
            seeds.Clear(); modal = null; modalContent = null;

            BuildTopBar();
            BuildBottomBar();

            var toast = R("Toast", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-420, -180), new Vector2(420, -100));
            Im(toast, new Color(0, 0, 0, 0.6f));
            toastGroup = toast.gameObject.AddComponent<CanvasGroup>(); toastGroup.alpha = 0f; toastGroup.blocksRaycasts = false;
            toastTx = Center(toast, "", 30, Color.white);
            OnChanged();
        }

        void BuildTopBar()
        {
            var top = R("Top", root, new Vector2(0, 1), O, new Vector2(0, -80), Z);
            Im(top, new Color(0, 0, 0, 0.55f));
            var coin = R("Coin", top, Z, new Vector2(0, 1), new Vector2(16, 10), new Vector2(76, -10));
            Im(coin, Color.white, Art.Icon("icon_money"));
            moneyTx = Tx(top, "", 36, TextAnchor.MiddleLeft, Gold, Z, new Vector2(0, 1), new Vector2(86, 0), new Vector2(380, 0));

            levelTx = Tx(top, "", 26, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 0.45f), new Vector2(0.5f, 1f), new Vector2(-220, 0), new Vector2(220, -2));
            var bar = R("XpBar", top, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200, 10), new Vector2(200, 26));
            Im(bar, new Color(1, 1, 1, 0.2f));
            xpFill = R("Fill", bar, Z, new Vector2(0, 1), Z, Z);
            Im(xpFill, new Color(0.45f, 0.85f, 0.35f));

            var lang = R("Lang", top, new Vector2(1, 0), O, new Vector2(-170, 12), new Vector2(-16, -12));
            Bt(lang, Green, () => { gs.ToggleLang(); Rebuild(); });
            var li = R("Icon", lang, Z, new Vector2(0, 1), new Vector2(8, 6), new Vector2(54, -6));
            Im(li, Color.white, Art.Icon("icon_language"));
            Tx(lang, L.Code, 30, TextAnchor.MiddleCenter, Color.white, Z, O, new Vector2(40, 0), Z);
        }

        void BuildBottomBar()
        {
            var bottom = R("Bottom", root, Z, new Vector2(1, 0), Z, new Vector2(0, 170));
            Im(bottom, new Color(0, 0, 0, 0.6f));

            for (int i = 0; i < Defs.Crops.Length; i++)
            {
                var c = Defs.Crops[i];
                float x = 16 + i * 150;
                var r = R("Seed_" + c.id, bottom, Z, new Vector2(0, 1), new Vector2(x, 12), new Vector2(x + 140, -12));
                string id = c.id;
                var btn = Bt(r, Grey, () => gs.SelectCrop(id));
                var icon = R("Icon", r, new Vector2(0.15f, 0.42f), new Vector2(0.85f, 0.97f), Z, Z);
                Im(icon, Color.white, Art.Icon("crop_" + c.id));
                Tx(r, L.T(c.id), 20, TextAnchor.MiddleCenter, Color.white, new Vector2(0, 0.2f), new Vector2(1, 0.42f), Z, Z);
                Tx(r, "$" + c.seedCost, 22, TextAnchor.MiddleCenter, Gold, Z, new Vector2(1, 0.22f), Z, Z);
                var lk = R("Lock", r, Z, O, Z, Z);
                Im(lk, new Color(0, 0, 0, 0.7f)).raycastTarget = false;
                Center(lk, L.T("locked", c.unlockLevel), 28, Color.white);
                seeds.Add(new SeedBtn { id = c.id, bg = btn.targetGraphic as Image, lockGo = lk.gameObject });
            }

            var market = R("MarketBtn", bottom, new Vector2(1, 0), O, new Vector2(-340, 12), new Vector2(-180, -12));
            Bt(market, new Color(0.75f, 0.5f, 0.15f), OpenMarket);
            Im(R("Icon", market, new Vector2(0.2f, 0.4f), new Vector2(0.8f, 1f), Z, Z), Color.white, Art.Icon("icon_sell"));
            Tx(market, L.T("market"), 26, TextAnchor.MiddleCenter, Color.white, Z, new Vector2(1, 0.4f), Z, Z);

            var shop = R("ShopBtn", bottom, new Vector2(1, 0), O, new Vector2(-166, 12), new Vector2(-16, -12));
            Bt(shop, new Color(0.25f, 0.45f, 0.75f), OpenShop);
            Im(R("Icon", shop, new Vector2(0.2f, 0.4f), new Vector2(0.8f, 1f), Z, Z), Color.white, Art.Icon("icon_shop"));
            Tx(shop, L.T("shop"), 26, TextAnchor.MiddleCenter, Color.white, Z, new Vector2(1, 0.4f), Z, Z);
        }

        void Rebuild()
        {
            Destroy(canvasGo);
            BuildCanvas();
        }

        // ---------------- live updates ----------------
        void OnChanged()
        {
            if (moneyTx == null) return;
            var d = gs.D;
            moneyTx.text = d.money.ToString();
            int need = Defs.XpForNext(d.level);
            levelTx.text = L.T("level", d.level) + "   " + d.xp + " / " + need;
            xpFill.anchorMax = new Vector2(Mathf.Clamp01((float)d.xp / need), 1f);
            foreach (var s in seeds)
            {
                var def = Defs.Crop(s.id);
                s.lockGo.SetActive(d.level < def.unlockLevel);
                s.bg.color = d.selected == s.id ? new Color(0.85f, 0.7f, 0.2f) : Grey;
            }
            if (modal != null && modalContent != null) RefreshModal();
        }

        void ShowToast(string s)
        {
            if (toastTx == null) return;
            toastTx.text = s; toastT = 2.4f; toastGroup.alpha = 1f;
        }

        void Update()
        {
            if (toastT > 0f)
            {
                toastT -= Time.unscaledDeltaTime;
                if (toastGroup != null) toastGroup.alpha = Mathf.Clamp01(toastT / 0.5f);
            }
        }

        // ---------------- modal panels ----------------
        void OpenModal(string titleKey, Action<RectTransform> fill)
        {
            CloseModal();
            resetArmed = false;
            modal = new GameObject("Modal", typeof(RectTransform));
            var mr = modal.GetComponent<RectTransform>(); mr.SetParent(root, false);
            mr.anchorMin = Z; mr.anchorMax = O; mr.offsetMin = Z; mr.offsetMax = Z;
            var dim = Im(mr, new Color(0, 0, 0, 0.6f));
            var panel = R("Panel", mr, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-450, -330), new Vector2(450, 330));
            Im(panel, Panel);
            Tx(panel, L.T(titleKey), 38, TextAnchor.MiddleLeft, Gold, new Vector2(0, 1), O, new Vector2(24, -76), new Vector2(-100, -8));
            var close = R("Close", panel, O, O, new Vector2(-84, -68), new Vector2(-16, -12));
            Bt(close, new Color(0.7f, 0.25f, 0.2f), CloseModal);
            Center(close, "X", 32, Color.white);
            modalContent = R("Content", panel, Z, O, new Vector2(16, 16), new Vector2(-16, -84));
            modalFill = fill;
            RefreshModal();
        }

        void RefreshModal()
        {
            for (int i = modalContent.childCount - 1; i >= 0; i--) Destroy(modalContent.GetChild(i).gameObject);
            modalFill(modalContent);
        }

        public void CloseModal()
        {
            if (modal != null) Destroy(modal);
            modal = null; modalContent = null; modalFill = null;
        }

        RectTransform Row(RectTransform c, int i, float h)
        {
            var row = R("Row", c, new Vector2(0, 1), O, new Vector2(0, -(i + 1) * (h + 8) + 8), new Vector2(0, -i * (h + 8)));
            Im(row, new Color(1, 1, 1, 0.07f));
            return row;
        }

        public void OpenMarket() { OpenModal("market", FillMarket); }
        public void OpenShop() { OpenModal("shop", FillShop); }

        void FillMarket(RectTransform c)
        {
            var items = Defs.AllItems();
            for (int i = 0; i < items.Length; i++)
            {
                string id = items[i];
                var row = Row(c, i, 52);
                Im(R("Icon", row, Z, new Vector2(0, 1), new Vector2(6, 4), new Vector2(54, -4)), Color.white, Art.Icon(Defs.IconName(id)));
                Tx(row, L.T(id), 26, TextAnchor.MiddleLeft, Color.white, Z, new Vector2(0, 1), new Vector2(66, 0), new Vector2(330, 0));
                int n = gs.Inv(id);
                Tx(row, "x" + n, 26, TextAnchor.MiddleCenter, n > 0 ? Color.white : Grey, Z, new Vector2(0, 1), new Vector2(330, 0), new Vector2(440, 0));
                Tx(row, "$" + Defs.Price(id), 24, TextAnchor.MiddleCenter, Gold, Z, new Vector2(0, 1), new Vector2(450, 0), new Vector2(580, 0));
                var b = R("Sell", row, O, O, new Vector2(-170, -46), new Vector2(-8, -6));
                b.anchorMin = new Vector2(1, 0); b.anchorMax = O; b.offsetMin = new Vector2(-170, 6); b.offsetMax = new Vector2(-8, -6);
                Bt(b, n > 0 ? Green : Grey, () => gs.SellOne(id)).interactable = n > 0;
                Center(b, L.T("sell"), 24, Color.white);
            }
            var all = R("SellAll", c, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-170, 0), new Vector2(170, 62));
            Bt(all, new Color(0.75f, 0.5f, 0.15f), gs.SellAll);
            Center(all, L.T("sellAll"), 30, Color.white);
        }

        void FillShop(RectTransform c)
        {
            for (int i = 0; i < Defs.Buildings.Length; i++)
            {
                var def = Defs.Buildings[i]; int idx = i; var save = gs.D.buildings[i];
                var row = Row(c, i, 100);
                Im(R("Icon", row, Z, new Vector2(0, 1), new Vector2(8, 8), new Vector2(92, -8)), Color.white, Art.Icon("building_" + def.id));
                Tx(row, L.T(def.id), 30, TextAnchor.MiddleLeft, Color.white, new Vector2(0, 0.55f), new Vector2(0, 1), new Vector2(104, 0), new Vector2(560, 0));
                Tx(row, L.T(def.product) + "  ·  " + L.T("each", def.productPrice), 21, TextAnchor.MiddleLeft, new Color(0.8f, 0.9f, 0.8f), new Vector2(0, 0.28f), new Vector2(0, 0.58f), new Vector2(104, 0), new Vector2(560, 0));
                Tx(row, L.T("feedInfo", def.feedCost, def.feedCycles), 20, TextAnchor.MiddleLeft, new Color(0.7f, 0.75f, 0.7f), new Vector2(0, 0f), new Vector2(0, 0.3f), new Vector2(104, 0), new Vector2(560, 0));
                var b = R("Buy", row, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-250, -32), new Vector2(-10, 32));
                string label; Color col; bool can = false;
                if (save.owned) { label = L.T("owned"); col = Grey; }
                else if (gs.D.level < def.unlockLevel) { label = L.T("locked", def.unlockLevel); col = Grey; }
                else { label = L.T("buy") + "  $" + def.cost; col = Green; can = true; }
                Bt(b, col, () => gs.BuyBuilding(idx)).interactable = can;
                Center(b, label, 24, Color.white);
            }
            var rs = R("Reset", c, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(220, 54));
            Bt(rs, new Color(0.6f, 0.2f, 0.18f), () =>
            {
                if (!resetArmed) { resetArmed = true; ShowToast(L.T("newGame") + "? (tap again)"); return; }
                resetArmed = false; gs.NewGame(true); Rebuild();
            });
            Center(rs, L.T("newGame"), 22, Color.white);
        }
    }
}
