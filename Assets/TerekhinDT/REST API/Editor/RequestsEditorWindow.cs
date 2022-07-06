using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Unity.Plastic.Newtonsoft.Json.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace TerekhinDT.REST_API.Editor
{
    public class RequestsEditorWindow : EditorWindow
    {
        private enum ContentTypes {Fields, Raw}
        private ContentTypes _contentTypes;
        private ResponseTypes _responseTypes;
        private RequestTypes _requestTypes;
        private FormDataTypes _formDataTypes;
        
        private readonly string [] _bodyParamsTypes    = new string[4] { "none", "form-data", "x-www-form-urlencoded", "raw" };
        private readonly string[] _tabs = {"params", "headers", "body"};
        private string _dataRaw = "{\n}";
        private string _url;
        private string _name;
        private string _testResponse;

        private int _bodyParamsIndex = 0;
        private int _tabIndex = 0;

        private Vector2 _scrollPos = new Vector2();

        private readonly Dictionary<string, List<QKeyValuePair>> _tabsParams = new Dictionary<string, List<QKeyValuePair>>
        {
            {"params", new List<QKeyValuePair>()},
            {"headers", new List<QKeyValuePair>()},
            {"body", new List<QKeyValuePair>()},
        };

        [MenuItem("TerekhinDT/REST API/Requests Editor")]
        private static void Init()
        {
            var window = (RequestsEditorWindow )EditorWindow.GetWindow(typeof(RequestsEditorWindow ));
            window.minSize = new Vector2(600, 600);
            window.Show();
        }
        
        private void OnGUI()
        {
            #region Header
            GUILayout.Space(50);
            _name = EditorGUILayout.TextField("Name",_name);
            _url = EditorGUILayout.TextField("URL",_url);
            _requestTypes = (RequestTypes)EditorGUILayout.EnumPopup("Request Type", _requestTypes);
            _responseTypes = (ResponseTypes)EditorGUILayout.EnumPopup("Response Type", _responseTypes);
            #endregion
            
            #region TopMenu
            _tabIndex = GUILayout.Toolbar (_tabIndex, _tabs);

            if (_tabs[_tabIndex] == "body")
            {
                _bodyParamsIndex = EditorGUILayout.Popup("Type", _bodyParamsIndex, new string[4] { "none", "form-data", "x-www-form-urlencoded", "raw" });
                _formDataTypes = (FormDataTypes)_bodyParamsIndex;
                _contentTypes = _bodyParamsTypes[_bodyParamsIndex] == "raw" ? ContentTypes.Raw : ContentTypes.Fields;
            }
            else _contentTypes = ContentTypes.Fields;

            #endregion

            #region Main
            switch (_contentTypes)
            {
                case ContentTypes.Fields:
                    ViewRequestFields();
                    break;
                case ContentTypes.Raw:
                    ViewRequestRawData();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            #endregion
            
            #region BoottomMenu
            GUILayout.Space(50);
            if (GUILayout.Button("Test"))
            {
                Test();
            }

            GUIStyle style = new GUIStyle(EditorStyles.textArea);
            style.wordWrap = true;
            style.stretchHeight = true;
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos,GUILayout.Height(300));
            EditorGUILayout.TextArea(_testResponse, style);
            EditorGUILayout.EndScrollView();
            
            if (GUILayout.Button("Save"))
            {
                var asset = CreateInstance<Request>();
                if (!Directory.Exists(Path.Combine(Application.streamingAssetsPath, "Requests"))) Directory.CreateDirectory(Path.Combine(Application.streamingAssetsPath, "Requests"));
                if (!AssetDatabase.IsValidFolder("Assets/Resources/Requests")) AssetDatabase.CreateFolder("Assets/Resources", "Requests");
                AssetDatabase.CreateAsset(asset, Path.Combine("Assets/Resources/Requests", _name+".asset"));
                AssetDatabase.SaveAssets();

                EditorUtility.FocusProjectWindow();

                Selection.activeObject = asset;
            }
            #endregion
        }

        private void ViewRequestFields()
        {
            if (GUILayout.Button("Add")) _tabsParams[_tabs[_tabIndex]].Add(new QKeyValuePair ("key","value"));
            
            foreach (var param in _tabsParams[_tabs[_tabIndex]].ToList())
            {
                GUILayout.Label($"Param {param.key}");
                param.key = GUILayout.TextField(param.key);
                param.value = GUILayout.TextField(param.value);
                GUILayout.Space(10);
                if (GUILayout.Button("Remove")) _tabsParams[_tabs[_tabIndex]].Remove(param);
            }
        }
        
        private void ViewRequestRawData()
        {
            _dataRaw = GUILayout.TextArea(_dataRaw, 300);
        }

        private async void Test()
        {
            if (_requestTypes == RequestTypes.Get || _requestTypes == RequestTypes.Delete)
                await SendRequest(_requestTypes);
            else await SendRequest(_requestTypes, _formDataTypes);
        }

        private async Task SendRequest(RequestTypes requestTypes)
        {
            _testResponse = "Waiting\n";
            HttpClient client = new HttpClient();
     
            try
            {
                HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
                
                switch (requestTypes)
                {
                    case RequestTypes.Get:
                        httpResponseMessage = await client.GetAsync(_url);
                        break;
                    case RequestTypes.Delete:
                        httpResponseMessage = await client.DeleteAsync(_url);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(requestTypes), requestTypes, null);
                }
                
                _testResponse += await httpResponseMessage.Content.ReadAsStringAsync()+"\n";
                
                httpResponseMessage.EnsureSuccessStatusCode();
            }

            catch (HttpRequestException e)
            {
                _testResponse += "Message :{0} " + e.Message + "\n";
            }
        }
        
        private async Task SendRequest(RequestTypes requestTypes, FormDataTypes formDataTypes)
        {
            _testResponse = "Waiting\n";
            HttpClient client = new HttpClient();
            HttpContent content = new MultipartContent();

            switch (formDataTypes)
            {
                //TODO Реализовать остальные части формы
                case FormDataTypes.None:
                    break;
                case FormDataTypes.FormData:
                    break;
                case FormDataTypes.UrlEncodedForm:
                    break;
                case FormDataTypes.Raw:
                    JObject jObject = JObject.Parse(_dataRaw);
                    content = new StringContent(jObject.ToString(), Encoding.UTF8, "application/json");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(formDataTypes), formDataTypes, null);
            }

            _testResponse += $"{content.Headers}\n";
            
            try
            {
                HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
                
                switch (requestTypes)
                {
                    case RequestTypes.Get:
                        httpResponseMessage = await client.GetAsync(_url);
                        break;
                    case RequestTypes.Post:
                        httpResponseMessage = await client.PostAsync(_url, content);
                        break;
                    case RequestTypes.Put:
                        //httpResponseMessage = await client.PutAsync(_url);
                        break;
                    case RequestTypes.Delete:
                        httpResponseMessage = await client.DeleteAsync(_url);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(requestTypes), requestTypes, null);
                }
                
                _testResponse += await httpResponseMessage.Content.ReadAsStringAsync()+"\n";
                
                httpResponseMessage.EnsureSuccessStatusCode();
            }

            catch (HttpRequestException e)
            {
                _testResponse += "Message :{0} " + e.Message + "\n";
            }
        }
    }
}
