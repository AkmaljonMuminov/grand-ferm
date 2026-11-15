using UnityEngine;

namespace GrandFerm
{
    /// <summary>Builds the whole 3D farm from code, so no scene authoring is required.</summary>
    public static class World
    {
        public static Transform Root;
        public static CameraRig Rig;

        public static void Build()
        {
            Root = new GameObject("World").transform;
            BuildCameraAndLight();
            BuildGround();
            BuildPlots();
            BuildAnimalBuildings();
            BuildDecor();
        }

        static void BuildCameraAndLight()
        {
            foreach (var c in Object.FindObjectsOfType<Camera>()) Object.Destroy(c.gameObject);
            foreach (var l in Object.FindObjectsOfType<Light>()) Object.Destroy(l.gameObject);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.78f, 0.95f);
            cam.fieldOfView = 45f; cam.nearClipPlane = 0.3f; cam.farClipPlane = 120f;
            camGo.AddComponent<AudioListener>();
            Rig = camGo.AddComponent<CameraRig>();

            var lgo = new GameObject("Sun");
            var light = lgo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.86f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Hard;
            lgo.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            QualitySettings.shadowDistance = 45f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.72f);
        }

        static void BuildGround()
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Plane);
            g.name = "Ground"; g.transform.SetParent(Root);
            g.transform.position = new Vector3(0, -0.06f, 0);
            g.transform.localScale = new Vector3(10f, 1f, 10f); // 100 x 100 m
            g.GetComponent<MeshRenderer>().sharedMaterial = Art.Mat("grass", Color.white, 40f);
            Object.Destroy(g.GetComponent<Collider>());
        }

        static void BuildPlots()
        {
            const float spacing = 1.6f;
            var parent = new GameObject("Field").transform; parent.SetParent(Root);
            for (int z = 0; z < GameState.GridSize; z++)
                for (int x = 0; x < GameState.GridSize; x++)
                {
                    int idx = z * GameState.GridSize + x;
                    var root = new GameObject("Plot" + idx);
                    root.transform.SetParent(parent);
                    root.transform.position = new Vector3((x - 2.5f) * spacing, 0f, (z - 2.5f) * spacing);

                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = "Soil"; cube.transform.SetParent(root.transform, false);
                    cube.transform.localScale = new Vector3(1.4f, 0.12f, 1.4f);

                    var view = root.AddComponent<PlotView>();
                    view.index = idx; view.soil = cube.GetComponent<Renderer>();
                    view.crops = new Transform[4];
                    for (int k = 0; k < 4; k++)
                    {
                        var c = new GameObject("Crop" + k);
                        c.transform.SetParent(root.transform, false);
                        c.transform.localPosition = new Vector3(k % 2 == 0 ? -0.35f : 0.35f, 0.06f, k < 2 ? -0.35f : 0.35f);
                        c.transform.localRotation = Quaternion.Euler(0, k * 70f, 0);
                        c.AddComponent<MeshFilter>(); c.AddComponent<MeshRenderer>();
                        c.SetActive(false);
                        view.crops[k] = c.transform;
                    }
                    var it = cube.AddComponent<Interactable>();
                    int captured = idx;
                    it.OnTap = () => GameState.I.TapPlot(captured);
                }
        }

        static void BuildAnimalBuildings()
        {
            for (int i = 0; i < Defs.Buildings.Length; i++)
            {
                var def = Defs.Buildings[i];
                var slot = new GameObject("Slot_" + def.id); slot.transform.SetParent(Root);
                slot.transform.position = def.pos;
                var vis = new GameObject("Visual"); vis.transform.SetParent(slot.transform, false);
                Model(vis.transform, def.model, def.texture, Vector3.zero, 0f, 1f, true);
                // collider for tapping on the visual root
                var mesh = Art.Mesh(def.model);
                if (mesh != null)
                {
                    var bc = vis.AddComponent<BoxCollider>();
                    bc.center = mesh.bounds.center; bc.size = mesh.bounds.size;
                }
                var it = vis.AddComponent<Interactable>();
                int captured = i;
                it.OnTap = () => GameState.I.TapBuilding(captured);

                var ind = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ind.name = "Indicator"; ind.transform.SetParent(vis.transform, false);
                ind.transform.localScale = Vector3.one * 0.7f;
                Object.Destroy(ind.GetComponent<Collider>());

                for (int a = 0; a < def.animals; a++)
                {
                    var an = new GameObject("Animal" + a); an.transform.SetParent(vis.transform, true);
                    var spawn = def.pos + new Vector3(2.6f + a * 0.6f, 0f, (a - 0.5f) * 1.2f);
                    an.transform.position = spawn;
                    Model(an.transform, def.animalModel, def.animalTexture, Vector3.zero, 0f, def.animalScale, true);
                    var w = an.AddComponent<AnimalWander>();
                    w.center = def.pos + new Vector3(3.2f, 0f, 0f);
                }
                var bv = slot.AddComponent<BuildingView>();
                bv.index = i; bv.visual = vis; bv.indicator = ind.GetComponent<Renderer>();
            }
        }

        static void BuildDecor()
        {
            Prop("farmhouse", "brick",      new Vector3(0, 0, -10f), 0f);
            Prop("barn",      "red_paint",  new Vector3(10.5f, 0, 3.5f), 0f);
            Prop("silo",      "metal",      new Vector3(13.8f, 0, 0.5f), 0f);
            Prop("warehouse", "concrete",   new Vector3(10.5f, 0, -8f), 0f);
            Prop("tractor",   "paint_green",new Vector3(6.5f, 0, 8f), 35f);
            Prop("truck",     "blue_paint", new Vector3(10f, 0, -2.5f), 90f);
            Prop("trailer",   "metal",      new Vector3(2f, 0, 9.5f), 10f);
            Prop("hay_bale",  "hay",        new Vector3(3.5f, 0, 8f), 0f);
            Prop("hay_bale",  "hay",        new Vector3(-2f, 0, 9f), 25f);
            Prop("irrigation_pump", "blue_paint", new Vector3(5.9f, 0, -2f), 0f);
            Prop("water_trough", "stone",   new Vector3(-6.2f, 0, 3f), 0f);
            Prop("farm_gate", "wood",       new Vector3(0, 0, 5.6f), 0f);
            for (int i = 0; i < 4; i++)
            {
                Prop("fence", "wood", new Vector3(-6.6f + i * 2.2f, 0, 5.6f), 0f);
                Prop("fence", "wood", new Vector3(1.1f + i * 2.2f, 0, 5.6f), 0f);
            }
            float[,] trees = { {-16,-10},{-16,9},{16,-12},{17,9},{-4,14},{4,15},{-12,14},{13,13},{-17,0},{18,-3},{0,-17},{-9,-15},{9,-16} };
            for (int i = 0; i < trees.GetLength(0); i++)
                Prop("tree", "grass", new Vector3(trees[i, 0], 0, trees[i, 1]), i * 47f);

            // Market stall — tapping it opens the market
            var m = Prop("market_stall", "wood", new Vector3(6.5f, 0, -7f), 0f);
            m.transform.localScale = Vector3.one * 1.6f;
            var mesh = Art.Mesh("market_stall");
            if (mesh != null)
            {
                var bc = m.AddComponent<BoxCollider>(); bc.center = mesh.bounds.center; bc.size = mesh.bounds.size;
            }
            m.AddComponent<Interactable>().OnTap = () => { if (GameUI.Instance != null) GameUI.Instance.OpenMarket(); };
        }

        static GameObject Prop(string model, string tex, Vector3 pos, float yaw)
        {
            var go = new GameObject(model);
            go.transform.SetParent(Root);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = Art.Mesh(model);
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = Art.Mat(tex);
            return go;
        }

        /// <summary>Attach a model to an existing transform (adds mesh components on the same object).</summary>
        static void Model(Transform t, string model, string tex, Vector3 localPos, float yaw, float scale, bool shadows)
        {
            var go = new GameObject("Mesh");
            go.transform.SetParent(t, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = Art.Mesh(model);
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = Art.Mat(tex);
            mr.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
