using UnityEngine;
namespace Emergence.Level01
{
    public sealed class AssemblySocket : MonoBehaviour
    {
        public string acceptedPartId;
        public SpriteRenderer installedVisual;
        public BoxCollider2D hitArea;
        public AssemblyOutline outline;
        public bool Filled { get; private set; }
        public bool Highlighted => outline && outline.Visible;
        void Awake() { if(installedVisual)installedVisual.enabled=false;Highlight(false); }
        public bool Accepts(AssemblyPiece piece) { return !Filled && piece && !piece.Consumed && piece.partId==acceptedPartId; }
        public void Highlight(bool value) { if(outline)outline.SetVisible(value && !Filled);if(hitArea)hitArea.enabled=value && !Filled; }
        public bool TryPlace(AssemblyPiece piece)
        {
            if(!Accepts(piece))return false;
            Filled=true;if(installedVisual)installedVisual.enabled=true;
            piece.Consume();Highlight(false);return true;
        }
    }
}
