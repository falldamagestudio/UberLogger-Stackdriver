﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class PostToLog : MonoBehaviour {

	// Use this for initialization
	void Start () {

        StartCoroutine(PostMessageToLog("hello"));
	}
	
	// Update is called once per frame
	void Update () {
	}

    [Serializable]
    public class JsonableMessages
    {
        public List<JsonableMessage> entries = new List<JsonableMessage>();
    }

    [Serializable]
    public class JsonableMessage
    {
        public string message;

        public JsonableMessage(string message)
        {
            this.message = message;
        }
    }

    private IEnumerator PostMessageToLog(string message)
    {
        string url = "https://us-central1-unity-log-to-stackdriver.cloudfunctions.net/appendToLog";

        JsonableMessages messages = new JsonableMessages();
        messages.entries.Add(new JsonableMessage(message));

        string jsonMessage = JsonUtility.ToJson(messages);
        byte[] jsonMessageBytes = Encoding.UTF8.GetBytes(jsonMessage);

        UnityWebRequest request = UnityWebRequest.Post(url, "");
        // Hack: provide the JSON data via a custom upload handler
        // UnityWebRequest.Post will assume that body is going to be sent as application/x-www-form-urlencoded format, and it will apply that conversion to the body string
        //   ( so {"a":"b"} turns into %7b%22a%22:%22b%22%7d )
        // To get around this, we provide a handler that will send raw JSON bytes and with the appropriate content type
        request.uploadHandler = new UploadHandlerRaw(jsonMessageBytes);
        request.uploadHandler.contentType = "application/json";

        yield return request.Send();

        if (request.isError)
            Debug.Log("Error: " + request.error);
        else
            Debug.Log("Done. Response code: " + request.responseCode + ", response: " + request.downloadHandler.text);
    }
}
