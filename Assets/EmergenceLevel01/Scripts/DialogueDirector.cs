using System;
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
        float revealStarted;
        bool revealSkipped;
        float advanceAt;
        int changedFrame;
        Font font;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics(){Instance=null;}
        void OnEnable(){if(Instance && Instance!=this){Debug.LogWarning("Only one DialogueDirector should be active.",this);enabled=false;return;}Instance=this;}
        void Start(){if(playOnStart)TryPlay(playOnStart);}
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
            if(IsRevealing){CompleteCurrentLine();return true;}
            if(!CanAdvance)return false;
            if(LineIndex+1<Sequence.lines.Length){LineIndex++;SetLineTiming();LineChanged?.Invoke(Sequence,LineIndex);}
            else {var finished=Sequence;Sequence=null;LineIndex=-1;onFinished.Invoke();Finished?.Invoke(finished);}
            return true;
        }
        public void Stop()
        {
            if(!IsPlaying)return;var cancelled=Sequence;Sequence=null;LineIndex=-1;Cancelled?.Invoke(cancelled);
        }
        public void CompleteCurrentLine(){if(IsPlaying)revealSkipped=true;}
        void SetLineTiming(){
            advanceAt=Time.unscaledTime+Mathf.Max(0,CurrentLine.minimumDisplaySeconds);
            changedFrame=Time.frameCount;revealStarted=Time.unscaledTime;revealSkipped=!Sequence.typewriter;
            float speed=CurrentLine.charactersPerSecond>0?CurrentLine.charactersPerSecond:Sequence.charactersPerSecond;
            reveal=new DialogueRevealTimeline(CurrentLine.text,speed);
        }
        void Update(){if(IsPlaying && Time.frameCount>changedFrame && Input.GetKeyDown(advanceKey))Advance();}
        void OnDisable(){Stop();if(Instance==this)Instance=null;}
        void OnGUI()
        {
            if(!IsPlaying)return;
            if(!font)font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},22);
            float scale=Mathf.Clamp(Screen.width/1280f,.65f,1.5f);
            float width=Mathf.Min(Screen.width-32,900*scale),x=(Screen.width-width)*.5f,y=20*scale,pad=22*scale;
            var body=new GUIStyle(GUI.skin.label){font=font,fontSize=Mathf.RoundToInt(23*scale),wordWrap=true,richText=false};body.normal.textColor=new Color(.88f,.95f,1);
            var heading=new GUIStyle(body){fontSize=Mathf.RoundToInt(19*scale),fontStyle=FontStyle.Bold};heading.normal.textColor=new Color(.4f,.88f,1);
            float speakerHeight=string.IsNullOrEmpty(CurrentLine.speaker)?0:32*scale;
            float textHeight=body.CalcHeight(new GUIContent(FullText),width-pad*2);
            float height=Mathf.Max(130*scale,pad*2+speakerHeight+textHeight+30*scale);
            var old=GUI.color;GUI.color=new Color(.035f,.065f,.1f,.97f);GUI.DrawTexture(new Rect(x,y,width,height),Texture2D.whiteTexture);
            GUI.color=new Color(.3f,.7f,.85f);GUI.DrawTexture(new Rect(x,y,width,3*scale),Texture2D.whiteTexture);GUI.color=old;
            if(speakerHeight>0)GUI.Label(new Rect(x+pad,y+pad,width-pad*2,speakerHeight),CurrentLine.speaker,heading);
            GUI.Label(new Rect(x+pad,y+pad+speakerHeight,width-pad*2,textHeight),VisibleText,body);
            var hint=new GUIStyle(heading){alignment=TextAnchor.MiddleRight,fontSize=Mathf.RoundToInt(15*scale),fontStyle=FontStyle.Normal};
            GUI.Label(new Rect(x+pad,y+height-30*scale,width-pad*2,25*scale),IsRevealing?advanceKey+"  显示全文":CanAdvance?advanceKey+"  "+(LineIndex==Sequence.lines.Length-1?"结束对话":"下一段"):"…",hint);
        }
        void OnDestroy(){if(font)Destroy(font);}
    }
}
