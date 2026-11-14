using System;
using UnityEngine;

namespace NextGenFarm
{
    [Serializable]
    public sealed class CropVisual
    {
        public string cropId = "carrot";
        public Mesh[] stages = new Mesh[3];
        public Material[] materials = new Material[3];
        public Vector3 scale = Vector3.one;
        public Material[] accentMaterials = new Material[3];
    }
    [CreateAssetMenu(menuName = "NextGen Farm/Visual Library")]
    public sealed class FarmVisualLibrary : ScriptableObject
    {
        public Mesh groundMesh;
        public Material grass, drySoil, wetSoil;
        public Material precipitationMaterial;
        public Material[] demoPalette = new Material[0];
        public Material[] artMaterials;
        public Mesh detailGrassMesh;
        public Material cliffMaterial, pathMaterial;
        public CropVisual[] crops = new CropVisual[0];
        public GameObject farmerPrefab, tractorPrefab, cowPrefab;
    }
}
