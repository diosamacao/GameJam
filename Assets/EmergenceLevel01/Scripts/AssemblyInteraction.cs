using UnityEngine;
namespace Emergence.Level01
{
    public sealed class AssemblyInteraction : MonoBehaviour
    {
        public Camera worldCamera;
        public AssemblyPiece[] pieces;
        public AssemblyDoor targetDoor;
        public AssemblyPiece Selected { get; private set; }
        Font font;
        void Awake(){if(!worldCamera)worldCamera=Camera.main;}
        void Update()
        {
            if(DialogueDirector.IsBlockingInput)return;
            if(Input.GetMouseButtonDown(1)||Input.GetKeyDown(KeyCode.Escape))CancelSelection();
            if(Input.GetMouseButtonDown(0)&&worldCamera)
            { var p=Input.mousePosition;p.z=-worldCamera.transform.position.z;ClickWorld(worldCamera.ScreenToWorldPoint(p)); }
        }
        public void ClickWorld(Vector2 point)
        {
            if(DialogueDirector.IsBlockingInput)return;
            foreach(var piece in pieces)
                if(piece && !piece.Consumed && piece.gameObject.activeInHierarchy && piece.hitArea.OverlapPoint(point))
                { Select(Selected==piece?null:piece);return; }
            if(Selected)
                foreach(var slot in targetDoor.sockets)
                    if(slot.Highlighted && slot.hitArea.OverlapPoint(point))
                    { if(slot.TryPlace(Selected)){Selected=null;RefreshHighlights();targetDoor.RefreshState();}return; }
            if(targetDoor.interactionArea.OverlapPoint(point))
            { if(targetDoor.TryUse())CancelSelection();return; }
            CancelSelection();
        }
        public void Select(AssemblyPiece piece)
        { if(Selected)Selected.Select(false);Selected=piece && !piece.Consumed?piece:null;if(Selected)Selected.Select(true);RefreshHighlights(); }
        public void CancelSelection(){Select(null);}
        void RefreshHighlights(){foreach(var slot in targetDoor.sockets)slot.Highlight(slot.Accepts(Selected));}
        void OnDisable(){if(targetDoor)CancelSelection();}
        void OnGUI()
        {
            if(!targetDoor || (DialogueDirector.Instance && DialogueDirector.Instance.IsPlaying))return;
            if(!font)font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},18);
            var style=new GUIStyle(GUI.skin.label){font=font,fontSize=18,alignment=TextAnchor.MiddleCenter};style.normal.textColor=new Color(.82f,.91f,1);
            string text=targetDoor.Complete?"门已修复  2 / 2  ·  点击门开关":"修复出口门  "+targetDoor.FilledCount+" / 2  ·  "+(Selected?"点击门上的虚线框放入选中门板":"点击场景中的门板进行选择");
            GUI.Label(new Rect(10,18,Screen.width-20,34),text,style);
            style.fontSize=15;style.normal.textColor=new Color(.58f,.7f,.8f);
            GUI.Label(new Rect(10,Screen.height-42,Screen.width-20,30),"A / D 移动    ·    鼠标左键选择 / 放入    ·    右键或 Esc 取消",style);
        }
        void OnDestroy(){if(font)Destroy(font);}
    }
}
