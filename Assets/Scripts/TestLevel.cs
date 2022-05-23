using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestLevel : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject level;

    private void Awake()
    {
        Unity.LEGO.Game.GameFlowManager.OnGameStateChanged += GameManagerOnGameStateChanged;
    }

    private void GameManagerOnGameStateChanged(GameState state)
    {
        if (state == GameState.SetupLevel)
        {
            ActivateLevel();
            Unity.LEGO.Game.GameFlowManager.Instance.UpdateGameState(GameState.SetupPlayer);
        }
    }

    void OnDestroy()
    {
        Unity.LEGO.Game.GameFlowManager.OnGameStateChanged -= GameManagerOnGameStateChanged;
    }

    public void ActivateLevel()
    {
        //gameObject.SetActive(true);
        GameObject con = GameObject.Find("Content");
        Instantiate(level, con.transform);
    }
}
