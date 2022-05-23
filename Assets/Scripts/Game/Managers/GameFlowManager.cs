using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.LEGO.Game
{
    // The Root component for the game.
    // It sets the game state and broadcasts events to notify the different systems of a game state change.

    public class GameFlowManager : MonoBehaviour
    {
        [Header("Win")]
        [SerializeField, Tooltip("The name of the scene you want to load when the game is won.")]
        string m_WinScene = "Menu Win";
        [SerializeField, Tooltip("The delay in seconds between the game is won and the win scene is loaded.")]
        float m_WinSceneDelay = 5.0f;

        [Header("Lose")]
        [SerializeField, Tooltip("The name of the scene you want to load when the game is lost.")]
        string m_LoseScene = "Menu Lose";
        [SerializeField, Tooltip("The delay in seconds between the game is lost and the lose scene is loaded.")]
        float m_LoseSceneDelay = 3.0f;

        [SerializeField, HideInInspector, Tooltip("The delay in seconds until we activate the controller look inputs.")]
        float m_StartGameLockedControllerTimer = 0.3f;

        public static string PreviousScene { get; private set; }

        public bool GameIsEnding { get; private set; }

        public static GameFlowManager Instance;
        public GameObject CurrentRoom;
        public bool isPlacedAtSpawn = false;

        public GameState State { get; private set; }

        float m_GameOverSceneTime;
        string m_GameOverSceneToLoad;
        public static event Action<GameState> OnGameStateChanged;
        public static event Action<GameState> OnObjectiveProgress;
        public Transform levelOrigin;
        public List<AudioClip> Sounds = new List<AudioClip>();

        // m_FreeLookCamera;

        string m_ControllerAxisXName;
        string m_ControllerAxisYName;

        void Awake()
        {
            /*if (Instance == null) */Instance = this;
            EventManager.AddListener<GameOverEvent>(OnGameOver);
            Application.targetFrameRate = 60;

            //m_FreeLookCamera = FindObjectOfType<CinemachineFreeLook>();
#if !UNITY_EDITOR
            //Cursor.lockState = CursorLockMode.Locked;
#endif

        }

        void Start()
        {
            VariableManager.Reset();
            UpdateGameState(GameState.DetectSurface);
        }

        public void UpdateGameState(GameState newState)
        {
            State = newState;

            switch (newState)
            {
                case GameState.DetectSurface:
                    OnObjectiveProgress?.Invoke(newState);
                    break;
                case GameState.DetectImage:
                    OnObjectiveProgress?.Invoke(newState);
                    break;
                case GameState.ApproveImage:
                    OnObjectiveProgress?.Invoke(newState);
                    break;
                case GameState.SetupLevel:
                    OnObjectiveProgress?.Invoke(newState);
                    break;
                case GameState.SetupPlayer:
                    OnObjectiveProgress?.Invoke(newState);
                    break;
                case GameState.PlayLevel:
                    OnObjectiveProgress?.Invoke(newState);
                    break;
                case GameState.CollectedBricks:
                    OnObjectiveProgress?.Invoke(newState);
                    break;
                case GameState.Lose:
                    break;
                case GameState.Win:
                    OnObjectiveProgress?.Invoke(newState);
                    PlaySound(1, -1);
                    Time.timeScale = 0;
                    break;
                default:
                    Debug.LogError("Invalid state");
                    break;
            }

            OnGameStateChanged?.Invoke(newState);
        }       

        public void PlaySound(int index, float time)
        {
            StartCoroutine(StartPlaying(index, time));
        }

        private IEnumerator StartPlaying(int sound, float time)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            audioSource.PlayOneShot(Sounds[sound]);

            if (time > 0)
            {
                yield return new WaitForSeconds(time);
                audioSource.Stop();

            }

        }

        IEnumerator StartGameLockLookRotation()
        {
            while (m_StartGameLockedControllerTimer > 0.0f)
            {
                m_StartGameLockedControllerTimer -= Time.deltaTime;
                if (m_StartGameLockedControllerTimer < 0.0f)
                {
                    //if (m_FreeLookCamera)
                    //{
                    //    m_FreeLookCamera.m_XAxis.m_InputAxisName = m_ControllerAxisXName;
                    //    m_FreeLookCamera.m_YAxis.m_InputAxisName = m_ControllerAxisYName;
                    //}
                }
                yield return new WaitForEndOfFrame();
            }
        }

        void Update()
        {
            if (GameIsEnding)
            {
                if (Time.time >= m_GameOverSceneTime)
                {
#if !UNITY_EDITOR
            Cursor.lockState = CursorLockMode.None;
#endif
                    //PreviousScene = SceneManager.GetActiveScene().name;
                    //SceneManager.LoadScene(m_GameOverSceneToLoad);
                }
            }
        }

        void OnGameOver(GameOverEvent evt)
        {
            if (!GameIsEnding)
            {
                GameIsEnding = true;

                // Remember the scene to load and handle the camera accordingly.
                if (evt.Win)
                {
                    UpdateGameState(GameState.Win);
                }
                else
                {
                    ResetPlayer();
                }
            }
        }

        public void ResetPlayer()
        {
            UpdateGameState(GameState.SetupPlayer);
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<GameOverEvent>(OnGameOver);
        }

    }
}

public enum GameState
{
    DetectSurface,
    DetectImage,
    ApproveImage,
    SetupLevel,
    SetupPlayer,
    PlayLevel,
    CollectedBricks,
    Lose,
    Win
}
