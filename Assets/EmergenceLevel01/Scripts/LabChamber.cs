using UnityEngine;
namespace Emergence.Level01
{
    public sealed class LabChamber : MonoBehaviour
    {
        public SpriteRenderer glass;
        public bool open;
        public float closedAlpha = .22f;
        public float seconds = .4f;
        public void SetOpen(bool value) { open = value; }
        void Update()
        {
            if (!glass) return;
            var c = glass.color;
            c.a = Mathf.MoveTowards(c.a, open ? 0f : closedAlpha, Time.deltaTime * closedAlpha / Mathf.Max(.01f, seconds));
            glass.color = c;
        }
    }
}
