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
    public static class VerifyGameMenu
    {
        const string Key="Emergence.GameMenuTest";
        static readonly List<string> results=new List<string>();
        static readonly Stack<IEnumerator> stack=new Stack<IEnumerator>();
        static double deadline;
        static VerifyGameMenu(){EditorApplication.playModeStateChanged+=Mode;}
        const string Fixture="Assets/__MenuGameplayValidation.unity";
        public static void Batch(){
            SetupGameMenu.CreateEmotionTriggerPrefab();
            EditorSceneManager.OpenScene("Assets/Scenes/Level01_InitialLab.unity");SetupGameMenu.Build();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Game Camera").AddComponent<Camera>();camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.047f,.075f);camera.tag="MainCamera";
            new GameObject("Dialogue").AddComponent<DialogueDirector>();
            var floor=new GameObject("Floor",typeof(BoxCollider2D));floor.transform.position=Vector3.down*.5f;floor.GetComponent<BoxCollider2D>().size=new Vector2(20,1);
            UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergenceLevel01/Prefabs/Characters/PlayerRobot.prefab"));
            EditorSceneManager.SaveScene(scene,Fixture);
            var builds=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);builds.RemoveAll(item=>item.path==Fixture);builds.Add(new EditorBuildSettingsScene(Fixture,true));EditorBuildSettings.scenes=builds.ToArray();
            EditorSceneManager.OpenScene(SetupGameMenu.ScenePath);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
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
        static void Finish(bool success){EditorApplication.update-=Tick;Directory.CreateDirectory("Logs/GameMenu");File.WriteAllLines("Logs/GameMenu/verification.txt",results);SessionState.SetBool(Key+"Success",success);EditorApplication.isPlaying=false;}
        static void Check(bool value,string label){results.Add((value?"PASS: ":"FAIL: ")+label);if(!value)throw new Exception(label);}
        static IEnumerator Wait(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}

        static RobotEmotionGameplay Spawn(bool npc,Vector2 position){
            var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergenceLevel01/Prefabs/Characters/PlayerRobot.prefab"),position,Quaternion.identity).GetComponent<RobotEmotionGameplay>();actor.isNpc=npc;actor.requireEmotionUnlocks=false;actor.keyboardInput=false;actor.GetComponent<RobotKeyboardInput>().enabled=false;actor.GetComponent<Rigidbody2D>().simulated=false;return actor;
        }
        static IEnumerator RealWait(float seconds){float end=Time.unscaledTime+seconds;while(Time.unscaledTime<end)yield return null;}
        static IEnumerator Capture(string name){
            var ui=GameMenuUI.Instance;var camera=Camera.main;
            var graphics=UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;var quality=QualitySettings.renderPipeline;
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=null;QualitySettings.renderPipeline=null;
            var texture=new RenderTexture(1280,720,24);texture.Create();var oldTarget=camera.targetTexture;int mask=camera.cullingMask;camera.cullingMask=1<<5;
            camera.targetTexture=texture;ui.Canvas.renderMode=RenderMode.ScreenSpaceCamera;ui.Canvas.worldCamera=camera;ui.Canvas.planeDistance=1;
            yield return null;yield return null;Canvas.ForceUpdateCanvases();camera.Render();
            var before=RenderTexture.active;RenderTexture.active=texture;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();Directory.CreateDirectory("Logs/GameMenu");File.WriteAllBytes("Logs/GameMenu/"+name+".png",image.EncodeToPNG());
            RenderTexture.active=before;camera.targetTexture=oldTarget;camera.cullingMask=mask;ui.Canvas.renderMode=RenderMode.ScreenSpaceOverlay;ui.Canvas.worldCamera=null;
            UnityEngine.Object.Destroy(image);texture.Release();UnityEngine.Object.Destroy(texture);QualitySettings.renderPipeline=quality;UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=graphics;
        }
        static IEnumerator Tests(){
            yield return RealWait(.3f);var ui=GameMenuUI.Instance;
            Check(ui && GameMenuUI.IsMainMenu && ui.StartButton.gameObject.activeInHierarchy,"Menu starts with title composition and start button");
            Check(!ui.GearButton.gameObject.activeInHierarchy,"Gear hidden on main menu");
            Check(ui.config.robotBody && ui.config.doctor && ui.config.robotFaces,"Composition references original robot and doctor sprites");
            yield return Capture("MainMenu");
            ui.config=UnityEngine.Object.Instantiate(ui.config);ui.config.firstLevel=Fixture;ui.config.startFadeSeconds=.04f;
            ui.config.startBlackWaitSeconds=.25f;ui.config.startTextFadeInSeconds=.15f;ui.config.startTextHoldSeconds=.3f;ui.config.startTextFadeOutSeconds=.15f;
            EmotionProgress.Unlock(RobotEmotion.Joy);ui.StartButton.onClick.Invoke();
            while(LevelFlowController.FadeOpacity<1)yield return null;
            Check(LevelFlowController.IntroTextOpacity==0 && GameMenuUI.IsMainMenu,"Signal intro waits on opaque black before showing text");
            yield return RealWait(.1f);Check(LevelFlowController.IntroTextOpacity==0,"Configured black wait is respected");
            while(LevelFlowController.IntroTextOpacity<1)yield return null;
            Check(GameMenuUI.IsMainMenu && LevelFlowController.HasHandoffCamera,"Signal text appears before loading with camera continuity");
            yield return RealWait(.1f);Check(LevelFlowController.IntroTextOpacity==1,"Signal text remains fully visible for configured hold");
            while(LevelFlowController.IntroTextOpacity>=1)yield return null;
            Check(LevelFlowController.IntroTextOpacity>0 && GameMenuUI.IsMainMenu,"Signal text gradually fades before loading level");
            while(LevelFlowController.IsTransitioning)yield return null;yield return RealWait(.2f);
            Check(LevelFlowController.IntroTextOpacity==0 && LevelFlowController.FadeOpacity==0,"Signal and black curtain are cleared after entry");
            Check(!GameMenuUI.IsMainMenu && UnityEngine.SceneManagement.SceneManager.GetActiveScene().path==Fixture,"Start button loads configured gameplay scene");
            Check(!EmotionProgress.IsUnlocked(RobotEmotion.Joy),"New game resets emotion progress");Check(ui.GearButton.gameObject.activeInHierarchy,"Gear appears inside gameplay");yield return Capture("GameplayGear");
            Check(UnityEngine.Object.FindObjectsOfType<UnityEngine.EventSystems.EventSystem>().Length==1,"UI retains one event system after scene load");
            var player=UnityEngine.Object.FindObjectOfType<RobotEmotionGameplay>();var motor=player.GetComponent<RobotMotor2D>();
            var dialogue=ScriptableObject.CreateInstance<DialogueSequence>();dialogue.typewriter=false;dialogue.autoPlay=true;dialogue.autoAdvanceDelay=.25f;dialogue.lines=new[]{new DialogueSequence.Line{text="暂停时对话不会继续。"},new DialogueSequence.Line{text="继续游戏后恢复。"}};
            var director=DialogueDirector.Instance;director.TryPlay(dialogue);Time.timeScale=.5f;ui.GearButton.onClick.Invoke();
            Check(GameMenuUI.IsPaused && Time.timeScale==0 && ui.PausePanel.activeSelf,"Gear click opens pause window and freezes game");
            Check(AudioListener.pause && motor.InputBlocked && !player.CanUseEmotionAbilities,"Pause blocks game audio, movement and emotion interaction");
            Check(!director.Advance() && !UnityEngine.Object.FindObjectOfType<SceneResetController>().TryReset(),"Pause blocks dialogue advance and scene reset");
            yield return RealWait(.4f);Check(director.LineIndex==0,"Autoplay dialogue stays on current line while paused");
            yield return Capture("PauseMenu");
            ui.ContinueButton.onClick.Invoke();Check(!GameMenuUI.IsPaused && Time.timeScale==.5f && !AudioListener.pause,"Continue restores prior speed and audio state");
            yield return RealWait(.08f);Check(director.LineIndex==0,"Resuming does not skip dialogue elapsed during pause");
            yield return RealWait(.3f);Check(director.LineIndex==1,"Dialogue autoplay resumes normally");
            Time.timeScale=1;director.Stop();Check(!motor.InputBlocked && player.CanUseEmotionAbilities,"Gameplay controls restored");
            var trigger=new GameObject("Emotion trigger test").AddComponent<EmotionSwitchTrigger>();
            trigger.transform.position=player.transform.position;trigger.GetComponent<BoxCollider2D>().isTrigger=true;
            trigger.GetComponent<BoxCollider2D>().size=new Vector2(10,10);trigger.targetEmotion=RobotEmotion.Memory;
            int triggerCount=0;trigger.onTriggered.AddListener(()=>triggerCount++);
            yield return RealWait(.15f);
            Check(trigger.HasTriggered && player.CurrentEmotion==RobotEmotion.Memory && EmotionProgress.IsUnlocked(RobotEmotion.Memory),"Physics entry switches and unlocks configured emotion");
            Check(triggerCount==1 && !trigger.TryTrigger(player),"One-shot trigger fires once despite staying inside");
            trigger.ResetTrigger();trigger.unlockTargetEmotion=false;trigger.targetEmotion=RobotEmotion.Guardian;
            Check(!trigger.TryTrigger(player) && !trigger.HasTriggered,"Locked emotion failure does not consume trigger");
            trigger.unlockTargetEmotion=true;ui.Pause();Check(!trigger.TryTrigger(player),"Pause blocks automatic emotion trigger");ui.Resume();
            trigger.triggerOnce=false;Check(trigger.TryTrigger(player) && player.CurrentEmotion==RobotEmotion.Guardian,"Repeatable trigger switches through gameplay API");
            UnityEngine.Object.Destroy(trigger.gameObject);
            player.requireEmotionUnlocks=false;player.TrySetEmotion(RobotEmotion.Joy);EmotionProgress.UnlockEnhancement();var npc=Spawn(true,(Vector2)player.transform.position+Vector2.right);npc.ReceiveEmotion(player);var resonance=player.GetComponent<EmotionResonance>();resonance.RefreshState();Check(resonance.Enhanced,"Enhancement fixture is active");ui.Pause();resonance.RefreshState();Check(resonance.Enhanced,"Pause preserves resonance and range mechanics");ui.Resume();UnityEngine.Object.Destroy(npc.gameObject);
            Check(ui.QuitButton && ui.ContinueButton,"Pause window contains quit and continue buttons");
            Check(EditorBuildSettings.scenes[0].path==SetupGameMenu.ScenePath,"Main menu is first build scene");
            var ending=new GameObject("Completion test").AddComponent<GameCompletionInteraction>();ending.transform.position=player.transform.position;
            ending.blackFadeSeconds=.04f;ending.blackWaitSeconds=.15f;ending.textFadeInSeconds=.1f;ending.textHoldSeconds=.2f;ending.revealMenuSeconds=.04f;
            Check(ending.TryInteract(player),"Completion object accepts player interaction");
            Check(!ending.TryInteract(player),"Completion cannot be triggered twice");
            while(LevelFlowController.IntroTextOpacity<1)yield return null;
            Check(!GameMenuUI.IsMainMenu && LevelFlowController.FadeOpacity==1,"Ending displays on black before leaving level");
            yield return RealWait(.08f);Check(LevelFlowController.IntroTextOpacity==1,"Ending stays visible for configured hold");
            while(LevelFlowController.IsTransitioning)yield return null;yield return RealWait(.1f);
            Check(GameMenuUI.IsMainMenu && ui.StartButton.interactable && !GameMenuUI.IsPaused,"Completion returns to usable main menu");
        }
    }
}
