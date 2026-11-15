using System;
using UnityEngine;

namespace GrandFerm
{
    public class Interactable : MonoBehaviour
    {
        public Action OnTap;
    }

    /// <summary>One field tile: soil + up to 4 crop meshes that grow with progress.</summary>
    public class PlotView : MonoBehaviour
    {
        public int index;
        public Renderer soil;
        public Transform[] crops;
        int lastState = -1; string lastCrop = ""; float lastProg = -1f; bool lastWet; float pop;
        float bob;

        static readonly Color Dry = new Color(1f, 0.95f, 0.8f);
        static readonly Color Wet = new Color(0.62f, 0.66f, 0.78f);

        void Update()
        {
            var p = GameState.I.D.plots[index];
            if (p.state != lastState || p.wet != lastWet)
            {
                if (lastState != -1 && p.state != lastState) pop = 0.3f;
                soil.sharedMaterial = p.state == 0 ? Art.Mat("soil", Color.white, 1f)
                                    : Art.Mat("plowed_soil", p.wet ? Wet : Dry, 1f);
                lastState = p.state; lastWet = p.wet;
            }
            if (p.crop != lastCrop)
            {
                lastCrop = p.crop;
                var def = Defs.Crop(p.crop);
                foreach (var c in crops)
                {
                    if (def == null) { c.gameObject.SetActive(false); continue; }
                    c.GetComponent<MeshFilter>().sharedMesh = Art.Mesh(def.model);
                    c.GetComponent<MeshRenderer>().sharedMaterial = Art.Mat(def.texture);
                    c.gameObject.SetActive(true);
                }
            }
            bool show = p.state == 2 || p.state == 3;
            if (show && !Mathf.Approximately(p.progress, lastProg))
            {
                lastProg = p.progress;
            }
            float s = show ? Mathf.Lerp(0.18f, 0.75f, p.state == 3 ? 1f : Mathf.Clamp01(p.progress)) : 0f;
            if (p.state == 3) { bob += Time.deltaTime * 3f; s *= 1f + Mathf.Sin(bob) * 0.04f; }
            foreach (var c in crops) c.localScale = Vector3.one * s;

            if (pop > 0f)
            {
                pop -= Time.deltaTime;
                float k = 1f + Mathf.Sin(Mathf.Clamp01(pop / 0.3f) * Mathf.PI) * 0.12f;
                soil.transform.localScale = new Vector3(1.4f * k, 0.12f, 1.4f * k);
            }
            else soil.transform.localScale = new Vector3(1.4f, 0.12f, 1.4f);
        }
    }

    /// <summary>Animal building slot: shows/hides the building, indicator and animals.</summary>
    public class BuildingView : MonoBehaviour
    {
        public int index;
        public GameObject visual;
        public Renderer indicator;
        static readonly Color Gold = new Color(1f, 0.82f, 0.1f), Red = new Color(0.9f, 0.2f, 0.15f);
        static Material matGold, matRed;

        void Update()
        {
            var b = GameState.I.D.buildings[index];
            if (visual.activeSelf != b.owned) visual.SetActive(b.owned);
            if (!b.owned) return;
            bool ready = b.ready > 0, hungry = b.feed <= 0;
            indicator.gameObject.SetActive(ready || hungry);
            if (ready || hungry)
            {
                if (matGold == null) { matGold = Art.Mat("", Gold); matRed = Art.Mat("", Red); }
                indicator.sharedMaterial = ready ? matGold : matRed;
                var t = indicator.transform;
                t.localPosition = new Vector3(0f, 3.3f + Mathf.Sin(Time.time * 4f) * 0.12f, 0f);
            }
        }
    }

    /// <summary>Random wandering inside a small circle.</summary>
    public class AnimalWander : MonoBehaviour
    {
        public Vector3 center; public float radius = 1.3f, speed = 0.6f;
        public float modelYaw = 0f; // adjust if a model walks sideways/backwards
        Vector3 target; float wait;

        void Start() { target = transform.position; wait = UnityEngine.Random.Range(0f, 2f); }

        void Update()
        {
            if (wait > 0f) { wait -= Time.deltaTime; return; }
            Vector3 to = target - transform.position; to.y = 0;
            if (to.magnitude < 0.08f)
            {
                var r = UnityEngine.Random.insideUnitCircle * radius;
                target = center + new Vector3(r.x, 0f, r.y);
                wait = UnityEngine.Random.Range(1f, 4f);
                return;
            }
            var dir = to.normalized;
            transform.position += dir * speed * Time.deltaTime;
            var look = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, modelYaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 6f * Time.deltaTime);
        }
    }
}
