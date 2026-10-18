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
    public static class VerifyMemoryNpc
    {
        const string Key="Emergence.MemoryNpcTest";
        static readonly List<string> results=new List<string>();
        static readonly Stack<IEnumerator> stack=new Stack<IEnumerator>();
        static double deadline;
        static VerifyMemoryNpc(){EditorApplication.playModeStateChanged+=Mode;}
        public static void Batch(){SetupMemoryNpc.Build();SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;}
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
        static void Finish(bool success){EditorApplication.update-=Tick;Directory.CreateDirectory("Logs/MemoryNpc");File.WriteAllLines("Logs/MemoryNpc/verification.txt",results);SessionState.SetBool(Key+"Success",success);EditorApplication.isPlaying=false;}
        static void Check(bool value,string label){results.Add((value?"PASS: ":"FAIL: ")+label);if(!value)throw new Exception(label);}
        static IEnumerator Wait(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}

        static RobotEmotionGameplay Spawn(string path,Vector2 position){var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),position,Quaternion.identity).GetComponent<RobotEmotionGameplay>();actor.requireEmotionUnlocks=false;actor.keyboardInput=false;actor.GetComponent<RobotKeyboardInput>().enabled=false;actor.GetComponent<Rigidbody2D>().gravityScale=0;return actor;}
        static IEnumerator Tests(){
            string playerPath="Assets/EmergenceLevel01/Prefabs/Characters/PlayerRobot.prefab",npcPath=SetupEmotionExpansion.Dir+"EmotionNPC.prefab";
            var player=Spawn(playerPath,new Vector2(1000,0));var partner=Spawn(npcPath,new Vector2(1001,0));var ghost=Spawn(SetupMemoryNpc.Path,new Vector2(1002,0));var state=ghost.GetComponent<MemoryNpcStateController>();var route=ghost.GetComponent<EmotionNpc>();route.route=new[]{new EmotionNpc.Waypoint{position=new Vector2(1003,0),requiresPlayerInteraction=true}};route.Activate();
            yield return Wait(.2f);Check(state.State==MemoryNpcState.Normal && !ghost.CanUseEmotionAbilities,"New NPC defaults to normal with emotions disabled");Check(Mathf.Abs(ghost.GetComponent<RobotEmotionController>().bodyRenderer.color.a-.5f)<.001f,"Normal opacity is 50 percent");Check(!route.TryInteract(player),"Normal NPC rejects route interaction");
            player.TrySetEmotion(RobotEmotion.Memory);Check(!ghost.ReceiveEmotion(player),"Normal NPC rejects direct emotion transmission");Check(partner.ReceiveEmotion(player),"Ordinary NPC still receives emotion");yield return Wait(.15f);
            Check(state.State==MemoryNpcState.Memory && ghost.CanUseEmotionAbilities,"Enhanced memory range activates NPC");Check(Mathf.Abs(ghost.GetComponent<RobotEmotionController>().bodyRenderer.color.a-.9f)<.001f,"Memory opacity is 90 percent");Check(ghost.ReceiveEmotion(player),"Memory NPC can receive emotions");ghost.GetComponent<EmotionResonance>().RefreshState();Check(ghost.GetComponent<EmotionResonance>().Enhanced,"Memory NPC participates in enhancement");Check(route.TryInteract(player),"Memory NPC supports route interaction");route.StopMoving();
            var sadnessPlayer=Spawn(playerPath,new Vector2(1002,3));var sadnessPartner=Spawn(npcPath,new Vector2(1003,3));sadnessPlayer.TrySetEmotion(RobotEmotion.Sadness);Check(sadnessPartner.ReceiveEmotion(sadnessPlayer),"Independent sadness field established");yield return Wait(.15f);Check(ghost.GetComponent<LocalTimeTarget>().Frozen && state.State==MemoryNpcState.Memory,"Sadness freezes NPC in memory state");
            player.TrySetEmotion(RobotEmotion.Joy);yield return Wait(.2f);Check(state.State==MemoryNpcState.Memory && Mathf.Abs(ghost.GetComponent<RobotEmotionController>().bodyRenderer.color.a-.9f)<.001f,"Leaving memory range while frozen preserves memory state and opacity");Check(!route.TryInteract(player),"Frozen memory NPC cannot interact");
            sadnessPlayer.TrySetEmotion(RobotEmotion.Joy);yield return Wait(.2f);Check(!ghost.GetComponent<LocalTimeTarget>().Frozen && state.State==MemoryNpcState.Normal && !ghost.CanUseEmotionAbilities,"Thaw rechecks range and returns NPC to normal");Check(ghost.CurrentEmotion==RobotEmotion.Memory,"State changes preserve previously received emotion");
            sadnessPlayer.TrySetEmotion(RobotEmotion.Sadness);yield return Wait(.1f);Check(ghost.GetComponent<LocalTimeTarget>().Frozen && state.State==MemoryNpcState.Normal,"Normal NPC is also affected by sadness freeze");player.TrySetEmotion(RobotEmotion.Memory);yield return Wait(.2f);Check(state.State==MemoryNpcState.Normal,"Entering memory range while frozen does not change normal state");sadnessPlayer.TrySetEmotion(RobotEmotion.Joy);yield return Wait(.2f);Check(state.State==MemoryNpcState.Memory && ghost.CanUseEmotionAbilities,"Thaw in memory range restores active state");
            player.TrySetEmotion(RobotEmotion.Joy);yield return Wait(.15f);Check(state.State==MemoryNpcState.Normal,"Leaving memory range without freeze immediately returns normal");Check(!ghost.GetComponent<EmotionResonance>().Enhanced,"Normal NPC cannot sustain enhancement");
            Check(AssetDatabase.LoadAssetAtPath<GameObject>(npcPath).GetComponent<MemoryNpcStateController>()==null,"Ordinary NPC prefab remains independent and unchanged");
        }
    }
}
