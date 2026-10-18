using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Emergence.Level01.Editor
{
    [InitializeOnLoad]
    public static class VerifyDoctorAndLevelFlow
    {
        const string Key="Emergence.DoctorFlowTest";
        static readonly List<string> results=new List<string>();static readonly Stack<IEnumerator> stack=new Stack<IEnumerator>();static double deadline;
        static readonly List<float> loadAlphas=new List<float>();static bool sawPartialFade;
        static void Loaded(Scene scene,LoadSceneMode mode){loadAlphas.Add(LevelFlowController.FadeOpacity);}
        static VerifyDoctorAndLevelFlow(){EditorApplication.playModeStateChanged+=Mode;}
        public static void Batch(){SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;}
        static void Mode(PlayModeStateChange state){if(!SessionState.GetBool(Key,false))return;if(state==PlayModeStateChange.EnteredPlayMode){Application.runInBackground=true;results.Clear();stack.Clear();stack.Push(Tests());deadline=EditorApplication.timeSinceStartup+240;EditorApplication.update+=Tick;}if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(SessionState.GetBool(Key+"Pass",false)?0:1);}}
        static void Tick(){try{if(LevelFlowController.IsFading && LevelFlowController.FadeOpacity>0 && LevelFlowController.FadeOpacity<1)sawPartialFade=true;if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout");if(stack.Count==0){Finish(true);return;}var iterator=stack.Peek();if(!iterator.MoveNext()){stack.Pop();return;}if(iterator.Current is IEnumerator nested)stack.Push(nested);}catch(Exception e){results.Add("FAIL: "+e);Finish(false);}}
        static void Finish(bool pass){Time.timeScale=1;SceneManager.sceneLoaded-=Loaded;EditorApplication.update-=Tick;Directory.CreateDirectory("Logs/DoctorFlow");File.WriteAllLines("Logs/DoctorFlow/verification.txt",results);SessionState.SetBool(Key+"Pass",pass);EditorApplication.isPlaying=false;}
        static void Check(bool value,string label){results.Add((value?"PASS: ":"FAIL: ")+label);if(!value)throw new Exception(label);}
        static IEnumerator Wait(float seconds){float until=Time.unscaledTime+seconds;while(Time.unscaledTime<until)yield return null;}
        static DialogueSequence Sequence(){var sequence=ScriptableObject.CreateInstance<DialogueSequence>();sequence.typewriter=false;sequence.lines=new[]{new DialogueSequence.Line{speaker="博士",text="自动验证对白。"}};return sequence;}
        static IEnumerator Tests(){
            EmotionProgress.Reset();
            var player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergenceLevel01/Prefabs/Characters/PlayerRobot.prefab"),new Vector3(1000,0),Quaternion.identity).GetComponent<RobotEmotionGameplay>();player.keyboardInput=false;player.GetComponent<RobotKeyboardInput>().enabled=false;player.GetComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Kinematic;
            Check(player.CurrentEmotion==RobotEmotion.Blank,"New robot starts with no emotion");Check(!player.TrySetEmotion(RobotEmotion.Joy),"Locked emotion cannot be equipped");Check(!player.BeginSwitch(RobotEmotion.Anger),"Locked emotion cannot begin a number-key transition");
            var doctor=new GameObject("DoctorProbe").AddComponent<DoctorInteraction>();doctor.transform.position=player.transform.position;
            Check(doctor.TryInteract(player) && !EmotionProgress.IsUnlocked(RobotEmotion.Joy),"Unconfigured doctor allows interaction without dialogue or unlock");
            doctor.unlockEmotion=RobotEmotion.Joy;Check(doctor.TryInteract(player) && EmotionProgress.IsUnlocked(RobotEmotion.Joy),"No-dialogue interaction immediately unlocks configured emotion");Check(player.TrySetEmotion(RobotEmotion.Joy),"Unlocked emotion can be equipped");
            var director=new GameObject("Director",typeof(DialogueDirector)).GetComponent<DialogueDirector>();doctor.dialogue=Sequence();doctor.unlockEmotion=RobotEmotion.Anger;
            Check(doctor.TryInteract(player) && director.IsPlaying,"Doctor interaction starts configured dialogue");Check(!EmotionProgress.IsUnlocked(RobotEmotion.Anger),"Emotion stays locked until dialogue completes");director.Stop();Check(!EmotionProgress.IsUnlocked(RobotEmotion.Anger),"Cancelled dialogue does not unlock emotion");
            Check(doctor.TryInteract(player),"Cancelled doctor conversation can be retried");director.Advance();Check(EmotionProgress.IsUnlocked(RobotEmotion.Anger),"Finishing conversation unlocks exactly the configured emotion");Check(!EmotionProgress.IsUnlocked(RobotEmotion.Sadness),"Other emotions remain locked");
            doctor.dialogue=null;doctor.unlockEmotion=RobotEmotion.Sadness;doctor.transform.position=player.transform.position+Vector3.right*3;Check(!doctor.TryInteract(player) && !EmotionProgress.IsUnlocked(RobotEmotion.Sadness),"Doctor cannot be interacted with outside radius");
            var appearance=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergenceLevel01/Prefabs/Characters/Scientist.prefab").GetComponent<DoctorIdleAppearance>();Check(appearance && appearance.neutral && appearance.joy && appearance.anger && appearance.sadness && appearance.courage && appearance.guardian && appearance.memory,"Scientist prefab contains neutral and six emotion idle sprites");
            var flow=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<LevelFlowConfig>(SetupDoctorAndLevelFlow.ConfigPath));Check(flow.routes.Length>=4,"Default config contains all four routes through existing five levels");foreach(var route in flow.routes)route.laboratoryDialogue=Sequence();
            var initialLoad=SceneManager.LoadSceneAsync("Level01_InitialLab");while(!initialLoad.isDone)yield return null;yield return Wait(.25f);if(DialogueDirector.Instance)DialogueDirector.Instance.Stop();
            var activePlayers=UnityEngine.Object.FindObjectsOfType<RobotEmotionGameplay>();RobotEmotionGameplay activePlayer=null;foreach(var actor in activePlayers)if(!actor.isNpc){activePlayer=actor;break;}Check(activePlayer && activePlayer.CurrentEmotion==RobotEmotion.Joy,"Equipped emotion restores on newly loaded player");var exit=new GameObject("ExitProbe").AddComponent<LevelExitInteraction>();exit.flow=flow;exit.transform.position=activePlayer.transform.position;loadAlphas.Clear();sawPartialFade=false;SceneManager.sceneLoaded+=Loaded;Time.timeScale=0;Check(exit.TryInteract(activePlayer),"Configured level exit interaction starts transition");Check(!LevelFlowController.Begin(flow),"Duplicate exit interaction rejected during transition");while(LevelFlowController.IsTransitioning)yield return null;
            Time.timeScale=1;Check(SceneManager.GetActiveScene().name=="Level02_InitialLab","Level 1 goes directly to level 2 even when time scale is zero");Check(!LevelFlowController.IsFading && LevelFlowController.FadeOpacity==0,"Fade is fully cleared after arriving");
            flow.laboratoryScene="Unused_Laboratory_Not_In_Build";
            for(int i=1;i<flow.routes.Length;i++){
                var route=flow.routes[i];yield return Wait(.2f);if(DialogueDirector.Instance)DialogueDirector.Instance.Stop();
                Check(SceneManager.GetActiveScene().name==route.fromScene,"Route starts in "+route.fromScene);
                if(route.fromScene=="Level04_InitialLab"){flow.fadeOutSeconds=0;flow.fadeInSeconds=0;flow.blackHoldSeconds=0;}
                int before=loadAlphas.Count;
                Check(LevelFlowController.Begin(flow),"Direct exit accepts route with invalid legacy lab and configured legacy dialogue");
                while(LevelFlowController.IsTransitioning)yield return null;
                Check(SceneManager.GetActiveScene().name==route.nextScene && loadAlphas.Count==before+1,"Exactly one load directly enters "+route.nextScene);
                Check(!LevelFlowController.SuppressSceneIntro,"Destination intro remains enabled");
                Check(EmotionProgress.IsUnlocked(RobotEmotion.Joy) && EmotionProgress.IsUnlocked(RobotEmotion.Anger) && !EmotionProgress.IsUnlocked(RobotEmotion.Sadness),"Unlock progress survives direct scene loads");
            }
            Check(sawPartialFade,"Transition includes intermediate opacity frames");Check(loadAlphas.Count==4 && loadAlphas.TrueForAll(alpha=>alpha>=.999f),"All four direct scene loads happen behind fully opaque black, including zero-duration transitions");
            Check(flow.Find("Level05_InitialLab")==null,"Final existing level has no invented next scene");
            EmotionProgress.Reset();Check(!EmotionProgress.IsUnlocked(RobotEmotion.Joy) && EmotionProgress.Current==RobotEmotion.Blank,"New-game reset clears emotion progress");
        }
    }
}
