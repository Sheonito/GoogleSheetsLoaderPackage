using UnityEngine;

namespace GoogleSheetLoader
{
    public enum GoogleSheetEditorPage
    {
        None,
        Load,
        Setting,
        History,
    }

    public enum GoogleSheetErrorCode
    {
        GIDNotFound = 400,
        UIDNotFound = 404,
        ConvertJsonError = 4000,
    }

    public class GoogleSheerDefine
    {
        #region ErrorLog

        public const string GIDNotFoundError =
            "Sheet Gid is wrong. Check if there are any Gid elements have wrong Gid.";
        
        public const string UIDNotFoundError = 
            "Sheet Uid is wrong. Check if there are any Uid elements have wrong Uid.";
        
        public const string ConvertJsonError = 
            "StartRowIndex or sheetData is wrong. startRowIndex has to start from variable name.";

        #endregion

        
        public const string GoogleSheetDataPath = "Assets/Plugins/GoogleSheetLoader";
        public const string GoogleSheetDataName = "GoogleSheetData.asset";
        public static readonly Vector2 EditorSize = new Vector2(500,750);
    }
}