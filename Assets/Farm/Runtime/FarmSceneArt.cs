using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NextGenFarm
{
    public enum FarmSurface { Leaf, LeafLight, Bark, Wood, Cream, Roof, Stone, Water, Metal, Glass, Skin, Hair, Denim, Rubber, White, Gold, Coral, Soil, Flower, Mountain, Snow, GrassBlade }

    public sealed class FarmSceneryRenderer : MonoBehaviour
    {
        private sealed class Batch { public Mesh mesh; public Material mat; public Matrix4x4[] matrices = new Matrix4x4[256]; public int count; }
        private readonly List<Batch> batches = new List<Batch>();
        public void Add(Mesh mesh, Material mat, Matrix4x4 matrix)
        {
            Batch target = null;
            foreach (var b in batches) if (b.mesh == mesh && b.mat == mat && b.count < 256) { target = b; break; }
            if (target == null) { target = new Batch { mesh = mesh, mat = mat }; mat.enableInstancing = true; batches.Add(target); }
            target.matrices[target.count++] = matrix;
        }
        private void LateUpdate()
        {
            foreach (var b in batches) Graphics.RenderMeshInstanced(new RenderParams(b.mat) {
                shadowCastingMode = ShadowCastingMode.On, receiveShadows = true }, b.mesh, 0, b.matrices, b.count);
        }
    }
    public sealed class FarmRotor : MonoBehaviour
    { private void Update() { transform.Rotate(0, 0, 14 * Time.deltaTime, Space.Self); } }

    public sealed class FarmActorMotion : MonoBehaviour
    {
        public Transform[] legs, arms, wheels;
        private Vector3 previous;
        private float phase;
        private void OnEnable() { previous = transform.parent != null ? transform.parent.position : transform.position; }
        private void LateUpdate()
        {
            Vector3 position = transform.parent != null ? transform.parent.position : transform.position;
            float distance = Vector3.Distance(previous, position); previous = position;
            phase += distance * 6;
            if (legs != null) for (int i = 0; i < legs.Length; i++) legs[i].localRotation = Quaternion.Euler(distance > 0.001f ? Mathf.Sin(phase + i * Mathf.PI) * 24 : 0, 0, 0);
            if (arms != null) for (int i = 0; i < arms.Length; i++) arms[i].localRotation = Quaternion.Euler(distance > 0.001f ? -Mathf.Sin(phase + i * Mathf.PI) * 19 : -5, 0, i == 0 ? -6 : 6);
            if (wheels != null) foreach (var wheel in wheels) wheel.Rotate(0, distance * 120, 0, Space.Self);
        }
    }

    public sealed class FarmSceneArt : MonoBehaviour
    {
        private FarmVisualLibrary library;
        private FarmGridManager grid;
        private FarmSceneryRenderer instances;
        private readonly Dictionary<PrimitiveType, Mesh> primitives = new Dictionary<PrimitiveType, Mesh>();
        private readonly List<Mesh> owned = new List<Mesh>();
        public void Initialize(FarmGridManager farm, FarmVisualLibrary art)
        { grid = farm; library = art; instances = gameObject.AddComponent<FarmSceneryRenderer>(); }
        public Material Mat(FarmSurface surface) => library.artMaterials[(int)surface];
        private Mesh Primitive(PrimitiveType type)
        {
            if (primitives.TryGetValue(type, out var mesh)) return mesh;
            var go = GameObject.CreatePrimitive(type); mesh = go.GetComponent<MeshFilter>().sharedMesh;
            go.SetActive(false); Destroy(go); primitives.Add(type, mesh); return mesh;
        }
        private Transform Root(string name, Transform parent, Vector3 position)
        { var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position; return go.transform; }
        private Transform Piece(string name, Transform parent, PrimitiveType type, FarmSurface surface, Vector3 position, Vector3 scale, Vector3 euler = default)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale; go.transform.localRotation = Quaternion.Euler(euler);
            go.AddComponent<MeshFilter>().sharedMesh = Primitive(type); go.AddComponent<MeshRenderer>().sharedMaterial = Mat(surface);
            return go.transform;
        }
        private void Stamp(PrimitiveType type, FarmSurface surface, Vector3 p, Vector3 size, Vector3 rotation = default)
            => instances.Add(Primitive(type), Mat(surface), Matrix4x4.TRS(grid.transform.position + p, Quaternion.Euler(rotation), size));
        public GameObject Farmer(Transform parent)
        {
            var root = Root("Farmer · denim & ponytail", parent, Vector3.zero);
            Piece("Shirt", root, PrimitiveType.Capsule, FarmSurface.White, new Vector3(0, 1.12f, 0), new Vector3(.46f,.33f,.28f));
            Piece("Overalls", root, PrimitiveType.Capsule, FarmSurface.Denim, new Vector3(0,.91f,.025f), new Vector3(.43f,.27f,.3f));
            Piece("Bib", root, PrimitiveType.Cube, FarmSurface.Denim, new Vector3(0,1.21f,.145f), new Vector3(.27f,.30f,.035f));
            for (int side = -1; side <= 1; side += 2)
            {
                Piece("Strap", root, PrimitiveType.Cube, FarmSurface.Denim, new Vector3(side * .12f,1.34f,.12f), new Vector3(.055f,.28f,.055f));
                Piece("Back strap", root, PrimitiveType.Cube, FarmSurface.Denim, new Vector3(side*.12f,1.18f,-.145f),new Vector3(.06f,.48f,.035f));
                Piece("Brass button", root, PrimitiveType.Sphere, FarmSurface.Gold, new Vector3(side * .12f,1.27f,.17f), Vector3.one * .035f);
            }
            Piece("Head", root, PrimitiveType.Sphere, FarmSurface.Skin, new Vector3(0,1.59f,.015f), new Vector3(.38f,.43f,.35f));
            Piece("Hair", root, PrimitiveType.Sphere, FarmSurface.Hair, new Vector3(0,1.73f,-.035f), new Vector3(.43f,.30f,.4f));
            Piece("Ponytail", root, PrimitiveType.Capsule, FarmSurface.Hair, new Vector3(0,1.51f,-.25f), new Vector3(.18f,.24f,.16f), new Vector3(15,0,0));
            Piece("Tie", root, PrimitiveType.Sphere, FarmSurface.Coral, new Vector3(0,1.69f,-.24f), Vector3.one * .12f);
            for (int side=-1;side<=1;side+=2)
            {
                Piece("Eye",root,PrimitiveType.Sphere,FarmSurface.Rubber,new Vector3(side*.085f,1.61f,.177f),new Vector3(.035f,.045f,.025f));
                Piece("Cheek",root,PrimitiveType.Sphere,FarmSurface.Coral,new Vector3(side*.13f,1.55f,.165f),new Vector3(.05f,.025f,.015f));
            }
            var legs = new Transform[2]; var arms = new Transform[2];
            for (int i=0;i<2;i++)
            {
                int side = i==0?-1:1;
                legs[i]=Root("Leg joint",root,new Vector3(side*.13f,.78f,0));
                Piece("Denim trouser",legs[i],PrimitiveType.Capsule,FarmSurface.Denim,new Vector3(0,-.28f,0),new Vector3(.2f,.28f,.22f));
                Piece("Leather boot",legs[i],PrimitiveType.Capsule,FarmSurface.Wood,new Vector3(0,-.64f,.06f),new Vector3(.22f,.14f,.32f));
                arms[i]=Root("Shoulder",root,new Vector3(side*.27f,1.28f,0));
                Piece("Sleeve",arms[i],PrimitiveType.Capsule,FarmSurface.White,new Vector3(side*.025f,-.12f,0),new Vector3(.18f,.15f,.18f));
                Piece("Arm",arms[i],PrimitiveType.Capsule,FarmSurface.Skin,new Vector3(side*.03f,-.36f,.01f),new Vector3(.12f,.18f,.12f));
                Piece("Hand",arms[i],PrimitiveType.Sphere,FarmSurface.Skin,new Vector3(side*.03f,-.55f,.02f),Vector3.one*.14f);
            }
            var motion=root.gameObject.AddComponent<FarmActorMotion>(); motion.legs=legs; motion.arms=arms;
            return root.gameObject;
        }
        public GameObject Tractor(Transform parent)
        {
            var root=Root("Cedar tractor",parent,Vector3.zero);
            Piece("Chassis",root,PrimitiveType.Cube,FarmSurface.Metal,new Vector3(0,.55f,0),new Vector3(1.2f,.22f,2.3f));
            Piece("Rounded bonnet",root,PrimitiveType.Capsule,FarmSurface.Coral,new Vector3(0,1.0f,.65f),new Vector3(.95f,.7f,.65f),new Vector3(90,0,0));
            Piece("Grille",root,PrimitiveType.Cube,FarmSurface.Metal,new Vector3(0,.98f,1.3f),new Vector3(.72f,.48f,.08f));
            for(int i=0;i<6;i++) Piece("Chrome grille slat",root,PrimitiveType.Cube,FarmSurface.Cream,new Vector3(-.29f+i*.115f,.98f,1.35f),new Vector3(.032f,.42f,.035f));
            Piece("Exhaust",root,PrimitiveType.Cylinder,FarmSurface.Metal,new Vector3(-.32f,1.6f,.35f),new Vector3(.09f,.48f,.09f));
            Piece("Seat",root,PrimitiveType.Cube,FarmSurface.Rubber,new Vector3(0,.96f,-.55f),new Vector3(.6f,.15f,.55f));
            Piece("Seat back",root,PrimitiveType.Cube,FarmSurface.Rubber,new Vector3(0,1.15f,-.8f),new Vector3(.6f,.43f,.13f));
            var wheels=new Transform[4]; int n=0;
            for(int s=-1;s<=1;s+=2) for(int e=-1;e<=1;e+=2)
            {
                bool rear=e==-1; float radius=rear?.55f:.36f;
                var wheel=Root("Wheel axle",root,new Vector3(s*.77f,radius,e*.8f)); wheel.localRotation=Quaternion.Euler(0,0,90); wheels[n++]=wheel;
                Piece("Tire",wheel,PrimitiveType.Cylinder,FarmSurface.Rubber,Vector3.zero,new Vector3(radius*2,.18f,radius*2));
                Piece("Hub",wheel,PrimitiveType.Cylinder,FarmSurface.Cream,new Vector3(0,-s*.19f,0),new Vector3(radius,.045f,radius));
                for(int t=0;t<12;t++)
                {
                    float a=t*Mathf.PI/6;
                    Piece("Tread",wheel,PrimitiveType.Cube,FarmSurface.Rubber,new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius),new Vector3(.13f,.39f,.07f),new Vector3(0,a*Mathf.Rad2Deg,12));
                }
                if(rear) Piece("Mudguard",root,PrimitiveType.Capsule,FarmSurface.Coral,new Vector3(s*.75f,1.16f,-.8f),new Vector3(.32f,.13f,1.15f),new Vector3(90,0,0));
                else Piece("Headlight",root,PrimitiveType.Sphere,FarmSurface.Gold,new Vector3(s*.4f,1.12f,1.31f),Vector3.one*.17f);
            }
            var driver=Farmer(root); driver.transform.localPosition=new Vector3(0,.67f,-.5f); driver.transform.localScale=Vector3.one*.65f;
            Destroy(driver.GetComponent<FarmActorMotion>());
            root.gameObject.AddComponent<FarmActorMotion>().wheels=wheels;
            return root.gameObject;
        }
        public void Cow(Transform parent, Vector3 position)
        {
            var root=Root("Cow",parent,position-grid.transform.position);
            Piece("Body",root,PrimitiveType.Sphere,FarmSurface.White,new Vector3(0,1.0f,0),new Vector3(.9f,.9f,1.5f));
            Piece("Head",root,PrimitiveType.Sphere,FarmSurface.White,new Vector3(0,1.2f,.83f),new Vector3(.6f,.62f,.65f));
            Piece("Muzzle",root,PrimitiveType.Sphere,FarmSurface.Skin,new Vector3(0,1.05f,1.1f),new Vector3(.52f,.27f,.25f));
            for(int s=-1;s<=1;s+=2)
            {
                Piece("Ear",root,PrimitiveType.Sphere,FarmSurface.Rubber,new Vector3(s*.35f,1.42f,.8f),new Vector3(.33f,.12f,.18f));
                Piece("Eye",root,PrimitiveType.Sphere,FarmSurface.Rubber,new Vector3(s*.23f,1.33f,1.03f),Vector3.one*.07f);
                Piece("Horn",root,PrimitiveType.Capsule,FarmSurface.Cream,new Vector3(s*.2f,1.58f,.7f),new Vector3(.07f,.15f,.07f),new Vector3(0,0,-s*20));
                for(int e=-1;e<=1;e+=2) Piece("Leg",root,PrimitiveType.Capsule,FarmSurface.Rubber,new Vector3(s*.3f,.42f,e*.49f),new Vector3(.17f,.39f,.17f));
                Piece("Patch",root,PrimitiveType.Sphere,FarmSurface.Rubber,new Vector3(s*.39f,1.12f,-.22f),new Vector3(.13f,.47f,.65f));
            }
        }
        public void Landscape()
        {
            Terrain(); Barn(new Vector3(48,1,57)); Windmill(new Vector3(9,1,58)); Pond(new Vector3(-5,-.05f,34));
            var rng=new System.Random(812);
            for(int i=0;i<54;i++)
            {
                float angle=i*Mathf.PI*2/54, radius=42+(float)rng.NextDouble()*25;
                Vector3 p=new Vector3(32+Mathf.Cos(angle)*radius,0,34+Mathf.Sin(angle)*radius);
                Tree(p, .8f+(float)rng.NextDouble()*.65f, i%3);
            }
            Tree(new Vector3(12,0,35),1,0); Tree(new Vector3(54,0,30),1.1f,1); Tree(new Vector3(4,1,51),1.2f,2);
            for(int i=0;i<40;i++)
            {
                float x=3+i*1.55f;
                Stamp(PrimitiveType.Sphere,FarmSurface.Stone,new Vector3(x,.45f,48),new Vector3(1.6f,.9f,.8f),new Vector3(0,i*23,0));
            }
            for(int i=0;i<4;i++) Stamp(PrimitiveType.Cube,FarmSurface.Stone,new Vector3(39,.125f*(i+1),47.5f+i*.65f),new Vector3(4,.25f*(i+1),.7f));
            for(int i=0;i<140;i++)
            {
                float x=35+(float)rng.NextDouble()*9, z=8+(float)rng.NextDouble()*30;
                if(x>38 && x<40) continue;
                Vector3 p=new Vector3(x,0,z);
                Stamp(PrimitiveType.Cylinder,FarmSurface.Leaf,p+Vector3.up*.18f,new Vector3(.025f,.18f,.025f));
                for(int k=0;k<5;k++) { float a=k*Mathf.PI*.4f; Stamp(PrimitiveType.Sphere,i%3==0?FarmSurface.Coral:FarmSurface.Flower,p+new Vector3(Mathf.Cos(a)*.09f,.38f,Mathf.Sin(a)*.09f),new Vector3(.14f,.05f,.1f)); }
                Stamp(PrimitiveType.Sphere,FarmSurface.Gold,p+Vector3.up*.4f,Vector3.one*.09f);
            }
            for(int z=44;z<=58;z+=2) for(int x=20;x<=56;x+=2)
                if(z==44 || z==58) FencePost(new Vector3(x,0,z));
        }
        private void FencePost(Vector3 p)
        {
            Stamp(PrimitiveType.Cube,FarmSurface.Cream,p+Vector3.up*.65f,new Vector3(.15f,1.3f,.15f));
            Stamp(PrimitiveType.Cube,FarmSurface.Wood,p+new Vector3(1,.45f,0),new Vector3(2,.1f,.1f));
            Stamp(PrimitiveType.Cube,FarmSurface.Wood,p+new Vector3(1,.95f,0),new Vector3(2,.1f,.1f));
        }
        private void Tree(Vector3 p,float size,int tint)
        {
            Stamp(PrimitiveType.Cylinder,FarmSurface.Bark,p+Vector3.up*size,new Vector3(.35f,1.1f,.35f)*size);
            for(int k=0;k<5;k++)
            {
                float a=k*Mathf.PI*.4f;
                Stamp(PrimitiveType.Sphere,(k+tint)%3==0?FarmSurface.LeafLight:FarmSurface.Leaf,p+new Vector3(Mathf.Cos(a)*1.05f,3+(k%2)*.6f,Mathf.Sin(a)*1.05f)*size,new Vector3(2.6f,2.5f,2.6f)*size);
            }
            for(int k=0;k<4;k++) { float a=k*1.7f; Stamp(PrimitiveType.Sphere,FarmSurface.Coral,p+new Vector3(Mathf.Cos(a)*1.8f,2.7f,Mathf.Sin(a)*1.8f)*size,Vector3.one*.23f*size); }
        }
        private void Barn(Vector3 p)
        {
            Stamp(PrimitiveType.Cube,FarmSurface.Coral,p+Vector3.up*2.4f,new Vector3(8,4.8f,7));
            for(int s=-1;s<=1;s+=2)
            {
                Stamp(PrimitiveType.Cube,FarmSurface.Roof,p+new Vector3(s*2.15f,5.25f,0),new Vector3(4.9f,.25f,8),new Vector3(0,0,-s*32));
                for(int z=-1;z<=1;z+=2) Stamp(PrimitiveType.Cube,FarmSurface.Cream,p+new Vector3(s*3.9f,2.4f,z*3.45f),new Vector3(.2f,4.8f,.2f));
            }
            Stamp(PrimitiveType.Cube,FarmSurface.Wood,p+new Vector3(0,1.5f,-3.56f),new Vector3(2.4f,3,.15f));
            Stamp(PrimitiveType.Cube,FarmSurface.Cream,p+new Vector3(0,1.5f,-3.68f),new Vector3(.12f,3.1f,.08f));
            for(int s=-1;s<=1;s+=2)
            {
                Stamp(PrimitiveType.Cube,FarmSurface.Cream,p+new Vector3(s*2.65f,2.8f,-3.57f),new Vector3(1.3f,1.4f,.13f));
                Stamp(PrimitiveType.Cube,FarmSurface.Glass,p+new Vector3(s*2.65f,2.8f,-3.66f),new Vector3(1.05f,1.1f,.08f));
                Stamp(PrimitiveType.Cube,FarmSurface.Cream,p+new Vector3(s*2.65f,2.8f,-3.72f),new Vector3(.08f,1.2f,.04f));
            }
            for(int i=0;i<12;i++) Stamp(PrimitiveType.Cube,FarmSurface.Wood,p+new Vector3(-3.7f+i*.68f,2.3f,-3.51f),new Vector3(.035f,4.4f,.04f));
            for(int i=0;i<3;i++) Stamp(PrimitiveType.Cylinder,FarmSurface.Gold,p+new Vector3(5+i*.7f,.65f,-1),new Vector3(1.2f,.65f,1.2f),new Vector3(0,0,90));
            Stamp(PrimitiveType.Cylinder,FarmSurface.Metal,p+new Vector3(0,7,2),new Vector3(.7f,1.6f,.7f));
        }
        private void Windmill(Vector3 p)
        {
            Stamp(PrimitiveType.Cylinder,FarmSurface.Cream,p+Vector3.up*3,new Vector3(2.5f,3,2.5f));
            Stamp(PrimitiveType.Sphere,FarmSurface.Roof,p+Vector3.up*6.1f,new Vector3(3.2f,2,3.2f));
            var rotor=Root("Windmill rotor",transform,p+new Vector3(0,4.8f,-1.6f)); rotor.gameObject.AddComponent<FarmRotor>();
            for(int k=0;k<4;k++) Piece("Sail",rotor,PrimitiveType.Cube,FarmSurface.Wood,new Vector3(Mathf.Sin(k*Mathf.PI/2)*2,Mathf.Cos(k*Mathf.PI/2)*2,0),new Vector3(.55f,4,.12f),new Vector3(0,0,-k*90));
        }
        private void Pond(Vector3 p)
        {
            Stamp(PrimitiveType.Cylinder,FarmSurface.Water,p,new Vector3(17,.08f,23));
            for(int i=0;i<36;i++) { float a=i*Mathf.PI*2/36; Stamp(PrimitiveType.Sphere,FarmSurface.Stone,p+new Vector3(Mathf.Cos(a)*8.5f,.15f,Mathf.Sin(a)*11.5f),new Vector3(1.1f,.7f,1.3f),new Vector3(0,i*31,0)); }
        }
        private void Terrain()
        {
            var vertices=new List<Vector3>(); var triangles=new List<int>(); const int count=32; const float step=14;
            for(int z=0;z<=count;z++) for(int x=0;x<=count;x++)
            {
                float px=x*step-180,pz=z*step-100;
                float edge=Mathf.Max(Mathf.Max(-px,px-64),Mathf.Max(-pz,pz-64));
                float height=Mathf.Max(0,edge-12)*.22f*(.5f+Mathf.PerlinNoise(px*.012f+20,pz*.012f+20));
                vertices.Add(new Vector3(px,height-.45f,pz));
            }
            for(int z=0;z<count;z++) for(int x=0;x<count;x++) { int a=z*(count+1)+x; triangles.AddRange(new[]{a,a+count+1,a+1,a+1,a+count+1,a+count+2}); }
            var mesh=new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); owned.Add(mesh);
            instances.Add(mesh,library.grass,Matrix4x4.TRS(grid.transform.position,Quaternion.identity,Vector3.one));
            for(int i=0;i<9;i++)
            {
                Vector3 p=new Vector3(-110+i*36,0,148+(i%3)*25);
                float radius=30+(i%3)*9,height=35+(i%4)*9;
                var mountain=Mountain(radius,height); owned.Add(mountain);
                instances.Add(mountain,Mat(FarmSurface.Mountain),Matrix4x4.TRS(p+grid.transform.position,Quaternion.identity,Vector3.one));
                var snow=Mountain(radius*.302f,height*.3f); owned.Add(snow);
                instances.Add(snow,Mat(FarmSurface.Snow),Matrix4x4.TRS(p+new Vector3(0,height*.7f+.015f,0)+grid.transform.position,Quaternion.identity,Vector3.one));
            }
        }
        private static Mesh Mountain(float radius,float height)
        {
            var v=new List<Vector3>(); var t=new List<int>();
            for(int i=0;i<9;i++) { float a=i*Mathf.PI*2/8,b=(i+1)*Mathf.PI*2/8; int n=v.Count;
                v.Add(new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius)); v.Add(new Vector3(0,height,0)); v.Add(new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius)); t.AddRange(new[]{n,n+1,n+2}); }
            var mesh=new Mesh(); mesh.SetVertices(v); mesh.SetTriangles(t,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        private void OnDestroy() { foreach(var mesh in owned) if(mesh!=null) Destroy(mesh); }
    }
}
