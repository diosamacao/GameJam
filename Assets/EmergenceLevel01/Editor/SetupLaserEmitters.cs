using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEditor.SceneManagement;
namespace Emergence.Level01.Editor
{
    public static class SetupLaserEmitters
    {
        public const string Root="Assets/EmergenceLevel01/";
        public const string Dir=Root+"Tilemap64/Lasers/";
        [MenuItem("Tools/Emergence/Setup Laser Emitters")]
        public static void Build(){
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Stop play first");
            Directory.CreateDirectory(Dir+"Tiles");Directory.CreateDirectory(Dir+"Prefabs");Directory.CreateDirectory(Dir+"Palettes");AssetDatabase.Refresh();
            string art=Root+"Art/Tiles64/Lasers/emitter.png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(art);importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=64;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.SaveAndReimport();
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(art);var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"Generated/LabSpriteUnlit.mat");
            string[] names={"Right","Up","Left","Down"};Vector2[] directions={Vector2.right,Vector2.up,Vector2.left,Vector2.down};var tiles=new LaserEmitterTile[4];
            for(int i=0;i<4;i++){
                var go=new GameObject("LaserEmitter_"+names[i]);try{
                    var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=material;sr.sortingOrder=20;
                    // Rotate the visual independently; emitter direction is explicit.
                    if(i!=0){var child=new GameObject("Housing");child.transform.SetParent(go.transform,false);child.transform.localRotation=Quaternion.Euler(0,0,i*90);var visual=child.AddComponent<SpriteRenderer>();visual.sprite=sprite;visual.sharedMaterial=material;visual.sortingOrder=20;Object.DestroyImmediate(sr);}
                    go.AddComponent<BoxCollider2D>().size=Vector2.one;
                    var emitter=go.AddComponent<LaserEmitter>();emitter.direction=directions[i];emitter.beamMaterial=material;
                    var prefab=PrefabUtility.SaveAsPrefabAsset(go,Dir+"Prefabs/"+go.name+".prefab");
                    string path=Dir+"Tiles/Laser_"+names[i]+".asset";var tile=AssetDatabase.LoadAssetAtPath<LaserEmitterTile>(path);if(!tile){tile=ScriptableObject.CreateInstance<LaserEmitterTile>();AssetDatabase.CreateAsset(tile,path);}
                    tile.sprite=sprite;tile.transform=Matrix4x4.Rotate(Quaternion.Euler(0,0,i*90));tile.colliderType=Tile.ColliderType.None;tile.emitterPrefab=prefab;EditorUtility.SetDirty(tile);tiles[i]=tile;
                }finally{Object.DestroyImmediate(go);}
            }
            string palettePath=Dir+"Palettes/Lasers_64x64.prefab";
            if(!File.Exists(palettePath))GridPaletteUtility.CreateNewPalette(Dir+"Palettes","Lasers_64x64",GridLayout.CellLayout.Rectangle,GridPalette.CellSizing.Manual,Vector3.one,GridLayout.CellSwizzle.XYZ);
            var palette=PrefabUtility.LoadPrefabContents(palettePath);try{var map=palette.GetComponentInChildren<Tilemap>();map.ClearAllTiles();map.GetComponent<TilemapRenderer>().sharedMaterial=material;for(int i=0;i<4;i++)map.SetTile(new Vector3Int(i,0,0),tiles[i]);PrefabUtility.SaveAsPrefabAsset(palette,palettePath);}finally{PrefabUtility.UnloadPrefabContents(palette);}
            var template=new GameObject("LaserGrid",typeof(Grid));try{var map=Layer(template.GetComponent<Grid>(),material);for(int i=0;i<4;i++)map.SetTile(new Vector3Int(i*3,0,0),tiles[i]);PrefabUtility.SaveAsPrefabAsset(template,Dir+"Prefabs/LaserGrid.prefab");}finally{Object.DestroyImmediate(template);}
            var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"})){
                string path=AssetDatabase.GUIDToAssetPath(guid);if(path.Contains("/Backup/") || !Path.GetFileName(path).StartsWith("Level"))continue;
                var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                foreach(var root in scene.GetRootGameObjects())foreach(var grid in root.GetComponentsInChildren<Grid>())Layer(grid,material);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
            }
            if(active.IsValid() && active.isLoaded)UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);AssetDatabase.SaveAssets();Debug.Log("LASERS_READY");
        }
        static Tilemap Layer(Grid grid,Material material){
            var child=grid.transform.Find("Hazard_Lasers");if(!child){var go=new GameObject("Hazard_Lasers",typeof(Tilemap),typeof(TilemapRenderer),typeof(LaserEmitterMap));child=go.transform;child.SetParent(grid.transform,false);}
            var renderer=child.GetComponent<TilemapRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=20;return child.GetComponent<Tilemap>();
        }
    }
}
