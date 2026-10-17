using UnityEngine;
namespace Emergence.Level01
{
    [System.Serializable]
    public sealed class BlockShatterSettings
    {
        [Tooltip("横向/纵向碎片数量。")][Range(1,16)] public int columns=4, rows=4;
        [Min(.01f)] public float lifetime=.55f;
        [Min(0)] public float horizontalSpeed=1.5f, verticalSpread=1.3f;
        public float upwardSpeed=1.4f;
        [Min(0)] public float gravity=7, rotationSpeed=180;
        [Min(.001f)] public float flashSeconds=.08f;
        public Color flashColor=new Color(1,.6f,.5f);
        public int sortingOffset=1;
    }
    public sealed class BlockShatterEffect : MonoBehaviour
    {
        SpriteRenderer[] pieces;Sprite[] sprites;Vector3[] velocities;float age;Color tint;
        public BlockShatterSettings settings=new BlockShatterSettings();
        public static void Spawn(Sprite sprite,Matrix4x4 matrix,Material material,int layer,int order,Color color,BlockShatterSettings settings=null)
        {
            if(!sprite)return;
            var root=new GameObject("BlockShatter");root.transform.position=matrix.MultiplyPoint3x4(Vector3.zero);var effect=root.AddComponent<BlockShatterEffect>();effect.tint=color;
            effect.settings=settings??new BlockShatterSettings();var config=effect.settings;
            int columns=Mathf.Clamp(config.columns,1,Mathf.Min(16,(int)sprite.rect.width)),rows=Mathf.Clamp(config.rows,1,Mathf.Min(16,(int)sprite.rect.height)),count=columns*rows;
            effect.pieces=new SpriteRenderer[count];effect.sprites=new Sprite[count];effect.velocities=new Vector3[count];
            var right=matrix.MultiplyVector(Vector3.right);var up=matrix.MultiplyVector(Vector3.up);
            float angle=Mathf.Atan2(right.y,right.x)*Mathf.Rad2Deg;
            for(int y=0;y<rows;y++)for(int x=0;x<columns;x++){
                int i=y*columns+x;float w=sprite.rect.width/columns,h=sprite.rect.height/rows;
                var part=Sprite.Create(sprite.texture,new Rect(sprite.rect.x+x*w,sprite.rect.y+y*h,w,h),new Vector2(.5f,.5f),sprite.pixelsPerUnit,0,SpriteMeshType.FullRect);
                effect.sprites[i]=part;var go=new GameObject("Shard");go.transform.SetParent(root.transform,false);
                var local=new Vector3(((x+.5f)*w-sprite.pivot.x)/sprite.pixelsPerUnit,((y+.5f)*h-sprite.pivot.y)/sprite.pixelsPerUnit,0);
                go.transform.position=matrix.MultiplyPoint3x4(local);go.transform.rotation=Quaternion.Euler(0,0,angle);go.transform.localScale=new Vector3(right.magnitude,up.magnitude,1);
                var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=part;renderer.sharedMaterial=material;renderer.sortingLayerID=layer;renderer.sortingOrder=order+config.sortingOffset;var flash=config.flashColor;flash.a=color.a;renderer.color=flash;effect.pieces[i]=renderer;
                effect.velocities[i]=matrix.MultiplyVector(new Vector3((x-(columns-1)*.5f)*config.horizontalSpeed,(y-(rows-1)*.5f)*config.verticalSpread+config.upwardSpeed,0));
            }
        }
        void Update(){
            if(LocalTimeTarget.IsStopped(this))return;
            float lifetime=Mathf.Max(.01f,settings.lifetime);age+=Time.deltaTime;if(age>=lifetime){Destroy(gameObject);return;}
            for(int i=0;i<pieces.Length;i++){
                velocities[i]+=Vector3.down*(settings.gravity*Time.deltaTime);pieces[i].transform.position+=velocities[i]*Time.deltaTime;
                pieces[i].transform.Rotate(0,0,(i%2==0?1:-1)*settings.rotationSpeed*Time.deltaTime);
                var c=Color.Lerp(settings.flashColor,tint,Mathf.Clamp01(age/Mathf.Max(.001f,settings.flashSeconds)));c.a=tint.a*(1-age/lifetime);pieces[i].color=c;
            }
        }
        void OnDestroy(){if(sprites!=null)foreach(var sprite in sprites)if(sprite)Destroy(sprite);}
    }
}
