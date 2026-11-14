using System;
using System.Collections.Generic;
using NextGenFarm.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace NextGenFarm
{
    public sealed class FarmInstanceRenderer : MonoBehaviour
    {
        private const int ChunkSize = 16;
        private sealed class Batch
        {
            public Mesh mesh;
            public Material material;
            public readonly Matrix4x4[] matrices = new Matrix4x4[ChunkSize * ChunkSize];
            public int count;
            public int submesh;
        }
        private sealed class Chunk
        {
            public int x, z;
            public bool dirty = true;
            public double nextChange = double.PositiveInfinity;
            public readonly List<Batch> batches = new List<Batch>();
        }
        private FarmGridManager grid;
        private FarmVisualLibrary library;
        private Mesh cube;
        private Material[] fallback;
        private Chunk[] chunks;
        private int chunksX;
        private bool subscribed;
        private TractorController driver;
        private readonly Matrix4x4[] marker = new Matrix4x4[4];
        public int DrawCalls { get; private set; }
        public void Track(TractorController tractor) { driver = tractor; }
        public void Initialize(FarmGridManager farm, FarmVisualLibrary visuals, Mesh mesh, Material[] materials)
        {
            grid = farm; library = visuals; cube = mesh; fallback = materials;
            chunksX = (grid.World.Width + ChunkSize - 1) / ChunkSize;
            int chunksZ = (grid.World.Depth + ChunkSize - 1) / ChunkSize;
            chunks = new Chunk[chunksX * chunksZ];
            for (int z = 0; z < chunksZ; z++)
                for (int x = 0; x < chunksX; x++) chunks[z * chunksX + x] = new Chunk { x = x, z = z };
            grid.World.Changed += Dirty; subscribed = true;
            if (!SystemInfo.supportsInstancing)
            { Debug.LogError("This farm renderer requires GPU instancing support."); enabled = false; }
        }
        private void Dirty(int index)
        {
            int x = index % grid.World.Width, z = index / grid.World.Width;
            chunks[(z / ChunkSize) * chunksX + x / ChunkSize].dirty = true;
        }
        private void Add(Chunk chunk, Mesh mesh, Material material, Matrix4x4 matrix, int submesh = 0)
        {
            if (mesh == null || material == null) return;
            Batch batch = null;
            foreach (var candidate in chunk.batches)
                if (candidate.mesh == mesh && candidate.material == material && candidate.submesh == submesh && candidate.count < candidate.matrices.Length)
                { batch = candidate; break; }
            if (batch == null)
            {
                material.enableInstancing = true;
                batch = new Batch { mesh = mesh, material = material, submesh = submesh }; chunk.batches.Add(batch);
            }
            batch.matrices[batch.count++] = matrix;
        }
        private void Rebuild(Chunk chunk, double now)
        {
            foreach (var batch in chunk.batches) batch.count = 0;
            chunk.nextChange = double.PositiveInfinity;
            int endX = Math.Min(grid.World.Width, (chunk.x + 1) * ChunkSize);
            int endZ = Math.Min(grid.World.Depth, (chunk.z + 1) * ChunkSize);
            for (int z = chunk.z * ChunkSize; z < endZ; z++)
                for (int x = chunk.x * ChunkSize; x < endX; x++)
                {
                    var tile = grid.World.Get(x, z);
                    var soil = tile.Soil(now);
                    Material ground = library == null ? null : soil == SoilState.Wet ? library.wetSoil :
                        soil == SoilState.Dry ? library.drySoil : library.grass;
                    Vector3 center = grid.CellCenter(x, z);
                    if (tile.road && library != null && library.pathMaterial != null) ground = library.pathMaterial;
                    float h = 0.3f + Mathf.Max(0, tile.height);
                    var groundMatrix = Matrix4x4.TRS(center - Vector3.up * (h * 0.5f), Quaternion.identity, new Vector3(grid.CellSize, h, grid.CellSize));
                    Add(chunk, library != null && library.groundMesh != null ? library.groundMesh : cube,
                        ground != null ? ground : fallback[(int)soil],
                        groundMatrix);
                    if (library != null && library.groundMesh != null && library.groundMesh.subMeshCount > 1)
                        Add(chunk, library.groundMesh, library.cliffMaterial, groundMatrix, 1);
                    if (soil == SoilState.Grass && !tile.road && !tile.blocked && library != null && library.detailGrassMesh != null)
                        for (int tuft = 0; tuft < 3; tuft++)
                        {
                            float a = ((x * 73 + z * 41 + tuft * 29) % 101) / 101f;
                            float b = ((x * 19 + z * 97 + tuft * 47) % 103) / 103f;
                            Add(chunk, library.detailGrassMesh, library.artMaterials[(int)FarmSurface.GrassBlade], Matrix4x4.TRS(
                                center + new Vector3((a - 0.5f) * grid.CellSize, 0.01f, (b - 0.5f) * grid.CellSize),
                                Quaternion.Euler(0, a * 360, 0), Vector3.one * (0.65f + b * 0.6f)));
                        }
                    if (tile.wetUntilUtc > now) chunk.nextChange = Math.Min(chunk.nextChange, tile.wetUntilUtc);
                    if (!tile.HasCrop) continue;
                    float growth = tile.Growth(now);
                    int stage = tile.Ready(now) ? 2 : growth < 0.33f ? 0 : 1;
                    double duration = Math.Max(1, tile.readyUtc - tile.plantedUtc);
                    double transition = stage == 0 ? tile.plantedUtc + duration * 0.33 : tile.readyUtc;
                    if (transition > now) chunk.nextChange = Math.Min(chunk.nextChange, transition);
                    CropVisual visual = null;
                    if (library != null && library.crops != null)
                        foreach (var candidate in library.crops)
                            if (candidate != null && candidate.cropId == tile.cropId) { visual = candidate; break; }
                    Mesh cropMesh = visual != null && visual.stages != null && visual.stages.Length > stage ? visual.stages[stage] : null;
                    Material cropMaterial = visual != null && visual.materials != null && visual.materials.Length > stage ? visual.materials[stage] : null;
                    Vector3 scale = visual != null ? visual.scale : new Vector3(0.55f, stage == 0 ? 0.2f : stage == 1 ? 0.6f : 1, 0.55f);
                    // Imported crop meshes must use a ground-level pivot; fallback cube has a center pivot.
                    Vector3 position = cropMesh != null ? center : center + Vector3.up * (scale.y * 0.5f);
                    Add(chunk, cropMesh != null ? cropMesh : cube, cropMaterial != null ? cropMaterial : fallback[3 + stage],
                        Matrix4x4.TRS(position, Quaternion.Euler(0, (x * 73 + z * 37) % 360, 0), scale));
                    if (cropMesh != null && cropMesh.subMeshCount > 1 && visual.accentMaterials != null && visual.accentMaterials.Length > stage)
                        Add(chunk, cropMesh, visual.accentMaterials[stage], Matrix4x4.TRS(position, Quaternion.Euler(0, (x * 73 + z * 37) % 360, 0), scale), 1);
                }
            chunk.dirty = false;
        }
        private void LateUpdate()
        {
            if (grid == null) return;
            double now = grid.Clock.Now;
            DrawCalls = 0;
            foreach (var chunk in chunks)
            {
                if (chunk.dirty || now >= chunk.nextChange) Rebuild(chunk, now);
                foreach (var batch in chunk.batches)
                {
                    if (batch.count == 0) continue;
                    var parameters = new RenderParams(batch.material) { shadowCastingMode = ShadowCastingMode.On,
                        receiveShadows = true, layer = gameObject.layer };
                    Graphics.RenderMeshInstanced(parameters, batch.mesh, batch.submesh, batch.matrices, batch.count);
                    DrawCalls++;
                }
            }
            if (driver != null)
            {
                var target = driver.TargetCell;
                if (!grid.World.Contains(target.x, target.y)) return;
                float size = grid.CellSize * (driver.Mounted ? 3 : 1);
                Vector3 center = grid.CellCenter(target.x, target.y) + Vector3.up * 0.035f;
                marker[0] = Matrix4x4.TRS(center + Vector3.forward * (size / 2), Quaternion.identity, new Vector3(size, 0.045f, 0.065f));
                marker[1] = Matrix4x4.TRS(center - Vector3.forward * (size / 2), Quaternion.identity, new Vector3(size, 0.045f, 0.065f));
                marker[2] = Matrix4x4.TRS(center + Vector3.right * (size / 2), Quaternion.identity, new Vector3(0.065f, 0.045f, size));
                marker[3] = Matrix4x4.TRS(center - Vector3.right * (size / 2), Quaternion.identity, new Vector3(0.065f, 0.045f, size));
                var parameters = new RenderParams(fallback[5]) { shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false };
                Graphics.RenderMeshInstanced(parameters, cube, 0, marker, marker.Length);
            }
        }
        private void OnDestroy() { if (subscribed) grid.World.Changed -= Dirty; }
    }
}
