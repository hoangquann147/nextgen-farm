using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NextGenFarm.Editor
{
    // Original procedural demo foliage. One mesh/material per growth stage for instancing.
    public static class FarmCropMeshBuilder
    {
        public static void Ensure(FarmVisualLibrary library)
        {
            if (!AssetDatabase.IsValidFolder("Assets/FarmGenerated/Crops")) AssetDatabase.CreateFolder("Assets/FarmGenerated", "Crops");
            var definitions = new CropVisual[3];
            for (int species = 0; species < 3; species++)
            {
                string id = species == 0 ? "carrot" : species == 1 ? "wheat" : "corn";
                var visual = new CropVisual { cropId = id, scale = Vector3.one };
                for (int stage = 0; stage < 3; stage++)
                {
                    string path = "Assets/FarmGenerated/Crops/" + id + "-stage" + stage + "-v3.asset";
                    var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (mesh == null)
                    {
                        mesh = Create(id, stage); mesh.name = id + " stage " + stage;
                        AssetDatabase.CreateAsset(mesh, path);
                    }
                    visual.stages[stage] = mesh;
                    visual.materials[stage] = library.demoPalette[species == 1 && stage == 2 ? 5 : stage == 0 ? 3 : 4];
                    if(species==2) visual.accentMaterials[stage]=library.artMaterials[(int)FarmSurface.Gold];
                }
                definitions[species] = visual;
            }
            library.crops = definitions;
            EditorUtility.SetDirty(library);
        }
        private sealed class Geometry
        {
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<int> indices = new List<int>();
            public void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int index = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
                indices.Add(index); indices.Add(index + 1); indices.Add(index + 2);
            }
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool doubleSided = false)
            {
                Triangle(a, b, c); Triangle(a, c, d);
                if (doubleSided) { Triangle(c, b, a); Triangle(d, c, a); }
            }
            public void Stem(Vector3 origin, float radius, float height)
            {
                for (int i = 0; i < 6; i++)
                {
                    float a = i * Mathf.PI / 3, b = (i + 1) * Mathf.PI / 3;
                    Vector3 p = origin + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius;
                    Vector3 q = origin + new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)) * radius;
                    Quad(p, p + Vector3.up * height, q + Vector3.up * height, q);
                }
            }
            public void Leaf(Vector3 origin, Vector3 outward, float height, float reach, float width)
            {
                Vector3 side = Vector3.Cross(Vector3.up, outward).normalized;
                for (int segment = 0; segment < 8; segment++)
                {
                    float t0 = segment / 8f, t1 = (segment + 1) / 8f;
                    Vector3 c0 = origin + Vector3.up * (height * Mathf.Sin(t0 * Mathf.PI * 0.85f)) + outward * (reach * t0 * t0);
                    Vector3 c1 = origin + Vector3.up * (height * Mathf.Sin(t1 * Mathf.PI * 0.85f)) + outward * (reach * t1 * t1);
                    float w0 = width * Mathf.Sin(t0 * Mathf.PI), w1 = width * Mathf.Sin(t1 * Mathf.PI);
                    Quad(c0 - side * w0, c1 - side * w1, c1 + side * w1, c0 + side * w0, true);
                }
            }
            public void Ear(Vector3 center, float width, float height)
            {
                const int sides = 6, rings = 5;
                for (int ring = 0; ring < rings; ring++)
                    for (int side = 0; side < sides; side++)
                    {
                        Vector3 a = EarPoint(center, width, height, ring, side, rings, sides);
                        Vector3 b = EarPoint(center, width, height, ring + 1, side, rings, sides);
                        Vector3 c = EarPoint(center, width, height, ring + 1, side + 1, rings, sides);
                        Vector3 d = EarPoint(center, width, height, ring, side + 1, rings, sides);
                        Quad(a, b, c, d);
                    }
            }
            private static Vector3 EarPoint(Vector3 c, float w, float h, int ring, int side, int rings, int sides)
            {
                float t = ring / (float)rings, a = side * Mathf.PI * 2 / sides;
                float r = Mathf.Sin(t * Mathf.PI) * w;
                return c + new Vector3(Mathf.Cos(a) * r, (t - 0.5f) * h, Mathf.Sin(a) * r);
            }
            public Mesh Finish()
            {
                var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0);
                mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
            }
        }
        private static Mesh Create(string id, int stage)
        {
            var g = new Geometry(); float growth = stage == 0 ? 0.28f : stage == 1 ? 0.65f : 1;
            if(id=="corn")
            {
                var ears=new Geometry(); growth=stage==0?.45f:stage==1?.85f:1;
                for(int i=0;i<4;i++)
                {
                    float a=i*Mathf.PI*.5f+.3f; Vector3 p=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.43f;
                    float h=(1.9f+(i%2)*.16f)*growth;g.Stem(p,.055f*growth,h);
                    for(int leaf=0;leaf<6;leaf++) { float angle=a+leaf*2.3f; g.Leaf(p+Vector3.up*(h*(.15f+leaf*.105f)),new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)),h*.25f,.73f*growth,.22f*growth); }
                    for(int e=0;e<2;e++)
                    {
                        Vector3 origin=p+new Vector3(e==0?.14f:-.14f,h*(e==0?.55f:.72f),0);
                        ears.Ear(origin,.115f*growth,.38f*growth);
                    }
                }
                var green=g.Finish();var yellow=ears.Finish();
                var combined=new Mesh();combined.CombineMeshes(new[]{new CombineInstance{mesh=green,transform=Matrix4x4.identity},new CombineInstance{mesh=yellow,transform=Matrix4x4.identity}},false,false);
                Object.DestroyImmediate(green);Object.DestroyImmediate(yellow);return combined;
            }
            if (id == "carrot")
            {
                for (int i = 0; i < 9; i++)
                {
                    float angle = i * Mathf.PI * 2 / 9;
                    Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    g.Leaf(Vector3.zero, direction, (i % 2 == 0 ? 0.85f : 0.65f) * growth, 0.65f * growth, 0.09f * growth);
                }
            }
            else
            {
                for (int i = 0; i < 7; i++)
                {
                    float angle = i * Mathf.PI * 2 / 7;
                    Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    Vector3 origin = direction * 0.26f;
                    float height = (0.95f + (i % 3) * 0.13f) * growth;
                    g.Stem(origin, 0.022f * growth, height);
                    g.Leaf(origin + Vector3.up * (height * 0.25f), direction, height * 0.55f, 0.25f * growth, 0.04f * growth);
                    g.Ear(origin + Vector3.up * height, 0.065f * growth, 0.30f * growth);
                }
            }
            return g.Finish();
        }
    }
}
