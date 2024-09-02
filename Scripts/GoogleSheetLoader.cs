using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Networking;

namespace GoogleSheetLoader
{
    public static class GoogleSheetLoader
    {
        private static int _loadingProgress;
        private static int _sheetCount;
        private static bool _isError;

        public static async void Load(GoogleSheetDataContainer googleSheetDataContainer)
        {
            _isError = false;

            List<UniTask> tasks = new List<UniTask>();
            
            foreach (UIDData data in googleSheetDataContainer.uidGidPairs)
            {
                string uid = data.uid;
                List<GIDData> gidList = data.gidList;
                foreach (GIDData gidData in gidList)
                {
                    tasks.Add(LoadTask(uid, gidData,googleSheetDataContainer));
                }
            }

#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("GoogleSheets Load", "Loading GoogleSheets...", 0);
#endif

            _sheetCount = tasks.Count;
            await UniTask.WhenAll(tasks);

#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
            _sheetCount = 0;
            _loadingProgress = 0;

            if (_isError == false)
                Debug.Log("All sheets has loaded successfully");
        }

        private static async UniTask LoadTask(string uid, GIDData gidData, GoogleSheetDataContainer googleSheetDataContainer)
        {
            int startRowIndex = googleSheetDataContainer.startRowIndex;
            int defineStartRowIndex = googleSheetDataContainer.defineStartRowIndex;
            int defineStartColumnIndex = googleSheetDataContainer.defineStartColumnIndex;
            string jsonPath = googleSheetDataContainer.jsonPath;
            
            bool isDefineSheet = gidData.isDefineSheet;
            GoogleSheetResponse response = await GetSheetTsv(uid, gidData);
            if (response == null)
                return;

            string json = isDefineSheet
                ? ConvertToDefineJson(response.tsv, defineStartRowIndex, defineStartColumnIndex)
                : ConvertToJson(response.tsv, startRowIndex);

            SaveJson(jsonPath, response.fileName, json);
            float progress = (++_loadingProgress / (float)_sheetCount) * 100;

#if UNITY_EDITOR
            if (!_isError)
                EditorUtility.DisplayProgressBar("GoogleSheets Load", "Loading GoogleSheets...", progress);
#endif
        }

        private static async UniTask<GoogleSheetResponse> GetSheetTsv(string uid, GIDData gidData)
        {
            int gid = gidData.gid;
            string url = GetURL(uid, gid);
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.downloadHandler = new DownloadHandlerBuffer();

                try
                {
                    await www.SendWebRequest();
                    if (www.result == UnityWebRequest.Result.Success && www.downloadHandler != null)
                    {
                        Dictionary<string, string> responseHeaders = www.GetResponseHeaders();
                        string contentDisposition = responseHeaders["Content-Disposition"];

                        string customSheetName = gidData.customName;
                        string sheetName = string.IsNullOrEmpty(customSheetName)
                            ? GetSheetName(contentDisposition)
                            : customSheetName;
                        string text = www.downloadHandler.text;
                        GoogleSheetResponse response = new GoogleSheetResponse()
                        {
                            tsv = text,
                            fileName = sheetName
                        };
                        return response;
                    }
                }
                catch
                {
                    long responseCode = www.responseCode;
                    OnError((GoogleSheetErrorCode)responseCode, uid, gid);

                    return null;
                }

                return null;
            }

            static string GetURL(string uid, int gid)
            {
                return $"https://docs.google.com/spreadsheets/d/{uid}/export?format=tsv&id={uid}&gid={gid}";
            }
        }

        private static string GetSheetName(string contentDisposition)
        {
            string fullFilename = "unknown";
            string fileName = fullFilename;

            // content-Disposition에서 "fileName=" 옆에 있는 파일 이름 추출
            Match match = Regex.Match(contentDisposition, @"filename=""(?:.*-)?(?<filename>[^""]+)(?:\.[^.]*)?""");

            if (match.Success)
            {
                fullFilename = match.Groups["filename"].Value;

                // 확장자 이름 제거
                fileName = fullFilename.Split(".")[0];
            }

            return fileName;
        }

