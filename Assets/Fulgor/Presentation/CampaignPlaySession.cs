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
        private GameObject slotChooser;
        private Text status;
        private int closedFrame=-1;
        public Button MenuButton {get;private set;}
        public Button ResumeButton {get;private set;}
        public Button SaveButton {get;private set;}
        public Button LoadButton {get;private set;}
        public bool IsChoosingSlot=>slotChooser!=null;
        public Button SaveAndReturnButton {get;private set;}
        public Button DiscardAndReturnButton {get;private set;}
        public bool IsOpen=>pauseRoot!=null&&pauseRoot.activeSelf||CampaignNavigation.IsTransitioning;
        public bool ClosedThisFrame=>closedFrame==Time.frameCount;
        public string EntryId=>entryId;
        public void Configure(CampaignLaunchCatalog value,string id){catalog=value;entryId=id;}
        private IEnumerator Start()
        {
            campaign=GetComponent<CampaignTurnController>();var ui=CampaignMenuUI.Canvas("Campaign session UI",transform,150);
            MenuButton=CampaignMenuUI.Button("Session menu",ui.transform,new Vector2(1,1),new Vector2(-20,-30),new Vector2(158,44),"MENÚ / PAUSA",()=>Open());
            MenuButton.GetComponentInChildren<Text>().fontSize=14;
            if(GetComponent<CampaignSelectionPanel>()?.HasCampaignHud==true)
            {
                ui.GetComponent<CanvasScaler>().matchWidthOrHeight=0;
                var original=MenuButton.gameObject;original.SetActive(false);Destroy(original);
                var holder=menuHolder=CampaignMenuUI.Rect("Menu anchor",ui.transform,new Vector2(1,1),new Vector2(-10,-6),new Vector2(320,52));
                MenuButton=CampaignHudArt.Button("Session menu",holder,Vector2.zero,new Vector2(320,52),"MENÚ / PAUSA",()=>Open());
            }
            yield return null;yield return null;
            string json=CampaignNavigation.ConsumeSnapshot(SceneManager.GetActiveScene().name);
            if(json!=null&&!campaign.TryLoadCampaignSnapshot(json))CampaignNavigation.ReturnToMenu(catalog.MenuSceneName);
        }
        private RectTransform menuHolder;
        // Sobre el mapa el menú corona la columna derecha; en la ciudad vuelve a su tamaño corto, junto al acceso a la diplomacia.
        private void FitMenuButton()
        {
            if(menuHolder==null)return;
            bool city=GetComponent<CampaignCityMapView>()?.IsOpen==true;
            var size=city?new Vector2(156,52):new Vector2(320,52);
            if(menuHolder.sizeDelta==size)return;
            menuHolder.sizeDelta=size;menuHolder.anchoredPosition=new Vector2(-10,-6);
            ((RectTransform)MenuButton.transform).sizeDelta=size;
            MenuButton.GetComponentInChildren<Text>().rectTransform.sizeDelta=size-new Vector2(22,10);
        }
        private bool CanLeave()=>campaign?.State!=null&&!campaign.HasPendingBattle&&!campaign.IsRivalPhaseActive&&!CampaignNavigation.IsTransitioning;
        private void Update()
        {
            if(MenuButton==null)return;
            bool modal=GetComponent<CampaignRecruitmentView>()?.IsOpen==true||GetComponent<CityManagementView>()?.IsOpen==true||GetComponent<BattleLabController>()?.IsOpen==true;
            MenuButton.gameObject.SetActive(!modal);MenuButton.interactable=CanLeave();FitMenuButton();
            var recruitment=GetComponent<CampaignRecruitmentView>();
            if(recruitment?.IsOpen==true)
            {
                if(Keyboard.current?.escapeKey.wasPressedThisFrame==true)recruitment.Close();
                return;
            }
            if(recruitment?.ClosedThisFrame==true)return;
            if(GetComponent<CampaignResearchView>()?.IsOpen==true){if(Keyboard.current?.escapeKey.wasPressedThisFrame==true)GetComponent<CampaignResearchView>().Close();return;}
            if(GetComponent<CampaignDiplomacyView>()?.IsOpen==true){if(Keyboard.current?.escapeKey.wasPressedThisFrame==true)GetComponent<CampaignDiplomacyView>().Close();return;}
            if(modal||GetComponent<CityManagementView>()?.ClosedThisFrame==true||Keyboard.current?.escapeKey.wasPressedThisFrame!=true)return;
            TogglePause();
        }
        public void TogglePause(){if(slotChooser!=null)CloseSlots();else if(pauseRoot!=null)Resume();else Open();}
        public bool Open()
        {
            if(!CanLeave()||catalog==null||catalog.Find(entryId)==null)return false;
            if(pauseRoot!=null)return true;
            pauseRoot=CampaignMenuUI.Canvas("Campaign pause UI",transform,200);var background=CampaignMenuUI.Full("Modal",pauseRoot.transform,new Color(.025f,.04f,.045f,.97f));var center=new Vector2(.5f,.5f);
            CampaignMenuUI.Text("Title",background,center,new Vector2(0,220),new Vector2(1000,65),catalog.Find(entryId).Title,38,CampaignMenuUI.Gold);
            status=CampaignMenuUI.Text("Notice",background,center,new Vector2(0,140),new Vector2(1100,65),"Guarda en una ranura o elige una partida para cargar.",21,CampaignMenuUI.Paper);
            ResumeButton=CampaignMenuUI.Button("Resume",background,center,new Vector2(0,65),new Vector2(700,58),"SEGUIR JUGANDO",Resume);
            SaveButton=CampaignMenuUI.Button("Save",background,center,new Vector2(-180,-15),new Vector2(340,58),"GUARDAR PARTIDA",()=>ShowSlots(true));
            LoadButton=CampaignMenuUI.Button("Load",background,center,new Vector2(180,-15),new Vector2(340,58),"CARGAR PARTIDA",()=>ShowSlots(false));
            SaveAndReturnButton=CampaignMenuUI.Button("Save and return",background,center,new Vector2(0,-105),new Vector2(700,58),"GUARDAR Y VOLVER AL INICIO",()=>SaveAndReturn());
            DiscardAndReturnButton=CampaignMenuUI.Button("Discard and return",background,center,new Vector2(0,-190),new Vector2(700,58),"VOLVER SIN GUARDAR",()=>DiscardAndReturn());
            return true;
        }
        public void ShowSlots(bool saving)
        {
            if(pauseRoot==null||!CanLeave())return;
            CloseSlots();
            var body=CampaignMenuUI.Full("Partidas guardadas",pauseRoot.transform,new Color(.025f,.04f,.045f,.99f));slotChooser=body.gameObject;
            var center=new Vector2(.5f,.5f);
            CampaignMenuUI.Text("Title",body,center,new Vector2(0,335),new Vector2(1050,60),saving?"GUARDAR PARTIDA · ELIGE UNA RANURA":"CARGAR PARTIDA · ELIGE UN GUARDADO",30,CampaignMenuUI.Gold);
            for(int i=0;i<CampaignSaveSlots.Count;i++)
            {
                int slot=i;var record=CampaignSaveSlots.Read(i);
                var label=SlotLabel(record?.snapshot);
                var button=CampaignMenuUI.Button("Ranura "+(i+1),body,center,new Vector2(0,235-i*85),new Vector2(1050,68),$"{i+1}. {label}"+(record!=null?" · "+record.savedAt:""),()=>{if(saving)SaveSlot(slot);else LoadSlot(slot);});
                button.GetComponentInChildren<Text>().fontSize=19;
                button.interactable=saving||IsValidSave(record?.snapshot);
            }
            if(!saving)
            {
                var latest=PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey,"");
                var button=CampaignMenuUI.Button("Último guardado",body,center,new Vector2(0,-190),new Vector2(1050,68),"ÚLTIMO GUARDADO · "+SlotLabel(latest),()=>LoadSnapshot(latest));
                button.GetComponentInChildren<Text>().fontSize=19;button.interactable=IsValidSave(latest);
            }
            CampaignMenuUI.Text("Notice",body,center,new Vector2(0,-270),new Vector2(1100,50),saving?"Elegir una ranura ocupada sustituye solo esa partida.":"Cargar sustituye los avances que no hayas guardado.",19,CampaignMenuUI.Paper);
            CampaignMenuUI.Button("Volver a pausa",body,center,new Vector2(0,-345),new Vector2(350,58),"VOLVER",CloseSlots);
        }
        private bool IsValidSave(string json)=>!string.IsNullOrEmpty(json)&&catalog.TryFindSnapshot(json,out _,out _);
        private string SlotLabel(string json)
        {
            if(string.IsNullOrEmpty(json))return "Ranura vacía";
            if(!catalog.TryFindSnapshot(json,out var entry,out _)||!entry.TryValidateSnapshot(json,out var data))return "Guardado no compatible";
            return entry.Title+" · turno "+data.turn;
        }
        public bool SaveSlot(int slot)
        {
            if(slot<0||slot>=CampaignSaveSlots.Count||pauseRoot==null||!CanLeave()||!campaign.TrySaveCampaign())return false;
            CampaignSaveSlots.Write(slot,PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey));
            CloseSlots();status.text=$"Partida guardada en la ranura {slot+1} · turno {campaign.State.Turn}.";return true;
        }
        public bool LoadSlot(int slot)=>slot>=0&&slot<CampaignSaveSlots.Count&&LoadSnapshot(CampaignSaveSlots.Read(slot)?.snapshot);
        private bool LoadSnapshot(string json)
        {
            if(pauseRoot==null||!CanLeave()||!IsValidSave(json))return false;
            catalog.TryFindSnapshot(json,out var entry,out _);
            if(entry.Id!=entryId)return CampaignNavigation.Start(entry,json);
            if(!campaign.TryLoadCampaignSnapshot(json))return false;
            GetComponent<CampaignCityMapView>()?.Close();Resume();return true;
        }
        private void CloseSlots(){if(slotChooser==null)return;slotChooser.SetActive(false);Destroy(slotChooser);slotChooser=null;}
        public void Resume()
        {
            if(pauseRoot==null)return;CloseSlots();pauseRoot.SetActive(false);Destroy(pauseRoot);pauseRoot=null;closedFrame=Time.frameCount;
            ResumeButton=null;SaveButton=null;LoadButton=null;SaveAndReturnButton=null;DiscardAndReturnButton=null;
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
