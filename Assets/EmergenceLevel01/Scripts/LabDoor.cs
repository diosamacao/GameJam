using UnityEngine;
namespace Emergence.Level01
{
    public sealed class LabDoor : MonoBehaviour
    {
        public Transform upper, lower;
        public BoxCollider2D blocker;
        public float travel = 26f / 16f;
        public float seconds = .6f;
        public bool open;
        Vector3 upperClosed, lowerClosed;
        float progress;
        void Awake() { upperClosed = upper.localPosition; lowerClosed = lower.localPosition; }
        public void SetOpen(bool value) { open = value; }
        [ContextMenu("Open (Play mode)")] void Open() { open = true; }
        [ContextMenu("Close (Play mode)")] void Close() { open = false; }
        void Update()
        {
            progress = Mathf.MoveTowards(progress, open ? 1f : 0f, Time.deltaTime / Mathf.Max(.01f, seconds));
            upper.localPosition = upperClosed + Vector3.up * travel * progress;
            lower.localPosition = lowerClosed + Vector3.down * travel * progress;
            if (blocker) blocker.enabled = progress < .98f;
        }
    }
}
