using System.IO;
using UnityEditor;
using UnityEngine;
namespace Emergence.Level01.Editor
{
    public static class SetupInteractionUpgrade
    {
        const string Dir=SetupEmotionExpansion.Dir;
        [MenuItem("Tools/Emergence/Setup Interaction Upgrade")]
        public static void Build(){
            string path=Dir+"MovingPlatform.prefab";
            var platform=PrefabUtility.LoadPrefabContents(path);
            try{platform.GetComponent<LocalMovingPlatform>().ConfigureOneWay();PrefabUtility.SaveAsPrefabAsset(platform,path);}finally{PrefabUtility.UnloadPrefabContents(platform);}
            foreach(var mode in new[]{PlatformActivation.Single,PlatformActivation.Continuous}){
                string groupPath=Dir+mode+"PlatformSwitch.prefab";if(File.Exists(groupPath))continue;
                var group=new GameObject(mode+"PlatformSwitch");
                try{
                    var moving=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));moving.transform.SetParent(group.transform,false);moving.transform.localPosition=new Vector3(3,2,0);
                    var logic=moving.GetComponent<LocalMovingPlatform>();logic.activation=mode;
                    var plate=new GameObject("PressureSwitch");plate.transform.SetParent(group.transform,false);
                    var trigger=plate.AddComponent<BoxCollider2D>();trigger.isTrigger=true;trigger.size=new Vector2(1,.15f);trigger.offset=new Vector2(0,.075f);
                    var control=plate.AddComponent<PlatformPressureSwitch>();control.platforms=new[]{logic};
                    var visual=new GameObject("Plate");visual.transform.SetParent(plate.transform,false);visual.transform.localPosition=new Vector3(0,.025f);visual.transform.localScale=new Vector3(1,.05f,1);
                    var sr=visual.AddComponent<SpriteRenderer>();var original=moving.GetComponent<SpriteRenderer>();sr.sprite=original.sprite;sr.sharedMaterial=original.sharedMaterial;sr.sortingOrder=25;sr.color=control.idleColor;control.indicator=sr;
                    PrefabUtility.SaveAsPrefabAsset(group,groupPath);
                }finally{Object.DestroyImmediate(group);}
            }
            string portalPath=Dir+"InteractionPortal.prefab";
            if(!File.Exists(portalPath)){
                var portal=new GameObject("InteractionPortal");
                try{
                    var area=portal.AddComponent<BoxCollider2D>();area.isTrigger=true;area.size=new Vector2(1.2f,2);area.offset=new Vector2(0,1);
                    portal.AddComponent<InteractionPortal>();
                    var line=portal.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=4;line.SetPositions(new[]{new Vector3(-.6f,0),new Vector3(-.6f,2),new Vector3(.6f,2),new Vector3(.6f,0)});line.startWidth=line.endWidth=.08f;line.startColor=line.endColor=Color.cyan;line.sortingOrder=25;line.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/EmergenceLevel01/Generated/LabSpriteUnlit.mat");
                    PrefabUtility.SaveAsPrefabAsset(portal,portalPath);
                }finally{Object.DestroyImmediate(portal);}
            }
            AssetDatabase.SaveAssets();Debug.Log("INTERACTION_UPGRADE_READY");
        }
    }
}
