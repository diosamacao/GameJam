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
            GameAudio.Play(GameSound.SceneTransition);
            fading=true;
            yield return Fade(1,config.fadeOutSeconds);
            // Render an opaque frame before loading, even when fade duration is zero.
            yield return null;
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null;
            float elapsed=0;
            while(elapsed<config.blackHoldSeconds){elapsed+=Time.unscaledDeltaTime;yield return null;}
            yield return Fade(0,config.fadeInSeconds);
            fading=false;
        }
        IEnumerator Fade(float target,float seconds){
            float from=opacity,elapsed=0;
            while(elapsed<seconds){elapsed+=Time.unscaledDeltaTime;opacity=Mathf.Lerp(from,target,Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/seconds)));yield return null;}
            opacity=target;
        }
        void OnGUI(){
            if(opacity<=0)return;
            var color=GUI.color;var matrix=GUI.matrix;int depth=GUI.depth;
            GUI.depth=int.MinValue;GUI.matrix=Matrix4x4.identity;GUI.color=new Color(0,0,0,opacity);
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
            GUI.color=color;GUI.matrix=matrix;GUI.depth=depth;
        }
        void OnDestroy(){if(instance==this)instance=null;}
    }
}
