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
    public static class VerifyInteractionUpgrade
    {
        const string Key="Emergence.InteractionUpgradeTest";
        static readonly List<string> results=new List<string>();
        static readonly Stack<IEnumerator> stack=new Stack<IEnumerator>();
        static double deadline;
        static VerifyInteractionUpgrade(){EditorApplication.playModeStateChanged+=Mode;}
        public static void Batch(){SetupInteractionUpgrade.Build();SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;}
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
        static void Finish(bool success){EditorApplication.update-=Tick;Directory.CreateDirectory("Logs/InteractionUpgrade");File.WriteAllLines("Logs/InteractionUpgrade/verification.txt",results);SessionState.SetBool(Key+"Success",success);EditorApplication.isPlaying=false;}
        static void Check(bool value,string label){results.Add((value?"PASS: ":"FAIL: ")+label);if(!value)throw new Exception(label);}
        static IEnumerator Wait(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}
        static LocalMovingPlatform Platform(Vector2 position,PlatformActivation mode){var go=new GameObject("TestPlatform");go.transform.position=position;go.AddComponent<BoxCollider2D>().size=new Vector2(3,.3f);go.AddComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Kinematic;var p=go.AddComponent<LocalMovingPlatform>();p.activation=mode;p.travel=Vector2.right;p.speed=4;return p;}
        static IEnumerator Tests(){
            EmotionProgress.UnlockEnhancement();
            var floor=new GameObject("Floor",typeof(BoxCollider2D));floor.transform.position=new Vector3(1000,-.5f);floor.GetComponent<BoxCollider2D>().size=new Vector2(20,1);
            var player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergenceLevel01/Prefabs/Characters/PlayerRobot.prefab"),new Vector3(1000,0),Quaternion.identity).GetComponent<RobotEmotionGameplay>();
            player.keyboardInput=false;player.requireEmotionUnlocks=false;player.GetComponent<RobotKeyboardInput>().enabled=false;
            var npc=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SetupEmotionExpansion.Dir+"EmotionNPC.prefab"),new Vector3(1001.5f,0),Quaternion.identity).GetComponent<RobotEmotionGameplay>();
            var transfer=player.GetComponent<EmotionTransfer>();var aura=player.GetComponent<EmotionResonance>();var body=player.GetComponent<Rigidbody2D>();
            yield return Wait(.4f);
            Check(player.BeginSwitch(RobotEmotion.Sadness),"Number-selected target begins transition");
            Check(player.IsSwitching && player.GetComponent<RobotMotor2D>().ControlsLocked && player.GetComponent<RobotEmotionController>().CurrentEmotion==RobotEmotion.Loading,"Transition shows loading and locks movement");
            yield return Wait(.25f);Check(player.IsSwitching,"Transition not complete before 0.5 seconds");
            yield return Wait(.35f);Check(!player.IsSwitching && player.CurrentEmotion==RobotEmotion.Sadness,"Transition completes without held key");
            Check(transfer.BeginTransfer(),"NPC within two units can receive");Check(!transfer.AdvanceTransfer(.25f),"Transfer does not complete early");Check(transfer.AdvanceTransfer(.25f),"Transfer completes at 0.5 seconds");
            yield return Wait(.1f);aura.RefreshState();Check(aura.Enhanced,"Matching NPC grants enhancement");
            var ring=player.transform.Find("EmotionAura").GetComponent<LineRenderer>();
            Check(ring.useWorldSpace && Mathf.Abs(Vector2.Distance(ring.GetPosition(0),player.transform.position)-aura.radius)<.01f,"Sadness ring exactly matches effect radius");
            var sample=ring.GetPosition(0);yield return Wait(.3f);Check(Vector3.Distance(sample,ring.GetPosition(0))<.02f,"Sadness range does not pulse");
            Check(EmotionResonance.InEnhancedRange((Vector2)player.transform.position+Vector2.left*4.9f,RobotEmotion.Sadness) && !EmotionResonance.InEnhancedRange((Vector2)player.transform.position+Vector2.left*5.1f,RobotEmotion.Sadness),"Freeze range agrees with visible boundary");
            npc.transform.position=new Vector3(1003,0);yield return Wait(.1f);Check(!transfer.BeginTransfer(),"Transfer rejected outside two units even inside resonance radius");
            npc.transform.position=new Vector3(1001.5f,0);player.TrySetEmotion(RobotEmotion.Memory);yield return Wait(.1f);
            Check(transfer.BeginTransfer(),"Second transfer can start after releasing");transfer.AdvanceTransfer(.2f);npc.transform.position=new Vector3(1003,0);Check(!transfer.AdvanceTransfer(.4f) && npc.CurrentEmotion==RobotEmotion.Sadness,"Leaving range cancels incomplete transfer");
            npc.transform.position=new Vector3(1001.5f,0);yield return Wait(.1f);Check(transfer.BeginTransfer() && transfer.AdvanceTransfer(.5f),"Memory transfer succeeds");yield return Wait(.1f);
            Check(Mathf.Abs(Vector2.Distance(ring.GetPosition(0),player.transform.position)-aura.radius)<.01f,"Memory ring uses same fixed range");
            var p=Platform(new Vector2(1030,0),PlatformActivation.Single);var sw=new GameObject("Switch").AddComponent<PlatformPressureSwitch>();sw.platforms=new[]{p};
            yield return Wait(.15f);Check(Vector2.Distance(p.transform.position,p.Origin)<.01f,"Single platform waits for activation");
            p.SetSwitch(sw,true);yield return Wait(.4f);Check(Mathf.Abs(p.transform.position.x-1031)<.03f,"Single activation reaches endpoint");
            p.SetSwitch(sw,true);yield return Wait(.2f);Check(Mathf.Abs(p.transform.position.x-1031)<.03f,"Held switch cannot retrigger single mode");
            p.SetSwitch(sw,false);p.SetSwitch(sw,true);yield return Wait(.4f);Check(Mathf.Abs(p.transform.position.x-1030)<.03f,"Fresh activation returns single platform home");
            p.SetSwitch(sw,false);p.activation=PlatformActivation.Continuous;p.SetSwitch(sw,true);yield return Wait(.4f);Check(p.RemainingWait>0,"Continuous platform waits at endpoint");
            yield return Wait(.4f);Check(Mathf.Abs(p.transform.position.x-1031)<.03f,"Endpoint pause lasts configured one second");
            yield return Wait(.6f);Check(p.transform.position.x<1030.8f,"Continuous platform reverses after pause");
            p.SetSwitch(sw,false);yield return Wait(.5f);Check(Mathf.Abs(p.transform.position.x-1030)<.03f,"Releasing continuous switch returns home");
            yield return Wait(.3f);Check(Mathf.Abs(p.transform.position.x-1030)<.03f,"Released platform remains home");
            var pass=Platform(new Vector2(1040,2),PlatformActivation.Single);var probe=new GameObject("OneWayProbe");probe.transform.position=new Vector3(1040,0);probe.AddComponent<BoxCollider2D>().size=Vector2.one*.3f;var rb=probe.AddComponent<Rigidbody2D>();rb.gravityScale=0;rb.velocity=Vector2.up*4;
            yield return Wait(.8f);Check(rb.position.y>2.7f,"Player-sized collider passes through underside");rb.velocity=Vector2.down*4;
            yield return Wait(.5f);Check(rb.position.y>2.2f && rb.position.y<2.4f,"One-way platform catches descending collider");
            UnityEngine.Object.Destroy(probe);
            var portalGo=new GameObject("TestPortal");portalGo.transform.position=player.transform.position+Vector3.up*.8f;portalGo.AddComponent<BoxCollider2D>().isTrigger=true;var portal=portalGo.AddComponent<InteractionPortal>();portal.destinationPosition=new Vector2(1005,0);
            yield return Wait(.15f);Check(Mathf.Abs(body.position.x-1000)<.1f,"Portal contact alone does not teleport");Check(portal.TryTeleport(player),"Interacting while touching teleports");Check(Mathf.Abs(body.position.x-1005)<.01f && body.velocity==Vector2.zero,"Portal uses destination and clears velocity");Check(!portal.TryTeleport(player),"Portal cannot retrigger outside contact");
            Check(player.joyKey==KeyCode.Alpha1 && player.memoryKey==KeyCode.Alpha6,"Default emotion bindings span 1 through 6");
            var sensor=new GameObject("ActualPressureSensor");sensor.transform.position=new Vector3(1005,0);var sensorCollider=sensor.AddComponent<BoxCollider2D>();sensorCollider.isTrigger=true;sensorCollider.size=new Vector2(1,.2f);sensorCollider.offset=new Vector2(0,.1f);var pressure=sensor.AddComponent<PlatformPressureSwitch>();pressure.platforms=new[]{p};
            yield return Wait(.2f);Check(pressure.Pressed,"Standing on ground switch triggers real collision sensor");body.position=new Vector2(1007,0);yield return Wait(.15f);Check(!pressure.Pressed,"Leaving ground switch releases sensor");
            npc.transform.position=player.transform.position+Vector3.left*1.5f;
            var frozen=Platform((Vector2)player.transform.position+Vector2.up*2,PlatformActivation.Automatic);frozen.travel=Vector2.zero;yield return Wait(.15f);
            player.TrySetEmotion(RobotEmotion.Sadness);npc.ReceiveEmotion(player);yield return Wait(.1f);float remaining=frozen.RemainingWait;yield return Wait(.3f);
            Check(frozen.GetComponent<LocalTimeTarget>().Frozen && Mathf.Abs(frozen.RemainingWait-remaining)<.001f,"Sadness pauses endpoint wait timer as well as motion");
            player.TrySetEmotion(RobotEmotion.Memory);yield return Wait(.15f);Check(frozen.RemainingWait<remaining,"Endpoint timer resumes after sadness ends");
            var ride=Platform(new Vector2(1080,0),PlatformActivation.Single);ride.speed=1;ride.travel=new Vector2(2,0);body.position=new Vector2(1080,.2f);body.velocity=Vector2.zero;yield return Wait(.4f);
            Check(player.GetComponent<RobotMotor2D>().IsGrounded,"Player stands on one-way platform");ride.SetSwitch(sw,true);yield return Wait(.6f);
            Check(body.position.x>1080.4f && Mathf.Abs(body.position.x-ride.transform.position.x)<.15f,"Moving platform carries idle player horizontally");
        }
    }
}
