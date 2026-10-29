using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    public sealed class CampaignLaunchMenu : MonoBehaviour
    {
        [SerializeField]private CampaignLaunchCatalog catalog;
        private Text status;
        private CampaignLaunchEntry continuation;
        private string snapshot;
        private readonly List<Button> campaigns=new();
        public CampaignLaunchCatalog Catalog=>catalog;
        public Button ContinueButton {get;private set;}
        public IReadOnlyList<Button> CampaignButtons=>campaigns.AsReadOnly();
        public string Status=>status!=null?status.text:"";
        public void Configure(CampaignLaunchCatalog value){catalog=value;}
        private GameObject modal,campaignChooser;
        private CanvasGroup navigation;
        private Button newGame;
        private RectTransform stage;
        private void Start()
        {
            var message=CampaignNavigation.EnterMenu();
            AudioListener.volume=PlayerPrefs.GetFloat("Fulgor.UI.Volume",1f);
            var ui=CampaignMenuUI.Canvas("Fulgor start UI",transform,100);
            stage=CampaignMenuUI.Full("Legacy cover",ui.transform,Color.black);
            var art=new GameObject("Original legacy artwork",typeof(RectTransform),typeof(RawImage));art.transform.SetParent(stage,false);
            var rect=(RectTransform)art.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var image=art.GetComponent<RawImage>();image.texture=Resources.Load<Texture2D>("LegacyUI/Cover");image.raycastTarget=false;
            var nav=CampaignMenuUI.Rect("Main navigation",stage,new Vector2(.5f,.5f),new Vector2(-720,0),new Vector2(370,900));navigation=nav.gameObject.AddComponent<CanvasGroup>();
            newGame=CampaignMenuUI.Button("New game",nav,new Vector2(.5f,.5f),new Vector2(0,140),new Vector2(350,66),"EMPEZAR PARTIDA",ShowCampaigns);
            ContinueButton=CampaignMenuUI.Button("Continue",nav,new Vector2(.5f,.5f),new Vector2(0,35),new Vector2(350,66),"CONTINUAR",()=>ContinueCampaign());
            CampaignMenuUI.Button("Settings",nav,new Vector2(.5f,.5f),new Vector2(0,-70),new Vector2(350,66),"CONFIGURACIÓN",ShowSettings);
            CampaignMenuUI.Button("Credits",nav,new Vector2(.5f,.5f),new Vector2(0,-175),new Vector2(350,66),"CRÉDITOS",()=>{var body=OpenModal("FULGOR MEDIEVAL");CampaignMenuUI.Text("Credits",body,new Vector2(.5f,.5f),Vector2.zero,new Vector2(820,180),"Cinco reinos · Una penumbra · Un Fulgor\n\nDiseño, lore y desarrollo — MABM-DEV\nCastilla · León · Aragón · Navarra · Granada",24,LegacyUI.Paper);});
            CampaignMenuUI.Button("Exit",nav,new Vector2(.5f,.5f),new Vector2(0,-280),new Vector2(350,66),"SALIR",()=>{var body=OpenModal("¿ABANDONAR HISPANIA?");CampaignMenuUI.Button("Confirm exit",body,new Vector2(.5f,.5f),Vector2.zero,new Vector2(500,64),"SALIR DEL JUEGO",()=>Application.Quit());});
            foreach(var button in nav.GetComponentsInChildren<Button>())LegacyUI.Ornate(button);
            status=CampaignMenuUI.Text("Status",nav,new Vector2(.5f,.5f),new Vector2(0,-395),new Vector2(350,100),"",17,LegacyUI.Paper);
            // Retain real campaign buttons for keyboard navigation and the existing session contract.
            if(catalog!=null){ShowCampaigns();CloseModal();RefreshContinuation();}else{newGame.interactable=false;ContinueButton.interactable=false;status.text="No hay campañas configuradas.";}
            if(!string.IsNullOrEmpty(message))status.text=message;
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(ContinueButton.interactable?ContinueButton.gameObject:newGame.gameObject);
        }
        private RectTransform OpenModal(string title)
        {
            CloseModal();navigation.interactable=false;navigation.blocksRaycasts=false;
            var dim=CampaignMenuUI.Full("Menu modal",stage,new Color(0,0,0,.7f));modal=dim.gameObject;
            var body=CampaignMenuUI.Rect("Modal body",dim,new Vector2(.5f,.5f),new Vector2(150,0),new Vector2(1000,650));CampaignMenuUI.Background(body,LegacyUI.Ink);LegacyUI.Panel(body);
            CampaignMenuUI.Text("Title",body,new Vector2(.5f,.5f),new Vector2(0,240),new Vector2(900,70),title,32,LegacyUI.Gold);
            var back=CampaignMenuUI.Button("Back",body,new Vector2(.5f,.5f),new Vector2(0,-250),new Vector2(350,58),"VOLVER",CloseModal);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(back.gameObject);return body;
        }
        private void ShowCampaigns()
        {
            if(catalog==null)return;
            if(campaignChooser!=null){CloseModal();modal=campaignChooser;modal.SetActive(true);navigation.interactable=false;navigation.blocksRaycasts=false;UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(campaigns[0].gameObject);return;}
            var body=OpenModal("ELIGE TU CAMPAÑA");campaignChooser=modal;campaigns.Clear();
            for(int i=0;i<catalog.Entries.Count;i++)
            {
                var entry=catalog.Entries[i];float y=120-i*155;
                campaigns.Add(CampaignMenuUI.Button("Start "+entry.Id,body,new Vector2(.5f,.5f),new Vector2(0,y),new Vector2(860,64),entry.Title.ToUpperInvariant(),()=>StartCampaign(entry.Id)));
                CampaignMenuUI.Text("Description",body,new Vector2(.5f,.5f),new Vector2(0,y-65),new Vector2(860,60),entry.Description,20,LegacyUI.Paper);
            }
            CampaignMenuUI.Text("Save notice",body,new Vector2(.5f,.5f),new Vector2(0,-175),new Vector2(870,65),"Empezar no sustituye tu guardado. Guardar una nueva partida reemplaza la ranura anterior.",18,LegacyUI.Gold);
        }
        private void ShowSettings()
        {
            var body=OpenModal("CONFIGURACIÓN");var center=new Vector2(.5f,.5f);
            var volume=CampaignMenuUI.Text("Volume",body,center,new Vector2(0,120),new Vector2(800,60),"",26,LegacyUI.Paper);
            void UpdateVolume(float delta){AudioListener.volume=Mathf.Clamp01(AudioListener.volume+delta);PlayerPrefs.SetFloat("Fulgor.UI.Volume",AudioListener.volume);PlayerPrefs.Save();volume.text=$"Volumen general · {Mathf.RoundToInt(AudioListener.volume*100)} %";}
            volume.text=$"Volumen general · {Mathf.RoundToInt(AudioListener.volume*100)} %";
            CampaignMenuUI.Button("Quieter",body,center,new Vector2(-160,35),new Vector2(260,60),"BAJAR",()=>UpdateVolume(-.1f));
            CampaignMenuUI.Button("Louder",body,center,new Vector2(160,35),new Vector2(260,60),"SUBIR",()=>UpdateVolume(.1f));
            CampaignMenuUI.Button("Fullscreen",body,center,new Vector2(0,-70),new Vector2(620,60),"ALTERNAR PANTALLA COMPLETA",()=>Screen.fullScreen=!Screen.fullScreen);
        }
        private void CloseModal()
        {
            // Hide instead of destroying the selected campaign controls while starting a scene.
            if(modal!=null){modal.SetActive(false);if(modal!=campaignChooser)Destroy(modal);modal=null;}
            if(navigation!=null){navigation.interactable=true;navigation.blocksRaycasts=true;}
            if(newGame!=null)UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(newGame.gameObject);
        }
        private void Update(){if(modal!=null&&UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame==true)CloseModal();}

        public void RefreshContinuation()
        {
            if(ContinueButton==null||CampaignNavigation.IsTransitioning)return;
            snapshot=PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey)?PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey):null;
            bool valid=snapshot!=null&&catalog.TryFindSnapshot(snapshot,out continuation,out _);
            ContinueButton.interactable=valid;
            if(valid&&continuation.TryValidateSnapshot(snapshot,out var payload))
            {ContinueButton.GetComponentInChildren<Text>().text=$"CONTINUAR · TURNO {payload.turn}";status.text=continuation.Title+" · guardado compatible";}
            else{continuation=null;ContinueButton.GetComponentInChildren<Text>().text="CONTINUAR";status.text=snapshot==null?"Todavía no hay una partida guardada.":"El guardado está dañado o no coincide con las campañas disponibles.";}
        }
        public bool StartCampaign(string id)
        {
            if(catalog==null||!CampaignNavigation.Start(catalog.Find(id))){if(status!=null)status.text=CampaignNavigation.LastError??"Campaña no disponible.";return false;}
            DisableButtons();return true;
        }
        public bool ContinueCampaign()
        {
            if(continuation==null||snapshot==null||!CampaignNavigation.Start(continuation,snapshot))
            {if(status!=null)status.text=CampaignNavigation.LastError??"No hay un guardado compatible.";return false;}
            DisableButtons();return true;
        }
        private void DisableButtons(){foreach(var button in campaigns)button.interactable=false;if(ContinueButton!=null)ContinueButton.interactable=false;status.text="Abriendo campaña…";}
        private void OnApplicationFocus(bool focused){if(focused&&ContinueButton!=null)RefreshContinuation();}
    }
}
