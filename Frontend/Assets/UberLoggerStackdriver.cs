using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UberLogger;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;

public class UberLoggerStackdriver : UberLogger.ILogger {

    public delegate Coroutine StartCoroutineDelegate(IEnumerator coroutine);

    private StartCoroutineDelegate startCoroutine;

    public enum LogSeverityLevel
    {
        AllMessages,
        WarningsAndErrorsOnly,
        ErrorsOnly,
        None
    };

    private readonly string backendUrl;

    /// <summary>
    /// Max number of messages included in one HTTP POST request to the backend
    /// This setting limits the size of each individual HTTP POST request body
    /// </summary>
    private readonly int maxMessagesPerPost = 10;

    /// <summary>
    /// Minimum interval between the start of one HTTP POST request and the start of the next
    /// </summary>
    private readonly float minIntervalBetweenPosts = 1.0f;

    private readonly LogSeverityLevel logSeverityLevel;

    public UberLoggerStackdriver(StartCoroutineDelegate startCoroutine, string backendUrl, int maxMessagesPerPost, float minIntervalBetweenPosts, LogSeverityLevel logSeverityLevel)
    {
        this.startCoroutine = startCoroutine;
        this.backendUrl = backendUrl;
        this.maxMessagesPerPost = maxMessagesPerPost;
        this.minIntervalBetweenPosts = minIntervalBetweenPosts;
        this.logSeverityLevel = logSeverityLevel;

        Assert.IsNotNull(this.backendUrl);
    }

    [Serializable]
    public class StackdriverEntries
    {
        public List<StackdriverEntry> entries = new List<StackdriverEntry>();
    }

    [Serializable]
    public class StackdriverEntry
    {
        public string message;
        public int severity;
        public StackdriverSourceLocation sourceLocation;

        public StackdriverEntry(string message, int severity, StackdriverSourceLocation sourceLocation)
        {
            this.message = message;
            this.severity = severity;
            this.sourceLocation = sourceLocation;
        }
    }

    [Serializable]
    public class StackdriverSourceLocation
    {
        public string file;
        public string line;
        public string function;

        public StackdriverSourceLocation(LogStackFrame logStackFrame)
        {
            file = logStackFrame.FileName;
            line = logStackFrame.LineNumber.ToString();
            function = logStackFrame.GetFormattedMethodName();
        }
    }

    private StackdriverEntries stackdriverEntries = new StackdriverEntries();
    private StackdriverEntries stackdriverEntriesInFlight = new StackdriverEntries();

    private float previousPostTimestamp = 0.0f;
    private bool postInProgress;

    private static int LogSeverityToStackdriverSeverity(LogSeverity severity)
    {
        switch (severity)
        {
            case LogSeverity.Message: return 200;
            case LogSeverity.Warning: return 400;
            case LogSeverity.Error: return 500;
            default: throw new NotImplementedException();
        }
    }

    public void Log(LogInfo logInfo)
    {
        switch (logInfo.Severity)
        {
            case LogSeverity.Message:
                if (logSeverityLevel != LogSeverityLevel.AllMessages) return; break;
            case LogSeverity.Warning:
                if (logSeverityLevel != LogSeverityLevel.AllMessages && logSeverityLevel != LogSeverityLevel.WarningsAndErrorsOnly) return; break;
            case LogSeverity.Error:
                if (logSeverityLevel != LogSeverityLevel.AllMessages && logSeverityLevel != LogSeverityLevel.WarningsAndErrorsOnly && logSeverityLevel != LogSeverityLevel.ErrorsOnly) return; break;
            default:
                throw new NotImplementedException();
        }

        lock (stackdriverEntries)
        {
            stackdriverEntries.entries.Add(new StackdriverEntry(logInfo.Message, LogSeverityToStackdriverSeverity(logInfo.Severity), (logInfo.Callstack.Count > 0 ? new StackdriverSourceLocation(logInfo.Callstack[0]) : null)));
        }
    }

    /// <summary>
    /// Extract the first maxMessages messages from stackdriverEntries into stackdriverEntriesInFlight
    /// </summary>
    private static void extractStackdriverEntries(StackdriverEntries stackdriverEntries, StackdriverEntries stackdriverEntriesInFlight, int maxMessages)
    {
        Assert.AreEqual(0, stackdriverEntriesInFlight.entries.Count);
        int messageExtractCount = Math.Min(stackdriverEntries.entries.Count, maxMessages);
        stackdriverEntriesInFlight.entries.AddRange(stackdriverEntries.entries.GetRange(0, messageExtractCount));
        stackdriverEntries.entries.RemoveRange(0, messageExtractCount);
    }

    /// <summary>
    /// Given a list of messages, construct a UnityWebRequest that will post these messages to the backend entry point in Google Cloud
    /// </summary>
    private static UnityWebRequest createPost(StackdriverEntries stackdriverEntries, string url)
    {
        string jsonMessage = JsonUtility.ToJson(stackdriverEntries);
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
        stackdriverEntriesInFlight.entries.Clear();

        postInProgress = false;
    }

    private void PostRequestFailed(UnityWebRequest request)
    {
        // In-flight messages failed to get transmitted. Put them back at the beginning of the list.
        stackdriverEntries.entries.InsertRange(0, stackdriverEntriesInFlight.entries);
        stackdriverEntriesInFlight.entries.Clear();

        postInProgress = false;

        if (request.isError)
            Debug.Log("Post failed. Error: " + request.error); // Unable to establish connection and perform HTTP request
        else
            Debug.Log("Post failed. Error: " + request.responseCode + " " + request.downloadHandler.text); // HTTP request performed, but backend responded with an HTTP error code
    }


    public void PostMessagesIfAvailable()
    {
        if (!postInProgress)
        {
            bool minimumDurationExceeded;

            if (previousPostTimestamp == 0.0f)
                minimumDurationExceeded = true;
            else
            {
                float delta = Time.time - previousPostTimestamp;
                minimumDurationExceeded = (delta > minIntervalBetweenPosts);
            }

            if (minimumDurationExceeded)
            {
                UnityWebRequest request = null;

                lock (stackdriverEntries)
                {
                    if (stackdriverEntries.entries.Count > 0)
                    {
                        extractStackdriverEntries(stackdriverEntries, stackdriverEntriesInFlight, maxMessagesPerPost);
                        request = createPost(stackdriverEntriesInFlight, backendUrl);
                    }
                }

                if (request != null)
                {
                    previousPostTimestamp = Time.time;

                    postInProgress = true;
                    startCoroutine(PostRequest(request, PostRequestSucceeded, PostRequestFailed));
                }
            }
        }
    }
}
