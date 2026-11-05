using System;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public static class CampaignSaveSlots
    {
        public const int Count = 5;
        public static string Key(int slot)
        {
            if(slot<0||slot>=Count)throw new ArgumentOutOfRangeException(nameof(slot));
            return "Fulgor.Campaign.ManualSlot.v1."+slot;
        }
        [Serializable] public sealed class Record
        {
            public string snapshot;
            public string savedAt;
        }
        public static Record Read(int slot)
        {
            var json=PlayerPrefs.GetString(Key(slot),"");
            if(string.IsNullOrEmpty(json))return null;
            try{return JsonUtility.FromJson<Record>(json);}catch(ArgumentException){return null;}
        }
        public static void Write(int slot,string snapshot)
        {
            if(string.IsNullOrWhiteSpace(snapshot))throw new ArgumentException("Missing snapshot.");
            PlayerPrefs.SetString(Key(slot),JsonUtility.ToJson(new Record{snapshot=snapshot,savedAt=DateTime.Now.ToString("dd/MM/yyyy HH:mm")}));
            PlayerPrefs.Save();
        }
    }
}
