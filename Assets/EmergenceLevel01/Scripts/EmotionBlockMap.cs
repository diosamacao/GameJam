using UnityEngine;
using UnityEngine.Tilemaps;
namespace Emergence.Level01
{
    [RequireComponent(typeof(Tilemap))]
    public sealed class EmotionBlockMap : MonoBehaviour
    {
        Tilemap map;
        public bool TryInteractCell(Vector3Int cell,RobotEmotionGameplay player,out bool respawned)
        {
            respawned=false;
            if(!isActiveAndEnabled || !player || !player.CanUseEmotionAbilities)return false;
            if(!map)map=GetComponent<Tilemap>();
            var tile=map.GetTile(cell) as EmotionBlockTile;if(!tile)return false;
            if(tile.kind==EmotionBlockKind.Electric){respawned=player.HandleElectricContact();return respawned;}
            if(!EmotionBlockRules.CanRemove(tile.kind,player.CurrentEmotion))return false;
            map.SetTile(cell,null);
            var collider=GetComponent<TilemapCollider2D>();if(collider)collider.ProcessTilemapChanges();
            return true;
        }
        public bool TryInteractContact(Vector2 point,Vector2 normal,RobotEmotionGameplay player,out bool respawned)
        {
            if(!map)map=GetComponent<Tilemap>();
            return TryInteractCell(map.WorldToCell(point-normal*.02f),player,out respawned);
        }
    }
}
