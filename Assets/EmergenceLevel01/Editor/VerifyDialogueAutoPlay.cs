using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Emergence.Level01.Editor
{
    [InitializeOnLoad]
    public static class VerifyDialogueAutoPlay
    {
        const string Key="Emergence.DialogueAutoPlayTest";
        static readonly List<string> results=new List<string>();
        static readonly Stack<IEnumerator> stack=new Stack<IEnumerator>();
        static double deadline;
        static VerifyDialogueAutoPlay(){EditorApplication.playModeStateChanged+=Mode;}
        public static void Batch(){SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;}
        static void Mode(PlayModeStateChange state){
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode){Application.runInBackground=true;results.Clear();stack.Push(Tests());deadline=EditorApplication.timeSinceStartup+120;EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(SessionState.GetBool(Key+"Success",false)?0:1);}
        }
        static void Tick(){try{
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Test timeout");
            if(stack.Count==0){Finish(true);return;}
            var current=stack.Peek();if(!current.MoveNext()){stack.Pop();return;}if(current.Current is IEnumerator nested)stack.Push(nested);
        }catch(Exception ex){results.Add("FAIL: "+ex);Finish(false);}}
        static void Finish(bool success){EditorApplication.update-=Tick;Directory.CreateDirectory("Logs/DialogueAutoPlay");File.WriteAllLines("Logs/DialogueAutoPlay/verification.txt",results);SessionState.SetBool(Key+"Success",success);EditorApplication.isPlaying=false;}
        static void Check(bool value,string label){results.Add((value?"PASS: ":"FAIL: ")+label);if(!value)throw new Exception(label);}
        static IEnumerator Wait(float seconds){float end=Time.unscaledTime+seconds;while(Time.unscaledTime<end)yield return null;}

        static DialogueSequence Sequence(bool auto=true,bool typing=true,float delay=.15f){var s=ScriptableObject.CreateInstance<DialogueSequence>();s.autoPlay=auto;s.typewriter=typing;s.autoAdvanceDelay=delay;s.lines=new[]{new DialogueSequence.Line{text="甲乙丙丁"},new DialogueSequence.Line{text="戊己"}};return s;}
        static IEnumerator Tests(){
            var director=new GameObject("AutoDialogue",typeof(DialogueDirector)).GetComponent<DialogueDirector>();int finished=0;director.Finished+=s=>finished++;
            var defaults=ScriptableObject.CreateInstance<DialogueSequence>();Check(defaults.charactersPerSecond==20 && defaults.autoPlay && defaults.autoAdvanceDelay==2,"New sequences default to 20 cps and configurable auto-play with 2-second delay");
            yield return Wait(1);var sequence=Sequence(delay:2);Check(director.TryPlay(sequence) && !director.ShowAdvanceHint,"Auto-play hides advance hints");Check(director.IsRevealing && new DialogueRevealTimeline("甲乙丙丁",sequence.charactersPerSecond).Duration==.2,"Typewriter reveals at configured speed");yield return Wait(.35f);Check(!director.IsRevealing && director.LineIndex==0,"Full text waits rather than advancing immediately");yield return Wait(1.5f);Check(director.LineIndex==0,"Two-second delay begins after reveal completion");yield return Wait(.5f);Check(director.LineIndex==1,"Automatically advances after complete text plus delay");director.Stop();
            sequence=Sequence(false,false);director.TryPlay(sequence);Check(director.ShowAdvanceHint,"Manual mode restores hints");yield return Wait(.4f);Check(director.LineIndex==0,"Manual mode never advances automatically");Check(director.Advance() && director.LineIndex==1,"Manual advance remains available");director.Stop();
            sequence=Sequence();sequence.lines[0].text="甲<pause=1>乙";director.TryPlay(sequence);yield return Wait(.3f);Check(director.IsRevealing && director.LineIndex==0,"Inline pause is part of reveal before auto delay");director.CompleteCurrentLine();yield return Wait(.05f);Check(director.LineIndex==0 && !director.IsRevealing,"Manual completion starts a new post-reveal wait");yield return Wait(.15f);Check(director.LineIndex==1,"Auto-play resumes after skipped reveal wait");director.Stop();
            sequence=Sequence(typing:false,delay:.1f);sequence.lines[0].minimumDisplaySeconds=.5f;director.TryPlay(sequence);yield return Wait(.25f);Check(director.LineIndex==0,"Minimum display time still applies");yield return Wait(.3f);Check(director.LineIndex==1,"Non-typewriter dialogue supports automatic advance");int before=finished;yield return Wait(.25f);Check(!director.IsPlaying && finished==before+1,"Last line auto-closes and fires Finished exactly once");
            Time.timeScale=0;sequence=Sequence(delay:.1f);director.TryPlay(sequence);yield return Wait(.4f);Check(director.LineIndex==1,"Auto-play uses real time when game time is paused");director.Stop();Time.timeScale=1;
            sequence=Sequence(false,false);director.TryPlay(sequence);yield return Wait(.3f);Check(director.LineIndex==0,"Starting another sequence clears previous auto timing");director.Stop();yield return Wait(.2f);Check(!director.IsPlaying,"Stopping dialogue cancels automatic progression");
            int assets=0;foreach(var guid in AssetDatabase.FindAssets("t:DialogueSequence",new[]{"Assets/EmergenceLevel01/Dialogue"})){var asset=AssetDatabase.LoadAssetAtPath<DialogueSequence>(AssetDatabase.GUIDToAssetPath(guid));Check(asset.charactersPerSecond==20,"Configured speed is 20: "+asset.name);foreach(var line in asset.lines)Check(line.charactersPerSecond==0 || line.charactersPerSecond==20,"Line inherits or uses 20 cps");assets++;}Check(assets>=50,"All existing dialogue assets imported successfully");
        }
    }
}
