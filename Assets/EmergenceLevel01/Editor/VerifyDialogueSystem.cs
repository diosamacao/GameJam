using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace Emergence.Level01.Editor
{
    [InitializeOnLoad]
    public static class VerifyDialogueSystem
    {
        const string Key="Emergence.DialogueCheck";
        static DialogueDirector director;static DialogueSequence sample;
        static RobotMotor2D motor;static RobotEmotionGameplay emotion;
        static float started,startX;static int phase,completed,cancelled,lineEvents;
        static readonly List<string> results=new List<string>();
        static VerifyDialogueSystem(){EditorApplication.playModeStateChanged+=Mode;}
        [MenuItem("Tools/Emergence/Verify Dialogue System")]
        public static void Run(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop play first");SessionState.SetBool(Key,true);SessionState.SetBool(Key+"Background",Application.runInBackground);EditorApplication.isPlaying=true;}
        static void Mode(PlayModeStateChange state){
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false)){Application.runInBackground=true;results.Clear();phase=completed=cancelled=lineEvents=0;started=Time.time;EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.ExitingPlayMode)EditorApplication.update-=Tick;
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key,false)){Application.runInBackground=SessionState.GetBool(Key+"Background",false);SessionState.SetBool(Key,false);}
        }
        static void Check(bool ok,string text){if(!ok)throw new Exception(text);results.Add("PASS: "+text);}
        static void Next(){phase++;started=Time.time;}
        static void Tick(){try{
            if(Time.time-started>8)throw new Exception("Timeout "+phase);
            if(phase==0){
                director=DialogueDirector.Instance;Check(director,"Scene contains dialogue system");director.Stop();
                Check(director.advanceKey==KeyCode.C,"C configured as advance key");
                motor=UnityEngine.Object.FindObjectOfType<RobotMotor2D>();emotion=motor.GetComponent<RobotEmotionGameplay>();motor.GetComponent<RobotKeyboardInput>().enabled=false;emotion.keyboardInput=false;
                var source=AssetDatabase.LoadAssetAtPath<DialogueSequence>("Assets/EmergenceLevel01/Dialogue/02_Greeting.asset");
                sample=ScriptableObject.CreateInstance<DialogueSequence>();sample.lines=new[]{new DialogueSequence.Line{speaker="",text=source.lines[0].text,minimumDisplaySeconds=.3f},source.lines[1]};
                director.Finished+=s=>completed++;director.Cancelled+=s=>cancelled++;director.LineChanged+=(s,i)=>lineEvents++;Next();return;
            }
            if(phase==1){if(Time.time-started<.5f)return;
                Check(emotion.BeginSwitch(),"Emotion charge can start before dialogue");Check(director.TryPlay(sample),"External trigger starts dialogue");
                Check(!emotion.IsSwitching && DialogueDirector.IsBlockingInput,"Dialogue cancels charge and blocks input");
                Check(director.LineIndex==0 && director.CurrentLine.speaker=="","Unnamed player line supported");
                Check(!director.Advance(),"Minimum display time prevents early advance");
                Check(!director.TryPlay(sample) && director.LineIndex==0,"Busy dialogue cannot be overwritten");
                Check(!emotion.BeginSwitch() && !motor.RequestJump(),"Dialogue blocks emotion switching and jumping");
                startX=motor.transform.position.x;motor.SetMoveInput(1);Next();return;
            }
            if(phase==2){if(Time.time-started<.4f)return;
                Check(Mathf.Abs(motor.transform.position.x-startX)<.05f,"Dialogue blocks horizontal movement");
                Check(director.Advance() && director.LineIndex==1 && director.CurrentLine.speaker=="博士","Advance displays next configured line and speaker");
                Directory.CreateDirectory("Logs/Dialogue");ScreenCapture.CaptureScreenshot("Logs/Dialogue/preview.png");Next();return;
            }
            if(phase==3){if(Time.time-started<.3f)return;
                Check(director.Advance() && !director.IsPlaying && !DialogueDirector.IsBlockingInput,"Last advance closes panel and unlocks input");
                Check(completed==1 && lineEvents==2,"Completion and line events fire correctly");Check(!director.Advance() && completed==1,"Repeated advance does not repeat completion");
                motor.SetControlsLocked(true);director.TryPlay(sample);director.Stop();Check(motor.ControlsLocked && cancelled==1 && completed==1,"Cancel preserves independent locks and does not fire completion");motor.SetControlsLocked(false);
                sample.lines[0].minimumDisplaySeconds=0;
                var go=new GameObject("DialogueTriggerVerification");var trigger=go.AddComponent<DialogueTrigger>();trigger.sequence=sample;int triggerCompletions=0;trigger.onCompleted.AddListener(()=>triggerCompletions++);
                Check(trigger.TryTrigger() && !trigger.TryTrigger(),"Trigger starts once and rejects duplicate during playback");director.Stop();Check(!trigger.HasPlayed && trigger.TryTrigger(),"Cancelled trigger can retry");director.Advance();director.Advance();
                Check(triggerCompletions==1 && !trigger.TryTrigger(),"Trigger completion fires once and playOnce persists");
                var empty=ScriptableObject.CreateInstance<DialogueSequence>();Check(!director.TryPlay(empty) && !director.TryPlay(null),"Empty and null configuration rejected");
                sample.blockPlayerInput=false;Check(director.TryPlay(sample) && !DialogueDirector.IsBlockingInput,"Optional nonblocking dialogue supported");director.Stop();
                sample.blockPlayerInput=true;director.TryPlay(sample);director.enabled=false;Check(!DialogueDirector.IsBlockingInput && !director.IsPlaying,"Disabling director clears dialogue lock");director.enabled=true;
                Check(emotion.BeginSwitch(),"Emotion switching restored after dialogue");emotion.CancelSwitch();
                Finish(null);
            }
        }catch(Exception e){Finish("FAIL: "+e.Message);}}
        static void Finish(string error){if(error!=null)results.Add(error);Directory.CreateDirectory("Logs/Dialogue");File.WriteAllLines("Logs/Dialogue/verification.txt",results);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;Debug.Log(string.Join("\n",results));}
    }
}
