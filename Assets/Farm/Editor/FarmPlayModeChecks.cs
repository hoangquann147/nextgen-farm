using System;
using System.IO;
using NextGenFarm.Core;
using UnityEditor;
using UnityEngine;

namespace NextGenFarm.Editor
{
    [InitializeOnLoad]
    public static class FarmPlayModeChecks
    {
        private const string Active = "NextGenFarm.Smoke.Active";
        private const string Phase = "NextGenFarm.Smoke.Phase";
        private const string Deadline = "NextGenFarm.Smoke.Deadline";
        static FarmPlayModeChecks() { if (SessionState.GetBool(Active, false)) EditorApplication.update += Pump; }
        public static void Run()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NEXTGEN_FARM_SAVE_DIR")))
                throw new InvalidOperationException("Set NEXTGEN_FARM_SAVE_DIR to an isolated test directory before the smoke run.");
            FarmProjectSetup.Configure();
            SessionState.SetBool(Active, true); SessionState.SetInt(Phase, 0); SessionState.SetInt("FarmSmokeExit", 0);
            SetDeadline(90);
            EditorApplication.update -= Pump; EditorApplication.update += Pump;
            EditorApplication.EnterPlaymode();
        }
        private static void SetDeadline(float seconds) => SessionState.SetFloat(Deadline, (float)EditorApplication.timeSinceStartup + seconds);
        private static void Pump()
        {
            if (!SessionState.GetBool(Active, false)) return;
            int phase = SessionState.GetInt(Phase, 0);
            try
            {
                if (phase == 0)
                {
                    if (EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode) return;
                    if (EditorApplication.isPlaying)
                    { SessionState.SetInt(Phase, 1); SetDeadline(3); }
                    else if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Deadline, 0)) throw new TimeoutException("Play Mode did not start");
                }
                else if (phase == 1 && EditorApplication.timeSinceStartup > SessionState.GetFloat(Deadline, 0))
                {
                    ExerciseGame();
                    SessionState.SetInt(Phase, 2); SetDeadline(3);
                }
                else if (phase == 2 && EditorApplication.timeSinceStartup > SessionState.GetFloat(Deadline, 0))
                {
                    var driver = UnityEngine.Object.FindFirstObjectByType<TractorController>();
                    Require(driver != null && driver.Automatic, "auto is active");
                    var before = new Vector3(SessionState.GetFloat("FarmSmokeX", 0), 0, SessionState.GetFloat("FarmSmokeZ", 0));
                    Vector3 after = driver.transform.position; after.y = 0;
                    Require(Vector3.Distance(before, after) > 0.1f, "auto tractor physically moves in Play Mode");
                    driver.StopAuto();
                    Directory.CreateDirectory("artifacts");
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/unity-playmode.png"));
                    SessionState.SetInt(Phase, 3); SetDeadline(3);
                }
                else if (phase == 3 && EditorApplication.timeSinceStartup > SessionState.GetFloat(Deadline, 0))
                { SessionState.SetInt(Phase, 4); EditorApplication.ExitPlaymode(); }
                else if (phase == 4 && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetBool(Active, false); EditorApplication.update -= Pump;
                    int result = SessionState.GetInt("FarmSmokeExit", 0);
                    if (result == 0) Debug.Log("FARM_PLAYMODE_CHECKS_PASSED");
                    EditorApplication.Exit(result);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex); SessionState.SetInt("FarmSmokeExit", 1);
                SessionState.SetInt(Phase, 4); EditorApplication.ExitPlaymode();
            }
        }
        private static void ExerciseGame()
        {
            var grid = UnityEngine.Object.FindFirstObjectByType<FarmGridManager>();
            var animals = UnityEngine.Object.FindFirstObjectByType<AnimalPenSystem>();
            var driver = UnityEngine.Object.FindFirstObjectByType<TractorController>();
            var saves = UnityEngine.Object.FindFirstObjectByType<SaveLoadSystem>();
            var renderer = UnityEngine.Object.FindFirstObjectByType<FarmInstanceRenderer>();
            Require(grid != null && animals != null && driver != null && saves != null, "bootstrap systems exist");
            Require(Camera.main != null && Camera.main.GetComponent<FarmCamera>() != null, "camera follows player");
            Require(SystemInfo.supportsInstancing && renderer != null && renderer.DrawCalls > 0, "instancing submits farm draw batches");
            var center = new Vector2Int(3, 3);
            Require(grid.Act(center, FarmAction.Plow, "carrot", true) == 9, "Unity plow 3x3");
            Require(grid.Act(center, FarmAction.Sow, "carrot", true) == 9, "Unity sow 3x3");
            Require(grid.Act(center, FarmAction.Water, "carrot", true) == 9, "Unity water 3x3");
            for (int z = 2; z <= 4; z++)
                for (int x = 2; x <= 4; x++) grid.World.Get(x, z).readyUtc = grid.Clock.Now - 1;
            Require(grid.Act(center, FarmAction.Harvest, "carrot", true) == 9, "Unity harvest 3x3");
            Require(grid.World.Wallet.inventory.Find(i => i.id == "carrot").count == 27, "Unity harvest yield");
            Require(animals.FeedAll() == 1, "Unity feed pen");
            animals.World.Pens[0].readyUtc = grid.Clock.Now - 1;
            Require(animals.HarvestAll() == 1, "Unity harvest animal");
            Require(saves.Save(), "Unity runtime save");
            var snapshot = JsonUtility.FromJson<FarmSnapshot>(File.ReadAllText(saves.SavePath));
            var restored = snapshot.Restore(new[] { new CropDefinition(), new CropDefinition { id = "wheat" } },
                new[] { new AnimalDefinition() }, out _);
            Require(restored.Wallet.inventory.Find(i => i.id == "carrot").count == 27, "Unity runtime save roundtrip");
            driver.SetMounted(true);
            SessionState.SetFloat("FarmSmokeX", driver.transform.position.x); SessionState.SetFloat("FarmSmokeZ", driver.transform.position.z);
            driver.ToggleAuto(); Require(driver.Automatic, "Unity auto tractor starts");
            Debug.Log("FARM_RUNTIME_ACTIONS_PASSED");
        }
        private static void Require(bool condition, string label)
        { if (!condition) throw new InvalidOperationException("Play Mode check failed: " + label); }
    }
}
