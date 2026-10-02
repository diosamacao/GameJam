using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace Emergence.Level01.Editor
{
    public static class PreparePlayableLab
    {
        const string Base="Assets/EmergenceLevel01/";
        const string Prefabs=Base+"Prefabs/";
        static Sprite Sprite(string group,string name) { return AssetDatabase.LoadAssetAtPath<Sprite>(Base+"Art/"+group+"/"+name+".png"); }
        [MenuItem("Tools/Emergence/Prepare Playable Level 01")]
        public static void Prepare()
        {
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=Base+"Generated/Level01_InitialLab.unity" || Application.isPlaying) throw new InvalidOperationException("Open Level01_InitialLab in edit mode first.");
            if(UnityEngine.Object.FindObjectOfType<RobotKeyboardInput>()) { Debug.Log("Playable lab already configured.");return; }
            Directory.CreateDirectory("Logs/Level01Movement");
            EditorSceneManager.SaveScene(scene);
            File.Copy(scene.path,"Logs/Level01Movement/Level01_before_movement.unity",false);
            File.Copy(Base+"Generated/Level01_Laboratory.prefab","Logs/Level01Movement/Level01_before_movement.prefab",false);
            Directory.CreateDirectory(Prefabs+"Equipment");Directory.CreateDirectory(Prefabs+"Environment");Directory.CreateDirectory(Prefabs+"Characters");AssetDatabase.Refresh();
            var root=GameObject.Find("Level01_InitialLaboratory").transform;
            if(PrefabUtility.IsPartOfPrefabInstance(root)) PrefabUtility.UnpackPrefabInstance(root.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Base+"Generated/LabSpriteUnlit.mat");
            for(int i=0;i<3;i++) Rebase(root.Find("SleepPod"+i),new Vector3((64+40*i)/16f,1.5f,0));
            var sleep=root.Find("SleepPod0");
            var sleepAsset=PrefabUtility.SaveAsPrefabAssetAndConnect(sleep.gameObject,Prefabs+"Equipment/SleepChamber.prefab",InteractionMode.AutomatedAction);
            for(int i=1;i<3;i++) Replace(root.Find("SleepPod"+i),sleepAsset);
            var empty=root.Find("ExperimentPod0");UnityEngine.Object.DestroyImmediate(empty.Find("robot_idle").gameObject);
            Rebase(empty,new Vector3(426/16f,1.5f,0));
            var experimentAsset=PrefabUtility.SaveAsPrefabAssetAndConnect(empty.gameObject,Prefabs+"Equipment/ExperimentChamber.prefab",InteractionMode.AutomatedAction);
            var occupied=root.Find("ExperimentPod1");var companion=occupied.Find("companion_idle");companion.SetParent(root,true);
            Rebase(occupied,new Vector3(466/16f,1.5f,0));var newOccupied=Replace(occupied,experimentAsset);companion.SetParent(newOccupied,true);
            Export(root.Find("Terminal"),"Equipment/Terminal",new Vector3(300/16f,76/16f,0));
            Export(root.Find("Scientist"),"Characters/Scientist",new Vector3(308/16f,1.5f,0));
            Export(root.Find("ExitDoor"),"Equipment/VerticalDoor",new Vector3(592/16f,1.5f,0));
            var data=root.Find("DataLines");for(int i=0;i<5;i++)root.Find("Packet_"+i).SetParent(data,true);
            Export(data,"Equipment/DataNetwork",Vector3.zero);
            var shell=new GameObject("LabShell").transform;shell.SetParent(root,false);
            foreach(string n in new[]{"Environment","Ground","LeftBoundary","RightBoundary","CeilingBoundary"})root.Find(n).SetParent(shell,true);
            Export(shell,"Environment/LabShell",Vector3.zero);
            foreach(string tile in new[]{"wall_plain","wall_bolts","floor","ceiling","wall_left","wall_right","corner","foundation"})
            {
                var go=new GameObject(tile);var sr=go.AddComponent<SpriteRenderer>();sr.sprite=Sprite("Tiles",tile);sr.sharedMaterial=mat;
                sr.sortingOrder=tile.StartsWith("wall_")&&tile!="wall_left"&&tile!="wall_right"?0:20;
                if(tile=="floor"||tile=="ceiling"||tile=="wall_left"||tile=="wall_right")go.AddComponent<BoxCollider2D>().size=sr.sprite.bounds.size;
                PrefabUtility.SaveAsPrefabAsset(go,Prefabs+"Environment/"+tile+".prefab");UnityEngine.Object.DestroyImmediate(go);
            }
            var player=new GameObject("PlayerRobot");player.tag="Player";
            var rb=player.AddComponent<Rigidbody2D>();rb.gravityScale=3;rb.freezeRotation=true;rb.interpolation=RigidbodyInterpolation2D.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            var physics=new PhysicsMaterial2D("RobotNoFriction"){friction=0,bounciness=0};AssetDatabase.CreateAsset(physics,Base+"Generated/RobotNoFriction.physicsMaterial2D");
            var box=player.AddComponent<BoxCollider2D>();box.size=new Vector2(.7f,1.6f);box.offset=new Vector2(0,.8f);box.sharedMaterial=physics;
            var visual=new GameObject("Visual");visual.transform.SetParent(player.transform,false);var renderer=visual.AddComponent<SpriteRenderer>();renderer.sprite=Sprite("Characters","robot_idle");renderer.sharedMaterial=mat;renderer.sortingOrder=15;
            var motor=player.AddComponent<RobotMotor2D>();motor.visual=renderer;motor.idle=renderer.sprite;motor.walk=new Sprite[4];for(int i=0;i<4;i++)motor.walk[i]=Sprite("Characters","robot_walk_"+i);
            player.AddComponent<RobotKeyboardInput>();
            var playerAsset=PrefabUtility.SaveAsPrefabAsset(player,Prefabs+"Characters/PlayerRobot.prefab");UnityEngine.Object.DestroyImmediate(player);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(playerAsset,root);instance.transform.position=root.Find("PlayerSpawn").position+Vector3.up*.05f;
            PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject,Base+"Generated/Level01_Laboratory.prefab",InteractionMode.AutomatedAction);
            GameObject.Find("Level01_Camera").tag="MainCamera";
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/Level01Movement/setup.txt","Player A/D; speed=5; 16 modular prefabs; original scene/prefab backed up.\n"+DateTime.UtcNow.ToString("O"));
            Debug.Log("PLAYABLE_LAB_READY: A/D movement and reusable prefabs created.");
        }
        static void Rebase(Transform group,Vector3 origin)
        {
            var children=new Transform[group.childCount];var positions=new Vector3[children.Length];
            for(int i=0;i<children.Length;i++){children[i]=group.GetChild(i);positions[i]=children[i].position;}
            var offset=origin-group.position;group.position=origin;
            foreach(var c in group.GetComponents<Collider2D>())c.offset-=(Vector2)offset;
            for(int i=0;i<children.Length;i++)children[i].position=positions[i];
        }
        static void Export(Transform group,string name,Vector3 origin)
        {Rebase(group,origin);PrefabUtility.SaveAsPrefabAssetAndConnect(group.gameObject,Prefabs+name+".prefab",InteractionMode.AutomatedAction);}
        static Transform Replace(Transform original,GameObject asset)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,original.parent);go.name=original.name;go.transform.SetPositionAndRotation(original.position,original.rotation);go.transform.localScale=original.localScale;
            UnityEngine.Object.DestroyImmediate(original.gameObject);return go.transform;
        }
    }
}
