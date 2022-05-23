using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NatveCode : MonoBehaviour
{
    public TextMeshProUGUI text;

    [DllImport("OpenCVUnity")]
    private static extern float FooPluginFunction();

    [DllImport("OpenCVUnity")]
    private static extern int GetNumber();


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        text.text = "LOL";
        text.text = "LOL : " + GetNumber();
    }
}
