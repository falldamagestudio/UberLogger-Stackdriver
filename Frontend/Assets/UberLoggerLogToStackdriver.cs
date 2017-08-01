using System;
using UnityEngine;

public class UberLoggerLogToStackdriver : MonoBehaviour {

    private UberLoggerStackdriver postToLog;

    [Serializable]
    public class Config
    {
        public string BackendUrl;
        public int MaxMessagesPerPost = 10;
        public float MinIntervalBetweenPosts = 1.0f;
        public UberLoggerStackdriver.LogSeverityLevel LogSeverityLevel = UberLoggerStackdriver.LogSeverityLevel.AllMessages;
        public UberLoggerStackdriver.IncludeCallstackMode IncludeCallstacks = UberLoggerStackdriver.IncludeCallstackMode.ErrorsOnly;
        public int MaxRetries = 3;
    }

    public Config EditorPlayModeConfig;
    public Config DevelopmentBuildConfig;
    public Config ReleaseBuildConfig;

    /// <summary>
    /// Generate a unique identifier for this session. Log files will be searchable/indexable by the session ID.
    /// </summary>
    private string GenerateSessionId()
    {
        return System.Guid.NewGuid().ToString();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
        Config config = EditorPlayModeConfig;
#else
#if DEVELOPMENT_BUILD
        Config config = DevelopmentBuildConfig;
#else
        Config config = ReleaseBuildConfig;
#endif
#endif

        string sessionId = GenerateSessionId();

        postToLog = new UberLoggerStackdriver((coroutine) => StartCoroutine(coroutine), config.BackendUrl, config.MaxMessagesPerPost, config.MinIntervalBetweenPosts, config.LogSeverityLevel, config.IncludeCallstacks, config.MaxRetries, sessionId);
        UberLogger.Logger.AddLogger(postToLog);
    }

    void Update () {
        postToLog.PostMessagesIfAvailable();
	}
}
