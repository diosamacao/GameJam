using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
namespace Emergence.Level01.Editor
{
    public static class VerifyDialogueTyping
    {
        static readonly List<string> results=new List<string>();
        static void Check(bool ok,string text){if(!ok)throw new Exception(text);results.Add("PASS: "+text);}
        [MenuItem("Tools/Emergence/Verify Dialogue Typing")]
        public static void Run()
        {
            results.Clear();GameObject go=null;DialogueSequence data=null;
            try{
                var timeline=new DialogueRevealTimeline("伊……<pause=1>你好。<pause=0.5>再见。",10);
                Check(timeline.Text=="伊……你好。再见。","Pause markers hidden from full text");
                Check(timeline.VisibleAt(0)=="" && timeline.VisibleAt(.11)=="伊","Characters appear progressively");
                Check(timeline.VisibleAt(.9)=="伊……" && timeline.VisibleAt(1.29)=="伊……","First one-second pause holds text");
                Check(timeline.VisibleAt(1.41)=="伊……你","Typing resumes after pause");
                Check(timeline.VisibleAt(2.09)=="伊……你好。" && timeline.VisibleAt(2.21)=="伊……你好。再","Second independent half-second pause");
                Check(timeline.VisibleAt(20)==timeline.Text,"Full text revealed at completion");
                var unicode=new DialogueRevealTimeline("😀e\u0301中",10);
                Check(unicode.VisibleAt(.11)=="😀" && unicode.VisibleAt(.21)=="😀e\u0301","Surrogate and combining characters are not split");
                var edges=new DialogueRevealTimeline("<pause=0.2>甲<pause=0.3><pause=0.4>",10);
                Check(edges.VisibleAt(.29)=="" && edges.VisibleAt(.31)=="甲" && Math.Abs(edges.Duration-1)<.0001,"Leading, consecutive and trailing pauses supported");
                Check(new DialogueRevealTimeline("<pause=bad>甲",10).Text=="<pause=bad>甲","Invalid marker remains readable without crashing");
                Check(new DialogueRevealTimeline("甲乙",20).Duration<new DialogueRevealTimeline("甲乙",10).Duration,"Typing speed controls duration");
                go=new GameObject("TypingVerification");var director=go.AddComponent<DialogueDirector>();
                data=ScriptableObject.CreateInstance<DialogueSequence>();data.blockPlayerInput=false;
                data.lines=new[]{new DialogueSequence.Line{text="甲<pause=10>乙",charactersPerSecond=1},new DialogueSequence.Line{text="丙"}};
                int completed=0,changed=0;director.Finished+=s=>completed++;director.LineChanged+=(s,i)=>changed++;
                Check(director.TryPlay(data) && director.IsRevealing,"Director starts typing");
                Check(director.Advance() && director.LineIndex==0 && !director.IsRevealing && director.VisibleText=="甲乙" && changed==1,"C reveals all and skips pauses without advancing or firing line event");
                Check(director.Advance() && director.LineIndex==1 && director.IsRevealing && changed==2,"Next C advances and resets typing");
                director.Advance();Check(completed==0,"Completing final line does not close dialogue");director.Advance();Check(completed==1 && !director.IsPlaying,"Next C closes final line once");
                data.lines[0].minimumDisplaySeconds=100;director.TryPlay(data);director.Advance();Check(!director.IsRevealing && !director.Advance(),"Minimum display time still applies after skip");director.Stop();
                data.typewriter=false;data.lines[0].minimumDisplaySeconds=0;director.TryPlay(data);Check(!director.IsRevealing && director.VisibleText=="甲乙","Typewriter can be disabled without showing pause markers");director.Stop();
            }catch(Exception e){results.Add("FAIL: "+e.Message);throw;}
            finally{
                if(go)UnityEngine.Object.DestroyImmediate(go);if(data)UnityEngine.Object.DestroyImmediate(data);
                Directory.CreateDirectory("Logs/Dialogue");File.WriteAllLines("Logs/Dialogue/typing-verification.txt",results);Debug.Log(string.Join("\n",results));
            }
        }
    }
}
