using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Emergence.Level01
{
    public sealed partial class LevelFlowController : MonoBehaviour
    {
        static LevelFlowController instance;
        public static bool IsTransitioning=>instance && instance.busy;
        public static bool IsFading=>instance && instance.fading;
        public static float FadeOpacity=>instance?instance.opacity:0;
        public static bool SuppressSceneIntro=>instance && instance.busy && (instance.resettingScene || instance.memoryTrip);
        bool resettingScene;
        bool busy;
        bool fading;
        float opacity;
        Color curtainColor=Color.black;
        Camera handoffCamera;
        string introText;
        float introOpacity;
        GUIStyle introStyle;
        public static float IntroTextOpacity=>instance?instance.introOpacity:0;
        public static void DisposeAfterTransition(LevelFlowConfig config){
            if(instance)instance.StartCoroutine(instance.DisposeConfig(config));else Destroy(config);
        }
        IEnumerator DisposeConfig(LevelFlowConfig config){while(busy)yield return null;Destroy(config);}
        public static Color FadeColor=>instance?instance.curtainColor:Color.black;
        public static bool HasHandoffCamera=>instance && instance.handoffCamera && instance.handoffCamera.isActiveAndEnabled;
        void SetHandoffCamera(bool active){
            if(active && !handoffCamera){
                var go=new GameObject("Transition Camera");go.transform.SetParent(transform,false);
                handoffCamera=go.AddComponent<Camera>();handoffCamera.enabled=false;
                handoffCamera.clearFlags=CameraClearFlags.SolidColor;handoffCamera.cullingMask=0;
                handoffCamera.depth=-10000;handoffCamera.orthographic=true;
                handoffCamera.allowHDR=false;handoffCamera.allowMSAA=false;handoffCamera.useOcclusionCulling=false;
            }
            if(handoffCamera){handoffCamera.backgroundColor=new Color(curtainColor.r,curtainColor.g,curtainColor.b,1);handoffCamera.enabled=active;}
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetState(){instance=null;}
        public static bool Begin(LevelFlowConfig config){
            if(EmotionFragment.Remaining(SceneManager.GetActiveScene())>0 || !config || IsTransitioning || (DialogueDirector.Instance && DialogueDirector.Instance.IsPlaying))return false;
            string source=SceneManager.GetActiveScene().name;var route=config.Find(source);
            if(route==null || string.IsNullOrWhiteSpace(route.nextScene))return false;
            if(!Application.CanStreamedLevelBeLoaded(route.nextScene)){Debug.LogError("关卡未加入Build Settings，请运行Setup Doctor And Level Flow或检查LevelFlow配置。",config);return false;}
            if(!instance){var go=new GameObject("LevelFlowController");instance=go.AddComponent<LevelFlowController>();DontDestroyOnLoad(go);}
            instance.busy=true;instance.StartCoroutine(instance.Transition(config,route.nextScene));return true;
        }
        public static bool ResetCurrentScene(float fadeOut=.35f,float fadeIn=.35f,float hold=.1f){
            var scene=SceneManager.GetActiveScene();
            if(IsTransitioning || string.IsNullOrEmpty(scene.path) || !Application.CanStreamedLevelBeLoaded(scene.path))return false;
            if(!instance){var go=new GameObject("LevelFlowController");instance=go.AddComponent<LevelFlowController>();DontDestroyOnLoad(go);}
            instance.busy=instance.resettingScene=true;
            SceneCheckpointState.PrepareReload(scene);
            EmotionFragment.PrepareReload(scene);
            if(DialogueDirector.Instance)DialogueDirector.Instance.Stop();
            instance.StartCoroutine(instance.ResetScene(scene.path,fadeOut,fadeIn,hold));return true;
        }
        IEnumerator ResetScene(string scene,float fadeOut,float fadeIn,float hold){
            foreach(var actor in FindObjectsOfType<RobotEmotionGameplay>()){actor.CancelSwitch();var motor=actor.GetComponent<RobotMotor2D>();motor.SetMoveInput(0);motor.CancelJumpRequest();motor.CancelDirectedJump();var transfer=actor.GetComponent<EmotionTransfer>();if(transfer)transfer.CancelTransfer();}
            var config=ScriptableObject.CreateInstance<LevelFlowConfig>();config.fadeOutSeconds=Mathf.Max(0,fadeOut);config.fadeInSeconds=Mathf.Max(0,fadeIn);config.blackHoldSeconds=Mathf.Max(0,hold);
            yield return LoadWithFade(scene,config);
            // Let scene Start coroutines observe intro suppression after the fade ends.
            yield return null;
            Destroy(config);resettingScene=busy=false;
        }
        IEnumerator Transition(LevelFlowConfig config,string next){
            foreach(var actor in FindObjectsOfType<RobotEmotionGameplay>()){actor.CancelSwitch();var motor=actor.GetComponent<RobotMotor2D>();motor.SetMoveInput(0);motor.CancelJumpRequest();motor.CancelDirectedJump();var transfer=actor.GetComponent<EmotionTransfer>();if(transfer)transfer.CancelTransfer();}
            yield return LoadWithFade(next,config);
            busy=false;
        }
        IEnumerator LoadWithFade(string scene,LevelFlowConfig config){
            curtainColor=Color.black;
            GameAudio.Play(GameSound.SceneTransition);
            fading=true;
            yield return Fade(1,config.fadeOutSeconds);
            // Render an opaque frame before loading, even when fade duration is zero.
            yield return null;
            if(!string.IsNullOrWhiteSpace(config.introText)){
                introText=config.introText;
                introStyle=new GUIStyle{alignment=TextAnchor.MiddleCenter,font=config.introFont,fontSize=Mathf.Max(1,config.introFontSize)};
                introStyle.normal.textColor=Color.white;
                yield return IntroWait(config.introWaitSeconds);
                yield return FadeIntro(1,config.introFadeInSeconds);
                yield return IntroWait(config.introHoldSeconds);
                yield return FadeIntro(0,config.introFadeOutSeconds);
                introText=null;introStyle=null;
            }
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null;
            float elapsed=0;
            while(elapsed<config.blackHoldSeconds){elapsed+=Time.unscaledDeltaTime;yield return null;}
            yield return Fade(0,config.fadeInSeconds);
            fading=false;
        }
        IEnumerator IntroWait(float seconds){
            float elapsed=0;while(elapsed<seconds){elapsed+=Time.unscaledDeltaTime;yield return null;}
        }
        IEnumerator FadeIntro(float target,float seconds){
            float from=introOpacity,elapsed=0;
            while(elapsed<seconds){elapsed+=Time.unscaledDeltaTime;introOpacity=Mathf.Lerp(from,target,Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/seconds)));yield return null;}
            introOpacity=target;
        }
        IEnumerator Fade(float target,float seconds){
            float from=opacity,elapsed=0;
            while(elapsed<seconds){elapsed+=Time.unscaledDeltaTime;opacity=Mathf.Lerp(from,target,Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/seconds)));yield return null;}
            opacity=target;
            // A GUI curtain alone does not count as a rendering camera in Game View.
            // Keep a base camera alive across unload/deactivation until the reveal ends.
            if(target>=1)SetHandoffCamera(true);else if(target<=0)SetHandoffCamera(false);
        }
        void OnGUI(){
            if(opacity<=0)return;
            var color=GUI.color;var matrix=GUI.matrix;int depth=GUI.depth;
            GUI.depth=int.MinValue;GUI.matrix=Matrix4x4.identity;GUI.color=new Color(curtainColor.r,curtainColor.g,curtainColor.b,opacity);
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
            if(introOpacity>0 && introStyle!=null){
                GUI.color=new Color(1,1,1,introOpacity);
                float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
                GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
                GUI.Label(new Rect(0,0,Screen.width/scale,Screen.height/scale),introText,introStyle);
            }
            GUI.color=color;GUI.matrix=matrix;GUI.depth=depth;
        }
        void OnDestroy(){if(instance==this)instance=null;}
    }
}
