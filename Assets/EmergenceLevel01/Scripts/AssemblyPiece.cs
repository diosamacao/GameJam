using UnityEngine;
namespace Emergence.Level01
{
    public sealed class AssemblyPiece : MonoBehaviour
    {
        public string partId;
        public SpriteRenderer visual;
        public BoxCollider2D hitArea;
        public AssemblyOutline outline;
        public bool Consumed { get; private set; }
        public void Select(bool selected)
        { if(outline)outline.SetVisible(selected && !Consumed);if(visual)visual.color=selected?new Color(1,.8f,.38f):Color.white; }
        public void Consume() { Consumed=true;Select(false);gameObject.SetActive(false); }
    }
}
