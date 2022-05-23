using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;
using Unity.LEGO.Game;

public class PlaneHider : MonoBehaviour
{
    ARPlaneManager planeManager;
    // Start is called before the first frame update
    private bool hidePlane = false;

    void Start()
    {
        planeManager = gameObject.GetComponent<ARPlaneManager>();
    }

    private void Awake()
    {
        GameFlowManager.OnGameStateChanged += GameManagerOnGameStateChanged;

    }

    private void GameManagerOnGameStateChanged(GameState state)
    {

        switch (state)
        {
            case GameState.DetectSurface:
                break;
            case GameState.DetectImage:
                hidePlanes();
                break;
            case GameState.PlayLevel:
                break;
            case GameState.Lose:
                break;
            case GameState.Win:
                break;
            default:
                Debug.LogWarning("No canvas for this state: " + state.ToString());
                break;
        }
    }

    private void hidePlanes()
    {
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(false);
        }

        planeManager.enabled = false;
        GetComponent<ARRaycastManager>().enabled = false;
    }

    void OnDestroy()
    {
        GameFlowManager.OnGameStateChanged -= GameManagerOnGameStateChanged;
    }
}
