using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
namespace Emergence.Level01
{
    [DefaultExecutionOrder(-1000)]
    public sealed class GameMenuUI : MonoBehaviour
    {
        public static GameMenuUI Instance {get;private set;}
        public static bool IsPaused=>Instance && Instance.paused;
        public static bool IsMainMenu=>Instance && Instance.mainMenu;
        public static bool BlocksGameplay=>IsPaused || IsMainMenu;
        public static bool BlocksPointer=>BlocksGameplay || (Instance && Instance.gear && Instance.gear.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(Instance.gear.GetComponent<RectTransform>(),Input.mousePosition));
        public GameMenuConfig config;
        public Canvas Canvas {get;private set;}
        public Font DisplayFont=>font;
        public Button StartButton {get;private set;}
        public Button GearButton=>gear;
        public Button ContinueButton {get;private set;}
        public Button QuitButton {get;private set;}
        public GameObject PausePanel=>pauseRoot;
        GameObject menuRoot,pauseRoot;Button gear;Font font;bool ownsFont,paused,mainMenu,starting;
        float previousTimeScale;bool previousAudioPause;DialogueDirector pausedDialogue;float dialoguePauseStart;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatic(){Instance=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Install(){if(!Instance)new GameObject("GameMenuUI").AddComponent<GameMenuUI>();}
        void Awake(){
            if(Instance && Instance!=this){Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);
            if(!config)config=Resources.Load<GameMenuConfig>("GameMenuConfig");
            if(!config){enabled=false;return;}
            font=config.font;if(!font){font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},32);ownsFont=true;}
            Build();SceneManager.activeSceneChanged+=ActiveChanged;ActiveChanged(default,SceneManager.GetActiveScene());
        }
        RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size){
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(position.x,-position.y);r.sizeDelta=size;return r;
        }
        void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        Image Box(Transform parent,string name,Vector2 pos,Vector2 size,Color color,bool raycast=false){var image=Rect(parent,name,pos,size).gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=raycast;return image;}
        Text Label(Transform parent,string text,Vector2 pos,Vector2 size,int pixels,Color color,TextAnchor align=TextAnchor.MiddleLeft){
            var label=Rect(parent,text,pos,size).gameObject.AddComponent<Text>();label.font=font;label.text=text;label.fontSize=pixels;label.color=color;label.alignment=align;label.raycastTarget=false;label.horizontalOverflow=HorizontalWrapMode.Overflow;return label;
        }
        Button Button(Transform parent,string text,Vector2 pos,Vector2 size,bool primary,UnityEngine.Events.UnityAction action){
            var image=Box(parent,text,pos,size,primary?config.accent:new Color(config.panel.r+.025f,config.panel.g+.03f,config.panel.b+.04f,1),true);var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.highlightedColor=new Color(.8f,.93f,1);colors.pressedColor=new Color(.55f,.7f,.8f);button.colors=colors;
            Label(image.transform,text,Vector2.zero,size,24,primary?config.background:config.text,TextAnchor.MiddleCenter);
            button.onClick.AddListener(()=>{GameAudio.Play(GameSound.DialogueLine);action();});return button;
        }
        RectTransform Design(Transform parent,string name){var r=Rect(parent,name,Vector2.zero,new Vector2(1280,720));r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=Vector2.zero;return r;}
        void Part(Transform parent,Sprite sprite,Vector2 offset,float scale,Color tint){
            if(!sprite)return;var image=new GameObject(sprite.name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();image.transform.SetParent(parent,false);image.sprite=sprite;image.color=tint;image.raycastTarget=false;
            var rect=image.rectTransform;rect.anchorMin=rect.anchorMax=Vector2.one*.5f;rect.pivot=sprite.pivot/sprite.rect.size;rect.sizeDelta=sprite.rect.size/sprite.pixelsPerUnit*scale;rect.anchoredPosition=offset*scale;
        }
        void Robot(Transform parent,string name,Sprite body,Sprite head,Vector2 position,float scale,float rotation,Color tint){
            var root=Rect(parent,name,position,Vector2.zero);root.localRotation=Quaternion.Euler(0,0,rotation);
            Part(root,body,Vector2.zero,scale,tint);Part(root,head,config.robotFaces?config.robotFaces.GetNeck(body):Vector2.up,scale,Color.white);
        }
        void Build(){
            Canvas=gameObject.AddComponent<Canvas>();Canvas.renderMode=RenderMode.ScreenSpaceOverlay;Canvas.sortingOrder=30000;
            var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            gameObject.AddComponent<GraphicRaycaster>();
            if(!EventSystem.current){var events=new GameObject("Menu EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));events.transform.SetParent(transform,false);}
            var background=Box(transform,"Main Menu",Vector2.zero,Vector2.zero,config.background,true);Stretch(background.rectTransform);menuRoot=background.gameObject;
            var design=Design(menuRoot.transform,"Menu composition");
            Box(design,"Illustration panel",new Vector2(590,54),new Vector2(630,610),config.panel);
            for(int x=610;x<1220;x+=48)Box(design,"Grid",new Vector2(x,54),new Vector2(1,610),new Color(.15f,.24f,.29f,.35f));
            for(int y=72;y<665;y+=48)Box(design,"Grid",new Vector2(590,y),new Vector2(630,1),new Color(.15f,.24f,.29f,.35f));
            Box(design,"Signal",new Vector2(80,113),new Vector2(40,4),config.accent);
            Label(design,"E M E R G E N C E",new Vector2(80,134),new Vector2(460,38),19,config.accent);
            Label(design,config.title,new Vector2(74,195),new Vector2(510,100),72,config.text);
            Label(design,config.subtitle,new Vector2(80,308),new Vector2(470,40),22,new Color(.5f,.64f,.7f));
            StartButton=Button(design,"开始游戏",new Vector2(80,411),new Vector2(370,66),true,StartGame);
            Button(design,"退出游戏",new Vector2(80,494),new Vector2(370,58),false,QuitGame);
            Label(design,"01  /  情感实验室",new Vector2(80,627),new Vector2(420,30),15,new Color(.4f,.55f,.61f));
            Label(design,"S I G N A L   D E T E C T E D",new Vector2(620,78),new Vector2(450,30),13,new Color(.4f,.64f,.7f));
            var doctor=Rect(design,"Doctor",config.doctorPosition,Vector2.zero);Part(doctor,config.doctor,Vector2.zero,config.doctorScale,Color.white);
            Robot(design,"Robot Yi",config.robotBody,config.robotFaces?config.robotFaces.joy:null,config.firstRobotPosition,config.firstRobotScale,config.firstRobotRotation,Color.white);
            Robot(design,"Robot Ni",config.secondRobotBody,config.robotFaces?config.robotFaces.delight:null,config.secondRobotPosition,config.secondRobotScale,config.secondRobotRotation,new Color(.82f,.95f,1));
            Label(design,"感受 · 共鸣 · 涌现",new Vector2(760,621),new Vector2(420,28),16,new Color(.45f,.66f,.71f),TextAnchor.MiddleRight);
            gear=Button(transform,"",Vector2.zero,new Vector2(48,48),false,Pause);
            var gr=gear.GetComponent<RectTransform>();gr.anchorMin=gr.anchorMax=gr.pivot=Vector2.one;gr.anchoredPosition=new Vector2(-24,-24);
            var icon=Rect(gear.transform,"Gear",new Vector2(11,11),new Vector2(26,26)).gameObject.AddComponent<GearGraphic>();icon.color=config.accent;icon.raycastTarget=false;
            var shade=Box(transform,"Pause overlay",Vector2.zero,Vector2.zero,new Color(.015f,.025f,.04f,.86f),true);Stretch(shade.rectTransform);pauseRoot=shade.gameObject;
            var modal=Box(pauseRoot.transform,"Pause window",Vector2.zero,new Vector2(440,330),config.panel,true);modal.rectTransform.anchorMin=modal.rectTransform.anchorMax=modal.rectTransform.pivot=Vector2.one*.5f;modal.rectTransform.anchoredPosition=Vector2.zero;
            Box(modal.transform,"Accent",Vector2.zero,new Vector2(440,3),config.accent);
            Label(modal.transform,"游戏已暂停",new Vector2(36,30),new Vector2(368,56),30,config.text,TextAnchor.MiddleCenter);
            Label(modal.transform,"PAUSED",new Vector2(36,87),new Vector2(368,24),13,config.accent,TextAnchor.MiddleCenter);
            ContinueButton=Button(modal.transform,"继续游戏",new Vector2(46,140),new Vector2(348,60),true,Resume);
            QuitButton=Button(modal.transform,"退出游戏",new Vector2(46,220),new Vector2(348,58),false,QuitGame);
            pauseRoot.SetActive(false);
            foreach(var rect in GetComponentsInChildren<RectTransform>(true))rect.gameObject.layer=5;
        }
        void ActiveChanged(Scene previous,Scene current){if(paused)Resume();mainMenu=config && current.name==config.mainMenuScene;if(menuRoot)menuRoot.SetActive(mainMenu);if(gear)gear.gameObject.SetActive(!mainMenu);}
        bool CanPause=>!mainMenu && !starting && !LevelFlowController.IsFading && (!LevelFlowController.IsTransitioning || LevelFlowController.IsMemoryTrip);
        void Update(){if(!config)return;if(gear)gear.interactable=CanPause && !paused;if(Input.GetKeyDown(config.pauseKey)){if(paused)Resume();else if(CanPause)Pause();}}
        public void Pause(){
            if(paused || !CanPause)return;previousTimeScale=Time.timeScale;previousAudioPause=AudioListener.pause;
            paused=true;dialoguePauseStart=Time.unscaledTime;pausedDialogue=DialogueDirector.Instance;Time.timeScale=0;AudioListener.pause=true;
            foreach(var actor in EmotionResonance.Actors)if(actor && !actor.Actor.isNpc){var motor=actor.GetComponent<RobotMotor2D>();motor.SetMoveInput(0);motor.CancelJumpRequest();var transfer=actor.GetComponent<EmotionTransfer>();if(transfer)transfer.CancelTransfer();}
            pauseRoot.SetActive(true);if(EventSystem.current)EventSystem.current.SetSelectedGameObject(ContinueButton.gameObject);
        }
        public void Resume(){
            if(!paused)return;if(pausedDialogue)pausedDialogue.DelayTimeline(Time.unscaledTime-dialoguePauseStart);pausedDialogue=null;
            paused=false;Time.timeScale=previousTimeScale;AudioListener.pause=previousAudioPause;if(pauseRoot)pauseRoot.SetActive(false);
            if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);
        }
        public void StartGame(){
            if(!mainMenu || starting || LevelFlowController.IsTransitioning)return;
            if(!Application.CanStreamedLevelBeLoaded(config.firstLevel)){Debug.LogError("开始关卡未加入 Build Settings："+config.firstLevel);return;}
            EmotionProgress.Reset();Time.timeScale=1;AudioListener.pause=false;starting=true;StartButton.interactable=false;StartCoroutine(StartLevel());
        }
        IEnumerator StartLevel(){
            var flow=ScriptableObject.CreateInstance<LevelFlowConfig>();flow.fadeInSeconds=flow.fadeOutSeconds=config.startFadeSeconds;flow.blackHoldSeconds=0;
            flow.introText=config.startSignalText;flow.introFont=font;flow.introFontSize=config.startTextFontSize;
            flow.introWaitSeconds=config.startBlackWaitSeconds;flow.introFadeInSeconds=config.startTextFadeInSeconds;
            flow.introHoldSeconds=config.startTextHoldSeconds;flow.introFadeOutSeconds=config.startTextFadeOutSeconds;
            flow.routes=new[]{new LevelFlowConfig.Route{fromScene=SceneManager.GetActiveScene().name,nextScene=config.firstLevel}};
            if(LevelFlowController.Begin(flow))while(LevelFlowController.IsTransitioning)yield return null;
            Destroy(flow);starting=false;if(StartButton)StartButton.interactable=true;
        }
        public void QuitGame(){
            Resume();Time.timeScale=1;AudioListener.pause=false;
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
            #else
            Application.Quit();
            #endif
        }
        void OnDestroy(){if(Instance!=this)return;Resume();SceneManager.activeSceneChanged-=ActiveChanged;if(ownsFont && font)Destroy(font);Instance=null;}
    }
}
