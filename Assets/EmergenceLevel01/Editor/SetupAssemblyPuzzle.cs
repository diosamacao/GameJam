using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Emergence.Level01.Editor
{
    public static class SetupAssemblyPuzzle
    {
        const string Base="Assets/EmergenceLevel01/";
        [MenuItem("Tools/Emergence/Setup Door Assembly Puzzle")]
        public static void Setup()
        {
            var scene=SceneManager.GetActiveScene();
            if(Application.isPlaying || scene.path!=Base+"Generated/Level01_InitialLab.unity")throw new InvalidOperationException("Open Level01_InitialLab in edit mode.");
            if(UnityEngine.Object.FindObjectOfType<AssemblyInteraction>()){Debug.Log("Door assembly puzzle already exists.");return;}
            Directory.CreateDirectory("Logs/AssemblyPuzzle");EditorSceneManager.SaveScene(scene);
            string stamp=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");File.Copy(scene.path,"Logs/AssemblyPuzzle/scene-before-"+stamp+".unity");
            File.Copy(Base+"Generated/Level01_Laboratory.prefab","Logs/AssemblyPuzzle/lab-before-"+stamp+".prefab");
            Directory.CreateDirectory(Base+"Prefabs/Puzzles");AssetDatabase.Refresh();
            var root=GameObject.Find("Level01_InitialLaboratory").transform;
            if(PrefabUtility.IsPartOfPrefabInstance(root))PrefabUtility.UnpackPrefabInstance(root.gameObject,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
            var existingDoor=root.Find("ExitDoor").gameObject;
            if(PrefabUtility.IsPartOfPrefabInstance(existingDoor))PrefabUtility.UnpackPrefabInstance(existingDoor,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
            var door=existingDoor.GetComponent<LabDoor>();var puzzle=existingDoor.AddComponent<AssemblyDoor>();puzzle.door=door;
            puzzle.indicator=existingDoor.transform.Find("door_switch").GetComponent<SpriteRenderer>();
            puzzle.interactionArea=existingDoor.AddComponent<BoxCollider2D>();puzzle.interactionArea.isTrigger=true;puzzle.interactionArea.size=new Vector2(2.8f,4);puzzle.interactionArea.offset=new Vector2(.2f,2);
            puzzle.sockets=new AssemblySocket[2];
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Base+"Generated/LabSpriteUnlit.mat");
            for(int i=0;i<2;i++)
            {
                var panel=(i==0?door.upper:door.lower).GetComponent<SpriteRenderer>();panel.enabled=false;
                var slotObject=new GameObject(i==0?"UpperSocket":"LowerSocket");slotObject.transform.SetParent(existingDoor.transform,false);slotObject.transform.position=panel.transform.position;
                var slot=slotObject.AddComponent<AssemblySocket>();slot.acceptedPartId=i==0?"door_upper":"door_lower";slot.installedVisual=panel;
                slot.hitArea=slotObject.AddComponent<BoxCollider2D>();slot.hitArea.size=new Vector2(1.5f,1.625f);slot.hitArea.isTrigger=true;slot.hitArea.enabled=false;
                slot.outline=slotObject.AddComponent<AssemblyOutline>();slot.outline.material=mat;slot.outline.size=slot.hitArea.size;puzzle.sockets[i]=slot;
            }
            var doorAsset=PrefabUtility.SaveAsPrefabAssetAndConnect(existingDoor,Base+"Prefabs/Puzzles/AssemblyDoor.prefab",InteractionMode.AutomatedAction);
            var system=new GameObject("AssemblyPuzzle");system.transform.SetParent(root,false);var interaction=system.AddComponent<AssemblyInteraction>();interaction.worldCamera=Camera.main;interaction.targetDoor=puzzle;interaction.pieces=new AssemblyPiece[2];
            for(int i=0;i<2;i++)
            {
                string id=i==0?"door_upper":"door_lower";var obj=new GameObject(i==0?"UpperDoorPiece":"LowerDoorPiece");
                var p=obj.AddComponent<AssemblyPiece>();p.partId=id;p.visual=obj.AddComponent<SpriteRenderer>();p.visual.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Base+"Art/Props/"+id+".png");p.visual.sharedMaterial=mat;p.visual.sortingOrder=16;
                p.hitArea=obj.AddComponent<BoxCollider2D>();p.hitArea.size=new Vector2(1.65f,1.8f);p.hitArea.isTrigger=true;
                p.outline=obj.AddComponent<AssemblyOutline>();p.outline.material=mat;p.outline.size=p.hitArea.size;p.outline.dashed=false;
                var prefab=PrefabUtility.SaveAsPrefabAsset(obj,Base+"Prefabs/Puzzles/"+obj.name+".prefab");UnityEngine.Object.DestroyImmediate(obj);
                var placed=(GameObject)PrefabUtility.InstantiatePrefab(prefab,system.transform);placed.transform.localPosition=new Vector3(i==0?12f:31f,2.45f,0);interaction.pieces[i]=placed.GetComponent<AssemblyPiece>();
            }
            var socketTemplate=new GameObject("AssemblySocket");var socketComponent=socketTemplate.AddComponent<AssemblySocket>();socketComponent.acceptedPartId="door_upper";
            socketComponent.hitArea=socketTemplate.AddComponent<BoxCollider2D>();socketComponent.hitArea.isTrigger=true;socketComponent.hitArea.size=new Vector2(1.5f,1.625f);socketComponent.hitArea.enabled=false;
            socketComponent.outline=socketTemplate.AddComponent<AssemblyOutline>();socketComponent.outline.material=mat;PrefabUtility.SaveAsPrefabAsset(socketTemplate,Base+"Prefabs/Puzzles/AssemblySocket.prefab");UnityEngine.Object.DestroyImmediate(socketTemplate);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject,Base+"Generated/Level01_Laboratory.prefab",InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/AssemblyPuzzle/setup.txt","Door assembly puzzle installed. 2 collectible panels; matching dashed sockets; click completed door to open/close.");Debug.Log("ASSEMBLY_PUZZLE_READY");
        }
    }
}
