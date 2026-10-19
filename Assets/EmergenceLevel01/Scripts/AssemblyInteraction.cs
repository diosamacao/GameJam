using UnityEngine;
namespace Emergence.Level01
{
    public sealed class AssemblyInteraction : MonoBehaviour
    {
        public Camera worldCamera;
        public AssemblyPiece[] pieces;
        public AssemblyDoor targetDoor;
        public AssemblyPiece Selected { get; private set; }
        [Header("鼠标与提示")]
        [Range(0,6)] public int selectMouseButton=0, cancelMouseButton=1;
        public KeyCode cancelKey=KeyCode.Escape;
        [Min(1)] public int titleFontSize=18, hintFontSize=15;
        public Color titleColor=new Color(.82f,.91f,1), hintColor=new Color(.58f,.7f,.8f);
        public float sideMargin=10, titleTop=18, titleHeight=34, hintBottom=42, hintHeight=30;
        Font font;
        void Awake(){if(!worldCamera)worldCamera=Camera.main;}
        void Update()
        {
            if(GameMenuUI.BlocksPointer || DialogueDirector.IsBlockingInput)return;
            if(Input.GetMouseButtonDown(cancelMouseButton)||Input.GetKeyDown(cancelKey))CancelSelection();
            if(Input.GetMouseButtonDown(selectMouseButton)&&worldCamera)
            { var p=Input.mousePosition;p.z=-worldCamera.transform.position.z;ClickWorld(worldCamera.ScreenToWorldPoint(p)); }
        }
        public void ClickWorld(Vector2 point)
        {
            if(GameMenuUI.BlocksPointer || DialogueDirector.IsBlockingInput)return;
            if(!targetDoor){CancelSelection();return;}
            if(pieces!=null)
            foreach(var piece in pieces)
                if(piece && !LocalTimeTarget.IsStopped(piece) && !piece.Consumed && piece.gameObject.activeInHierarchy && piece.hitArea && piece.hitArea.OverlapPoint(point))
                { Select(Selected==piece?null:piece);return; }
            if(Selected && targetDoor.sockets!=null)
                foreach(var slot in targetDoor.sockets)
                    if(slot && slot.Highlighted && slot.hitArea && slot.hitArea.OverlapPoint(point))
                    { if(slot.TryPlace(Selected)){Selected=null;RefreshHighlights();targetDoor.RefreshState();}return; }
            if(targetDoor.interactionArea && targetDoor.interactionArea.OverlapPoint(point))
            { if(targetDoor.TryUse())CancelSelection();return; }
            CancelSelection();
        }
        public void Select(AssemblyPiece piece)
        { if(Selected)Selected.Select(false);Selected=piece && !piece.Consumed?piece:null;if(Selected)Selected.Select(true);RefreshHighlights(); }
        public void CancelSelection(){Select(null);}
        void RefreshHighlights(){if(!targetDoor || targetDoor.sockets==null)return;foreach(var slot in targetDoor.sockets)if(slot)slot.Highlight(slot.Accepts(Selected));}
        void OnDisable(){CancelSelection();}
        void OnGUI()
        {
            if(GameMenuUI.BlocksGameplay)return;
            if(!targetDoor || targetDoor.sockets==null || (DialogueDirector.Instance && DialogueDirector.Instance.IsPlaying))return;
            if(!font)font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},titleFontSize);
            var style=new GUIStyle(GUI.skin.label){font=font,fontSize=titleFontSize,alignment=TextAnchor.MiddleCenter};style.normal.textColor=titleColor;
            string text=targetDoor.Complete?"门已修复  "+targetDoor.FilledCount+" / "+targetDoor.sockets.Length+"  ·  点击门开关":"修复出口门  "+targetDoor.FilledCount+" / "+targetDoor.sockets.Length+"  ·  "+(Selected?"点击门上的虚线框放入选中门板":"点击场景中的门板进行选择");
            GUI.Label(new Rect(sideMargin,titleTop,Screen.width-sideMargin*2,titleHeight),text,style);
            style.fontSize=hintFontSize;style.normal.textColor=hintColor;
            GUI.Label(new Rect(sideMargin,Screen.height-hintBottom,Screen.width-sideMargin*2,hintHeight),"A / D 移动    ·    鼠标左键选择 / 放入    ·    右键或 Esc 取消",style);
        }
        void OnDestroy(){if(font)Destroy(font);}
    }
}
