using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateTestLogMessages : MonoBehaviour {

    // Use this for initialization
    void Start()
    {
        Debug.Log("test");
    }

    int x = 0;
    int y = 0;


    // Update is called once per frame
    void Update()
    {

        if (x > 10)
        {
            x = 0;
            y++;
            Debug.Log("test " + y);
        }
        x++;
    }

}
