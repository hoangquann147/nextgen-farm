using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using NextGenFarm.Core;

namespace NextGenFarm
{
    // Engine-native QA capture. It does not automate or capture another desktop app.
    public sealed class FarmCaptureRunner : MonoBehaviour
    {
        private IEnumerator Start()
        {
            string[] args=Environment.GetCommandLineArgs();string path=null;float delay=5;bool quit=false,shop=false,verify=false,menu=false,settings=false,flow=false;
            for(int i=0;i<args.Length;i++)
            {
                if(args[i]=="-farmCapture"&&i+1<args.Length)path=args[++i];
                else if(args[i]=="-farmCaptureDelay"&&i+1<args.Length)float.TryParse(args[++i],out delay);
                else if(args[i]=="-farmCaptureAndQuit")quit=true;
                else if(args[i]=="-farmCaptureShop")shop=true;
                else if(args[i]=="-farmVerifyPresentation")verify=true;
                else if(args[i]=="-farmCaptureMenu")menu=true;
                else if(args[i]=="-farmCaptureSettings")settings=true;
                else if(args[i]=="-farmVerifyFlow")flow=true;
            }
            if(path==null)yield break;
            FarmInput.Suppressed=true;
            var gameFlow=FindFirstObjectByType<FarmGameFlow>();
            if(!menu&&!settings)gameFlow.Play();
            path=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(path));
            yield return new WaitForSecondsRealtime(Mathf.Max(2,delay));
            if(flow)
            {
                bool valid=false;
                try { FarmFlowChecks.Verify(gameFlow); valid=true; } catch(Exception ex){Debug.LogException(ex);}
                if(!valid){Application.Quit(2);yield break;}
            }
            if(verify)
            {
                bool valid=false;
                try { VerifyPresentation(); valid=true; }
                catch(Exception ex){Debug.LogException(ex);}
                if(!valid){Application.Quit(2);yield break;}
                yield return new WaitForSecondsRealtime(.8f);
            }
            if(menu)gameFlow.ShowMainMenu();
            if(settings)gameFlow.OpenSettings();
            if(menu||settings)yield return new WaitForSecondsRealtime(1);
            if(shop){FindFirstObjectByType<FarmHUD>().OpenShop(0);yield return new WaitForSecondsRealtime(1);}
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(path);
            float deadline=Time.realtimeSinceStartup+8;
            while(!File.Exists(path)&&Time.realtimeSinceStartup<deadline)yield return null;
            if(File.Exists(path))Debug.Log("FARM_NATIVE_CAPTURE_PASSED: "+path);else Debug.LogError("Farm capture did not write PNG: "+path);
            yield return new WaitForSecondsRealtime(.3f);
            FarmInput.Suppressed=false;
            if(quit)Application.Quit(File.Exists(path)?0:1);
        }
        private static void VerifyPresentation()
        {
            if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NEXTGEN_FARM_SAVE_DIR")))throw new InvalidOperationException("Presentation QA requires an isolated save directory");
            var app=FindFirstObjectByType<FarmBootstrap>();var hud=FindFirstObjectByType<FarmHUD>();
            if(app==null||hud==null)throw new Exception("Missing presentation systems");
            app.Driver.SetMounted(false);app.Driver.SelectedAction=FarmAction.Water;
            Button action=null;
            foreach(var button in hud.GetComponentsInChildren<Button>()) if(button.GetComponentInChildren<Text>()?.text=="SPACE   Thao tác") { action=button;break; }
            if(action==null)throw new Exception("Missing use-tool button");
            action.onClick.Invoke();
            if(app.Driver.LastAffected!=1)throw new Exception("Context button did not water one crop");
            hud.OpenShop(0);Button corn=null;
            foreach(var button in hud.GetComponentsInChildren<Button>())if(button.transform.parent.name=="corn")corn=button;
            if(corn==null)throw new Exception("Missing corn catalog card");corn.onClick.Invoke();
            if(app.Driver.SelectedCrop!="corn"||app.Driver.SelectedAction!=FarmAction.Sow)throw new Exception("Catalog did not select seed and sow tool");
            app.Grid.Act(new Vector2Int(13,12),FarmAction.Water,"corn",true);
            if(!app.Grid.World.ClaimCareReward(true)||app.Grid.World.ClaimCareReward(true))throw new Exception("Care reward is missing or repeatable");
            if(app.Grid.World.Wallet.diamonds!=5||!app.Saves.Save())throw new Exception("Reward/save mismatch");
            var saved=JsonUtility.FromJson<FarmSnapshot>(File.ReadAllText(app.Saves.SavePath));
            if(!saved.wallet.waterRewardClaimed||saved.wallet.diamonds!=5)throw new Exception("Claim state did not persist");
            app.Driver.SelectedAction=FarmAction.Water;
            Debug.Log("FARM_PRESENTATION_INTERACTION_PASSED: context button, seed catalog, watering, one-time reward, save persistence");
        }
    }
}
