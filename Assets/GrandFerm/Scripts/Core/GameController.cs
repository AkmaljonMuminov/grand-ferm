using UnityEngine;
using UnityEngine.EventSystems;

namespace GrandFerm
{
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (GameObject.Find("GrandFermRoot") != null) return;
            new GameObject("GrandFermRoot").AddComponent<GameController>();
        }
    }

    public class GameController : MonoBehaviour
    {
        GameState gs;
        float saveTimer;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            gs = new GameState();
            gs.Load();
            World.Build();
            gameObject.AddComponent<InputController>();
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            var ui = gameObject.AddComponent<GameUI>();
            ui.Build(gs);
        }

        void Update()
        {
            gs.Tick(Time.deltaTime);
            saveTimer += Time.unscaledDeltaTime;
            if (saveTimer > 15f) { saveTimer = 0f; gs.Save(); }
        }

        void OnApplicationPause(bool paused) { if (paused && gs != null) gs.Save(); }
        void OnApplicationQuit() { if (gs != null) gs.Save(); }
    }
}
