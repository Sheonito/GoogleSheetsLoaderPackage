using System.Collections.Generic;
using UnityEngine;

namespace GoogleSheetLoader
{
    [CreateAssetMenu(fileName = "GoogleSheetDataContainer", menuName = "Custom Data/GoogleSheetData Container", order = 1)]
    public class GoogleSheetDataContainer : ScriptableObject
    {
        public List<UIDData> uidGidPairs = new List<UIDData>();
        public int startRowIndex;
        public int defineStartRowIndex;
        public int defineStartColumnIndex;
        public string jsonPath;
    }

    [System.Serializable]
    public class UIDData
    {
        public string displayName;
        public string uid;
        public List<GIDData> gidList = new List<GIDData>();
    }

    [System.Serializable]
    public class GIDData
    {
        public int gid;
        public bool isDefineSheet;
        public string customName;
    }
}
