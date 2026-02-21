using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphicsInfo : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Graphics API: " + SystemInfo.graphicsDeviceType);
        Debug.Log("GPU: " + SystemInfo.graphicsDeviceName);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
