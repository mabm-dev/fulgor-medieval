using System;
using System.Collections;
using System.IO;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>Explicit opt-in executable walkthrough; never touches the player's save slot.</summary>
    public sealed class BattleGraphicalReviewRunner : MonoBehaviour
    {
        private string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void Create()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-fulgorGraphicalReview");
            if(at<0||at+1>=args.Length||FindAnyObjectByType<BattleGraphicalReviewRunner>()!=null)return;
            var root=new GameObject("Optional graphical review");DontDestroyOnLoad(root);root.AddComponent<BattleGraphicalReviewRunner>().output=args[at+1];
        }
        private IEnumerator Start()
        {
            Application.runInBackground=true;
            var run=Run();
            while(true)
            {
                bool next;object current=null;
                try{next=run.MoveNext();if(next)current=run.Current;}
                catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
                if(!next)break;yield return current;
            }
        }
        private IEnumerator Run()
        {
            Directory.CreateDirectory(output);
            bool had=PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey);string saved=PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey,"");
            CampaignLaunchMenu menu=null;
            for(int i=0;i<600;i++){menu=FindAnyObjectByType<CampaignLaunchMenu>();if(menu?.ContinueButton!=null)break;yield return null;}
            if(menu==null)throw new InvalidOperationException("Review could not find the menu.");
            string menuScene=menu.Catalog.MenuSceneName;
            if(!menu.StartCampaign("castilla-conquest"))throw new InvalidOperationException("Review could not start the campaign.");
            CampaignTurnController campaign=null;
            for(int i=0;i<600;i++){campaign=FindAnyObjectByType<CampaignTurnController>();if(!CampaignNavigation.IsTransitioning&&campaign?.State!=null)break;yield return null;}
            yield return null;yield return null;
            var selection=campaign.GetComponent<CampaignBoardSelection>();var map=ProceduralMapGenerator.Generate(24,16,12345);var graph=selection.Routes;
            var home=map.Cells.First(c=>graph.CanStandAt(c.Coordinates)&&c.Coordinates.Neighbors().Any(n=>graph.TryGetMovementCost(c.Coordinates,n,out _))).Coordinates;
            var enemy=home.Neighbors().First(n=>graph.TryGetMovementCost(home,n,out _));
            CampaignFormationState Form(string id,TroopProfile profile){var p=TroopCatalog.Get(profile);return new CampaignFormationState(id,p.Attributes,(long)p.DefaultQuantity*p.Attributes.HealthPerMember,70,p.Kind);}
            CampaignArmyPlacement Army(string id,string faction,HexCoordinates at)=>new(new CampaignArmyState(id,faction,new[]{Form(id+"-1",TroopProfile.Spearmen),Form(id+"-2",TroopProfile.Crossbowmen),Form(id+"-3",TroopProfile.LightCavalry)}),at,6);
            campaign.Initialize(campaign.GetComponent<CampaignSelectionPanel>(),map,null,new CampaignArmyRegistry(graph,new[]{Army("Castilla","castilla",home),Army("Hueste rival","rival",enemy)}),"castilla");
            int mask=Camera.main.cullingMask;selection.LocateCell(enemy);
            if(!campaign.AttackSelectedArmy())throw new InvalidOperationException("Review encounter did not open.");
            var battle=campaign.GetComponent<BattleLabController>();var encounter=battle.ActiveCampaignEncounter;
            for(int i=0;i<30&&encounter.Roster.ActiveSide!=battle.PlayerBattleSide;i++)battle.AdvanceCampaignAutomaticStep();
            // Pause only automatic rival actions during the opt-in visual inspection.
            battle.GraphicalReviewPaused=true;
            yield return new WaitForSecondsRealtime(1);
            var view=battle.GraphicalView;
            if(view.Stage.CellCount!=117||view.Stage.FormationCount!=6)throw new InvalidOperationException("Graphical deployment differs from the encounter.");
            int actor=encounter.Roster.ActiveFormationId.Value;
            var destination=view.Stage.Cells.First(c=>encounter.Commands.TryPreviewMove(actor,c,out _,out _));
            view.SelectCell(destination);yield return null;
            CaptureStage(view.Stage,Path.Combine(output,"campo-3d.png"));
            yield return new WaitForEndOfFrame();LegacyUIReviewRunner.CaptureFrame(Path.Combine(output,"batalla.png"));
            Debug.Log("BATTLE_GRAPHICAL_READY");
            if(Environment.GetCommandLineArgs().Contains("-fulgorGraphicalHold"))
            {
                battle.GraphicalReviewPaused=false;
                while(view!=null&&view.IsOpen)yield return null;
                CampaignNavigation.ReturnToMenu(menuScene);yield break;
            }
            if(!view.ConfirmButton.interactable)throw new InvalidOperationException("Graphical move is not enabled.");
            view.ConfirmButton.onClick.Invoke();yield return new WaitForSecondsRealtime(1);
            encounter.Commands.TryGetPosition(actor,out var moved);
            if(moved!=destination)throw new InvalidOperationException("Graphical move did not reach its destination.");
            view.ReturnButton.onClick.Invoke();if(Camera.main.cullingMask!=mask||!campaign.HasPendingBattle)throw new InvalidOperationException("Closing the battlefield lost the encounter or camera.");
            campaign.ResumeBattle();yield return null;view=battle.GraphicalView;
            for(int step=0;step<1000&&encounter.Roster.Outcome==BattleOutcome.InProgress;step++)battle.AdvanceCampaignAutomaticStep();
            yield return null;yield return null;
            if(!view.ResultVisible)throw new InvalidOperationException("Finished encounter has no result panel.");
            long casualties=encounter.CreateReport().Attacker.Casualties+encounter.CreateReport().Defender.Casualties;
            yield return new WaitForEndOfFrame();LegacyUIReviewRunner.CaptureFrame(Path.Combine(output,"resultado.png"));yield return null;
            string id=encounter.Id;view.ReturnButton.onClick.Invoke();yield return null;
            if(!campaign.ArmyRegistry.HasResolvedEncounter(id)||battle.IsOpen||Camera.main.cullingMask!=mask)throw new InvalidOperationException("Battle result did not return to the campaign.");
            if(had!=PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey)||saved!=PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey,""))throw new InvalidOperationException("Graphical review modified the player's save.");
            File.WriteAllText(Path.Combine(output,"revision_batalla.json"),JsonUtility.ToJson(new Summary{cells=117,formations=6,casualties=casualties,moveConfirmed=true,encounterResumed=true,resultApplied=true,cameraRestored=true,slotUnchanged=true},true));
            Debug.Log("BATTLE_GRAPHICAL_REVIEW_OK");Application.Quit(0);
        }
        private static void CaptureStage(BattleGraphicalStage stage,string path)
        {
            stage.Camera.Render();var previous=RenderTexture.active;Texture2D image=null;
            try{RenderTexture.active=stage.Texture;image=new Texture2D(stage.Texture.width,stage.Texture.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{RenderTexture.active=previous;if(image!=null)Destroy(image);}
        }
        [Serializable]private sealed class Summary
        {public int cells,formations;public long casualties;public bool moveConfirmed,encounterResumed,resultApplied,cameraRestored,slotUnchanged;}
    }
}
