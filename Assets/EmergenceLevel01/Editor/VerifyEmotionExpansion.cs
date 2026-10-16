using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Emergence.Level01.Editor
{
    [InitializeOnLoad]
    public static class VerifyEmotionExpansion
    {
        const string Key="Emergence.VerifyExpansion";static readonly List<string> results=new List<string>();
        static RobotEmotionGameplay player,npc;static RobotMotor2D motor;static Rigidbody2D body;static EmotionResonance resonance,npcResonance;static float start,peak,launchY;static int phase;static bool released;static float shortHeight;static LocalMovingPlatform platform;static Vector2 pausedPosition;static GameObject memoryGrid;static EmotionNpc mover;
        static VerifyEmotionExpansion(){EditorApplication.playModeStateChanged+=Mode;if(SessionState.GetBool(Key+"Regression",false)){regressionStep=SessionState.GetInt(Key+"RegressionStep",0);EditorApplication.playModeStateChanged+=RegressionMode;}}
        public static void Batch(){SetupEmotionExpansion.Build();CaptureHeads();Run();}
        static int regressionStep;
        public static void RegressionBatch(){
            EditorSceneManager.OpenScene("Assets/Scenes/Level01_InitialLab.unity");regressionStep=0;SessionState.SetBool(Key+"Regression",true);SessionState.SetInt(Key+"RegressionStep",0);EditorApplication.playModeStateChanged+=RegressionMode;VerifyEmotionBlocks.Run();
        }
        static void RegressionMode(PlayModeStateChange state){
            if(state!=PlayModeStateChange.EnteredEditMode)return;
            string[] reports={"Logs/EmotionBlocks/verification.txt","Logs/BlockRecovery/verification.txt","Logs/EmotionGameplay/verification.txt"};
            if(!File.Exists(reports[regressionStep]) || File.ReadAllText(reports[regressionStep]).Contains("FAIL:")){EditorApplication.Exit(1);return;}
            regressionStep++;SessionState.SetInt(Key+"RegressionStep",regressionStep);EditorApplication.delayCall+=()=>{if(regressionStep==1)VerifyBlockRecovery.Run();else if(regressionStep==2)VerifyEmotionGameplay.Run();else{SessionState.SetBool(Key+"Regression",false);EditorApplication.playModeStateChanged-=RegressionMode;EditorApplication.Exit(0);}};
        }
        [MenuItem("Tools/Emergence/Verify Emotion Expansion")]
        public static void Run(){if(EditorApplication.isPlaying)throw new Exception("Stop play first");if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Save scene before testing");SessionState.SetString(Key+"Scene",UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);SessionState.SetBool(Key,true);SessionState.SetBool(Key+"Bg",Application.runInBackground);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;}
        static void Mode(PlayModeStateChange state){
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false)){results.Clear();phase=0;start=Time.time;Application.runInBackground=true;EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.ExitingPlayMode)EditorApplication.update-=Tick;
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);Application.runInBackground=SessionState.GetBool(Key+"Bg",false);var path=SessionState.GetString(Key+"Scene","");if(!string.IsNullOrEmpty(path))EditorSceneManager.OpenScene(path);if(Application.isBatchMode)EditorApplication.Exit(results.Exists(x=>x.StartsWith("FAIL"))?1:0);}
        }
        static void Check(bool success,string message){if(!success)throw new Exception(message);if(!results.Contains("PASS: "+message))results.Add("PASS: "+message);}
        public static void CaptureHeads(){
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
            var scene=EditorSceneManager.NewPreviewScene();Texture2D sheet=null;RenderTexture rt=null;
            try{
                var robot=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergenceLevel01/Prefabs/Characters/PlayerRobot.prefab"),scene);var face=robot.GetComponent<RobotEmotionController>();
                var go=new GameObject("HeadPreviewCamera",typeof(Camera));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);var cam=go.GetComponent<Camera>();cam.scene=scene;cam.orthographic=true;cam.orthographicSize=.56f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.06f,.09f,.14f);
                sheet=new Texture2D(800,128,TextureFormat.RGB24,false);rt=new RenderTexture(160,128,24);cam.targetTexture=rt;var old=RenderTexture.active;
                var emotions=new[]{RobotEmotion.Blank,RobotEmotion.Loading,RobotEmotion.Memory,RobotEmotion.Guardian,RobotEmotion.Courage};
                for(int i=0;i<emotions.Length;i++){var serialized=new SerializedObject(face);serialized.FindProperty("currentEmotion").intValue=(int)emotions[i];serialized.ApplyModifiedPropertiesWithoutUndo();face.RefreshVisual();cam.transform.position=face.headRenderer.transform.position+new Vector3(0,.43f,-10);cam.Render();RenderTexture.active=rt;var image=new Texture2D(160,128,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,160,128),0,0);image.Apply();sheet.SetPixels(i*160,0,160,128,image.GetPixels());UnityEngine.Object.DestroyImmediate(image);}
                RenderTexture.active=old;cam.targetTexture=null;sheet.Apply();Directory.CreateDirectory("Logs/EmotionExpansion");File.WriteAllBytes("Logs/EmotionExpansion/heads-preview.png",sheet.EncodeToPNG());
            }finally{if(rt)UnityEngine.Object.DestroyImmediate(rt);if(sheet)UnityEngine.Object.DestroyImmediate(sheet);EditorSceneManager.ClosePreviewScene(scene);}
        }
        static void Next(){phase++;start=Time.time;}
        static void Emotion(RobotEmotion emotion){player.TrySetEmotion(emotion);var previous=npc.transform.position;npc.transform.position=player.transform.position+Vector3.right*1.5f;npc.ReceiveEmotion(player);npc.transform.position=previous;resonance.RefreshState();npcResonance.RefreshState();}
        static void Tick(){try{
            if(Time.time-start>15)throw new Exception("Timeout phase "+phase);
            if(phase==0){
                new GameObject("Camera",typeof(Camera)).transform.position=new Vector3(1000,2,-10);new GameObject("Light",typeof(Light));
                var floor=new GameObject("Floor",typeof(BoxCollider2D));floor.transform.position=new Vector3(1000,-.5f);floor.GetComponent<BoxCollider2D>().size=new Vector2(30,1);
                player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergenceLevel01/Prefabs/Characters/PlayerRobot.prefab"),new Vector3(1000,0),Quaternion.identity).GetComponent<RobotEmotionGameplay>();player.keyboardInput=false;player.GetComponent<RobotKeyboardInput>().enabled=false;motor=player.GetComponent<RobotMotor2D>();body=player.GetComponent<Rigidbody2D>();resonance=player.GetComponent<EmotionResonance>();
                npc=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SetupEmotionExpansion.Dir+"EmotionNPC.prefab"),new Vector3(1003,0),Quaternion.identity).GetComponent<RobotEmotionGameplay>();npcResonance=npc.GetComponent<EmotionResonance>();mover=npc.GetComponent<EmotionNpc>();Next();return;
            }
            if(phase==1){if(Time.time-start<.3f)return;
                Check(motor.IsGrounded,"Player grounded");var config=player.GetComponent<RobotEmotionController>().config;Check(config.neutralScreen && config.neutralScreen.pixelsPerUnit==128 && Mathf.Abs(config.neutralScreen.pivot.y-33)<.1f,"Screen layer has correct size and center pivot");Check(!npc.TrySetEmotion(RobotEmotion.Anger),"NPC cannot change emotion independently");Emotion(RobotEmotion.Joy);Check(resonance.Enhanced && npcResonance.Enhanced,"Player transmission enhances both matching actors");
                npc.transform.position=new Vector3(1010,0);resonance.RefreshState();Check(!resonance.Enhanced,"Leaving radius ends enhancement");npc.transform.position=new Vector3(1003,0);Emotion(RobotEmotion.Joy);
                mover.destinationPosition=new Vector2(1004,0);mover.Activate();launchY=body.position.y;peak=launchY;Check(motor.RequestJump(),"First jump starts");motor.SetJumpHeld(false);Next();return;
            }
            if(phase==2){peak=Mathf.Max(peak,body.position.y);if(Time.time-start<.15f || !motor.IsGrounded)return;shortHeight=peak-launchY;Check(Mathf.Abs(shortHeight-player.MinimumJumpHeight)<.35f,"Tap jump reaches configured minimum");Check(!mover.Moving && npc.transform.position.x>1003.8f,"Activated NPC reaches its own destination");launchY=body.position.y;peak=launchY;Check(motor.RequestJump(),"Held jump starts");motor.SetJumpHeld(true);Next();return;}
            if(phase==3){peak=Mathf.Max(peak,body.position.y);if(Time.time-start<.15f || !motor.IsGrounded)return;Check(peak-launchY>shortHeight+.5f && Mathf.Abs(peak-launchY-player.JumpHeight)<.4f,"Held jump reaches configured maximum");npc.transform.position=new Vector3(1002,0);Emotion(RobotEmotion.Joy);Check(motor.RequestJump(),"First jump for double-jump test");Next();return;}
            if(phase==4){if(Time.time-start<.12f)return;resonance.RefreshState();Check(motor.RequestJump(),"Enhanced joy permits second jump");launchY=body.position.y;peak=launchY;motor.SetJumpHeld(true);Next();return;}
            if(phase==5){peak=Mathf.Max(peak,body.position.y);if(Time.time-start<.15f)return;Check(!motor.RequestJump(),"Third jump rejected");if(body.velocity.y>0)return;Check(Mathf.Abs(peak-launchY-resonance.SecondJumpMaximum)<.35f,"Second jump maximum is two cells");Next();return;}
            if(phase==6){if(!motor.IsGrounded)return;Emotion(RobotEmotion.Anger);Check(player.EffectiveEmotion==RobotEmotion.StrongAnger,"Enhanced anger uses reinforced breaking ability");
                Emotion(RobotEmotion.Sadness);platform=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SetupEmotionExpansion.Dir+"MovingPlatform.prefab"),new Vector3(1001,3),Quaternion.identity).GetComponent<LocalMovingPlatform>();Next();return;
            }
            if(phase==7){if(Time.time-start<.2f)return;Check(platform.GetComponent<LocalTimeTarget>().Frozen,"Enhanced sadness freezes nearby moving platform");pausedPosition=platform.transform.position;Next();return;}
            if(phase==8){if(Time.time-start<.3f)return;Check(Vector2.Distance(pausedPosition,platform.transform.position)<.001f,"Frozen platform remains still");Emotion(RobotEmotion.Guardian);Next();return;}
            if(phase==9){if(Time.time-start<.2f)return;Check(!platform.GetComponent<LocalTimeTarget>().Frozen && Vector2.Distance(pausedPosition,platform.transform.position)>.1f,"Platform resumes after sadness ends");Check(player.GetComponentInChildren<CircleCollider2D>()!=null,"Enhanced guardian creates circular shield");
                var laser=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergenceLevel01/Tilemap64/Lasers/Prefabs/LaserEmitter_Right.prefab"),new Vector3(995,.8f),Quaternion.identity).GetComponent<LaserEmitter>();Physics2D.SyncTransforms();var before=body.position;laser.Trace(true);Check(Vector2.Distance(before,body.position)<.01f && laser.BeamLength<4,"Guardian shield stops laser before actor");UnityEngine.Object.Destroy(laser.gameObject);
                Emotion(RobotEmotion.Memory);var item=new GameObject("MemoryItem",typeof(MemoryInteractable)).GetComponent<MemoryInteractable>();item.transform.position=player.transform.position;Check(item.Interact(player) && item.state,"Memory interaction toggles object state");
                memoryGrid=new GameObject("MemoryGrid",typeof(Grid));memoryGrid.transform.position=player.transform.position;var control=new GameObject("MemoryControl",typeof(MemoryGridTarget)).GetComponent<MemoryGridTarget>();control.target=memoryGrid;Next();return;
            }
            if(phase==10){if(Time.time-start<.2f)return;Check(!memoryGrid.activeSelf,"Enhanced memory hides configured Grid");Emotion(RobotEmotion.Courage);Next();return;}
            if(phase==11){if(Time.time-start<.2f)return;Check(memoryGrid.activeSelf,"Grid restores after memory enhancement ends");Check(resonance.Enhanced && resonance.Emotion==RobotEmotion.Courage,"Enhanced courage state available to danger zones");Check(player.BeginSwitch() && player.GetComponent<RobotEmotionController>().CurrentEmotion==RobotEmotion.Loading,"Charge displays dedicated loading expression");player.CancelSwitch();var danger=new GameObject("DangerTest",typeof(BoxCollider2D),typeof(DangerZone)).GetComponent<DangerZone>();Check(!danger.TryContact(player),"Enhanced courage survives danger zone");Emotion(RobotEmotion.Joy);Check(danger.TryContact(player),"Other emotions die in danger zone");Finish(null);}
        }catch(Exception e){Finish(e.ToString());}}
        static void Finish(string error){if(error!=null)results.Add("FAIL: "+error);Directory.CreateDirectory("Logs/EmotionExpansion");File.WriteAllLines("Logs/EmotionExpansion/verification.txt",results);Debug.Log(string.Join("\n",results));EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
    }
}
