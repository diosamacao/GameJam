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
    public static class VerifyMemoryDoor
    {
        const string Key="Emergence.MemoryDoorTest";
        static readonly List<string> results=new List<string>();
        static readonly Stack<IEnumerator> stack=new Stack<IEnumerator>();
        static double deadline;
        static VerifyMemoryDoor(){EditorApplication.playModeStateChanged+=Mode;}
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
        static void Finish(bool success){EditorApplication.update-=Tick;Directory.CreateDirectory("Logs/MemoryDoor");File.WriteAllLines("Logs/MemoryDoor/verification.txt",results);SessionState.SetBool(Key+"Success",success);EditorApplication.isPlaying=false;}
        static void Check(bool value,string label){results.Add((value?"PASS: ":"FAIL: ")+label);if(!value)throw new Exception(label);}
        static IEnumerator Wait(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}

        static IEnumerator Tests(){
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergenceLevel01/Prefabs/Equipment/VerticalDoor.prefab");var door=UnityEngine.Object.Instantiate(prefab,Vector3.zero,Quaternion.identity).GetComponent<LabDoor>();
            var player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergenceLevel01/Prefabs/Characters/PlayerRobot.prefab"),new Vector3(-1,0),Quaternion.identity).GetComponent<RobotEmotionGameplay>();player.requireEmotionUnlocks=false;player.keyboardInput=false;player.GetComponent<RobotKeyboardInput>().enabled=false;player.GetComponent<Rigidbody2D>().simulated=false;
            yield return Wait(.1f);Check(door.enableMemoryStates && door.state==VerticalDoorState.Normal,"VerticalDoor prefab starts in normal state");Check(door.blocker.enabled && !door.open,"Normal door is closed and solid");foreach(var sr in door.GetComponentsInChildren<SpriteRenderer>())Check(Mathf.Abs(sr.color.a-1)<.001f,"Normal visual opacity is 100 percent");
            Check(!door.TryToggleOpen(player),"F cannot open a normal-state door");player.TrySetEmotion(RobotEmotion.Joy);Check(!door.TryToggleMemory(player),"Non-memory emotion cannot change state");
            player.TrySetEmotion(RobotEmotion.Memory);player.transform.position=Vector3.left*10;Check(!door.TryToggleMemory(player),"E rejects out-of-range interaction");player.transform.position=Vector3.left;
            Check(LabDoor.TryRememberNearest(player) && door.state==VerticalDoorState.Memory,"E dispatch changes normal door to memory state");foreach(var sr in door.GetComponentsInChildren<SpriteRenderer>())Check(Mathf.Abs(sr.color.a-.5f)<.001f,"Memory opacity is 50 percent");Check(door.blocker.enabled,"Entering memory retains collision");
            var closed=door.upper.localPosition;Check(door.TryToggleOpen(player),"F opens memory door");yield return Wait(.8f);Check(door.open && door.upper.localPosition.y>closed.y+1,"Opening retains existing vertical animation");Check(door.blocker.enabled,"Fully open memory door still retains collision");
            player.TrySetEmotion(RobotEmotion.Joy);Check(door.TryToggleOpen(player),"F works in memory state even after player changes emotion");yield return Wait(.8f);Check(!door.open && Vector3.Distance(door.upper.localPosition,closed)<.01f && door.blocker.enabled,"F closes memory door with collision retained");
            player.TrySetEmotion(RobotEmotion.Memory);door.TryToggleOpen(player);yield return Wait(.2f);Check(door.TryToggleMemory(player) && door.state==VerticalDoorState.Normal && !door.open,"E returns to normal and resets open command");yield return Wait(.8f);Check(door.blocker.enabled && Vector3.Distance(door.upper.localPosition,closed)<.01f,"Returning normal closes panels and preserves blocker");
            door.enableMemoryStates=false;door.SetOpen(true);yield return Wait(.8f);Check(!door.blocker.enabled,"Legacy doors retain original open-collision behavior");
        }
    }
}
