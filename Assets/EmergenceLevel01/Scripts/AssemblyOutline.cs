using UnityEngine;
namespace Emergence.Level01
{
    public sealed class AssemblyOutline : MonoBehaviour
    {
        public Vector2 size = new Vector2(1.5f, 1.625f);
        public Color color = new Color(1f, .76f, .22f);
        public Material material;
        public bool dashed = true;
        public int sortingOrder = 25;
        GameObject drawing;
        static Sprite pixel;
        public bool Visible { get; private set; }
        public void SetVisible(bool value)
        {
            if (value && !drawing) Build();
            Visible = value;
            if (drawing) drawing.SetActive(value);
        }
        void Build()
        {
            if (!pixel) { pixel = Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);pixel.hideFlags=HideFlags.HideAndDontSave; }
            drawing = new GameObject("OutlineDrawing");drawing.transform.SetParent(transform,false);
            Edge(new Vector2(-size.x/2,-size.y/2),Vector2.right,size.x);
            Edge(new Vector2(-size.x/2,size.y/2),Vector2.right,size.x);
            Edge(new Vector2(-size.x/2,-size.y/2),Vector2.up,size.y);
            Edge(new Vector2(size.x/2,-size.y/2),Vector2.up,size.y);
        }
        void Edge(Vector2 start,Vector2 direction,float length)
        {
            float step=dashed?.3125f:length, dash=dashed?.1875f:length;
            for(float t=0;t<length;t+=step)
            {
                float segment=Mathf.Min(dash,length-t);
                var go=new GameObject("Dash");go.transform.SetParent(drawing.transform,false);
                go.transform.localPosition=start+direction*(t+segment*.5f);
                go.transform.localScale=direction.x!=0?new Vector3(segment,.0625f,1):new Vector3(.0625f,segment,1);
                var sr=go.AddComponent<SpriteRenderer>();sr.sprite=pixel;sr.sharedMaterial=material;sr.color=color;sr.sortingOrder=sortingOrder;
            }
        }
    }
}
