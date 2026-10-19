using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
namespace Emergence.Level01
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class DialogueDirector : MonoBehaviour
    {
        public static DialogueDirector Instance {get;private set;}
        public static bool IsBlockingInput => Instance && Instance.IsPlaying && Instance.Sequence.blockPlayerInput;
        public DialogueSequence playOnStart;
        public KeyCode advanceKey=KeyCode.C;
        [Header("对话框布局（参考分辨率下的像素）")]
        [Min(1)] public float referenceWidth=1280;
        public Vector2 scaleRange=new Vector2(.65f,1.5f);
        [Min(1)] public float panelWidth=900, minimumHeight=130;
        [Min(0)] public float screenMargin=32, topOffset=20, padding=22;
        [Min(0)] public float speakerLineHeight=32, footerHeight=30, hintHeight=25, accentHeight=3;
        [Header("对话文字与颜色")]
        [Min(1)] public int bodyFontSize=23, speakerFontSize=19, hintFontSize=15;
        public Color bodyColor=new Color(.88f,.95f,1), speakerColor=new Color(.4f,.88f,1);
        public Color panelColor=new Color(.035f,.065f,.1f,.97f), accentColor=new Color(.3f,.7f,.85f);
        public UnityEvent onStarted=new UnityEvent();
        public UnityEvent onFinished=new UnityEvent();
        public event Action<DialogueSequence> Started;
        public event Action<DialogueSequence,int> LineChanged;
        public event Action<DialogueSequence> Finished;
        public event Action<DialogueSequence> Cancelled;
        public DialogueSequence Sequence {get;private set;}
        public int LineIndex {get;private set;}=-1;
        public bool IsPlaying => Sequence && LineIndex>=0 && LineIndex<Sequence.lines.Length;
        public DialogueSequence.Line CurrentLine => IsPlaying?Sequence.lines[LineIndex]:null;
        public bool IsRevealing => IsPlaying && !revealSkipped && Time.unscaledTime-revealStarted<reveal.Duration;
        public string FullText => IsPlaying ? reveal.Text : "";
        public string VisibleText => !IsPlaying ? "" : IsRevealing ? reveal.VisibleAt(Time.unscaledTime-revealStarted) : reveal.Text;
        public bool CanAdvance => IsPlaying && !IsRevealing && Time.unscaledTime>=advanceAt;
        DialogueRevealTimeline reveal;
        float revealStarted, completedAt;
        public bool ShowAdvanceHint=>IsPlaying && !Sequence.autoPlay;
        bool revealSkipped;
        float advanceAt;
        int changedFrame;
        Font font;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics(){Instance=null;}
        void OnEnable(){if(Instance && Instance!=this){Debug.LogWarning("Only one DialogueDirector should be active.",this);enabled=false;return;}Instance=this;}
        IEnumerator Start(){while(LevelFlowController.IsFading)yield return null;if(playOnStart && !LevelFlowController.SuppressSceneIntro)TryPlay(playOnStart);}
        public bool TryPlay(DialogueSequence sequence)
        {
            if(!isActiveAndEnabled || IsPlaying || !sequence || sequence.lines==null || sequence.lines.Length==0)return false;
            foreach(var line in sequence.lines)if(line==null || string.IsNullOrWhiteSpace(line.text))return false;
            Sequence=sequence;LineIndex=0;SetLineTiming();
            if(sequence.blockPlayerInput)foreach(var motor in FindObjectsOfType<RobotMotor2D>()){
                var emotion=motor.GetComponent<RobotEmotionGameplay>();if(emotion)emotion.CancelSwitch();
                motor.SetMoveInput(0);motor.CancelJumpRequest();
            }
            onStarted.Invoke();Started?.Invoke(sequence);LineChanged?.Invoke(sequence,LineIndex);return true;
        }
        // Void wrapper is exposed in Inspector UnityEvents.
        public void Play(DialogueSequence sequence){TryPlay(sequence);}
        public bool Advance()
        {
            if(GameMenuUI.IsPaused)return false;
            if(IsRevealing){CompleteCurrentLine();return true;}
            if(!CanAdvance)return false;
            if(LineIndex+1<Sequence.lines.Length){LineIndex++;SetLineTiming();LineChanged?.Invoke(Sequence,LineIndex);}
            else {var finished=Sequence;Sequence=null;LineIndex=-1;onFinished.Invoke();Finished?.Invoke(finished);if(finished.memoryAfterCompletion && !LevelFlowController.IsMemoryTrip)LevelFlowController.BeginMemory(finished.memoryAfterCompletion);}
            return true;
        }
        public void Stop()
        {
            if(!IsPlaying)return;var cancelled=Sequence;Sequence=null;LineIndex=-1;Cancelled?.Invoke(cancelled);
        }
        public void CompleteCurrentLine(){if(IsRevealing){completedAt=Time.unscaledTime;revealSkipped=true;}}
        void SetLineTiming(){
            GameAudio.Play(GameSound.DialogueLine);
            advanceAt=Time.unscaledTime+Mathf.Max(0,CurrentLine.minimumDisplaySeconds);
            changedFrame=Time.frameCount;revealStarted=Time.unscaledTime;revealSkipped=!Sequence.typewriter;completedAt=revealStarted;
            float speed=CurrentLine.charactersPerSecond>0?CurrentLine.charactersPerSecond:Sequence.charactersPerSecond;
            reveal=new DialogueRevealTimeline(CurrentLine.text,speed);
        }
        public void DelayTimeline(float seconds){if(!IsPlaying)return;float delay=Mathf.Max(0,seconds);revealStarted+=delay;completedAt+=delay;advanceAt+=delay;}
        void Update(){
            if(GameMenuUI.IsPaused)return;
            if(!IsPlaying || Time.frameCount<=changedFrame)return;
            if(Input.GetKeyDown(advanceKey)){Advance();return;}
            if(Sequence.autoPlay && CanAdvance){
                double finishedAt=revealSkipped?completedAt:revealStarted+reveal.Duration;
                if(Time.unscaledTime>=finishedAt+Mathf.Max(0,Sequence.autoAdvanceDelay))Advance();
            }
        }
        void OnDisable(){Stop();if(Instance==this)Instance=null;}
        void OnGUI()
        {
            if(GameMenuUI.BlocksGameplay)return;
            if(!IsPlaying)return;
            if(!font)font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},bodyFontSize);
            float scale=Mathf.Clamp(Screen.width/Mathf.Max(1,referenceWidth),Mathf.Max(.01f,scaleRange.x),Mathf.Max(scaleRange.x,scaleRange.y));
            float width=Mathf.Min(Screen.width-screenMargin,panelWidth*scale),x=(Screen.width-width)*.5f,y=topOffset*scale,pad=padding*scale;
            var body=new GUIStyle(GUI.skin.label){font=font,fontSize=Mathf.RoundToInt(bodyFontSize*scale),wordWrap=true,richText=false};body.normal.textColor=bodyColor;
            var heading=new GUIStyle(body){fontSize=Mathf.RoundToInt(speakerFontSize*scale),fontStyle=FontStyle.Bold};heading.normal.textColor=speakerColor;
            float speakerHeight=string.IsNullOrEmpty(CurrentLine.speaker)?0:speakerLineHeight*scale;
            float textHeight=body.CalcHeight(new GUIContent(FullText),width-pad*2);
            float height=Mathf.Max(minimumHeight*scale,pad*2+speakerHeight+textHeight+footerHeight*scale);
            var old=GUI.color;GUI.color=panelColor;GUI.DrawTexture(new Rect(x,y,width,height),Texture2D.whiteTexture);
            GUI.color=accentColor;GUI.DrawTexture(new Rect(x,y,width,accentHeight*scale),Texture2D.whiteTexture);GUI.color=old;
            if(speakerHeight>0)GUI.Label(new Rect(x+pad,y+pad,width-pad*2,speakerHeight),CurrentLine.speaker,heading);
            GUI.Label(new Rect(x+pad,y+pad+speakerHeight,width-pad*2,textHeight),VisibleText,body);
            if(!ShowAdvanceHint)return;
            var hint=new GUIStyle(heading){alignment=TextAnchor.MiddleRight,fontSize=Mathf.RoundToInt(hintFontSize*scale),fontStyle=FontStyle.Normal};
            GUI.Label(new Rect(x+pad,y+height-footerHeight*scale,width-pad*2,hintHeight*scale),IsRevealing?advanceKey+"  显示全文":CanAdvance?advanceKey+"  "+(LineIndex==Sequence.lines.Length-1?"结束对话":"下一段"):"…",hint);
        }
        void OnDestroy(){if(font)Destroy(font);}
    }
}
