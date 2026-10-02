using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Emergence.Level01.Editor
{
    public static class BuildLabScene
    {
        const string Base = "Assets/EmergenceLevel01/";
        [Serializable] public class Item { public string name, group, parent; public float x,y,w,h,alpha; public int order; }
        [Serializable] public class Point { public float x,y; }
        [Serializable] public class Path { public string color; public Point[] points; }
        [Serializable] public class Layout { public int width,height,ppu; public Item[] items; public Path[] paths; }
        static Sprite Art(string group, string name)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(Base + "Art/" + group + "/" + name + ".png");
            if (!s) throw new InvalidOperationException("Missing sprite: " + group + "/" + name);
            return s;
        }
        [MenuItem("Tools/Emergence/Build Level 01 Art Scene")]
        public static void Build()
        {
            foreach (var file in Directory.GetFiles(Base + "Art", "*.png", SearchOption.AllDirectories))
                AssetDatabase.ImportAsset(file.Replace('\\','/'), ImportAssetOptions.ForceUpdate);
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(Base + "layout.json"));
            // Validate all art before creating a scene. Existing open scenes remain open.
            foreach (var item in layout.items) Art(item.group,item.name);
            string folder = AssetDatabase.GenerateUniqueAssetPath(Base + "Generated");
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (!shader) shader = Shader.Find("Sprites/Default");
            var material = new Material(shader) { name = "LabSpriteUnlit" };
            AssetDatabase.CreateAsset(material, folder + "/LabSpriteUnlit.mat");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var root = new GameObject("Level01_InitialLaboratory");
            var groups = new Dictionary<string, Transform>();
            var objects = new Dictionary<string, GameObject>();
            foreach (var a in layout.items)
            {
                if (!groups.ContainsKey(a.parent)) { var g = new GameObject(a.parent);g.transform.SetParent(root.transform,false);groups.Add(a.parent,g.transform); }
                var go = new GameObject(a.name);go.transform.SetParent(groups[a.parent],false);
                var sr = go.AddComponent<SpriteRenderer>();sr.sprite=Art(a.group,a.name);sr.sortingOrder=a.order;
                sr.sharedMaterial=material;
                sr.color = new Color(1,1,1,a.alpha);
                go.transform.localPosition = new Vector3((a.x+a.w*.5f)/16f,(a.y+(a.group=="Characters"?0:a.h*.5f))/16f,0);
                go.transform.localScale = new Vector3(a.w/sr.sprite.rect.width,a.h/sr.sprite.rect.height,1);
                objects[a.parent+"/"+a.name]=go;
            }
            foreach (var group in groups)
            {
                string glassName = group.Key.StartsWith("SleepPod") ? "pod_sleep_glass" : group.Key.StartsWith("ExperimentPod") ? "pod_experiment_glass" : null;
                if (glassName != null) group.Value.gameObject.AddComponent<LabChamber>().glass = objects[group.Key+"/"+glassName].GetComponent<SpriteRenderer>();
            }
            int pathIndex=0;
            foreach (var p in layout.paths)
            {
                var go=new GameObject("Packet_"+pathIndex);go.transform.SetParent(root.transform,false);
                var sr=go.AddComponent<SpriteRenderer>();sr.sprite=Art("Data","packet_"+p.color);sr.sortingOrder=4;
                sr.sharedMaterial=material;
                sr.color=p.color=="green"?new Color(.3f,1,.4f):new Color(.3f,.85f,1);
                var packet=go.AddComponent<LabDataPacket>();packet.phase=pathIndex++*.17f;
                packet.points=Array.ConvertAll(p.points,pnt=>new Vector3(pnt.x/16f,pnt.y/16f,0));go.transform.localPosition=packet.points[0];
            }
            var door=groups["ExitDoor"].gameObject.AddComponent<LabDoor>();
            door.upper=objects["ExitDoor/door_upper"].transform;door.lower=objects["ExitDoor/door_lower"].transform;
            door.blocker=groups["ExitDoor"].gameObject.AddComponent<BoxCollider2D>();door.blocker.offset=new Vector2(592/16f,56/16f);door.blocker.size=new Vector2(24/16f,64/16f);
            var maskObject=new GameObject("DoorTravelMask");maskObject.transform.SetParent(groups["ExitDoor"],false);
            maskObject.transform.localPosition=new Vector3(592/16f,56/16f,0);maskObject.transform.localScale=new Vector3(24/64f,52/64f,1);
            var mask=maskObject.AddComponent<SpriteMask>();mask.sprite=Art("Tiles","wall_plain");mask.isCustomRangeActive=true;mask.frontSortingOrder=11;mask.backSortingOrder=9;
            door.upper.GetComponent<SpriteRenderer>().maskInteraction=SpriteMaskInteraction.VisibleInsideMask;
            door.lower.GetComponent<SpriteRenderer>().maskInteraction=SpriteMaskInteraction.VisibleInsideMask;
            Collider(root.transform,"Ground",new Vector2(20,20/16f),new Vector2(40,.5f));
            Collider(root.transform,"LeftBoundary",new Vector2(.25f,7.5f),new Vector2(.5f,15));
            Collider(root.transform,"RightBoundary",new Vector2(39.75f,7.5f),new Vector2(.5f,15));
            Collider(root.transform,"CeilingBoundary",new Vector2(20,222/16f),new Vector2(40,.75f));
            var spawn=new GameObject("PlayerSpawn");spawn.transform.SetParent(root.transform,false);spawn.transform.localPosition=new Vector3(384/16f,24/16f,0);
            MakeAnimations(folder,objects);
            PrefabUtility.SaveAsPrefabAsset(root,folder+"/Level01_Laboratory.prefab");
            var cameraObject=new GameObject("Level01_Camera");var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;
            camera.depth=100;
            camera.orthographicSize=Mathf.Max(layout.height/32f,layout.width/32f/camera.aspect);
            camera.transform.position=new Vector3(layout.width/32f,layout.height/32f,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color32(15,23,33,255);
            EditorSceneManager.SaveScene(scene,folder+"/Level01_InitialLab.unity");AssetDatabase.SaveAssets();Selection.activeGameObject=root;
            Debug.Log("Level 01 art scene saved to "+folder+". Includes art, colliders and visual animations; gameplay/story logic is not included.");
        }
        static void Collider(Transform root,string name,Vector2 center,Vector2 size)
        { var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=center;go.AddComponent<BoxCollider2D>().size=size; }
        static AnimationClip Clip(string folder,string name,string[] frames,float fps,bool loop)
        {
            var clip=new AnimationClip { name=name,frameRate=fps };
            var keys=new ObjectReferenceKeyframe[frames.Length+1];
            for(int i=0;i<frames.Length;i++) keys[i]=new ObjectReferenceKeyframe { time=i/fps,value=Art("Characters",frames[i]) };
            keys[frames.Length]=new ObjectReferenceKeyframe { time=frames.Length/fps,value=Art("Characters",frames[frames.Length-1]) };
            AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding { path="",type=typeof(SpriteRenderer),propertyName="m_Sprite" },keys);
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
            AssetDatabase.CreateAsset(clip,folder+"/"+name+".anim");return clip;
        }
        static void MakeAnimations(string folder,Dictionary<string,GameObject> objects)
        {
            Clip(folder,"RobotIdle",new[]{"robot_idle"},1,true);
            Clip(folder,"RobotWalk",new[]{"robot_walk_0","robot_walk_1","robot_walk_2","robot_walk_3"},8,true);
            Clip(folder,"RobotJump",new[]{"robot_jump","robot_fall","robot_land"},6,false);
            Clip(folder,"RobotInteract",new[]{"robot_idle","robot_interact","robot_idle"},4,false);
            var work=Clip(folder,"ScientistWork",new[]{"scientist_work_0","scientist_work_1"},2,true);
            var controller=AnimatorController.CreateAnimatorControllerAtPath(folder+"/Scientist.controller");
            var state=controller.layers[0].stateMachine.AddState("Working");state.motion=work;
            controller.layers[0].stateMachine.defaultState=state;
            objects["Scientist/scientist_work_0"].AddComponent<Animator>().runtimeAnimatorController=controller;
        }
    }
}
