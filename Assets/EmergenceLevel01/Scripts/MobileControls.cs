using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Emergence.Level01
{
    public enum TouchAction { Left, Right, Jump, Transfer }

    [DefaultExecutionOrder(-900)]
    public sealed class MobileControls : MonoBehaviour
    {
        public static MobileControls Instance {get;private set;}
        public static bool Active=>Instance && (Application.isMobilePlatform || Instance.config.mobilePreview);
        public static bool Held(TouchAction action)=>Active && Instance.held[(int)action]>0;
        public static bool TakeDown(TouchAction action)=>Active && Instance.down.Remove(action);
        public static float MoveAxis=>(Held(TouchAction.Right)?1:0)-(Held(TouchAction.Left)?1:0);
        readonly int[] held=new int[4];
        readonly HashSet<TouchAction> down=new HashSet<TouchAction>();
        readonly List<Button> emotionButtons=new List<Button>();
        GameMenuConfig config;
        RectTransform safeRoot;
        GameObject gameplay,dialogue;
        RobotEmotionGameplay player;
        Text status;
        public Canvas Canvas {get;private set;}
        static readonly RobotEmotion[] emotions={RobotEmotion.Joy,RobotEmotion.Anger,RobotEmotion.Sadness,RobotEmotion.Courage,RobotEmotion.Guardian,RobotEmotion.Memory};
        public void SetHeld(TouchAction action,bool value){
            int i=(int)action;if(value){if(held[i]==0)down.Add(action);held[i]++;}else held[i]=Mathf.Max(0,held[i]-1);
        }
        public void Clear(){System.Array.Clear(held,0,held.Length);down.Clear();}
        void Awake(){
            Instance=this;config=GameMenuUI.Instance.config;
            if(Application.isMobilePlatform){Screen.autorotateToPortrait=Screen.autorotateToPortraitUpsideDown=false;Screen.autorotateToLandscapeLeft=Screen.autorotateToLandscapeRight=true;Screen.orientation=ScreenOrientation.AutoRotation;Application.targetFrameRate=config.mobileTargetFrameRate;Input.simulateMouseWithTouches=false;}
            var canvas=gameObject.AddComponent<Canvas>();Canvas=canvas;canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20000;
            var scale=gameObject.AddComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1280,720);scale.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            gameObject.AddComponent<GraphicRaycaster>();
            safeRoot=new GameObject("Safe area",typeof(RectTransform)).GetComponent<RectTransform>();safeRoot.SetParent(transform,false);
            gameplay=new GameObject("Touch gameplay",typeof(RectTransform));gameplay.transform.SetParent(safeRoot,false);Stretch((RectTransform)gameplay.transform);
            var left=Button(gameplay.transform,"◀",new Vector2(0,0),new Vector2(75,110),new Vector2(100,100),null);Hold(left,TouchAction.Left);
            var right=Button(gameplay.transform,"▶",new Vector2(0,0),new Vector2(195,110),new Vector2(100,100),null);Hold(right,TouchAction.Right);
            var jump=Button(gameplay.transform,"跳跃",new Vector2(1,0),new Vector2(-85,110),new Vector2(120,120),null);Hold(jump,TouchAction.Jump);
            Button(gameplay.transform,"交互",new Vector2(1,0),new Vector2(-225,100),new Vector2(100,90),()=>{if(player){var t=player.GetComponent<EmotionTransfer>();if(t){t.CancelTransfer();t.TryInteract();}}});
            var transfer=Button(gameplay.transform,"传递\n长按",new Vector2(1,0),new Vector2(-95,250),new Vector2(110,90),null);Hold(transfer,TouchAction.Transfer);
            Button(gameplay.transform,"取消选择",new Vector2(.5f,0),new Vector2(0,135),new Vector2(140,48),()=>{foreach(var item in FindObjectsOfType<AssemblyInteraction>())item.CancelSelection();});
            string[] labels={"开心","愤怒","悲伤","勇气","守护","怀念"};
            for(int i=0;i<6;i++){int index=i;emotionButtons.Add(Button(gameplay.transform,labels[i],new Vector2(.5f,0),new Vector2((i-2.5f)*88,45),new Vector2(80,64),()=>{if(player)player.BeginSwitch(emotions[index]);}));}
            status=new GameObject("Emotion status",typeof(RectTransform),typeof(Text)).GetComponent<Text>();status.transform.SetParent(gameplay.transform,false);
            status.rectTransform.anchorMin=status.rectTransform.anchorMax=new Vector2(.5f,0);status.rectTransform.anchoredPosition=new Vector2(0,195);status.rectTransform.sizeDelta=new Vector2(480,40);status.font=GameMenuUI.Instance.DisplayFont;status.fontSize=22;status.alignment=TextAnchor.MiddleCenter;status.color=config.text;status.raycastTarget=false;
            dialogue=new GameObject("Touch dialogue",typeof(RectTransform));dialogue.transform.SetParent(safeRoot,false);Stretch((RectTransform)dialogue.transform);
            Button(dialogue.transform,"继续对话",new Vector2(1,0),new Vector2(-110,90),new Vector2(180,70),()=>{if(DialogueDirector.Instance)DialogueDirector.Instance.Advance();});
            foreach(var rect in GetComponentsInChildren<RectTransform>(true))rect.gameObject.layer=5;
            SceneManager.activeSceneChanged+=SceneChanged;
        }
        void Hold(Button button,TouchAction action){var hold=button.gameObject.AddComponent<TouchHoldButton>();hold.owner=this;hold.action=action;}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        Button Button(Transform parent,string label,Vector2 anchor,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction click){
            var go=new GameObject(label,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=anchor;rect.sizeDelta=size*config.mobileButtonScale;rect.anchoredPosition=position;
            go.GetComponent<Image>().color=new Color(.06f,.14f,.2f,config.mobileButtonOpacity);
            var button=go.GetComponent<Button>();if(click!=null)button.onClick.AddListener(click);
            var text=new GameObject("Label",typeof(RectTransform),typeof(Text)).GetComponent<Text>();text.transform.SetParent(go.transform,false);Stretch(text.rectTransform);
            text.font=GameMenuUI.Instance.DisplayFont;text.fontSize=23;text.alignment=TextAnchor.MiddleCenter;text.color=config.text;text.text=label;text.raycastTarget=false;
            go.layer=text.gameObject.layer=5;
            return button;
        }
        void Update(){
            config=GameMenuUI.Instance.config;
            var area=Screen.safeArea;safeRoot.anchorMin=new Vector2(area.xMin/Screen.width,area.yMin/Screen.height);safeRoot.anchorMax=new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);safeRoot.offsetMin=safeRoot.offsetMax=Vector2.zero;
            player=null;foreach(var actor in EmotionResonance.Actors)if(actor && !actor.Actor.isNpc && actor.gameObject.scene==SceneManager.GetActiveScene()){player=actor.Actor;break;}
            bool visible=Active && !GameMenuUI.BlocksGameplay && !LevelFlowController.IsFading;
            bool talking=DialogueDirector.Instance && DialogueDirector.Instance.IsPlaying;
            gameplay.SetActive(visible && player && !DialogueDirector.IsBlockingInput && !LevelFlowController.IsTransitioning);
            dialogue.SetActive(visible && talking && DialogueDirector.Instance.ShowAdvanceHint);
            if(!gameplay.activeSelf)Clear();
            for(int i=0;i<emotionButtons.Count;i++)emotionButtons[i].interactable=player && player.CanUseEmotionAbilities && player.IsEmotionUnlocked(emotions[i]);
            if(player){var transfer=player.GetComponent<EmotionTransfer>();status.text=player.IsSwitching?"切换中 "+Mathf.RoundToInt(player.Progress*100)+"%":transfer && transfer.Progress>0?"传递中 "+Mathf.RoundToInt(transfer.Progress*100)+"%":"当前："+RobotEmotionGameplay.EmotionName(player.CurrentEmotion)+(player.GetComponent<EmotionResonance>().Enhanced?"（增强）":"");}
        }
        void SceneChanged(Scene oldScene,Scene newScene){Clear();}
        void OnApplicationFocus(bool focused){if(!focused)Clear();}
        void OnApplicationPause(bool paused){if(paused){Clear();if(GameMenuUI.Instance)GameMenuUI.Instance.Pause();}}
        void OnDisable(){Clear();}
        void OnDestroy(){SceneManager.activeSceneChanged-=SceneChanged;if(Instance==this)Instance=null;}
        public static bool PointerOverUI(int pointerId){
            return EventSystem.current && EventSystem.current.IsPointerOverGameObject(pointerId);
        }
        public static bool PointerOverUI(Vector2 position){
            if(!EventSystem.current)return false;
            var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=position},hits);
            return hits.Count>0;
        }
    }

    public sealed class TouchHoldButton : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
    {
        public MobileControls owner;public TouchAction action;
        readonly HashSet<int> fingers=new HashSet<int>();
        public void OnPointerDown(PointerEventData e){if(fingers.Add(e.pointerId))owner.SetHeld(action,true);}
        public void OnPointerUp(PointerEventData e){if(fingers.Remove(e.pointerId))owner.SetHeld(action,false);}
        public void OnPointerExit(PointerEventData e){OnPointerUp(e);}
        void OnDisable(){foreach(int finger in fingers)if(owner)owner.SetHeld(action,false);fingers.Clear();}
    }
}
