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
    public static class VerifyEmotionPolish
    {
        const string Key="Emergence.EmotionPolishTest";
        static readonly List<string> results=new List<string>();
        static readonly Stack<IEnumerator> stack=new Stack<IEnumerator>();
        static double deadline;
        static VerifyEmotionPolish(){EditorApplication.playModeStateChanged+=Mode;}
        public static void Batch(){
            var path="Assets/EmergenceLevel01/Prefabs/Characters/PlayerRobot.prefab";var prefab=PrefabUtility.LoadPrefabContents(path);try{if(!prefab.GetComponent<MemoryScreenEffect>())prefab.AddComponent<MemoryScreenEffect>();PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;}
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
        static void Finish(bool success){EditorApplication.update-=Tick;Directory.CreateDirectory("Logs/EmotionPolish");File.WriteAllLines("Logs/EmotionPolish/verification.txt",results);SessionState.SetBool(Key+"Success",success);EditorApplication.isPlaying=false;}
        static void Check(bool value,string label){results.Add((value?"PASS: ":"FAIL: ")+label);if(!value)throw new Exception(label);}
        static IEnumerator Wait(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}

        static RobotEmotionGameplay Spawn(bool npc,Vector2 position){
            var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergenceLevel01/Prefabs/Characters/PlayerRobot.prefab"),position,Quaternion.identity).GetComponent<RobotEmotionGameplay>();actor.isNpc=npc;actor.requireEmotionUnlocks=false;actor.keyboardInput=false;actor.GetComponent<RobotKeyboardInput>().enabled=false;actor.GetComponent<Rigidbody2D>().simulated=false;return actor;
        }
        static IEnumerator Tests(){
            yield return Wait(1);
            EmotionProgress.Reset();
            var player=Spawn(false,Vector2.zero);var a=Spawn(true,Vector2.right);var b=Spawn(true,Vector2.right*2);
            player.TrySetEmotion(RobotEmotion.Joy);Check(a.ReceiveEmotion(player) && b.ReceiveEmotion(player),"Both NPCs receive emotion from player");
            var ra=a.GetComponent<EmotionResonance>();var rb=b.GetComponent<EmotionResonance>();
            ra.RefreshState();Check(!ra.Enhanced,"Enhancement starts locked");
            EmotionProgress.CheckEnhancementScene("Level02_InitialLab","Level03_InitialLab",true);Check(!EmotionProgress.EnhancementUnlocked,"Level02 remains locked");
            EmotionProgress.CheckEnhancementScene("Level03_InitialLab","Level03_InitialLab",true);Check(EmotionProgress.EnhancementUnlocked,"Level03 unlocks enhancement");
            player.transform.position=Vector3.left*100;ra.RefreshState();rb.RefreshState();Check(ra.Enhanced && rb.Enhanced,"NPC pair enhances without player nearby");
            rb.radius=.2f;ra.RefreshState();rb.RefreshState();Check(!ra.Enhanced && !rb.Enhanced,"Pair respects both NPC radii symmetrically");rb.radius=5;
            EmotionProgress.CheckEnhancementScene("Level01_InitialLab","Level03_InitialLab",true);Check(EmotionProgress.EnhancementUnlocked,"Returning to an earlier scene retains unlock");
            EmotionProgress.Reset();EmotionProgress.CheckEnhancementScene("Level04_InitialLab","Level03_InitialLab",true);Check(EmotionProgress.EnhancementUnlocked,"Direct launch of later numbered level unlocks");
            EmotionProgress.Reset();EmotionProgress.CheckEnhancementScene("Level04_InitialLab","Level03_InitialLab",false);Check(!EmotionProgress.EnhancementUnlocked,"Later-level shortcut is configurable");
            player.transform.position=Vector3.zero;var transfer=player.GetComponent<EmotionTransfer>();Check(transfer.interactKey==KeyCode.E && transfer.interactionKey==KeyCode.F,"E transfer and F interaction bindings");
            var door=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergenceLevel01/Prefabs/Equipment/VerticalDoor.prefab"),Vector3.zero,Quaternion.identity).GetComponent<LabDoor>();
            player.TrySetEmotion(RobotEmotion.Memory);yield return Wait(.15f);
            Check(player.GetComponent<MemoryScreenEffect>().Opacity==0,"Entering memory does not whiten the screen");
            Check(transfer.TryInteract() && door.state==VerticalDoorState.Memory,"F recalls ordinary door");
            Check(transfer.TryInteract() && door.open,"Next F opens memory door");yield return Wait(.8f);Check(!door.blocker.enabled,"Fully open memory door has no collision");
            Check(transfer.TryInteract() && !door.open && door.blocker.enabled,"Closing immediately restores collision");
            player.TrySetEmotion(RobotEmotion.Joy);yield return Wait(.6f);Check(player.GetComponent<MemoryScreenEffect>().Opacity<.001f,"Leaving memory fades white overlay out");
            Check(transfer.BeginTransfer(),"E transfer remains available");Check(transfer.AdvanceTransfer(1),"Held transfer reaches NPC");Check(door.state==VerticalDoorState.Memory && !door.open,"Transfer does not operate door");
            EmotionProgress.Reset();Check(!EmotionProgress.EnhancementUnlocked,"New play session resets enhancement lock");
        }
    }
}
