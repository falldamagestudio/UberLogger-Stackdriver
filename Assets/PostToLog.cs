using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;

public class PostToLog : MonoBehaviour {

	// Use this for initialization
	void Start () {
        AddLogMessage("test");
	}

    int x = 0;
    int y = 0;


	// Update is called once per frame
	void Update () {

        if (x > 10)
        {
            x = 0;
            y++;
            AddLogMessage("test " + y);
        }
        x++;

        PostMessagesIfAvailable();
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

    private JsonableMessages jsonableMessages = new JsonableMessages();
    private JsonableMessages jsonableMessagesInFlight = new JsonableMessages();

    public void AddLogMessage(string message)
    {
        lock(jsonableMessages)
        {
            jsonableMessages.entries.Add(new JsonableMessage(message));
        }
    }

    private bool postInProgress;

    /// <summary>
    /// Extract the first maxMessages messages from jsonableMessages into jsonableMessagesInFlight
    /// </summary>
    private static void extractJsonableMessages(JsonableMessages jsonableMessages, JsonableMessages jsonableMessagesInFlight, int maxMessages)
    {
        Assert.AreEqual(0, jsonableMessagesInFlight.entries.Count);
        int messageExtractCount = Math.Min(jsonableMessages.entries.Count, maxMessages);
        jsonableMessagesInFlight.entries.AddRange(jsonableMessages.entries.GetRange(0, messageExtractCount));
        jsonableMessages.entries.RemoveRange(0, messageExtractCount);
    }

    /// <summary>
    /// Given a list of messages, construct a UnityWebRequest that will post these messages to the backend entry point in Google Cloud
    /// </summary>
    private static UnityWebRequest createPost(JsonableMessages jsonableMessages, string url)
    {
        string jsonMessage = JsonUtility.ToJson(jsonableMessages);
        byte[] jsonMessageBytes = Encoding.UTF8.GetBytes(jsonMessage);

        UnityWebRequest request = UnityWebRequest.Post(url, "");
        // Hack: provide the JSON data via a custom upload handler
        // UnityWebRequest.Post will assume that body is going to be sent as application/x-www-form-urlencoded format, and it will apply that conversion to the body string
        //   ( so {"a":"b"} turns into %7b%22a%22:%22b%22%7d )
        // To get around this, we provide a handler that will send raw JSON bytes and with the appropriate content type
        request.uploadHandler = new UploadHandlerRaw(jsonMessageBytes);
        request.uploadHandler.contentType = "application/json";

        return request;
    }

    /// <summary>
    /// Coroutine which posts the request to the backend, and determines success or failure
    /// </summary>
    private static IEnumerator PostRequest(UnityWebRequest request, Action<UnityWebRequest> success, Action<UnityWebRequest> failure)
    {
        yield return request.Send();
        if (!request.isError && (request.responseCode >= 200 && request.responseCode < 300))
            success(request);
        else
            failure(request);
    }

    private void PostRequestSucceeded(UnityWebRequest request)
    {
        // In-flight messages have been successfully transmitted. We no longer need to keep them around on the client.
        jsonableMessagesInFlight.entries.Clear();

        postInProgress = false;

        Debug.Log("Post succeeded: " + request.responseCode + " " + request.downloadHandler.text);
    }

    private void PostRequestFailed(UnityWebRequest request)
    {
        // In-flight messages failed to get transmitted. Put them back at the beginning of the list.
        jsonableMessages.entries.InsertRange(0, jsonableMessagesInFlight.entries);
        jsonableMessagesInFlight.entries.Clear();

        postInProgress = false;

        if (request.isError)
            Debug.Log("Post failed. Error: " + request.error); // Unable to establish connection and perform HTTP request
        else
            Debug.Log("Post failed. Error: " + request.responseCode + " " + request.downloadHandler.text); // HTTP request performed, but backend responded with an HTTP error code
    }

    private const int MaxMessagesPerPost = 10;
    private const string url = "https://us-central1-unity-log-to-stackdriver.cloudfunctions.net/appendToLog";

    private void PostMessagesIfAvailable()
    {
        if (!postInProgress)
        {
            UnityWebRequest request = null;

            lock (jsonableMessages)
            {
                if (jsonableMessages.entries.Count > 0)
                {
                    extractJsonableMessages(jsonableMessages, jsonableMessagesInFlight, MaxMessagesPerPost);
                    request = createPost(jsonableMessagesInFlight, url);
                }
            }

            if (request != null)
            {
                postInProgress = true;
                StartCoroutine(PostRequest(request, PostRequestSucceeded, PostRequestFailed));
            }
        }
    }
}
