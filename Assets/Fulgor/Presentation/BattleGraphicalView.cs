using System.Collections.Generic;
using System.Text;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    /// <summary>Playable tactical screen inspired by the original web battlefield.</summary>
    public sealed class BattleGraphicalView : MonoBehaviour
    {
        private static readonly Color Ink=new(.035f,.052f,.058f),Paper=new(.93f,.9f,.79f),Gold=new(.91f,.75f,.4f),Muted=new(.59f,.65f,.63f),Blue=new(.35f,.68f,.88f),Red=new(.91f,.43f,.36f);
        private BattleLabController controller;
        private GameObject ui;
        private RectTransform field;
        private Text heading,phase,instruction,selectionText,history,ownTotal,rivalTotal,resultText,retreatText;
        private Button confirm,defend,wait,automatic,leave,withdraw;
        public Button CancelRetreatButton {get;private set;}
        private GameObject retreatDialog,result;
        private readonly Dictionary<int,Text> cards=new(),badges=new();
        private readonly Dictionary<int,Image> cardBars=new();
        private readonly HashSet<HexCoordinates> reachable=new(),targets=new();
        private HexCoordinates? selected,hovered;
        private MapPath selectedPath;
        private int? selectedTarget,inspected;
        private int revision=-1;
        private BattleOutcome outcome;
        private int? active;
        private bool hadManual;
        private float lastAspect;
        private readonly Queue<string> recent=new();
        public bool CanNavigate=>IsOpen&&!RetreatConfirmationVisible&&!ResultVisible;
        public bool IsOpen=>ui!=null&&ui.activeSelf;
        public BattleGraphicalStage Stage {get;private set;}
        public RawImage Viewport {get;private set;}
        public Button ConfirmButton=>confirm;
        public Button DefendButton=>defend;
        public Button WaitButton=>wait;
        public Button AutomaticButton=>automatic;
        public Button ReturnButton=>leave;
        public Button WithdrawButton=>withdraw;
        public bool ResultVisible=>result!=null&&result.activeSelf;
        public bool RetreatConfirmationVisible=>retreatDialog!=null&&retreatDialog.activeSelf;
        public string Instruction=>instruction!=null?instruction.text:"";
        public int ReachableCount=>reachable.Count;
        public int TargetCount=>targets.Count;
        public IReadOnlyList<HexCoordinates> PreviewRoute=>selectedPath?.Cells;

        public void Open(BattleLabController owner)
        {
            if(IsOpen)return;
            Close();controller=owner;revision=-1;active=null;selected=null;hovered=null;inspected=null;recent.Clear();
            try
            {
                var root=new GameObject("Escenario táctico");root.transform.SetParent(transform,false);Stage=root.AddComponent<BattleGraphicalStage>();Stage.Initialize(owner);
                BuildUI();controller.VisualOrderApplied+=OnOrderApplied;Refresh(true);
            }
            catch {Close();throw;}
        }
        public void Close()
        {
            if(controller!=null)controller.VisualOrderApplied-=OnOrderApplied;
            if(Viewport!=null)Viewport.texture=null;
            if(Stage!=null){Stage.Release();Destroy(Stage.gameObject);Stage=null;}
            if(ui!=null){ui.SetActive(false);Destroy(ui);ui=null;}
            cards.Clear();badges.Clear();cardBars.Clear();reachable.Clear();targets.Clear();selected=null;hovered=null;selectedPath=null;selectedTarget=null;
            Viewport=null;field=null;controller=null;retreatDialog=null;result=null;
        }
        private void BuildUI()
        {
            ui=CampaignMenuUI.Canvas("Vista gráfica de batalla",transform,220);ui.GetComponent<Canvas>().enabled=true;
            CampaignMenuUI.Full("Fondo opaco",ui.transform,Ink);
            var header=Box("Cabecera",ui.transform,0,0,0,0,new Color(.055f,.075f,.08f));Stretch(header,18,-1,18,18);header.anchorMin=new Vector2(0,1);header.anchorMax=Vector2.one;header.offsetMin=new Vector2(18,-100);header.offsetMax=new Vector2(-18,-16);
            Label(header,"ENCUENTRO TÁCTICO",new Rect(24,8,600,22),17,Gold);
            heading=Label(header,"Campo de batalla",new Rect(24,31,960,42),32,Paper);
            heading.resizeTextForBestFit=true;heading.resizeTextMinSize=20;heading.resizeTextMaxSize=32;
            phase=Label(header,"",new Rect(1050,16,780,54),22,Gold,TextAnchor.MiddleRight);
            var left=Box("Tus fuerzas",ui.transform,18,222,274,118,new Color(.045f,.105f,.14f));
            left.anchorMax=new Vector2(0,1);left.offsetMax=new Vector2(292,-118);
            var right=Box("Fuerzas rivales",ui.transform,0,222,18,118,new Color(.14f,.065f,.062f));right.anchorMin=new Vector2(1,0);right.anchorMax=Vector2.one;right.offsetMin=new Vector2(-292,222);right.offsetMax=new Vector2(-18,-118);
            ownTotal=Label(left,"",new Rect(18,14,238,74),25,Blue);rivalTotal=Label(right,"",new Rect(18,14,238,74),25,Red);
            var ownSide=controller.PlayerBattleSide;
            int ownIndex=0,rivalIndex=0;
            foreach(int id in controller.VisualRoster.FormationIds)
            {
                bool own=controller.VisualRoster.GetFormation(id).Slot.Side==ownSide;int index=own?ownIndex++:rivalIndex++;
                var parent=own?left:right;
                var card=CampaignMenuUI.Rect("Ficha de formación",parent,new Vector2(0,1),new Vector2(12,-104-index*90),new Vector2(250,82));
                CampaignMenuUI.Background(card,new Color(.02f,.035f,.04f,.75f));
                var button=card.gameObject.AddComponent<Button>();button.targetGraphic=card.GetComponent<Image>();int captured=id;button.onClick.AddListener(()=>Inspect(captured));
                var stripe=CampaignMenuUI.Rect("Iniciativa",card,new Vector2(0,.5f),Vector2.zero,new Vector2(4,68));CampaignMenuUI.Background(stripe,own?Blue:Red);cardBars.Add(id,stripe.GetComponent<Image>());
                cards.Add(id,Label(card,"",new Rect(14,5,226,72),18,Paper));
            }
            Label(left,"Azul · tus tropas\nFiguras representativas; el número\nindica los soldados reales.",new Rect(14,650,245,88),16,Muted);
            Label(right,"Rojo · rival\nPulsa una formación para consultar\nsus fuerzas y su estado.",new Rect(14,650,245,88),16,Muted);
            field=CampaignMenuUI.Rect("Campo",ui.transform,Vector2.zero,Vector2.zero,Vector2.zero);Stretch(field,308,222,308,118);
            Viewport=field.gameObject.AddComponent<RawImage>();Viewport.color=Color.white;Viewport.texture=Stage.Texture;
            field.gameObject.AddComponent<RectMask2D>();
            var input=field.gameObject.AddComponent<BattlefieldPointer>();input.Owner=this;
            foreach(int id in controller.VisualRoster.FormationIds)
            {
                var label=CampaignMenuUI.Text("Estandarte de formación",field,new Vector2(.5f,.5f),Vector2.zero,new Vector2(110,34),"",18,Paper);
                var backing=label.gameObject.AddComponent<Outline>();backing.effectColor=new Color(.01f,.02f,.015f,1);backing.effectDistance=new Vector2(1,-1);badges.Add(id,label);
            }
            MapCameraControls.Create(field,new Vector2(.5f,0),new Vector2(0,12),d=>Stage.PanView(d),d=>Stage.SetZoom(Stage.Zoom+d*.08f),()=>Stage.ResetView(),()=>CanNavigate);
            var footer=Box("Órdenes y parte",ui.transform,18,18,18,0,new Color(.055f,.075f,.08f));footer.anchorMax=new Vector2(1,0);footer.offsetMin=new Vector2(18,18);footer.offsetMax=new Vector2(-18,204);
            instruction=Label(footer,"",new Rect(20,8,1240,35),21,Gold);
            confirm=SmallButton(footer,"ELIGE UN DESTINO",new Rect(20,52,258,45),ConfirmSelection);
            confirm.targetGraphic.color=new Color(.43f,.33f,.15f);
            defend=SmallButton(footer,"DEFENDER · +2",new Rect(290,52,190,45),()=>Order(BattleRecordedOrderKind.Defend));
            wait=SmallButton(footer,"ESPERAR",new Rect(492,52,150,45),()=>Order(BattleRecordedOrderKind.Wait));
            automatic=SmallButton(footer,"COMBATE AUTOMÁTICO",new Rect(654,52,265,45),()=>{if(retreatDialog.activeSelf)return;controller.ToggleVisualAutomatic();Refresh(true);});
            withdraw=SmallButton(footer,"RETIRAR HUESTE",new Rect(931,52,220,45),ShowRetreatConfirmation);
            leave=SmallButton(footer,"VOLVER AL MAPA",new Rect(1163,52,246,45),()=>{if(!retreatDialog.activeSelf)controller.ReturnToCampaign();});
            selectionText=Label(footer,"",new Rect(20,111,1020,62),18,Paper);
            history=Label(footer,"",new Rect(1070,108,778,66),16,Muted);
            result=CampaignMenuUI.Full("Resultado",field,new Color(.025f,.04f,.045f,.92f)).gameObject;
            resultText=CampaignMenuUI.Text("Parte final",result.transform,new Vector2(.5f,.5f),new Vector2(0,40),new Vector2(850,340),"",26,Paper);resultText.alignment=TextAnchor.MiddleCenter;
            var apply=CampaignMenuUI.Button("Aplicar resultado",result.transform,new Vector2(.5f,.5f),new Vector2(0,-175),new Vector2(380,52),"APLICAR Y VOLVER AL MAPA",()=>controller.ReturnToCampaign());apply.targetGraphic.color=new Color(.34f,.28f,.14f);
            result.SetActive(false);
            retreatDialog=CampaignMenuUI.Full("Confirmar retirada",ui.transform,new Color(.01f,.02f,.025f,.92f)).gameObject;
            var dialog=CampaignMenuUI.Rect("Aviso de retirada",retreatDialog.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(900,440));CampaignMenuUI.Background(dialog,new Color(.13f,.10f,.075f));
            retreatText=Label(dialog,"",new Rect(40,30,820,280),24,Paper);
            CancelRetreatButton=SmallButton(dialog,"CONTINUAR COMBATIENDO",new Rect(40,340,380,56),CancelRetreat);
            var retreatAction=SmallButton(dialog,"CONFIRMAR RETIRADA",new Rect(450,340,400,56),ConfirmRetreat);
            CancelRetreatButton.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnRight=retreatAction,selectOnLeft=retreatAction,selectOnUp=retreatAction,selectOnDown=retreatAction};
            retreatAction.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnRight=CancelRetreatButton,selectOnLeft=CancelRetreatButton,selectOnUp=CancelRetreatButton,selectOnDown=CancelRetreatButton};
            retreatDialog.SetActive(false);
        }

        private void Update()
        {
            if(!IsOpen||controller==null)return;
            if(!controller.IsOpen){Close();return;}
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true)
            {if(retreatDialog.activeSelf)CancelRetreat();else ClearSelection();}
            if(CanNavigate&&Application.isFocused)Stage.PanView(MapCameraControls.KeyboardDirection()*Time.unscaledDeltaTime);
            Stage.Sync(controller);
            float aspect=field.rect.width/Mathf.Max(1,field.rect.height);
            if(Mathf.Abs(aspect-lastAspect)>.005f){lastAspect=aspect;Stage.FitCamera(aspect);}
            Refresh(false);PositionBadges();
        }
        private void OnOrderApplied(BattleRecordedOrder order,MapPath path){Stage?.QueueOrder(order,path);}
        private void Refresh(bool force)
        {
            if(!IsOpen)return;
            var roster=controller.VisualRoster;bool manual=controller.CanGiveVisualOrder;
            bool changed=force||revision!=controller.VisualRevision||active!=roster.ActiveFormationId||outcome!=roster.Outcome||hadManual!=manual;
            if(!changed)return;
            if(revision!=controller.VisualRevision&&revision>=0)
            {
                recent.Enqueue(FriendlyMessage(controller.LatestBattleMessage));while(recent.Count>3)recent.Dequeue();
                selected=null;hovered=null;selectedPath=null;selectedTarget=null;
            }
            revision=controller.VisualRevision;active=roster.ActiveFormationId;outcome=roster.Outcome;hadManual=manual;
            heading.text=controller.OwnArmyName+"   contra   "+controller.RivalArmyName;
            phase.text="RONDA "+roster.Round+"   ·   "+(outcome!=BattleOutcome.InProgress?"COMBATE CONCLUIDO":manual?"TU INICIATIVA":controller.IsAutomatic?"COMBATE AUTOMÁTICO":"MANIOBRA RIVAL");
            reachable.Clear();targets.Clear();
            if(manual&&active.HasValue)
            {
                foreach(var cell in Stage.Cells)
                {
                    if(controller.VisualCommands.TryPreviewMove(active.Value,cell,out _,out _))reachable.Add(cell);
                    if(CanAttack(cell,out _))targets.Add(cell);
                }
            }
            long own=0,rival=0;
            foreach(int id in roster.FormationIds)
            {
                var f=roster.GetFormation(id);bool ours=f.Slot.Side==controller.PlayerBattleSide;
                if(ours)own+=f.Quantity;else rival+=f.Quantity;
                string status=f.IsDestroyed?"Destruida":f.IsWithdrawn?"Retirada":active==id?"Tiene la iniciativa":roster.DefenseBonus(id)>0?"Defendiendo · +2":"En formación";
                cards[id].text=$"{FormationName(f)}   {f.Quantity}\nMoral {f.Morale} · {status}";
                cards[id].color=f.IsInBattle?Paper:Muted;cardBars[id].color=active==id?Gold:ours?Blue:Red;
                badges[id].text=(ours?"PROPIA ":"RIVAL ")+f.Quantity;badges[id].color=active==id?Gold:ours?new Color(.65f,.87f,1):new Color(1,.65f,.56f);
            }
            ownTotal.text=$"TUS FUERZAS\n{own} soldados";rivalTotal.text=$"HUESTE RIVAL\n{rival} soldados";
            defend.interactable=manual;wait.interactable=manual&&active.HasValue&&!roster.HasWaited(active.Value);
            automatic.interactable=outcome==BattleOutcome.InProgress;
            automatic.GetComponentInChildren<Text>().text=controller.IsAutomatic?"PAUSAR AUTOMÁTICO":"COMBATE AUTOMÁTICO";
            withdraw.interactable=outcome==BattleOutcome.InProgress;
            leave.GetComponentInChildren<Text>().text=outcome==BattleOutcome.InProgress?"VOLVER AL MAPA":"APLICAR Y VOLVER";
            history.text=recent.Count==0?"Mover o atacar consume una activación.\nVolver al mapa deja el combate pendiente.":string.Join("\n",recent);
            if(!inspected.HasValue&&active.HasValue)inspected=active;
            UpdateInspector();Preview(selected??hovered);
            result.SetActive(outcome!=BattleOutcome.InProgress);
            if(outcome!=BattleOutcome.InProgress)
            {
                var report=BattleReport.Create(roster);bool won=(outcome==BattleOutcome.AttackerVictory&&controller.PlayerBattleSide==BattleSide.Attacker)||(outcome==BattleOutcome.DefenderVictory&&controller.PlayerBattleSide==BattleSide.Defender);
                var ownReport=controller.PlayerBattleSide==BattleSide.Attacker?report.Attacker:report.Defender;
                var enemyReport=controller.PlayerBattleSide==BattleSide.Attacker?report.Defender:report.Attacker;
                resultText.text=(outcome==BattleOutcome.Draw?"EMPATE":won?"VICTORIA":"DERROTA")+$"\n\nTus fuerzas\n{ownReport.Survivors} supervivientes · {ownReport.Casualties} bajas\n\nHueste rival\n{enemyReport.Survivors} supervivientes · {enemyReport.Casualties} bajas\n\nEl resultado se aplica al volver al mapa.";
                confirm.interactable=defend.interactable=wait.interactable=false;
            }
        }
        public void SelectCell(HexCoordinates cell)
        {
            if(!IsOpen||retreatDialog.activeSelf||ResultVisible||!controller.VisualCommands.TryGetTerrain(cell,out _))return;
            selected=cell;hovered=null;
            if(controller.VisualCommands.TryGetOccupant(cell,out int id))Inspect(id);
            Preview(cell);
        }
        internal void HoverCell(HexCoordinates? cell)
        {if(!IsOpen||selected.HasValue||hovered==cell)return;hovered=cell;Preview(cell);}
        public void ClearSelection(){if(!IsOpen)return;selected=null;hovered=null;Preview(null);}
        private void Preview(HexCoordinates? cell)
        {
            selectedPath=null;selectedTarget=null;confirm.interactable=false;
            bool manual=controller.CanGiveVisualOrder;
            string text=controller.VisualRoster.Outcome!=BattleOutcome.InProgress?"El combate ha terminado.":manual?"Tu formación tiene la iniciativa. Clic en casilla azul para mover o en enemigo resaltado para atacar.":"La hueste rival está actuando. Puedes consultar sus formaciones.";
            string button="ELIGE UN DESTINO";
            if(cell.HasValue)
            {
                controller.VisualCommands.TryGetTerrain(cell.Value,out var terrain);
                string name=terrain==TacticalTerrain.Clear?"Pradera":terrain==TacticalTerrain.Woodland?"Arbolado · defensa +2":"Pedregal · defensa +3";
                if(CanAttack(cell.Value,out int enemy))
                {selectedTarget=enemy;button="ATACAR FORMACIÓN";text=name+" · Objetivo al alcance. Clic para atacar.";confirm.interactable=manual&&selected.HasValue;}
                else if(manual&&active.HasValue&&controller.VisualCommands.TryPreviewMove(active.Value,cell.Value,out selectedPath,out var failure))
                {button="MOVER AQUÍ";text=$"{name} · Ruta de {selectedPath.Steps} pasos · Coste {selectedPath.TotalCost}/{controller.VisualRoster.GetFormation(active.Value).Attributes.Movement}.";confirm.interactable=selected.HasValue;}
                else if(controller.VisualCommands.TryGetOccupant(cell.Value,out int occupant))
                {text=name+" · "+(controller.VisualRoster.GetFormation(occupant).Slot.Side==controller.PlayerBattleSide?"Formación propia. La iniciativa determina quién puede actuar.":"Enemigo fuera de alcance.");}
                else text=name+" · Fuera del movimiento disponible.";
            }
            instruction.text=text;confirm.GetComponentInChildren<Text>().text=button;Stage.Highlight(reachable,targets,cell,selectedPath);
        }
        private bool CanAttack(HexCoordinates cell,out int enemy)
        {
            enemy=0;
            if(!controller.CanGiveVisualOrder||!controller.VisualRoster.ActiveFormationId.HasValue||!controller.VisualCommands.TryGetOccupant(cell,out enemy))return false;
            var f=controller.VisualRoster.GetFormation(controller.VisualRoster.ActiveFormationId.Value);var target=controller.VisualRoster.GetFormation(enemy);
            return target.IsInBattle&&f.Slot.Side!=target.Slot.Side&&controller.VisualCommands.TryGetPosition(f.Slot.Id,out var origin)&&origin.DistanceTo(cell)<=f.Attributes.Range;
        }
        public void ConfirmSelection()
        {
            if(!IsOpen||!confirm.interactable||retreatDialog.activeSelf||!selected.HasValue)return;
            bool accepted=selectedTarget.HasValue?controller.GiveVisualOrder(BattleRecordedOrderKind.Attack,targetId:selectedTarget.Value):controller.GiveVisualOrder(BattleRecordedOrderKind.Move,selected);
            if(accepted){selected=null;selectedTarget=null;selectedPath=null;}Refresh(true);
        }
        private void Order(BattleRecordedOrderKind kind)
        {if(retreatDialog.activeSelf)return;controller.GiveVisualOrder(kind);Refresh(true);}
        private void Inspect(int id){inspected=id;UpdateInspector();}
        private void UpdateInspector()
        {
            if(!inspected.HasValue)return;var f=controller.VisualRoster.GetFormation(inspected.Value);
            selectionText.text=$"{FormationName(f)} · {f.Quantity}/{f.InitialQuantity} soldados · Moral {f.Morale}\nAtaque {f.Attributes.Attack} · Defensa {f.Attributes.Defense} · Alcance {f.Attributes.Range} · Movimiento {f.Attributes.Movement}";
        }
        public void ShowRetreatConfirmation()
        {
            if(!IsOpen||controller.VisualRoster.Outcome!=BattleOutcome.InProgress)return;
            retreatText.text="¿RETIRAR TU HUESTE?\n\n"+controller.VisualRetreat+"\n\nConservará sus supervivientes y heridas. La retirada se aplicará al volver al mapa.";retreatDialog.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(CancelRetreatButton.gameObject);
        }
        private void CancelRetreat(){retreatDialog.SetActive(false);EventSystem.current?.SetSelectedGameObject(withdraw.gameObject);}
        public void ConfirmRetreat()
        {if(!IsOpen||!retreatDialog.activeSelf)return;controller.WithdrawPlayerArmy();retreatDialog.SetActive(false);Refresh(true);EventSystem.current?.SetSelectedGameObject(leave.gameObject);}
        private void PositionBadges()
        {
            foreach(var pair in badges)
            {
                bool visible=Stage.IsFormationVisible(pair.Key);var point=Stage.Camera.WorldToViewportPoint(Stage.PositionOf(pair.Key)+Vector3.up*1.6f);
                pair.Value.gameObject.SetActive(visible&&point.z>0&&point.x>.02f&&point.x<.98f&&point.y>.02f&&point.y<.96f);
                pair.Value.rectTransform.anchoredPosition=new Vector2((point.x-.5f)*field.rect.width,(point.y-.5f)*field.rect.height);
            }
        }
        public static string FormationName(BattleFormationState f)=>f.Slot.Kind==FormationKind.Cavalry?"Jinetes ligeros":f.Slot.Kind==FormationKind.Ranged?"Ballesteros":f.Attributes.Attack>=7?"Hombres de armas":"Lanceros";
        private string FriendlyMessage(string raw)
        {
            if(raw.Contains("daño"))return "Las formaciones intercambian un ataque. Consulta las bajas y la moral.";
            if(raw.Contains("mueve"))return "Una formación ha avanzado por el campo.";
            if(raw.Contains("defensa"))return "Una formación ha adoptado una posición defensiva.";
            if(raw.Contains("aplazada"))return "Una formación espera su siguiente oportunidad.";
            if(raw.Contains("retira"))return "La hueste se retira del combate.";
            return "Las huestes preparan su siguiente maniobra.";
        }
        private static RectTransform Box(string name,Transform parent,float left,float bottom,float right,float top,Color color)
        {var rect=CampaignMenuUI.Rect(name,parent,Vector2.zero,Vector2.zero,Vector2.zero);Stretch(rect,left,bottom,right,top);CampaignMenuUI.Background(rect,color);return rect;}
        private static void Stretch(RectTransform rect,float left,float bottom,float right,float top)
        {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(left,bottom);rect.offsetMax=new Vector2(-right,-top);}
        private static Text Label(Transform parent,string text,Rect box,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft)
        {var label=CampaignMenuUI.Text("Texto",parent,new Vector2(0,1),new Vector2(box.x,-box.y),box.size,text,size,color);label.alignment=align;return label;}
        private static Button SmallButton(Transform parent,string text,Rect box,UnityEngine.Events.UnityAction click)
        {var button=CampaignMenuUI.Button(text,parent,new Vector2(0,1),new Vector2(box.x,-box.y),box.size,text,click);button.targetGraphic.color=new Color(.18f,.23f,.23f);button.GetComponentInChildren<Text>().fontSize=18;return button;}
        private void OnDisable(){Close();}
        private void OnDestroy(){Close();}
    }

    public sealed class BattlefieldPointer : MonoBehaviour,IPointerClickHandler,IPointerMoveHandler,IPointerExitHandler,IScrollHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IPointerDownHandler
    {
        public BattleGraphicalView Owner {get;set;}
        private bool dragged;
        public void OnPointerDown(PointerEventData data){dragged=false;}
        public void OnBeginDrag(PointerEventData data){if(data.button!=PointerEventData.InputButton.Right)dragged=true;}
        public void OnDrag(PointerEventData data)
        {
            if(!dragged||Owner?.CanNavigate!=true)return;
            var rect=(RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,data.position,data.pressEventCamera,out var now);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,data.position-data.delta,data.pressEventCamera,out var before);
            Owner.Stage.DragView((now-before)/new Vector2(Mathf.Max(1,rect.rect.width),Mathf.Max(1,rect.rect.height)));
        }
        public void OnEndDrag(PointerEventData data){}
        private bool TryCell(PointerEventData data,out HexCoordinates cell)
        {
            cell=default;var rect=(RectTransform)transform;
            if(Owner?.Stage==null||!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,data.position,data.pressEventCamera,out var at))return false;
            return Owner.Stage.TryPick(new Vector2((at.x-rect.rect.xMin)/rect.rect.width,(at.y-rect.rect.yMin)/rect.rect.height),out cell);
        }
        public void OnPointerClick(PointerEventData data){if(dragged||Owner?.CanNavigate!=true)return;if(data.button==PointerEventData.InputButton.Right)Owner.ClearSelection();else if(data.button==PointerEventData.InputButton.Left&&TryCell(data,out var cell)){Owner.SelectCell(cell);Owner.ConfirmSelection();}}
        public void OnPointerMove(PointerEventData data){Owner.HoverCell(TryCell(data,out var cell)?cell:(HexCoordinates?)null);}
        public void OnPointerExit(PointerEventData data){Owner.HoverCell(null);}
        public void OnScroll(PointerEventData data){if(Owner?.CanNavigate==true)Owner.Stage.SetZoom(Owner.Stage.Zoom-data.scrollDelta.y*.04f);}
    }
}
