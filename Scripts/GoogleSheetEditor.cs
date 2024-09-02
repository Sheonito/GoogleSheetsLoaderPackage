#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using Directory = UnityEngine.Windows.Directory;

namespace GoogleSheetLoader
{
    public class GoogleSheetEditor : EditorWindow
    {
        private static GUIContent _folderIcon;

        private SerializedObject _loadPageRootObject;
        private SerializedProperty _uidGidPairsProperty;

        private GoogleSheetDataContainer _dataContainer;

        private Vector2 _scrollPosition;
        private GoogleSheetEditorPage _curPage;
        private GoogleSheetEditorPage _prePage;
        private int _startRowIndex;
        private int _defineStartRowIndex;
        private int _defineStartColumnIndex;

        [MenuItem("Tools/GoogleSheets Loader")]
        public static void ShowWindow()
        {
            GoogleSheetEditor window = GetWindow<GoogleSheetEditor>("Google Sheet Editor");

            // EditorWindow size fixed
            window.minSize = GoogleSheerDefine.EditorSize;
            window.maxSize = GoogleSheerDefine.EditorSize;
        }

        private void OnEnable()
        {
            // Icon initialization
            _folderIcon = EditorGUIUtility.IconContent("Folder Icon");

            LoadData();
            _curPage = GoogleSheetEditorPage.Load;
        }
        

        private void LoadData()
        {
            // Load or create ScriptableObject for persistent data storage
            string assetDirPath = GoogleSheerDefine.GoogleSheetDataPath;
            string assetPath = Path.Combine(assetDirPath, GoogleSheerDefine.GoogleSheetDataName);

            if (Directory.Exists(assetDirPath) == false)
                Directory.CreateDirectory(assetDirPath);

            _dataContainer = AssetDatabase.LoadAssetAtPath<GoogleSheetDataContainer>(assetPath);

            if (_dataContainer == null)
            {
                _dataContainer = CreateInstance<GoogleSheetDataContainer>();
                AssetDatabase.CreateAsset(_dataContainer, assetPath);
                AssetDatabase.SaveAssets();
            }

            _loadPageRootObject = new SerializedObject(_dataContainer);
            _uidGidPairsProperty = _loadPageRootObject.FindProperty("uidGidPairs");
        }

        private void OnDisable()
        {
            EditorUtility.SetDirty(_dataContainer);
            AssetDatabase.SaveAssets();
        }

        private void OnGUI()
        {
            _loadPageRootObject.Update();

            // Top Load, History menu
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            _prePage = _curPage;

            // Load Page
            bool isLoadPage = _curPage == GoogleSheetEditorPage.Load;
            if (GUILayout.Toggle(isLoadPage, "Load", EditorStyles.toolbarButton) != isLoadPage)
            {
                _curPage = GoogleSheetEditorPage.Load;
            }

            // Setting Page
            bool isSettingPage = _curPage == GoogleSheetEditorPage.Setting;
            if (GUILayout.Toggle(isSettingPage, "Setting", EditorStyles.toolbarButton) != isSettingPage)
            {
                _curPage = GoogleSheetEditorPage.Setting;
            }

            // History Page
            bool isHistoryPage = _curPage == GoogleSheetEditorPage.History;
            if (GUILayout.Toggle(isHistoryPage, "History", EditorStyles.toolbarButton) != isHistoryPage)
            {
                _curPage = GoogleSheetEditorPage.History;
            }

            EditorGUILayout.EndHorizontal();

            if (_curPage != _prePage)
                GUI.FocusControl(null);

            // Display UI based on current page
            if (isLoadPage)
            {
                ShowLoadPage();
            }
            else if (isSettingPage)
            {
                ShowSettingPage();
            }
            else
            {
                ShowHistoryPage();
            }

            _loadPageRootObject.ApplyModifiedProperties();
            
            EditorUtility.SetDirty(_dataContainer);
        }


        private void ShowLoadPage()
        {
            // UID list
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(650));

            EditorGUILayout.PropertyField(_uidGidPairsProperty, new GUIContent("UID"));

            EditorGUILayout.EndScrollView();

            // Bottom center Load button
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // 구글시트에서 테이블 로드
            if (GUILayout.Button("Load", GUILayout.Width(100), GUILayout.Height(30)))
            {
                GoogleSheetLoader.Load(_dataContainer);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void ShowSettingPage()
        {
            const float LabelWidth = 160;
            const float ButtonIconSize = 25;

            EditorGUILayout.Space();

            _startRowIndex = _dataContainer.startRowIndex;
            _defineStartRowIndex = _dataContainer.defineStartRowIndex;
            _defineStartColumnIndex = _dataContainer.defineStartColumnIndex;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("StartRow Index", GUILayout.Width(LabelWidth));
            _startRowIndex = EditorGUILayout.IntField(_startRowIndex);
            _dataContainer.startRowIndex = _startRowIndex;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Define StartRow Index", GUILayout.Width(LabelWidth));
            _defineStartRowIndex = EditorGUILayout.IntField(_defineStartRowIndex);
            _dataContainer.defineStartRowIndex = _defineStartRowIndex;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Define StartColumn Index", GUILayout.Width(LabelWidth));
            _defineStartColumnIndex = EditorGUILayout.IntField(_defineStartColumnIndex);
            _dataContainer.defineStartColumnIndex = _defineStartColumnIndex;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Json Save Path", GUILayout.Width(LabelWidth));
            _dataContainer.jsonPath = EditorGUILayout.TextField(_dataContainer.jsonPath);
            string rootPath = "Assets/";
            if (GUILayout.Button(_folderIcon, GUILayout.Width(ButtonIconSize), GUILayout.Height(ButtonIconSize)))
            {
                _dataContainer.jsonPath = EditorUtility.OpenFolderPanel("Select Folder", "", "");
                string filePath = _dataContainer.jsonPath.Split(rootPath)[1];
                string fullPath = Path.Combine(rootPath, filePath);
                _dataContainer.jsonPath = fullPath; 
            }

            if (string.IsNullOrEmpty(_dataContainer.jsonPath) || _dataContainer.jsonPath.Contains(rootPath) == false)
            {
                _dataContainer.jsonPath = rootPath;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ShowHistoryPage()
        {
            // Content to display on the History page
            EditorGUILayout.LabelField("History", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This is where history details will be displayed.", MessageType.Info);
        }
    }
}
#endif