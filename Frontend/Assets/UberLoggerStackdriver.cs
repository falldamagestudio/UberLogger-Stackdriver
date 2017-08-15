using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    /// Which types of log messages shall include full callstacks
    /// </summary>
    public enum IncludeCallstackMode
    {
        Always,
        WarningsAndErrorsOnly,
        ErrorsOnly,
        Never
    }

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

    private readonly IncludeCallstackMode includeCallstacks;

    private readonly int maxRetries;

    private readonly string sessionId;

    public UberLoggerStackdriver(StartCoroutineDelegate startCoroutine, string backendUrl, int maxMessagesPerPost, float minIntervalBetweenPosts, LogSeverityLevel logSeverityLevel, IncludeCallstackMode includeCallstacks, int maxRetries, string sessionId)
    {
        this.startCoroutine = startCoroutine;
        this.backendUrl = backendUrl;
        this.maxMessagesPerPost = maxMessagesPerPost;
        this.minIntervalBetweenPosts = minIntervalBetweenPosts;
        this.logSeverityLevel = logSeverityLevel;
        this.includeCallstacks = includeCallstacks;
        this.maxRetries = maxRetries;
        this.sessionId = sessionId;

        stackdriverEntries = new StackdriverEntries(sessionId);
        stackdriverEntriesInFlight = new StackdriverEntries(sessionId);

        Assert.IsNotNull(this.backendUrl, "You must supply a target URL for the UberLoggerStackdriver backend API. UberLoggerStackdriver will be inactive.");
    }

    /// <summary>
    /// All jsonable objects in this project will derive from this interface
    /// </summary>
    public interface ToJson
    {
        /// <summary>
        /// Converts the object to a json string representation
        /// </summary>
        string ToJson();
    }

    /// <summary>
    /// Convert a list of jsonable objects into a JSON string on the format of "[ object1, object2, object3, ..., objectN ]"
    /// </summary>
    public static string ListToJson<T>(List<T> items) where T : ToJson
    {
        string str = "[ ";

        for (int i = 0; i < items.Count; i++)
        {
            str += items[i].ToJson();
            if (i + 1 != items.Count)
                str += ", ";
        }

        str += " ]";
        return str;
    }

    /// <summary>
    /// Convert a string to JSON-compatible format (escape critical characters with leading backslashes)
    /// </summary>
    public static string StringToJson(string inputString)
    {
        string str = "";
        foreach (char c in inputString)
            if (c == '\\')
                str += "\\\\";
            else if (c == '"')
                str += "\"";
            else if ((int)c < 0x20)
                str += "\\" + c;
            else
                str += c;

        return str;
    }

    public class StackdriverEntries : ToJson
    {
        public string logName;
        public List<StackdriverEntry> entries = new List<StackdriverEntry>();

        public StackdriverEntries(string logName)
        {
            this.logName = logName;
        }

        public string ToJson()
        {
            return string.Format("{{ \"logName\": \"{0}\", \"entries\": {1} }}", StringToJson(logName), ListToJson(entries));
        }
    }

    public class StackdriverEntry : ToJson
    {
        public string sessionId;
        public string message;
        public int severity;
        public StackdriverSourceLocation sourceLocation;
        public List<StackdriverSourceLocation> callStack;

        public StackdriverEntry(string sessionId, string message, int severity, StackdriverSourceLocation sourceLocation, List<StackdriverSourceLocation> callStack)
        {
            this.sessionId = sessionId;
            this.message = message;
            this.severity = severity;
            this.sourceLocation = sourceLocation;
            this.callStack = callStack;
        }

        public string ToJson()
        {
            return string.Format("{{ \"sessionId\": \"{0}\", \"message\": \"{1}\", \"severity\": {2}, \"sourceLocation\": {3}, \"callStack\": {4} }}",
                StringToJson(sessionId), StringToJson(message), severity, (sourceLocation != null) ? sourceLocation.ToJson() : "null", (callStack != null) ? ListToJson(callStack) : "null");
        }
    }

    public class StackdriverSourceLocation : ToJson
    {
        public readonly string file;
        public readonly string line;
        public readonly string function;

        public StackdriverSourceLocation(LogStackFrame logStackFrame)
        {
            file = logStackFrame.FileName;
            line = logStackFrame.LineNumber.ToString();
            function = string.Format("{0}.{1}({2})", logStackFrame.DeclaringType, logStackFrame.MethodName, logStackFrame.ParameterSig);
        }

        public string ToJson()
        {
            return string.Format("{{ \"file\": \"{0}\", \"line\": \"{1}\", \"function\": \"{2}\" }}", StringToJson(file), StringToJson(line), StringToJson(function));
        }
    }

    private StackdriverEntries stackdriverEntries;
    private StackdriverEntries stackdriverEntriesInFlight;

    private float previousPostTimestamp = 0.0f;
    private bool postInProgress;

    private int retryCounter;

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

    private List<StackdriverSourceLocation> LogCallstackToStackdriverCallstack(List<LogStackFrame> logCallstack)
    {
        return logCallstack.Select(logStackFrame => new StackdriverSourceLocation(logStackFrame)).ToList();
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

        bool includeCallstack;

        switch (logInfo.Severity)
        {
            case LogSeverity.Message:
                includeCallstack = (includeCallstacks == IncludeCallstackMode.Always); break;
            case LogSeverity.Warning:
                includeCallstack = (includeCallstacks == IncludeCallstackMode.Always || includeCallstacks == IncludeCallstackMode.WarningsAndErrorsOnly); break;
            case LogSeverity.Error:
                includeCallstack = (includeCallstacks == IncludeCallstackMode.Always || includeCallstacks == IncludeCallstackMode.WarningsAndErrorsOnly || includeCallstacks == IncludeCallstackMode.ErrorsOnly); break;
            default:
                throw new NotImplementedException();
        }

        lock (stackdriverEntries)
        {
            stackdriverEntries.entries.Add(new StackdriverEntry(sessionId, logInfo.Message, LogSeverityToStackdriverSeverity(logInfo.Severity), (logInfo.Callstack.Count > 0 ? new StackdriverSourceLocation(logInfo.Callstack[0]) : null), includeCallstack ? LogCallstackToStackdriverCallstack(logInfo.Callstack) : null));
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
        string jsonMessage = stackdriverEntries.ToJson();
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
#if UNITY_2017_1_OR_NEWER
		bool requestNetworkError = request.isNetworkError;
#else
		bool requestNetworkError = request.isError;
#endif
        if (!requestNetworkError && (request.responseCode >= 200 && request.responseCode < 300))
            success(request);
        else
            failure(request);
    }

    private void PostRequestSucceeded(UnityWebRequest request)
    {
        retryCounter = 0;

        // In-flight messages have been successfully transmitted. We no longer need to keep them around on the client.
        stackdriverEntriesInFlight.entries.Clear();

        postInProgress = false;
    }

    private void PostRequestFailed(UnityWebRequest request)
    {
        if (retryCounter < maxRetries)
        {
            retryCounter++;
            // In-flight messages failed to get transmitted. Put them back at the beginning of the list
            stackdriverEntries.entries.InsertRange(0, stackdriverEntriesInFlight.entries);
        }

        stackdriverEntriesInFlight.entries.Clear();

        postInProgress = false;

#if UNITY_2017_1_OR_NEWER
		bool requestNetworkError = request.isNetworkError;
#else
		bool requestNetworkError = request.isError;
#endif
        if (requestNetworkError)
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
