using System;
using System.Collections;
using System.Collections.Generic;
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
    }

    public Config EditorPlayModeConfig;
    public Config DevelopmentBuildConfig;
    public Config ReleaseBuildConfig;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

#if UNITY_STANDALONE
#if DEVELOPMENT_BUILD 
        Config config = DevelopmentBuildConfig;
#else
        Config config = ReleaseBuildConfig;
#endif
#else
        Config config = EditorPlayModeConfig;
#endif

        postToLog = new UberLoggerStackdriver((coroutine) => StartCoroutine(coroutine), config.BackendUrl, config.MaxMessagesPerPost, config.MinIntervalBetweenPosts, config.LogSeverityLevel);
        UberLogger.Logger.AddLogger(postToLog);
    }

    void Update () {
        postToLog.PostMessagesIfAvailable();
	}
}
