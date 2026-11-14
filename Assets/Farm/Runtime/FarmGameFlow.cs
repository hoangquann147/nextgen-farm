using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace NextGenFarm
{
    public enum FarmScreen { MainMenu, Playing, Pointer, Paused, Settings }

    // Owns cursor and screen transitions. No Time.timeScale changes: crop deadlines stay in UTC.
    [DefaultExecutionOrder(-150)]
    public sealed class FarmGameFlow : MonoBehaviour
    {
        public static FarmGameFlow Instance { get; private set; }
        public static bool GameplayAllowed => Instance == null || Instance.Screen == FarmScreen.Playing;
        public FarmScreen Screen { get; private set; }
        public FarmSettings Settings { get; private set; }
        public FarmAudio Audio { get; private set; }
        public bool IsWorldVisible => Screen == FarmScreen.Playing || Screen == FarmScreen.Pointer;
        private FarmBootstrap app;
        private FarmHUD hud;
        private FarmCamera cameraRig;
        private RectTransform root, panel;
        private Font font;
        private FarmScreen settingsOrigin;
        private float ignoreLookUntil;
        private bool focused = true;
        private readonly Color cream = new Color(1, .97f, .86f), ink = new Color(.15f, .28f, .22f);
        public bool CanLook => GameplayAllowed && focused && UnityEngine.Cursor.lockState == CursorLockMode.Locked && Time.unscaledTime >= ignoreLookUntil;

        public void Initialize(FarmBootstrap bootstrap, FarmHUD gameHud, FarmCamera camera)
        {
            Instance = this; app = bootstrap; hud = gameHud; cameraRig = camera;
            Settings = gameObject.AddComponent<FarmSettings>(); Settings.Initialize(camera.GetComponent<Camera>());
            Audio = gameObject.AddComponent<FarmAudio>(); Audio.Initialize(Settings, app.Grid);
            font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 24);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject("Farm Menus", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false); root = go.GetComponent<RectTransform>();
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            ShowMainMenu();
        }
        private void Update()
        {
            if (!focused || FarmInput.Suppressed) return;
            bool escape = FarmInput.MenuPress(KeyCode.Escape);
            bool leftAlt = FarmInput.MenuPress(KeyCode.LeftAlt), rightAlt = FarmInput.MenuPress(KeyCode.RightAlt), tab = FarmInput.MenuPress(KeyCode.Tab);
            if (escape) Escape();
            else if (leftAlt || rightAlt || tab) TogglePointer();
        }
        public void Escape()
        {
            if (Screen == FarmScreen.Settings) CloseSettings();
            else if (Screen == FarmScreen.Paused) Play();
            else if (Screen == FarmScreen.Playing || Screen == FarmScreen.Pointer)
            {
                if (hud.IsShopOpen) hud.CloseShop();
                else ShowPause();
            }
        }
        public void TogglePointer()
        {
            if (Screen == FarmScreen.Playing) SetState(FarmScreen.Pointer);
            else if (Screen == FarmScreen.Pointer) { hud.CloseShop(); SetState(FarmScreen.Playing); }
        }
        public void CatalogOpened() { if (IsWorldVisible) SetState(FarmScreen.Pointer); }
        public void CatalogClosed() { if (Screen == FarmScreen.Pointer) SetState(FarmScreen.Playing); }
        public void Play() { Clear(); hud.CloseShop(); SetState(FarmScreen.Playing); }
        public void ShowMainMenu()
        {
            if (app != null && app.Saves != null) app.Saves.Save();
            hud.CloseShop(); SetState(FarmScreen.MainMenu); Clear();
            panel = Box(root, "Main menu", new Vector2(440, 560), new Vector2(.23f, .5f), new Color(.08f, .19f, .15f, .94f));
            TextAt(panel, "ĐỒNG NỘI", 48, new Vector2(0, 190), new Vector2(400, 70), cream);
            TextAt(panel, "Một khoảng bình yên giữa thung lũng", 20, new Vector2(0, 130), new Vector2(400, 55), new Color(.77f, .85f, .64f));
            MakeButton(panel, "Chơi", new Vector2(0, 35), Play);
            MakeButton(panel, "Cài đặt", new Vector2(0, -55), OpenSettings);
            MakeButton(panel, "Thoát", new Vector2(0, -145), ExitGame);
            TextAt(panel, "Nông trại vẫn lớn lên khi bạn nghỉ ngơi", 16, new Vector2(0, -233), new Vector2(400, 44), cream);
        }
        public void ShowPause()
        {
            hud.CloseShop(); app.Saves.Save(); SetState(FarmScreen.Paused); Clear();
            panel = Overlay("TẠM NGHỈ", new Vector2(500, 590));
            MakeButton(panel, "Tiếp tục", new Vector2(0, 110), Play);
            MakeButton(panel, "Cài đặt", new Vector2(0, 20), OpenSettings);
            MakeButton(panel, "Về sảnh chính", new Vector2(0, -70), ShowMainMenu);
            MakeButton(panel, "Thoát game", new Vector2(0, -160), ExitGame);
            TextAt(panel, "Cây vẫn tăng trưởng theo thời gian thực", 17, new Vector2(0, -245), new Vector2(450, 40), ink);
        }
        public void OpenSettings()
        {
            if (Screen == FarmScreen.Settings) return;
            settingsOrigin = Screen;
            if (hud.IsShopOpen) { hud.CloseShop(); settingsOrigin = FarmScreen.Playing; }
            SetState(FarmScreen.Settings); Clear();
            panel = Overlay("CÀI ĐẶT", new Vector2(1040, 830));
            TextAt(panel, "ÂM THANH", 23, new Vector2(-340, 295), new Vector2(280, 40), ink);
            VolumeRow("Âm lượng tổng", "Master", 220, Settings.Data.master, v => Settings.SetVolumes(v, Settings.Data.music, Settings.Data.sfx));
            VolumeRow("Nhạc nền", "Music", 140, Settings.Data.music, v => Settings.SetVolumes(Settings.Data.master, v, Settings.Data.sfx));
            VolumeRow("Hiệu ứng", "SFX", 60, Settings.Data.sfx, v => Settings.SetVolumes(Settings.Data.master, Settings.Data.music, v));
            TextAt(panel, "HÌNH ẢNH", 23, new Vector2(-340, -35), new Vector2(280, 40), ink);
            ChoiceRow("Chất lượng", "Quality", -112, new[] { "Thấp", "Trung bình", "Cao", "Siêu cao" }, Settings.Data.quality, Settings.SetQuality);
            int[] caps = { 30, 45, 60, -1 }; int index = Array.IndexOf(caps, Settings.Data.fps);
            ChoiceRow("Giới hạn FPS", "FPS", -197, new[] { "30 FPS", "45 FPS", "60 FPS", "Không giới hạn" }, index, i => Settings.SetFPS(caps[i]));
            TextAt(panel, "Thay đổi áp dụng ngay • Tắt VSync để dùng giới hạn FPS", 17, new Vector2(0, -275), new Vector2(950, 40), ink);
            MakeButton(panel, "Lưu và quay lại", new Vector2(0, -350), CloseSettings);
        }
        public void CloseSettings()
        {
            bool saved = Settings.Save();
            if (settingsOrigin == FarmScreen.MainMenu) ShowMainMenu();
            else if (settingsOrigin == FarmScreen.Paused) ShowPause();
            else { Play(); if (settingsOrigin == FarmScreen.Pointer) SetState(FarmScreen.Pointer); }
            if (!saved) hud.ShowToast("Chưa lưu được cài đặt. Kiểm tra quyền ghi thư mục.");
        }
        private void SetState(FarmScreen state)
        {
            Screen = state; FarmInput.ClearGUIState();
            if (hud != null) hud.SetVisible(IsWorldVisible);
            if (hud != null) hud.SetPointerInteraction(state == FarmScreen.Pointer);
            if (EventSystem.current != null)
            {
                EventSystem.current.sendNavigationEvents = state != FarmScreen.Playing;
                EventSystem.current.SetSelectedGameObject(null);
            }
            if (cameraRig != null) cameraRig.MenuOrbit = state == FarmScreen.MainMenu || (state == FarmScreen.Settings && settingsOrigin == FarmScreen.MainMenu);
            RefreshCursor();
        }
        private void RefreshCursor()
        {
            bool locked = focused && Screen == FarmScreen.Playing;
            UnityEngine.Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            UnityEngine.Cursor.visible = !locked;
            ignoreLookUntil = Time.unscaledTime + .12f;
        }
        private void OnApplicationFocus(bool value)
        {
            focused = value;
            if (!value && Screen == FarmScreen.Playing && app != null) SetState(FarmScreen.Pointer);
            RefreshCursor();
        }
        private void ExitGame() { Settings.Save(); app.Saves.Save(); Application.Quit(); }
        private void Clear()
        {
            foreach (Transform child in root) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        }
        private RectTransform Box(Transform parent, string name, Vector2 size, Vector2 anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = size;
            var image = go.GetComponent<Image>(); image.sprite = FarmUIArt.Rounded; image.type = Image.Type.Sliced; image.color = color;
            return rect;
        }
        private RectTransform Overlay(string title, Vector2 size)
        {
            var shade = Box(root, "Menu backdrop", Vector2.zero, Vector2.zero, new Color(.02f, .08f, .06f, .55f));
            shade.anchorMax = Vector2.one; shade.offsetMin = shade.offsetMax = Vector2.zero;
            var card = Box(shade, title, size, new Vector2(.5f, .5f), cream);
            TextAt(card, title, 34, new Vector2(0, size.y / 2 - 55), new Vector2(size.x - 70, 60), ink);
            return card;
        }
        private Text TextAt(Transform parent, string value, int size, Vector2 position, Vector2 dimensions, Color color)
        {
            var go = new GameObject(value, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = dimensions;
            var label = go.GetComponent<Text>(); label.font = font; label.text = value; label.fontSize = size; label.color = color;
            label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false; return label;
        }
        private Button MakeButton(Transform parent, string caption, Vector2 position, Action clicked, Vector2? size = null)
        {
            var rect = Box(parent, caption, size ?? new Vector2(350, 66), new Vector2(.5f, .5f), new Color(.82f, .88f, .63f)); rect.anchoredPosition = position;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
            TextAt(rect, caption, 23, Vector2.zero, (size ?? new Vector2(350, 66)) - new Vector2(10, 4), ink);
            button.onClick.AddListener(() => { Audio.Click(); clicked(); }); return button;
        }
        private void VolumeRow(string title, string id, float y, float value, Action<float> changed)
        {
            TextAt(panel, title, 22, new Vector2(-312, y), new Vector2(300, 50), ink);
            var track = Box(panel, id + " slider", new Vector2(420, 32), new Vector2(.5f, .5f), new Color(.7f, .76f, .65f)); track.anchoredPosition = new Vector2(120, y);
            var handle = Box(track, "Handle", new Vector2(30, 44), new Vector2(.5f, .5f), new Color(.22f, .44f, .31f));
            var slider = track.gameObject.AddComponent<Slider>(); slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight; slider.minValue = 0; slider.maxValue = 1; slider.SetValueWithoutNotify(value);
            var number = TextAt(panel, Mathf.RoundToInt(value * 100) + "%", 21, new Vector2(420, y), new Vector2(100, 45), ink);
            slider.onValueChanged.AddListener(v => { changed(v); number.text = Mathf.RoundToInt(v * 100) + "%"; });
        }
        private void ChoiceRow(string title, string id, float y, string[] choices, int selected, Action<int> changed)
        {
            TextAt(panel, title, 22, new Vector2(-312, y), new Vector2(300, 50), ink);
            var value = TextAt(panel, choices[selected], 24, new Vector2(135, y), new Vector2(380, 58), ink);
            int current = selected;
            MakeButton(panel, "‹", new Vector2(-75, y), () => { current = (current + choices.Length - 1) % choices.Length; changed(current); value.text = choices[current]; }, new Vector2(65, 58)).name = id + " Previous";
            MakeButton(panel, "›", new Vector2(345, y), () => { current = (current + 1) % choices.Length; changed(current); value.text = choices[current]; }, new Vector2(65, 58)).name = id + " Next";
        }
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            UnityEngine.Cursor.lockState = CursorLockMode.None; UnityEngine.Cursor.visible = true;
            if (font != null) Destroy(font);
        }
    }
}
