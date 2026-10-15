using UnityEngine;
namespace Emergence.Level01
{
    public sealed class LabChamber : MonoBehaviour
    {
        public SpriteRenderer glass;
        public bool open;
        [Range(0,1)] public float openAlpha=0;
        [Range(0,1)] public float closedAlpha = .22f;
        public float seconds = .4f;
        public void SetOpen(bool value) { open = value; }
        void Update()
        {
            if (!glass) return;
            var c = glass.color;
            c.a = Mathf.MoveTowards(c.a, open ? openAlpha : closedAlpha, Time.deltaTime * Mathf.Abs(closedAlpha-openAlpha) / Mathf.Max(.01f, seconds));
            glass.color = c;
        }
    }
}
