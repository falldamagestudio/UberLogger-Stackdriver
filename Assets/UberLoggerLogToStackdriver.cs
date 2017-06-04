using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UberLoggerLogToStackdriver : MonoBehaviour {

    private UberLoggerStackdriver postToLog;

    public string BackendUrl;
    public int MaxMessagesPerPost = 10;
    public float MinIntervalBetweenPosts = 1.0f;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        postToLog = new UberLoggerStackdriver((coroutine) => StartCoroutine(coroutine), BackendUrl, MaxMessagesPerPost, MinIntervalBetweenPosts);
        UberLogger.Logger.AddLogger(postToLog);
    }

    void Update () {
        postToLog.PostMessagesIfAvailable();
	}
}
