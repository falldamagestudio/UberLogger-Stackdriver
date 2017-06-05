using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UberLoggerLogToStackdriver : MonoBehaviour {

    private UberLoggerStackdriver postToLog;

    public string BackendUrl;
    public int MaxMessagesPerPost = 10;
    public float MinIntervalBetweenPosts = 1.0f;

    public UberLoggerStackdriver.LogSeverityLevel EditorPlayModeLogLevel;
    public UberLoggerStackdriver.LogSeverityLevel DevelopmentBuildLogLevel;
    public UberLoggerStackdriver.LogSeverityLevel ReleaseBuildLogLevel;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

#if UNITY_STANDALONE
#if DEVELOPMENT_BUILD 
        UberLoggerStackdriver.LogSeverityLevel logSeverityLevel = DevelopmentBuildLogLevel;
#else
        UberLoggerStackdriver.LogSeverityLevel logSeverityLevel = ReleaseBuildLogLevel;
#endif
#else
        UberLoggerStackdriver.LogSeverityLevel logSeverityLevel = EditorPlayModeLogLevel;
#endif

        postToLog = new UberLoggerStackdriver((coroutine) => StartCoroutine(coroutine), BackendUrl, MaxMessagesPerPost, MinIntervalBetweenPosts, logSeverityLevel);
        UberLogger.Logger.AddLogger(postToLog);
    }

    void Update () {
        postToLog.PostMessagesIfAvailable();
	}
}
