using UnityEngine;
namespace Emergence.Level01
{
    public sealed class AssemblyPiece : MonoBehaviour
    {
        public Color selectedColor=new Color(1,.8f,.38f), normalColor=Color.white;
        public string partId;
        public SpriteRenderer visual;
        public BoxCollider2D hitArea;
        public AssemblyOutline outline;
        public bool Consumed { get; private set; }
        public void Select(bool selected)
        { if(outline)outline.SetVisible(selected && !Consumed);if(visual)visual.color=selected?selectedColor:normalColor; }
        public void Consume() { Consumed=true;Select(false);gameObject.SetActive(false); }
    }
}
