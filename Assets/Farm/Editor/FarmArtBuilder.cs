using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace NextGenFarm.Editor
{
    public static class FarmArtBuilder
    {
        public static void Ensure(FarmVisualLibrary library)
        {
            if(!AssetDatabase.IsValidFolder("Assets/FarmGenerated/Art")) AssetDatabase.CreateFolder("Assets/FarmGenerated","Art");
            string[] colors={"397D27","78A83F","6B4832","A78052","F5E7C5","875342","AAA798","359BAB","515E60","8BC8D4","F4C69A","9B522A","3C83AD","263735","F5F0D8","EDBE43","D66D46","684A30","F1D8AE","779B8A","DCE5D9","79A32D"};
            library.artMaterials=new Material[colors.Length];
            for(int i=0;i<colors.Length;i++)
            {
                string path="Assets/FarmGenerated/Art/"+((FarmSurface)i)+".mat";
                var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(m==null){m=new Material(Shader.Find("HDRP/Lit"));AssetDatabase.CreateAsset(m,path);}
                ColorUtility.TryParseHtmlString("#"+colors[i],out var color); m.SetColor("_BaseColor",color);
                m.SetFloat("_Smoothness",i==(int)FarmSurface.Water?.88f:i==(int)FarmSurface.Metal?.5f:.16f);
                if(m.HasProperty("_Metallic")) m.SetFloat("_Metallic",i==(int)FarmSurface.Metal?.55f:i==(int)FarmSurface.Water?.18f:0);
                m.enableInstancing=true; library.artMaterials[i]=m; EditorUtility.SetDirty(m);
            }
            SetGround(library.grass, "Meadow", new Color(.39f,.56f,.14f), 0);
            SetGround(library.drySoil, "Earth", new Color(.43f,.29f,.15f), 1);
            SetGround(library.wetSoil, "MoistEarth", new Color(.24f,.18f,.105f), 1);
            library.wetSoil.SetFloat("_Smoothness",.46f);
            library.pathMaterial=library.artMaterials[(int)FarmSurface.Stone];
            SetGround(library.pathMaterial,"Cobblestone",new Color(.59f,.60f,.46f),2);
            library.cliffMaterial=library.artMaterials[(int)FarmSurface.Soil];
            library.demoPalette[3].SetColor("_BaseColor",new Color(.35f,.62f,.10f));
            library.demoPalette[4].SetColor("_BaseColor",new Color(.23f,.52f,.055f));
            library.demoPalette[5].SetColor("_BaseColor",new Color(.88f,.64f,.16f));
            foreach(var m in library.demoPalette) EditorUtility.SetDirty(m);
            const string grassPath="Assets/FarmGenerated/Art/GrassTuft.asset";
            library.detailGrassMesh=AssetDatabase.LoadAssetAtPath<Mesh>(grassPath);
            if(library.detailGrassMesh==null){library.detailGrassMesh=Grass();AssetDatabase.CreateAsset(library.detailGrassMesh,grassPath);}
            const string soilPath="Assets/FarmGenerated/Art/TerrainColumn.asset";
            library.groundMesh=AssetDatabase.LoadAssetAtPath<Mesh>(soilPath);
            if(library.groundMesh==null){library.groundMesh=Ground();AssetDatabase.CreateAsset(library.groundMesh,soilPath);}
            EditorUtility.SetDirty(library);
        }
        private static void SetGround(Material mat,string id,Color color,int style)
        {
            string path="Assets/FarmGenerated/Art/"+id+"-v2.asset";
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(tex==null)
            {
                const int size=128; tex=new Texture2D(size,size,TextureFormat.RGBA32,true); tex.name=id; tex.wrapMode=TextureWrapMode.Repeat;
                for(int y=0;y<size;y++) for(int x=0;x<size;x++)
                {
                    float u=x/(float)size,v=y/(float)size;
                    float noise=Noise(u*8,v*8,8)*.55f+Noise(u*24,v*24,24)*.3f+Noise(u*56,v*56,56)*.15f;
                    float tone=.9f+noise*.22f;
                    if(style==1)
                    {
                        float ridges=Mathf.Sin(v*Mathf.PI*12+noise)*.035f;
                        float stones=Mathf.Pow(Noise(u*16,v*16,16),4);
                        tone=.74f+noise*.32f+ridges+stones*.3f;
                    }
                    if(style==2)
                    {
                        float row=Mathf.Floor(v*4), uu=Mathf.Repeat(u*4+(row%2)*.5f,1), vv=Mathf.Repeat(v*4,1);
                        if(uu<.055f||vv<.055f||uu>.945f||vv>.945f) tone=.48f;
                        else tone=.86f+noise*.25f;
                    }
                    tex.SetPixel(x,y,new Color(color.r*tone,color.g*tone,color.b*tone,1));
                }
                tex.Apply(true,false);AssetDatabase.CreateAsset(tex,path);
            }
            mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseColorMap",tex); mat.enableInstancing=true; EditorUtility.SetDirty(mat);
        }
        private static float Hash(int x,int y,int period)
        {
            x=(x%period+period)%period;y=(y%period+period)%period;
            unchecked { uint h=(uint)(x*374761393+y*668265263+177);h=(h^(h>>13))*1274126177u;return (h&65535)/65535f; }
        }
        private static float Noise(float x,float y,int period)
        {
            int ix=Mathf.FloorToInt(x),iy=Mathf.FloorToInt(y);float a=x-ix,b=y-iy;a=a*a*(3-2*a);b=b*b*(3-2*b);
            return Mathf.Lerp(Mathf.Lerp(Hash(ix,iy,period),Hash(ix+1,iy,period),a),Mathf.Lerp(Hash(ix,iy+1,period),Hash(ix+1,iy+1,period),a),b);
        }
        private static Mesh Grass()
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<9;i++)
            {
                float a=i*2.39996f; Vector3 outwards=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));Vector3 side=Vector3.Cross(Vector3.up,outwards)*.035f;
                Vector3 p=outwards*.11f, tip=p+outwards*.16f+Vector3.up*(.3f+(i%3)*.12f);int n=v.Count;
                v.Add(p-side);v.Add(tip);v.Add(p+side);v.Add(p+side);v.Add(tip);v.Add(p-side);
                t.AddRange(new[]{n,n+1,n+2,n+3,n+4,n+5});
            }
            var m=new Mesh();m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        private static Mesh Ground()
        {
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var source=cube.GetComponent<MeshFilter>().sharedMesh;
            var m=Object.Instantiate(source);Object.DestroyImmediate(cube);
            var top=new List<int>();var sides=new List<int>();var triangles=m.triangles;var normals=m.normals;
            for(int i=0;i<triangles.Length;i+=3)
            {
                var target=normals[triangles[i]].y>.5f?top:sides;
                target.Add(triangles[i]);target.Add(triangles[i+1]);target.Add(triangles[i+2]);
            }
            m.subMeshCount=2;m.SetTriangles(top,0);m.SetTriangles(sides,1);return m;
        }
    }
}
