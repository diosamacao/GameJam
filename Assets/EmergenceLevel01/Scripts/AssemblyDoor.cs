using UnityEngine;
namespace Emergence.Level01
{
    [RequireComponent(typeof(LabDoor))]
    public sealed class AssemblyDoor : MonoBehaviour
    {
        public LabDoor door;
        public AssemblySocket[] sockets;
        public BoxCollider2D interactionArea;
        public SpriteRenderer indicator;
        public int FilledCount { get { int n=0;foreach(var socket in sockets)if(socket && socket.Filled)n++;return n; } }
        public bool Complete => sockets!=null && sockets.Length>0 && FilledCount==sockets.Length;
        void Start() { door.SetOpen(false);RefreshState(); }
        public void RefreshState()
        { if(!Complete)door.SetOpen(false);if(indicator)indicator.color=Complete?new Color(.4f,1,.65f):new Color(1,.55f,.2f); }
        public bool TryUse() { if(!Complete)return false;door.SetOpen(!door.open);return true; }
    }
}
