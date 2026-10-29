using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    public sealed class CampaignPlaySession : MonoBehaviour
    {
        [SerializeField]private CampaignLaunchCatalog catalog;
        [SerializeField]private string entryId;
        private CampaignTurnController campaign;
        private GameObject pauseRoot;
        private Text status;
        private int closedFrame=-1;
        public Button MenuButton {get;private set;}
        public Button ResumeButton {get;private set;}
        public Button SaveAndReturnButton {get;private set;}
        public Button DiscardAndReturnButton {get;private set;}
        public bool IsOpen=>pauseRoot!=null&&pauseRoot.activeSelf||CampaignNavigation.IsTransitioning;
        public bool ClosedThisFrame=>closedFrame==Time.frameCount;
        public string EntryId=>entryId;
        public void Configure(CampaignLaunchCatalog value,string id){catalog=value;entryId=id;}
        private IEnumerator Start()
        {
            campaign=GetComponent<CampaignTurnController>();var ui=CampaignMenuUI.Canvas("Campaign session UI",transform,150);
            MenuButton=CampaignMenuUI.Button("Session menu",ui.transform,new Vector2(1,1),new Vector2(-24,-22),new Vector2(230,42),"INICIO / PAUSA",()=>Open());
            yield return null;yield return null;
            string json=CampaignNavigation.ConsumeSnapshot(SceneManager.GetActiveScene().name);
            if(json!=null&&!campaign.TryLoadCampaignSnapshot(json))CampaignNavigation.ReturnToMenu(catalog.MenuSceneName);
        }
        private bool CanLeave()=>campaign?.State!=null&&!campaign.HasPendingBattle&&!campaign.IsRivalPhaseActive&&!CampaignNavigation.IsTransitioning;
        private void Update()
        {
            if(MenuButton==null)return;
            bool modal=GetComponent<CityManagementView>()?.IsOpen==true||GetComponent<BattleLabController>()?.IsOpen==true;
            MenuButton.gameObject.SetActive(!modal);MenuButton.interactable=CanLeave();
            if(GetComponent<CampaignDiplomacyView>()?.IsOpen==true){if(Keyboard.current?.escapeKey.wasPressedThisFrame==true)GetComponent<CampaignDiplomacyView>().Close();return;}
            if(modal||GetComponent<CityManagementView>()?.ClosedThisFrame==true||Keyboard.current?.escapeKey.wasPressedThisFrame!=true)return;
            if(pauseRoot!=null)Resume();else Open();
        }
        public bool Open()
        {
            if(!CanLeave()||catalog==null||catalog.Find(entryId)==null)return false;
            if(pauseRoot!=null)return true;
            pauseRoot=CampaignMenuUI.Canvas("Campaign pause UI",transform,200);var background=CampaignMenuUI.Full("Modal",pauseRoot.transform,new Color(.025f,.04f,.045f,.97f));var center=new Vector2(.5f,.5f);
            CampaignMenuUI.Text("Title",background,center,new Vector2(0,220),new Vector2(1000,65),catalog.Find(entryId).Title,38,CampaignMenuUI.Gold);
            status=CampaignMenuUI.Text("Notice",background,center,new Vector2(0,125),new Vector2(1100,90),"Volver sin guardar descarta los avances desde el último guardado. La ranura anterior se conserva.",23,CampaignMenuUI.Paper);
            ResumeButton=CampaignMenuUI.Button("Resume",background,center,new Vector2(0,20),new Vector2(700,62),"SEGUIR JUGANDO",Resume);
            SaveAndReturnButton=CampaignMenuUI.Button("Save and return",background,center,new Vector2(0,-75),new Vector2(700,62),"GUARDAR Y VOLVER AL INICIO",()=>SaveAndReturn());
            DiscardAndReturnButton=CampaignMenuUI.Button("Discard and return",background,center,new Vector2(0,-170),new Vector2(700,62),"VOLVER SIN GUARDAR",()=>DiscardAndReturn());
            return true;
        }
        public void Resume()
        {
            if(pauseRoot==null)return;pauseRoot.SetActive(false);Destroy(pauseRoot);pauseRoot=null;closedFrame=Time.frameCount;
            ResumeButton=null;SaveAndReturnButton=null;DiscardAndReturnButton=null;
        }
        public bool SaveAndReturn()
        {
            if(!CanLeave()||pauseRoot==null)return false;
            if(!campaign.TrySaveCampaign()){status.text="No se pudo guardar. La partida sigue abierta.";return false;}
            string json=PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey);
            if(!catalog.Find(entryId).TryValidateSnapshot(json,out _)){status.text="El guardado no coincide con esta campaña. La partida sigue abierta.";return false;}
            return NavigateBack();
        }
        public bool DiscardAndReturn()=>CanLeave()&&pauseRoot!=null&&NavigateBack();
        private bool NavigateBack()
        {
            bool changed=CampaignNavigation.ReturnToMenu(catalog.MenuSceneName);
            if(!changed)status.text=CampaignNavigation.LastError;return changed;
        }
    }
}
