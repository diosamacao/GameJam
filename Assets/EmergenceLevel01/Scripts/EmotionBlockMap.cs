using UnityEngine;
using UnityEngine.Tilemaps;
namespace Emergence.Level01
{
    [RequireComponent(typeof(Tilemap))]
    public sealed class EmotionBlockMap : MonoBehaviour
    {
        Tilemap map;
        struct HiddenCell {public TileBase tile;public Color color;public TileFlags flags;public Tile.ColliderType collider;}
        readonly System.Collections.Generic.Dictionary<Vector3Int,HiddenCell> hidden=new System.Collections.Generic.Dictionary<Vector3Int,HiddenCell>();
        public bool TryInteractCell(Vector3Int cell,RobotEmotionGameplay player,out bool respawned)
        {
            respawned=false;
            if(!isActiveAndEnabled || !player || !player.CanUseEmotionAbilities)return false;
            if(!map)map=GetComponent<Tilemap>();
            var tile=map.GetTile(cell) as EmotionBlockTile;if(!tile)return false;
            if(hidden.ContainsKey(cell))return false;
            if(tile.kind==EmotionBlockKind.Electric){respawned=player.HandleElectricContact();return respawned;}
            if(!EmotionBlockRules.CanRemove(tile.kind,player.CurrentEmotion))return false;
            if(tile.kind==EmotionBlockKind.Vanishing){StartCoroutine(Vanish(cell,tile));return true;}
            var renderer=GetComponent<TilemapRenderer>();
            if(renderer)BlockShatterEffect.Spawn(tile.sprite,map.transform.localToWorldMatrix*Matrix4x4.Translate(map.CellToLocalInterpolated((Vector3)cell+map.tileAnchor))*map.GetTransformMatrix(cell),renderer.sharedMaterial,renderer.sortingLayerID,renderer.sortingOrder,map.GetColor(cell)*map.color);
            map.SetTile(cell,null);RefreshCollision();
            return true;
        }
        System.Collections.IEnumerator Vanish(Vector3Int cell,TileBase tile){
            var state=new HiddenCell{tile=tile,color=map.GetColor(cell),flags=map.GetTileFlags(cell),collider=map.GetColliderType(cell)};hidden.Add(cell,state);
            map.SetTileFlags(cell,state.flags & ~TileFlags.LockColor);var color=state.color;color.a=.3f;map.SetColor(cell,color);map.SetColliderType(cell,Tile.ColliderType.None);RefreshCollision();
            yield return new WaitForSeconds(3);Restore(cell);
        }
        void Restore(Vector3Int cell){HiddenCell state;if(!hidden.TryGetValue(cell,out state))return;
            if(map && map.GetTile(cell)==state.tile){map.SetTileFlags(cell,TileFlags.None);map.SetColor(cell,state.color);map.SetColliderType(cell,state.collider);map.SetTileFlags(cell,state.flags);RefreshCollision();}hidden.Remove(cell);
        }
        void RefreshCollision(){var collider=GetComponent<TilemapCollider2D>();if(collider)collider.ProcessTilemapChanges();}
        void OnDisable(){StopAllCoroutines();foreach(var cell in new System.Collections.Generic.List<Vector3Int>(hidden.Keys))Restore(cell);}
        public bool TryInteractContact(Vector2 point,Vector2 normal,RobotEmotionGameplay player,out bool respawned)
        {
            if(!map)map=GetComponent<Tilemap>();
            return TryInteractCell(map.WorldToCell(point-normal*.02f),player,out respawned);
        }
    }
}