        private static string ConvertToJson(string response, int startRowIndex)
        {
            if (string.IsNullOrEmpty(response))
                return null;

            try
            {
                // 데이터를 줄 단위로 분리합니다.
                string[] rows = response.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

                // 헤더 정보를 startRowIndex를 기준으로 추출합니다.
                string[] headers = rows[startRowIndex]
                    .Split('\t')
                    .Select(header => header.Trim('"'))
                    .ToArray();

                // 열의 배열 여부를 결정합니다.
                bool[] isArrayColumn = new bool[headers.Length];

                // 모든 행을 탐색하여 각 열에 쉼표가 있는지 확인합니다.
                foreach (string row in rows.Skip(startRowIndex + 1))
                {
                    string[] values = row.Split('\t');
                    for (int i = 0; i < headers.Length; i++)
                    {
                        if (values[i].Contains(','))
                        {
                            isArrayColumn[i] = true; // 해당 열은 배열로 처리
                        }
                    }
                }

                // 데이터 행을 반복 처리하여 JSON 객체로 변환합니다.
                var jsonObjects = rows
                    .Skip(startRowIndex + 1) // 헤더 행을 건너뜁니다.
                    .Select(row => ConvertRowToDictionary(row, headers, isArrayColumn))
                    .ToList();

                // JSON 형식으로 변환하여 반환합니다.
                return JsonConvert.SerializeObject(jsonObjects, Formatting.Indented);
            }
            catch
            {
                OnError(GoogleSheetErrorCode.ConvertJsonError);
            }

            static Dictionary<string, object> ConvertRowToDictionary(string row, string[] headers,
                bool[] isArrayColumn)
            {
                string[] values = row.Split('\t');
                Dictionary<string, object> dictionary = new Dictionary<string, object>(headers.Length);

                for (int i = 0; i < headers.Length; i++)
                {
                    string value = values[i].Trim('"');

                    dictionary[headers[i]] = isArrayColumn[i]
                        ? value.Split(',').Select(v => v.Trim()).ToList() // 배열로 처리
                        : value; // 단일 문자열로 처리
                }

                return dictionary;
            }

            return null;
        }

        private static string ConvertToDefineJson(string response, int startRowIndex, int startColumnIndex)
        {
            // 줄 단위로 데이터를 나누고, 시작 행 인덱스 이후의 줄들을 가져옵니다.
            string[] lines = response
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Skip(startRowIndex + 1)
                .ToArray();

            // 결과를 담을 Dictionary를 초기화합니다.
            var result = new Dictionary<string, object>();

            // 각 줄을 순회합니다.
            foreach (string line in lines)
            {
                // 탭 단위로 컬럼을 나누고, 유효한 데이터인지 확인합니다.
                string[] columns = line.Split('\t');
                if (columns.Length <= 1)
                {
                    continue; // 유효한 데이터가 아닐 경우 무시합니다.
                }

                // 첫 번째 컬럼을 키로, 나머지 컬럼을 값으로 사용합니다.
                string key = columns[startColumnIndex].Trim();
                List<string> values = columns.Skip(1).Select(v => v.Trim()).ToList();

                // 첫 번째 값을 추출합니다.
                if (values.Count == 0)
                {
                    continue; // 값이 없는 경우 무시합니다.
                }

                string firstValue = values[0];
        
                // 첫 번째 값에 콤마가 포함된 경우 리스트로 변환합니다.
                object finalValue = firstValue.Contains(',')
                    ? firstValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList()
                    : firstValue;

                // 결과 딕셔너리에 키와 값을 추가합니다.
                result[key] = finalValue;
            }

            // 결과를 JSON 형식으로 변환하여 반환합니다.
            return JsonConvert.SerializeObject(result, Formatting.Indented);
        }


        private static void SaveJson(string path, string sheetName, string json)
        {
            if (path.Contains("StreamingAssets"))
            {
                path = Application.streamingAssetsPath;
            }

            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(path);
            }

            string fullPath = Path.Combine(path, sheetName + ".txt");
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            File.WriteAllBytes(fullPath, bytes);

#if UNITY_EDITOR
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif

            Debug.Log($"{sheetName} sheet saved as json successfully.");
        }

        private static void OnError(GoogleSheetErrorCode errorCode, string uid = null, int gid = -1)
        {
            _isError = true;
            _sheetCount = 0;
            _loadingProgress = 0;

#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif

            switch (errorCode)
            {
                case GoogleSheetErrorCode.GIDNotFound:
                    Debug.LogError($"uid: {uid} / gid: {gid}\n{GoogleSheerDefine.GIDNotFoundError}");
                    break;

                case GoogleSheetErrorCode.UIDNotFound:
                    Debug.LogError($"uid: {uid}\n{GoogleSheerDefine.UIDNotFoundError}");
                    break;

                case GoogleSheetErrorCode.ConvertJsonError:
                    Debug.LogError(GoogleSheerDefine.ConvertJsonError);
                    break;
            }
        }
    }
}