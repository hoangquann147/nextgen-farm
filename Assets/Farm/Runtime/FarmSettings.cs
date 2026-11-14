using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace NextGenFarm
{
    [Serializable]
    public sealed class FarmSettingsData
    {
        public float master = .8f, music = .45f, sfx = .8f;
        public int quality = 2, fps = 60;
        public void Validate()
        {
            master = Level(master); music = Level(music); sfx = Level(sfx);
            quality = Mathf.Clamp(quality, 0, 3);
            if (fps != 30 && fps != 45 && fps != 60 && fps != -1) fps = 60;
        }
        private static float Level(float value) => float.IsNaN(value) || float.IsInfinity(value) ? .5f : Mathf.Clamp01(value);
    }

    // Explicit HDRP camera/light overrides: built-in shadowDistance alone does not configure HDRP.
    public sealed class FarmSettings : MonoBehaviour
    {
        public FarmSettingsData Data { get; private set; } = new FarmSettingsData();
        public float DrawDistance { get; private set; } = 600;
        public string StoragePath { get; private set; }
        public event Action Changed;
        private Camera view;
        private HDAdditionalLightData[] lights;
        private float targetDistance;
        private VolumeProfile qualityProfile;
        public void Initialize(Camera camera)
        {
            view = camera;
            lights = FindObjectsByType<HDAdditionalLightData>(FindObjectsSortMode.None);
            var volume = new GameObject("Farm quality overrides").AddComponent<Volume>();
            volume.transform.SetParent(transform, false); volume.isGlobal = true; volume.priority = 30;
            qualityProfile = ScriptableObject.CreateInstance<VolumeProfile>(); volume.sharedProfile = qualityProfile;
            qualityProfile.Add<HDShadowSettings>(true); qualityProfile.Add<Bloom>(true); qualityProfile.Add<ScreenSpaceAmbientOcclusion>(true);
            string directory = Environment.GetEnvironmentVariable("NEXTGEN_FARM_SAVE_DIR");
            if (string.IsNullOrWhiteSpace(directory)) directory = Application.persistentDataPath;
            StoragePath = Path.Combine(directory, "settings-v1.json");
            try { if (File.Exists(StoragePath)) Data = JsonUtility.FromJson<FarmSettingsData>(File.ReadAllText(StoragePath)) ?? new FarmSettingsData(); }
            catch (Exception ex) { Debug.LogWarning("Settings fallback: " + ex.Message); }
            Apply(); DrawDistance = targetDistance; view.farClipPlane = DrawDistance;
        }
        public void SetVolumes(float master, float music, float sfx)
        { Data.master = master; Data.music = music; Data.sfx = sfx; Data.Validate(); Changed?.Invoke(); }
        public void SetQuality(int quality) { Data.quality = quality; Apply(); }
        public void SetFPS(int fps) { Data.fps = fps; Apply(); }
        public void Apply()
        {
            Data.Validate(); int q = Data.quality;
            targetDistance = new[] { 180f, 320f, 600f, 1000f }[q];
            qualityProfile.TryGet<HDShadowSettings>(out var shadows); shadows.maxShadowDistance.Override(new[] { 30f, 60f, 110f, 180f }[q]);
            qualityProfile.TryGet<Bloom>(out var bloom); bloom.intensity.Override(q == 0 ? 0 : q == 1 ? .08f : .15f);
            qualityProfile.TryGet<ScreenSpaceAmbientOcclusion>(out var ao); ao.intensity.Override(q < 2 ? 0 : .65f);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Data.fps;
            QualitySettings.lodBias = new[] { .6f, 1f, 1.5f, 2f }[q];
            QualitySettings.globalTextureMipmapLimit = q == 0 ? 1 : 0;
            QualitySettings.anisotropicFiltering = q < 2 ? AnisotropicFiltering.Enable : AnisotropicFiltering.ForceEnable;
            var hd = view.GetComponent<HDAdditionalCameraData>();
            if (hd != null)
            {
                hd.customRenderingSettings = true;
                // Keep exposure/tonemapping enabled at every level for a consistent HDRP image.
                SetFrame(hd, FrameSettingsField.Postprocess, true);
                SetFrame(hd, FrameSettingsField.Bloom, q >= 1);
                SetFrame(hd, FrameSettingsField.SSAO, q >= 2);
                SetFrame(hd, FrameSettingsField.Volumetrics, q >= 2);
                hd.antialiasing = q == 0 ? HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing : HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
            }
            int resolution = new[] { 512, 1024, 2048, 4096 }[q];
            foreach (var light in lights)
            {
                if (light == null) continue;
                light.SetShadowResolutionOverride(true); light.SetShadowResolution(resolution);
                light.SetShadowFadeDistance(new[] { 30f, 60f, 110f, 180f }[q]);
            }
            Changed?.Invoke();
        }
        private static void SetFrame(HDAdditionalCameraData hd, FrameSettingsField field, bool enabled)
        {
            hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field] = true;
            hd.renderingPathCustomFrameSettings.SetEnabled(field, enabled);
        }
        private void Update()
        {
            if (view == null) return;
            DrawDistance = Mathf.Lerp(DrawDistance, targetDistance, 1 - Mathf.Exp(-5 * Time.unscaledDeltaTime));
            view.farClipPlane = DrawDistance;
        }
        public bool Save()
        {
            try
            {
                Data.Validate(); Directory.CreateDirectory(Path.GetDirectoryName(StoragePath));
                string temporary = StoragePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(Data, true));
                if (File.Exists(StoragePath)) File.Replace(temporary, StoragePath, null);
                else File.Move(temporary, StoragePath);
                return true;
            }
            catch (Exception ex) { Debug.LogWarning("Cannot save settings: " + ex.Message); return false; }
        }
        private void OnApplicationQuit() { if (StoragePath != null) Save(); }
        private void OnDestroy()
        {
            if (qualityProfile == null) return;
            foreach (var component in qualityProfile.components) Destroy(component);
            Destroy(qualityProfile);
        }
    }
}
