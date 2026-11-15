using System.Collections.Generic;
using UnityEngine;

namespace GrandFerm
{
    /// <summary>Runtime loaders/caches for everything under Resources/.</summary>
    public static class Art
    {
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static Material baseMat;

        static Material Base()
        {
            if (baseMat != null) return baseMat;
            baseMat = Resources.Load<Material>("Materials/Base");
            if (baseMat == null)
            {
                var sh = Shader.Find("Standard") ?? Shader.Find("Mobile/Diffuse") ?? Shader.Find("Diffuse");
                baseMat = new Material(sh);
            }
            return baseMat;
        }

        public static Mesh Mesh(string name)
        {
            Mesh m;
            if (meshes.TryGetValue(name, out m)) return m;
            m = Resources.Load<Mesh>("Models/" + name);
            if (m == null)
            {
                var go = Resources.Load<GameObject>("Models/" + name);
                if (go != null) { var mf = go.GetComponentInChildren<MeshFilter>(); if (mf != null) m = mf.sharedMesh; }
            }
            if (m == null) Debug.LogWarning("Missing mesh: " + name);
            meshes[name] = m;
            return m;
        }

        public static Material Mat(string texture, Color tint, float tiling = 1f)
        {
            string key = texture + "|" + ColorUtility.ToHtmlStringRGB(tint) + "|" + tiling;
            Material m;
            if (mats.TryGetValue(key, out m)) return m;
            m = new Material(Base());
            var tex = string.IsNullOrEmpty(texture) ? null : Resources.Load<Texture2D>("Textures/" + texture);
            if (tex != null) { m.mainTexture = tex; m.mainTextureScale = new Vector2(tiling, tiling); }
            m.color = tint;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.08f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            mats[key] = m;
            return m;
        }

        public static Material Mat(string texture) { return Mat(texture, Color.white, 1f); }

        public static Sprite Icon(string name)
        {
            Sprite s;
            if (sprites.TryGetValue(name, out s)) return s;
            s = Resources.Load<Sprite>("Icons/" + name);
            if (s == null)
            {
                var t = Resources.Load<Texture2D>("Icons/" + name);
                if (t != null) s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
            }
            sprites[name] = s;
            return s;
        }
    }
}
