using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UberLoggerLogToStackdriver : MonoBehaviour {

    private UberLoggerStackdriver postToLog;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        postToLog = new UberLoggerStackdriver((coroutine) => StartCoroutine(coroutine));
        UberLogger.Logger.AddLogger(postToLog);
    }

    void Update () {
        postToLog.PostMessagesIfAvailable();
	}
}
