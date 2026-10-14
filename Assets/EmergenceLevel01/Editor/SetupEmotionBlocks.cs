using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEditor.SceneManagement;
namespace Emergence.Level01.Editor
{
    public static class SetupEmotionBlocks
    {
        const string Root="Assets/EmergenceLevel01/";
        public const string Dir=Root+"Tilemap64/EmotionBlocks/";
        static readonly string[] Names={"anger","sadness","reinforced","electric"};
        [MenuItem("Tools/Emergence/Setup Four Emotion Blocks")]
        public static void Build(){
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Stop play mode first");
            Directory.CreateDirectory(Dir+"Tiles");Directory.CreateDirectory(Dir+"Prefabs");Directory.CreateDirectory(Dir+"Palettes");AssetDatabase.Refresh();
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"Generated/LabSpriteUnlit.mat");var tiles=new EmotionBlockTile[4];
            for(int i=0;i<4;i++){
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/Tiles64/EmotionBlocks/"+Names[i]+".png");
                if(!sprite || sprite.rect.width!=64 || sprite.pixelsPerUnit!=64)throw new System.Exception("Invalid block sprite "+Names[i]);
                string path=Dir+"Tiles/"+Names[i]+".asset";var tile=AssetDatabase.LoadAssetAtPath<EmotionBlockTile>(path);
                if(!tile){tile=ScriptableObject.CreateInstance<EmotionBlockTile>();AssetDatabase.CreateAsset(tile,path);}
                tile.kind=(EmotionBlockKind)i;tile.sprite=sprite;tile.colliderType=Tile.ColliderType.Grid;tile.color=Color.white;EditorUtility.SetDirty(tile);tiles[i]=tile;
                var go=new GameObject("Block_"+Names[i]);try{
                    var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=material;sr.sortingOrder=20;
                    go.AddComponent<BoxCollider2D>().size=Vector2.one;go.AddComponent<EmotionBlock>().kind=(EmotionBlockKind)i;
                    PrefabUtility.SaveAsPrefabAsset(go,Dir+"Prefabs/"+go.name+".prefab");
                }finally{Object.DestroyImmediate(go);}
            }
            string palettePath=Dir+"Palettes/EmotionBlocks_64x64.prefab";
            if(!File.Exists(palettePath))GridPaletteUtility.CreateNewPalette(Dir+"Palettes","EmotionBlocks_64x64",GridLayout.CellLayout.Rectangle,GridPalette.CellSizing.Manual,Vector3.one,GridLayout.CellSwizzle.XYZ);
            var palette=PrefabUtility.LoadPrefabContents(palettePath);try{
                var map=palette.GetComponentInChildren<Tilemap>();map.ClearAllTiles();map.GetComponent<TilemapRenderer>().sharedMaterial=material;
                for(int i=0;i<4;i++)map.SetTile(new Vector3Int(i,0,0),tiles[i]);PrefabUtility.SaveAsPrefabAsset(palette,palettePath);
            }finally{PrefabUtility.UnloadPrefabContents(palette);}
            var template=new GameObject("EmotionBlocksGrid",typeof(Grid));try{
                var map=CreateLayer(template.transform,material);for(int i=0;i<4;i++)map.SetTile(new Vector3Int(i,0,0),tiles[i]);
                PrefabUtility.SaveAsPrefabAsset(template,Dir+"Prefabs/EmotionBlocksGrid.prefab");
            }finally{Object.DestroyImmediate(template);}
            var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"})){
                var path=AssetDatabase.GUIDToAssetPath(guid);if(path.Contains("/Backup/") || !Path.GetFileName(path).StartsWith("Level"))continue;
                var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                Grid grid=null;foreach(var root in scene.GetRootGameObjects()){
                    if(!grid)grid=root.GetComponentInChildren<Grid>();
                    foreach(var map in root.GetComponentsInChildren<Tilemap>(true))if(map.GetComponent<TilemapCollider2D>() && !map.GetComponent<EmotionBlockMap>())map.gameObject.AddComponent<EmotionBlockMap>();
                }
                if(grid && !grid.transform.Find("EmotionBlocks"))CreateLayer(grid.transform,material);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
            }
            if(active.IsValid() && active.isLoaded)UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);
            AssetDatabase.SaveAssets();ConfigureEmotionBlockLayers.Build();Debug.Log("FOUR_EMOTION_BLOCKS_READY");
        }
        static Tilemap CreateLayer(Transform parent,Material material){
            var go=new GameObject("EmotionBlocks",typeof(Tilemap),typeof(TilemapRenderer),typeof(TilemapCollider2D),typeof(EmotionBlockMap));go.transform.SetParent(parent,false);
            var renderer=go.GetComponent<TilemapRenderer>();renderer.sortingOrder=20;renderer.sharedMaterial=material;return go.GetComponent<Tilemap>();
        }
    }
}
