using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.HighDefinition;

namespace NextGenFarm
{
    // Invoked only by the explicit native QA flag; never mutates the user's normal save/settings.
    public static class FarmFlowChecks
    {
        public static void Verify(FarmGameFlow flow)
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NEXTGEN_FARM_SAVE_DIR")))
                throw new InvalidOperationException("Flow checks require an isolated save directory");
            flow.ShowMainMenu(); Require(flow.Screen == FarmScreen.MainMenu && !FarmGameFlow.GameplayAllowed, "main menu blocks farming");
            Require(Cursor.visible && Cursor.lockState == CursorLockMode.None, "main menu cursor");
            Click(flow, "Cài đặt"); Require(flow.Screen == FarmScreen.Settings, "main settings callback");
            var master = Slider(flow, "Master slider"); var music = Slider(flow, "Music slider"); var sfx = Slider(flow, "SFX slider");
            master.value = .6f; music.value = .3f; sfx.value = .7f;
            Require(Mathf.Approximately(flow.Audio.MusicSource.volume, .18f) && Mathf.Approximately(flow.Audio.SfxSource.volume, .42f), "independent audio buses");
            var hd = Camera.main.GetComponent<HDAdditionalCameraData>();
            for (int i = 0; i < 4; i++)
            {
                flow.Settings.SetQuality(i);
                Require(flow.Settings.Data.quality == i && hd.renderingPathCustomFrameSettings.IsEnabled(FrameSettingsField.Volumetrics) == (i >= 2), "HDRP quality preset " + i);
            }
            foreach (int cap in new[] { 30, 45, 60, -1 })
            {
                flow.Settings.SetFPS(cap); Require(Application.targetFrameRate == cap && QualitySettings.vSyncCount == 0, "FPS cap " + cap);
            }
            flow.Settings.SetQuality(2); flow.Settings.SetFPS(45);
            Click(flow, "Lưu và quay lại"); Require(flow.Screen == FarmScreen.MainMenu, "settings returns to main");
            var saved = JsonUtility.FromJson<FarmSettingsData>(File.ReadAllText(flow.Settings.StoragePath));
            Require(saved.fps == 45 && saved.quality == 2 && Mathf.Approximately(saved.master, .6f), "settings persistence");
            Click(flow, "Chơi"); Require(FarmGameFlow.GameplayAllowed && flow.Screen == FarmScreen.Playing, "play callback");
            flow.TogglePointer(); Require(!FarmGameFlow.GameplayAllowed && Cursor.visible, "pointer releases controls");
            flow.TogglePointer(); Require(flow.Screen == FarmScreen.Playing, "pointer resume");
            var hud = UnityEngine.Object.FindFirstObjectByType<FarmHUD>(); hud.OpenShop(0);
            Require(flow.Screen == FarmScreen.Pointer && hud.IsShopOpen, "catalog unlocks cursor");
            flow.Escape(); Require(flow.Screen == FarmScreen.Playing && !hud.IsShopOpen, "escape closes catalog first");
            flow.ShowPause(); Click(flow, "Cài đặt"); Click(flow, "Lưu và quay lại");
            Require(flow.Screen == FarmScreen.Paused, "settings returns to pause");
            flow.Escape(); Require(flow.Screen == FarmScreen.Playing, "escape resumes");
            var invalid = new FarmSettingsData { fps = 1, quality = 50, master = float.NaN, music = -5, sfx = 5 }; invalid.Validate();
            Require(invalid.fps == 60 && invalid.quality == 3 && invalid.master == .5f && invalid.music == 0 && invalid.sfx == 1, "invalid settings sanitized");
            flow.Settings.SetFPS(60); flow.Settings.Save();
            Debug.Log("FARM_FLOW_CHECKS_PASSED: menu callbacks, pointer/catalog/pause transitions, audio buses, four HDRP presets, FPS caps, settings persistence, invalid data");
        }
        private static Slider Slider(FarmGameFlow flow, string name)
        {
            foreach (var slider in flow.GetComponentsInChildren<Slider>()) if (slider.name == name) return slider;
            throw new Exception("Missing slider: " + name);
        }
        private static void Click(FarmGameFlow flow, string caption)
        {
            foreach (var button in flow.GetComponentsInChildren<Button>())
                if (button.GetComponentInChildren<Text>()?.text == caption) { button.onClick.Invoke(); return; }
            throw new Exception("Missing button: " + caption);
        }
        private static void Require(bool condition, string message) { if (!condition) throw new Exception("Flow check failed: " + message); }
    }
}
