using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.LEGO.Game;

public class CanvasManager : MonoBehaviour
{
    [SerializeField] private GameObject gameHUD;
    [SerializeField] private GameObject victoryMenu;
    [SerializeField] private GameObject detectSurfaceMenu;
    [SerializeField] private GameObject captureImageMenu;
    [SerializeField] private GameObject approveImageMenu;
    [SerializeField] private GameObject objectives;
    [SerializeField] private GameObject variables;

    private void Awake()
    {
        EventManager.AddListener<GameOverEvent>(OnGameOver);
        GameFlowManager.OnGameStateChanged += GameManagerOnGameStateChanged;

        ResetMenus();
    }

    private void GameManagerOnGameStateChanged(GameState state)
    {
        ResetMenus();

        switch (state)
        {
            case GameState.DetectSurface:
                detectSurfaceMenu.SetActive(true);
                break;
            case GameState.DetectImage:
                captureImageMenu.SetActive(true);
                break;
            case GameState.ApproveImage:
                approveImageMenu.SetActive(true);
                break;
            case GameState.PlayLevel:
                gameHUD.SetActive(true);
                break;
            case GameState.CollectedBricks:
                gameHUD.SetActive(true);
                break;
            case GameState.Lose:
                victoryMenu.SetActive(true);
                break;
            case GameState.Win:
                objectives.SetActive(false);
                variables.SetActive(false);
                victoryMenu.SetActive(true);
                break;
            default:
                Debug.LogWarning("No canvas for this state: " + state.ToString());
                break;
        }
    }

    public void ScrapImage()
    {
        GameFlowManager.Instance.UpdateGameState(GameState.DetectImage);
    }

    private void ResetMenus()
    {
        // Set all to false
        gameHUD.SetActive(false);
        victoryMenu.SetActive(false);
        detectSurfaceMenu.SetActive(false);
        captureImageMenu.SetActive(false);
        approveImageMenu.SetActive(false);
    }

    public void Retry()
    {
        StartCoroutine(Reload());
    }

    private IEnumerator Reload()
    {
        SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());

        yield return new WaitForSeconds(2);
        SceneManager.LoadScene(0);
    }

    public void PlayerJumpButton()
    {
        Unity.LEGO.Minifig.MinifigController.Instance.Jump();
    }

    void OnDestroy()
    {
        EventManager.RemoveListener<GameOverEvent>(OnGameOver);
        GameFlowManager.OnGameStateChanged -= GameManagerOnGameStateChanged;
    }

    private void OnGameOver(GameOverEvent evt)
    {
        gameHUD.SetActive(false);
        victoryMenu.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

}
