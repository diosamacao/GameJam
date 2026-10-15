using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
namespace Emergence.Level01.Editor
{
    [InitializeOnLoad]
    public static class VerifyEmotionBlocks
    {
        const string Key="Emergence.BlockCheck";
        static RobotEmotionGameplay player;static RobotMotor2D motor;static Rigidbody2D body;
        static GameObject block;static Tilemap map;static EmotionBlockMap rules;static EmotionBlockTile[] tiles;
        static int phase;static float started;static readonly List<string> results=new List<string>();
        static VerifyEmotionBlocks(){EditorApplication.playModeStateChanged+=Mode;}
        [MenuItem("Tools/Emergence/Verify Four Emotion Blocks")]
        public static void Run(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop play first");SessionState.SetBool(Key,true);SessionState.SetBool(Key+"Background",Application.runInBackground);EditorApplication.isPlaying=true;}
        static void Mode(PlayModeStateChange state){
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false)){Application.runInBackground=true;phase=0;started=Time.time;results.Clear();EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.ExitingPlayMode)EditorApplication.update-=Tick;
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key,false)){Application.runInBackground=SessionState.GetBool(Key+"Background",false);SessionState.SetBool(Key,false);}
        }
        static void Check(bool value,string text){if(!value)throw new Exception(text);results.Add("PASS: "+text);}
        static void Next(){phase++;started=Time.time;}
        static void Tick(){try{
            if(Time.time-started>10)throw new Exception("Timeout "+phase);
            if(phase==0){
                if(DialogueDirector.Instance)DialogueDirector.Instance.Stop();
                player=UnityEngine.Object.FindObjectOfType<RobotEmotionGameplay>();Check(player,"Player available");motor=player.GetComponent<RobotMotor2D>();body=player.GetComponent<Rigidbody2D>();player.keyboardInput=false;player.GetComponent<RobotKeyboardInput>().enabled=false;
                tiles=new EmotionBlockTile[4];string[] names={"anger","sadness","reinforced","electric"};
                for(int i=0;i<4;i++){tiles[i]=AssetDatabase.LoadAssetAtPath<EmotionBlockTile>(SetupEmotionBlocks.Dir+"Tiles/"+names[i]+".asset");Check(tiles[i] && tiles[i].sprite.rect.width==64 && tiles[i].sprite.pixelsPerUnit==64,"64px tile configured: "+names[i]);}
                foreach(RobotEmotion e in new[]{RobotEmotion.Blank,RobotEmotion.Joy,RobotEmotion.Anger,RobotEmotion.Sadness,RobotEmotion.StrongAnger,RobotEmotion.Courage}){
                    Check(player.TrySetEmotion(e),"Expression accepted: "+e);
                    for(int k=0;k<3;k++){
                        var go=new GameObject("RuleFixture");var b=go.AddComponent<EmotionBlock>();b.kind=(EmotionBlockKind)k;
                        bool expected=(k==0 && (e==RobotEmotion.Anger || e==RobotEmotion.StrongAnger)) || (k==1 && e==RobotEmotion.Sadness) || (k==2 && e==RobotEmotion.StrongAnger);
                        Check(b.TryInteract(player)==expected,"Block permission "+e+" / "+(EmotionBlockKind)k);UnityEngine.Object.Destroy(go);
                    }
                }
                var floor=new GameObject("BlockTestFloor");floor.transform.position=new Vector3(1000,-.5f);floor.AddComponent<BoxCollider2D>().size=new Vector2(30,1);
                var checkpoint=new GameObject("Checkpoint");checkpoint.transform.position=new Vector3(1005,.03f);player.SetRespawnPoint(checkpoint.transform);
                body.position=new Vector2(1000,.03f);body.velocity=Vector2.zero;
                block=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SetupEmotionBlocks.Dir+"Prefabs/Block_sadness.prefab"));block.transform.position=new Vector3(1000.85f,.5f);player.TrySetEmotion(RobotEmotion.Joy);motor.SetMoveInput(1);Physics2D.SyncTransforms();Next();return;
            }
            if(phase==1){if(Time.time-started<.4f)return;Check(block.activeSelf,"Wrong emotion preserves touched blue block");player.TrySetEmotion(RobotEmotion.Sadness);motor.SetMoveInput(0);Next();return;}
            if(phase==2){if(Time.time-started<.7f)return;Check(block.activeSelf && !block.GetComponent<Collider2D>().enabled && Mathf.Approximately(block.GetComponent<SpriteRenderer>().color.a,.3f),"Sadness fades and disables already touching blue block");
                var grid=new GameObject("BlockTestGrid",typeof(Grid));var child=new GameObject("Tiles",typeof(Tilemap),typeof(TilemapCollider2D),typeof(EmotionBlockMap));child.transform.SetParent(grid.transform,false);map=child.GetComponent<Tilemap>();rules=child.GetComponent<EmotionBlockMap>();
                for(int i=0;i<4;i++)map.SetTile(new Vector3Int(i,0,0),tiles[i]);bool respawned;
                player.TrySetEmotion(RobotEmotion.Anger);Check(!rules.TryInteractCell(new Vector3Int(2,0,0),player,out respawned),"Normal anger cannot remove hard tile");
                player.TrySetEmotion(RobotEmotion.StrongAnger);Check(rules.TryInteractCell(new Vector3Int(2,0,0),player,out respawned) && map.HasTile(Vector3Int.zero),"Strong anger removes single hard cell only");Check(player.CanBreak,"Strong anger also supports legacy breakable blocks");
                player.TrySetEmotion(RobotEmotion.Sadness);Check(rules.TryInteractCell(new Vector3Int(1,0,0),player,out respawned),"Sadness removes blue tile");
                player.TrySetEmotion(RobotEmotion.Courage);var before=body.position;Check(!rules.TryInteractCell(new Vector3Int(3,0,0),player,out respawned) && body.position==before && map.HasTile(new Vector3Int(3,0,0)),"Courage safely touches electric tile; tile remains solid");
                player.TrySetEmotion(RobotEmotion.Joy);Check(rules.TryInteractCell(new Vector3Int(3,0,0),player,out respawned) && respawned && Vector2.Distance(body.position,player.respawnPoint.position)<.01f && body.velocity==Vector2.zero,"Electric tile respawns unsafe player and clears velocity");
                Check(!player.HandleElectricContact(),"Respawn grace prevents repeated shock");Next();return;
            }
            if(phase==3){if(Time.time-started<1.2f)return;
                body.position=new Vector2(1000,.03f);body.velocity=Vector2.zero;UnityEngine.Object.Destroy(block);block=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SetupEmotionBlocks.Dir+"Prefabs/Block_electric.prefab"));block.transform.position=new Vector3(1000.85f,.5f);player.TrySetEmotion(RobotEmotion.Courage);motor.SetMoveInput(1);Physics2D.SyncTransforms();Next();return;
            }
            if(phase==4){if(Time.time-started<.4f)return;Check(body.position.x<1001,"Physical electric contact is safe with courage");player.TrySetEmotion(RobotEmotion.Anger);motor.SetMoveInput(0);Next();return;}
            if(phase==5){if(Time.time-started<.2f)return;Check(body.position.x>1004,"Leaving courage while touching electric block triggers respawn");player.TrySetEmotion(RobotEmotion.Joy);Check(player.BeginSwitch() && !player.CanUseEmotionAbilities,"Charge suspends new block abilities");Next();return;}
            if(phase>=6 && phase<=10){if(Time.time-started<1.2f)return;var cycle=new[]{RobotEmotion.Anger,RobotEmotion.Sadness,RobotEmotion.StrongAnger,RobotEmotion.Courage,RobotEmotion.Joy};Check(!player.IsSwitching && player.CurrentEmotion==cycle[phase-6],"Automatic Q cycle reaches "+cycle[phase-6]);if(phase==10){Finish(null);return;}Check(player.BeginSwitch(),"Next grounded charge starts");Next();}
        }catch(Exception e){Finish("FAIL: "+e.Message);}}
        static void Finish(string failure){if(failure!=null)results.Add(failure);Directory.CreateDirectory("Logs/EmotionBlocks");File.WriteAllLines("Logs/EmotionBlocks/verification.txt",results);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;Debug.Log(string.Join("\n",results));}
    }
}
