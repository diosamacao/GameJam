using System.IO;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace Emergence.Level01.Editor
{
    public static class SetupMemoryBlocks
    {
        public const string Dir="Assets/EmergenceLevel01/Tilemap64/MemoryBlocks/";
        [MenuItem("Tools/Emergence/Setup Memory Blocks")]
        public static void Build(){
            Directory.CreateDirectory(Dir+"Tiles");Directory.CreateDirectory(Dir+"Palettes");Directory.CreateDirectory(Dir+"Prefabs");AssetDatabase.Refresh();
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/EmergenceLevel01/Generated/LabSpriteUnlit.mat");
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EmergenceLevel01/Art/Tiles64/EmotionBlocks/sadness.png");
            var names=new[]{"MemoryAppear","MemoryDisappear"};var tiles=new MemoryBlockTile[2];
            for(int i=0;i<2;i++){
                string path=Dir+"Tiles/"+names[i]+".asset";var tile=AssetDatabase.LoadAssetAtPath<MemoryBlockTile>(path);
                if(!tile){tile=ScriptableObject.CreateInstance<MemoryBlockTile>();tile.sprite=sprite;tile.visibleInMemory=i==0;tile.colliderType=Tile.ColliderType.Grid;tile.color=i==0?new Color(.75f,.5f,1):new Color(1,.65f,.8f);AssetDatabase.CreateAsset(tile,path);}tiles[i]=tile;
                string palettePath=Dir+"Palettes/"+names[i]+"_64x64.prefab";
                if(!File.Exists(palettePath)){GridPaletteUtility.CreateNewPalette(Dir+"Palettes",names[i]+"_64x64",GridLayout.CellLayout.Rectangle,GridPalette.CellSizing.Manual,Vector3.one,GridLayout.CellSwizzle.XYZ);var palette=PrefabUtility.LoadPrefabContents(palettePath);try{var map=palette.GetComponentInChildren<Tilemap>();map.SetTile(Vector3Int.zero,tile);map.GetComponent<TilemapRenderer>().sharedMaterial=material;PrefabUtility.SaveAsPrefabAsset(palette,palettePath);}finally{PrefabUtility.UnloadPrefabContents(palette);}}
            }
            string gridPath=Dir+"Prefabs/MemoryBlocksGrid.prefab";
            if(!File.Exists(gridPath)){var grid=new GameObject("MemoryBlocksGrid",typeof(Grid));try{for(int i=0;i<2;i++){var go=new GameObject(names[i],typeof(Tilemap),typeof(TilemapRenderer),typeof(TilemapCollider2D),typeof(MemoryBlockMap));go.transform.SetParent(grid.transform,false);go.GetComponent<TilemapRenderer>().sharedMaterial=material;go.GetComponent<TilemapRenderer>().sortingOrder=20;go.GetComponent<Tilemap>().SetTile(new Vector3Int(i*2,0,0),tiles[i]);}PrefabUtility.SaveAsPrefabAsset(grid,gridPath);}finally{Object.DestroyImmediate(grid);}}
            AssetDatabase.SaveAssets();
        }
    }
}
