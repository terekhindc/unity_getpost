using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace TerekhinDT.REST_API.Editor
{
    [Serializable]
    [CreateAssetMenu(fileName = "Request", menuName = "ScriptableObjects/Request", order = 1)]
    public class Request : ScriptableObject
    {
        public enum RequestTypes {GET, POST, PUT, DELETE}

        public RequestTypes type;
        public string url;
        public string raw;
        
        public QKeyValuePair [] @params;
        public QKeyValuePair [] headers;
        public QKeyValuePair [] body;

        public UnityEvent callbacks = new UnityEvent();
        public string response;
    }
    
    [Serializable]
    public class QKeyValuePair
    {
        public string key;
        public string value;

        public QKeyValuePair(string key, string value)
        {
            this.key = key;
            this.value = value;
        }
    }
}
