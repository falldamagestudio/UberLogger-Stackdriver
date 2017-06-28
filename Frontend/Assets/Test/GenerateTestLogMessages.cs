using UnityEngine;
using UnityEngine.Assertions;

public class GenerateTestLogMessages : MonoBehaviour {

    private int updateCount = 0;

    private object nullReference = null;

	void Update()
    {
        updateCount++;

        if ((updateCount % 50) == 0)
            Debug.Log("Debug.Log: updateCount == " + updateCount);

        if ((updateCount % 73) == 12)
            Debug.LogWarning("Debug.LogWarning: updateCount == " + updateCount);

        if ((updateCount % 73) == 27)
            Debug.LogError("Debug.LogError: updateCount == " + updateCount);

        if ((updateCount % 73) == 29)
            Assert.IsTrue(false, "Assert.True failed: updateCount == " + updateCount);

        if ((updateCount % 73) == 33)
            throw new System.Exception("thrown System.Exception: updateCount == " + updateCount);

        if ((updateCount % 73) == 37)
            nullReference.GetType();    // Will throw null reference exception
    }
}
