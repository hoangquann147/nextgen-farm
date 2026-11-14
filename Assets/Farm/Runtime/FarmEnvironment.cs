using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace NextGenFarm
{
    // HDRP 17.x presentation only. Weather does not grant offline watering bonuses.
    public sealed class FarmEnvironment : MonoBehaviour
    {
        private SeasonTimeManager clock;
        private Camera view;
        private Light sun;
        private Light moon;
        private Exposure exposure;
        private VolumeProfile profile;
        private GradientSky sky;
        private Fog fog;
        private ParticleSystem precipitation;
        private ParticleSystemRenderer particlesRenderer;
        private Material particleMaterial;
        private float nextTick;
        public string Weather { get; private set; }

        public void Initialize(SeasonTimeManager time, Camera camera, Transform root, Material weatherMaterial = null)
        {
            clock = time; view = camera;
            var cameraData = camera.GetComponent<HDAdditionalCameraData>() ?? camera.gameObject.AddComponent<HDAdditionalCameraData>();
            cameraData.volumeLayerMask = ~0;
            var lightObject = new GameObject("Farm Sun"); lightObject.transform.SetParent(root, false);
            sun = lightObject.AddComponent<Light>(); sun.type = LightType.Directional;
            if (lightObject.GetComponent<HDAdditionalLightData>() == null) lightObject.AddComponent<HDAdditionalLightData>();
            sun.lightUnit = LightUnit.Lux; sun.shadows = LightShadows.Soft;
            var moonObject = new GameObject("Farm Moon Fill"); moonObject.transform.SetParent(root, false);
            moon = moonObject.AddComponent<Light>(); moon.type = LightType.Directional;
            if (moonObject.GetComponent<HDAdditionalLightData>() == null) moonObject.AddComponent<HDAdditionalLightData>();
            moon.lightUnit = LightUnit.Lux; moon.color = new Color(0.45f, 0.62f, 1);
            moon.transform.rotation = Quaternion.Euler(55, 145, 0);
            var volumeObject = new GameObject("Farm Environment Volume"); volumeObject.transform.SetParent(root, false);
            var volume = volumeObject.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 10;
            profile = ScriptableObject.CreateInstance<VolumeProfile>(); volume.sharedProfile = profile;
            var visual = profile.Add<VisualEnvironment>(true); visual.skyType.Override(3); // HDRP GradientSky ID
            sky = profile.Add<GradientSky>(true);
            sky.updateMode.Override(EnvironmentUpdateMode.Realtime); sky.updatePeriod.Override(2);
            exposure = profile.Add<Exposure>(true); exposure.mode.Override(ExposureMode.Fixed); exposure.fixedExposure.Override(12.5f);
            var tonemapping = profile.Add<Tonemapping>(true); tonemapping.mode.Override(TonemappingMode.ACES);
            fog = profile.Add<Fog>(true); fog.enabled.Override(true); fog.meanFreePath.Override(250);
            fog.enableVolumetricFog.Override(true);
            var particles = new GameObject("Cosmetic Rain Snow"); particles.transform.SetParent(root, false);
            precipitation = particles.AddComponent<ParticleSystem>(); precipitation.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = precipitation.main; main.loop = true; main.maxParticles = 5000;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.startLifetime = 2; main.startSpeed = 12;
            main.startSize = 0.035f; main.playOnAwake = false;
            var shape = precipitation.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(35, 35, 0.1f);
            particles.transform.rotation = Quaternion.Euler(90, 0, 0);
            particlesRenderer = particles.GetComponent<ParticleSystemRenderer>();
            var particleShader = Shader.Find("HDRP/Unlit");
            if (weatherMaterial == null && particleShader == null) throw new InvalidOperationException("Missing HDRP/Unlit shader");
            particleMaterial = weatherMaterial != null ? new Material(weatherMaterial) : new Material(particleShader);
            particleMaterial.SetColor("_UnlitColor", new Color(0.55f, 0.65f, 0.75f));
            particlesRenderer.sharedMaterial = particleMaterial;
        }
        private void Update()
        {
            if (clock == null || Time.unscaledTime < nextTick) return;
            nextTick = Time.unscaledTime + 0.2f;
            float day = Mathf.Max(0, Mathf.Sin((clock.DayFraction - 0.25f) * 2 * Mathf.PI));
            sun.transform.rotation = Quaternion.Euler(clock.DayFraction * 360 - 90, -25, 0);
            sun.intensity = Mathf.Lerp(300, 100000, day);
            moon.intensity = 300 * (1 - day);
            float groundIlluminance = Mathf.Max(300, 100000 * day * day);
            exposure.fixedExposure.Override(12.95f + Mathf.Log(groundIlluminance / 100000, 2));
            sun.color = Color.Lerp(new Color(1, 0.56f, 0.3f), Color.white, day);
            sky.exposure.Override(Mathf.Lerp(5, 12.8f, day));
            sky.top.Override(new Color(0.13f, 0.30f, 0.62f));
            sky.middle.Override(new Color(0.5f, 0.65f, 0.8f));
            sky.bottom.Override(new Color(0.17f, 0.22f, 0.12f));
            bool snowy = clock.CurrentSeason == Core.Season.Winter;
            long minute = (long)Math.Floor(Math.Max(0, clock.Now - clock.EpochUtc) / 60);
            bool rainy = !snowy && clock.CurrentSeason != Core.Season.Summer && minute % 3 == 1;
            Weather = snowy ? "Snow" : rainy ? "Rain" : "Clear";
            fog.meanFreePath.Override(snowy ? 100 : rainy ? 180 : 850);
            precipitation.transform.position = view.transform.position + view.transform.forward * 12 + Vector3.up * 14;
            var emission = precipitation.emission; emission.rateOverTime = snowy ? 150 : rainy ? 650 : 0;
            var main = precipitation.main; main.startSpeed = snowy ? 2 : 12;
            main.startLifetime = snowy ? 10 : 2; main.startSize = snowy ? 0.06f : 0.035f;
            particleMaterial.SetColor("_UnlitColor", snowy ? Color.white * 4 : new Color(0.5f, 0.6f, 0.7f) * 4);
            particlesRenderer.renderMode = snowy ? ParticleSystemRenderMode.Billboard : ParticleSystemRenderMode.Stretch;
            particlesRenderer.lengthScale = snowy ? 1 : 4;
            if (!precipitation.isPlaying) precipitation.Play();
        }
        private void OnDestroy()
        {
            if (profile != null)
            {
                foreach (var component in profile.components) if (component != null) Destroy(component);
                Destroy(profile);
            }
            if (particleMaterial != null) Destroy(particleMaterial);
        }
    }
}
