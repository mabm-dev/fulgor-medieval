using System.Collections;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
namespace Fulgor.Presentation.Tests.Map
{
    public sealed class CampaignDiplomacyIntegrationTests
    {
        [UnityTest]public IEnumerator CouncilBlocksMapAndSnapshotRestoresDiplomacyAtomically()
        {
            var previous=EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/Playable/FulgorConquest.unity");
            yield return new EnterPlayMode();yield return null;yield return null;
            var c=Object.FindAnyObjectByType<CampaignTurnController>();var view=c.GetComponent<CampaignDiplomacyView>();
            Assert.That(c.CanManageDiplomacy,Is.True);string rival=c.Diplomacy.Factions.First();
            Assert.That(c.SetDiplomaticRelation(rival,DiplomaticRelation.Pact,DiplomaticIntention.Conquest),Is.True);
            Assert.That(c.Diplomacy.CanAttack(c.PlayerFactionId,rival),Is.False);
            view.Open();Assert.That(view.IsOpen,Is.True);Assert.That(c.GetComponent<CampaignBoardSelection>().IsInteractionSuspended,Is.True);
            view.Close();Assert.That(c.GetComponent<CampaignBoardSelection>().IsInteractionSuspended,Is.False);
            Assert.That(c.ProposeDiplomacy(rival,DiplomaticRelation.Peace),Is.True);
            var payload=c.CaptureCampaignSnapshot();string json=JsonUtility.ToJson(payload);
            c.SetDiplomaticRelation(rival,DiplomaticRelation.War,DiplomaticIntention.Conquest);
            Assert.That(c.TryLoadCampaignSnapshot(json),Is.True);Assert.That(c.Diplomacy.Relation(rival).relation,Is.EqualTo(DiplomaticRelation.Pact));
            Assert.That(c.Diplomacy.Proposals.Any(p=>p.receiver==rival&&p.type==DiplomaticRelation.Peace),Is.True);
            payload.diplomacy.links[0].remaining=-1;
            LogAssert.Expect(LogType.Warning,new System.Text.RegularExpressions.Regex("No se pudo cargar:"));
            Assert.That(c.TryLoadCampaignSnapshot(JsonUtility.ToJson(payload)),Is.False);
            Assert.That(c.Diplomacy.Relation(rival).relation,Is.EqualTo(DiplomaticRelation.Pact));
            yield return new ExitPlayMode();if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous);
        }
    }
}
