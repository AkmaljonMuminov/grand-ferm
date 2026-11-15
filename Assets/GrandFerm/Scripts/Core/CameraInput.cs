using UnityEngine;
using UnityEngine.EventSystems;

namespace GrandFerm
{
    public class CameraRig : MonoBehaviour
    {
        public Vector3 target = Vector3.zero;
        public float dist = 16f, pitch = 55f, yaw = 0f;

        public void Pan(Vector2 px)
        {
            float k = dist * 0.0022f;
            var right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
            var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            target -= (right * px.x + fwd * px.y) * k;
            target.x = Mathf.Clamp(target.x, -16f, 16f);
            target.z = Mathf.Clamp(target.z, -14f, 14f);
        }

        public void Zoom(float d) { dist = Mathf.Clamp(dist - d, 7f, 28f); }

        void LateUpdate()
        {
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = target - transform.forward * dist;
        }
    }

    /// <summary>One-finger drag = pan, two-finger pinch = zoom, short tap = interact. Mouse works in the editor.</summary>
    public class InputController : MonoBehaviour
    {
        Camera cam; CameraRig rig;
        bool active; Vector2 last, start; float startTime, moved;
        bool useTouch;

        void Start()
        {
            cam = Camera.main; rig = World.Rig;
            useTouch = Input.touchSupported && Application.isMobilePlatform;
        }

        static bool OverUI(int fingerId)
        {
            if (EventSystem.current == null) return false;
            return fingerId >= 0 ? EventSystem.current.IsPointerOverGameObject(fingerId) : EventSystem.current.IsPointerOverGameObject();
        }

        void Update()
        {
            if (cam == null) { cam = Camera.main; if (cam == null) return; }
            if (useTouch)
            {
                if (Input.touchCount >= 2)
                {
                    var a = Input.GetTouch(0); var b = Input.GetTouch(1);
                    float prev = ((a.position - a.deltaPosition) - (b.position - b.deltaPosition)).magnitude;
                    float cur = (a.position - b.position).magnitude;
                    rig.Zoom((cur - prev) * 0.03f);
                    active = false;
                }
                else if (Input.touchCount == 1)
                {
                    var t = Input.GetTouch(0);
                    if (t.phase == TouchPhase.Began) Begin(t.position, OverUI(t.fingerId));
                    else if (t.phase == TouchPhase.Moved) Move(t.position);
                    else if (t.phase == TouchPhase.Ended) End(t.position);
                    else if (t.phase == TouchPhase.Canceled) active = false;
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0)) Begin(Input.mousePosition, OverUI(-1));
                else if (Input.GetMouseButton(0)) Move(Input.mousePosition);
                if (Input.GetMouseButtonUp(0)) End(Input.mousePosition);
                float w = Input.mouseScrollDelta.y;
                if (Mathf.Abs(w) > 0.01f && !OverUI(-1)) rig.Zoom(w * 1.5f);
            }
        }

        void Begin(Vector2 p, bool overUI)
        {
            active = !overUI; last = start = p; startTime = Time.unscaledTime; moved = 0f;
        }

        void Move(Vector2 p)
        {
            if (!active) return;
            var d = p - last; moved += d.magnitude; last = p;
            if (moved > 18f) rig.Pan(d);
        }

        void End(Vector2 p)
        {
            if (!active) return;
            active = false;
            if (moved < 18f && Time.unscaledTime - startTime < 0.6f) Tap(p);
        }

        void Tap(Vector2 p)
        {
            var ray = cam.ScreenPointToRay(p);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f))
            {
                var it = hit.collider.GetComponentInParent<Interactable>();
                if (it != null && it.OnTap != null) it.OnTap();
            }
        }
    }
}
