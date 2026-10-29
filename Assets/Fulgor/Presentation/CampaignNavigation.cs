using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fulgor.Presentation.Map
{
    public static class CampaignNavigation
    {
        private static string pendingScene,pendingSnapshot;
        public static bool IsTransitioning {get;private set;}
        public static string LastError {get;private set;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){pendingScene=null;pendingSnapshot=null;IsTransitioning=false;LastError=null;}
        public static bool Start(CampaignLaunchEntry entry,string snapshot=null)
        {
            if(IsTransitioning||entry==null)return false;
            if(snapshot!=null&&!entry.TryValidateSnapshot(snapshot,out _)){LastError="El guardado no es válido para esta campaña.";return false;}
            try
            {
                entry.Validate();pendingScene=entry.SceneName;pendingSnapshot=snapshot;LastError=null;IsTransitioning=true;
                SceneManager.LoadSceneAsync(entry.SceneName,LoadSceneMode.Single);return true;
            }
            catch(Exception){pendingScene=null;pendingSnapshot=null;IsTransitioning=false;LastError="No se pudo abrir la campaña. La partida actual se conserva.";return false;}
        }
        public static string ConsumeSnapshot(string sceneName)
        {
            string snapshot=pendingScene==sceneName?pendingSnapshot:null;
            pendingScene=null;pendingSnapshot=null;IsTransitioning=false;return snapshot;
        }
        public static bool ReturnToMenu(string sceneName)
        {
            if(IsTransitioning)return false;
            try{IsTransitioning=true;SceneManager.LoadSceneAsync(sceneName,LoadSceneMode.Single);return true;}
            catch(Exception){IsTransitioning=false;LastError="No se pudo abrir el inicio. La partida actual se conserva.";return false;}
        }
        public static string EnterMenu()
        {IsTransitioning=false;pendingScene=null;pendingSnapshot=null;var message=LastError;LastError=null;return message;}
    }
}
