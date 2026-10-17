using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Emergence.Level01
{
    public sealed class LevelFlowController : MonoBehaviour
    {
        static LevelFlowController instance;
        public static bool IsTransitioning=>instance && instance.busy;
        public static bool IsFading=>instance && instance.fading;
        public static float FadeOpacity=>instance?instance.opacity:0;
        public static bool SuppressSceneIntro=>instance && instance.busy && instance.returningToLab;
        bool busy,returningToLab,finished,cancelled;
        bool fading;
        float opacity;
        DialogueDirector listening;DialogueSequence expected;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetState(){instance=null;}
        public static bool Begin(LevelFlowConfig config){
            if(!config || IsTransitioning || (DialogueDirector.Instance && DialogueDirector.Instance.IsPlaying))return false;
            string source=SceneManager.GetActiveScene().name;var route=config.Find(source);
            if(route==null || string.IsNullOrWhiteSpace(route.nextScene))return false;
            bool viaLab=source!=config.laboratoryScene;
            if(!Application.CanStreamedLevelBeLoaded(route.nextScene) || (viaLab && !Application.CanStreamedLevelBeLoaded(config.laboratoryScene))){Debug.LogError("关卡未加入Build Settings，请运行Setup Doctor And Level Flow或检查LevelFlow配置。",config);return false;}
            if(!instance){var go=new GameObject("LevelFlowController");instance=go.AddComponent<LevelFlowController>();DontDestroyOnLoad(go);}
            instance.busy=true;instance.StartCoroutine(instance.Transition(config,route.nextScene,route.laboratoryDialogue,viaLab));return true;
        }
        IEnumerator Transition(LevelFlowConfig config,string next,DialogueSequence dialogue,bool viaLab){
            foreach(var actor in FindObjectsOfType<RobotEmotionGameplay>()){actor.CancelSwitch();var motor=actor.GetComponent<RobotMotor2D>();motor.SetMoveInput(0);motor.CancelJumpRequest();}
            if(viaLab){
                returningToLab=true;
                yield return LoadWithFade(config.laboratoryScene,config);
                if(dialogue){
                    listening=DialogueDirector.Instance;
                    if(!listening)listening=new GameObject("ReturnDialogue",typeof(DialogueDirector)).GetComponent<DialogueDirector>();
                    expected=dialogue;finished=cancelled=false;listening.Finished+=Finished;listening.Cancelled+=Cancelled;
                    if(!listening.TryPlay(dialogue)){Debug.LogError("返回实验室的对话配置无效，已停止自动换关。",dialogue);Detach();busy=returningToLab=false;yield break;}
                    while(!finished && !cancelled && listening)yield return null;
                    bool completed=finished;Detach();
                    if(!completed){busy=returningToLab=false;yield break;}
                }
            }
            // Clear intro suppression before starting the destination scene's normal intro.
            returningToLab=false;
            yield return LoadWithFade(next,config);
            busy=false;
        }
        IEnumerator LoadWithFade(string scene,LevelFlowConfig config){
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
        void Finished(DialogueSequence sequence){if(sequence==expected)finished=true;}
        void Cancelled(DialogueSequence sequence){if(sequence==expected)cancelled=true;}
        void Detach(){if(listening){listening.Finished-=Finished;listening.Cancelled-=Cancelled;}listening=null;expected=null;}
        void OnDestroy(){Detach();if(instance==this)instance=null;}
    }
}
