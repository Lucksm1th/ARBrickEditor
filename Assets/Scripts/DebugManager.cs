using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance;

    public TextMeshProUGUI DebugText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        DebugText = gameObject.GetComponent<TextMeshProUGUI>();
    }
}
