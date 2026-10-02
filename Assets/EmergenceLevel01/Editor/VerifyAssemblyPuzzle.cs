using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Emergence.Level01.Editor
{
    [InitializeOnLoad]
    public static class VerifyAssemblyPuzzle
    {
        const string Key="Emergence.PuzzleVerification";
        static AssemblyInteraction input;
        static AssemblyPiece first,second;
        static AssemblySocket upper,lower;
        static int phase;
        static float timestamp;
        static readonly List<string> results=new List<string>();
        static VerifyAssemblyPuzzle(){EditorApplication.playModeStateChanged+=OnMode;}
        [MenuItem("Tools/Emergence/Verify Assembly Puzzle")]
        static void Run(){if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop play mode first.");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        static void OnMode(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
            {phase=0;timestamp=Time.time;results.Clear();EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.ExitingPlayMode)EditorApplication.update-=Tick;
        }
        static void Check(bool condition,string name){if(!condition)throw new Exception(name);results.Add("PASS: "+name);}
        static void Tick()
        {
            if(!EditorApplication.isPlaying || Time.time-timestamp<.8f)return;
            try
            {
                switch(phase)
                {
                    case 0:
                        input=UnityEngine.Object.FindObjectOfType<AssemblyInteraction>();Check(input && input.worldCamera,"Puzzle and click camera bound");
                        first=input.pieces[0];second=input.pieces[1];upper=input.targetDoor.sockets[0];lower=input.targetDoor.sockets[1];
                        Check(!upper.Filled && !lower.Filled && !upper.installedVisual.enabled && !lower.installedVisual.enabled,"Door starts with both panels missing");
                        Check(!upper.Highlighted && !lower.Highlighted && !input.targetDoor.TryUse(),"No selection: no ghost targets; incomplete door locked");
                        input.ClickWorld(first.transform.position);
                        Check(input.Selected==first && upper.Highlighted && !lower.Highlighted && first.outline.Visible,"Click upper piece selects it and highlights only matching socket");
                        Check(!lower.TryPlace(first) && !first.Consumed,"Wrong socket rejects piece without consuming it");
                        input.ClickWorld(first.transform.position);Check(input.Selected==null && !upper.Highlighted,"Second click deselects");
                        input.ClickWorld(first.transform.position);input.ClickWorld(second.transform.position);
                        Check(input.Selected==second && !upper.Highlighted && lower.Highlighted,"Click another piece switches selection and target");
                        input.CancelSelection();Check(input.Selected==null && !lower.Highlighted,"Cancel clears selection and ghost");
                        input.ClickWorld(second.transform.position);input.ClickWorld(new Vector2(20,10));Check(input.Selected==null,"Click empty space cancels selection");
                        input.ClickWorld(second.transform.position);input.ClickWorld(lower.transform.position);
                        Check(lower.Filled && second.Consumed && !second.gameObject.activeSelf && lower.installedVisual.enabled && input.targetDoor.FilledCount==1,"Place lower panel first; reveal installed panel and consume source");
                        Check(!input.targetDoor.TryUse() && input.targetDoor.door.blocker.enabled,"One installed panel cannot unlock door");
                        Check(!lower.TryPlace(second) && input.targetDoor.FilledCount==1,"Cannot place consumed piece twice");
                        input.ClickWorld(first.transform.position);input.ClickWorld(upper.transform.position);
                        Check(input.targetDoor.Complete && first.Consumed && upper.installedVisual.enabled && input.Selected==null && !upper.Highlighted && !lower.Highlighted,"Both installed: assembled, sources consumed, no stale highlights");
                        input.ClickWorld(input.targetDoor.transform.position+Vector3.up*2);Check(input.targetDoor.door.open,"Click completed door requests opening");break;
                    case 1:
                        Check(!input.targetDoor.door.blocker.enabled,"Open animation finishes and passage collision releases");
                        input.ClickWorld(input.targetDoor.transform.position+Vector3.up*2);Check(!input.targetDoor.door.open,"Click completed door again requests closing");break;
                    case 2:
                        Check(input.targetDoor.door.blocker.enabled && input.targetDoor.Complete,"Closing restores collision and preserves assembly");
                        Check(UnityEngine.Object.FindObjectOfType<RobotKeyboardInput>()!=null,"A/D player remains present");
                        Finish(null);return;
                }
                phase++;timestamp=Time.time;
            }
            catch(Exception e){Finish("FAIL: "+e.Message);}
        }
        static void Finish(string failure)
        {
            if(failure!=null)results.Add(failure);Directory.CreateDirectory("Logs/AssemblyPuzzle");File.WriteAllLines("Logs/AssemblyPuzzle/playmode-verification.txt",results);
            SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;
            if(failure==null)Debug.Log("ASSEMBLY_VERIFICATION_PASSED");else Debug.LogError(failure);
        }
    }
}
