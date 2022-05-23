using System.Collections;
using System.Collections.Generic;
using Unity.LEGO.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.LEGO.UI
{
    public class ObjectiveHUDManager : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Tooltip("The UI panel containing the layoutGroup for displaying objectives.")]
        RectTransform m_ObjectivePanel = default;

        [SerializeField, Tooltip("The prefab for winning objectives.")]
        GameObject m_WinObjectivePrefab = default;

        [SerializeField, Tooltip("The prefab for losing objectives.")]
        GameObject m_LoseObjectivePrefab = default;

        const int s_TopMargin = 20;
        const int s_Spacing = 60;
        float m_NextY;

        public List<GameObject> objectives = new List<GameObject>();
        public List<Sprite> stickers = new List<Sprite>();
        private int objectivesCompleted = 0;
        private int stickersAdded = 0;

        private GameState prevState;

        protected void Awake()
        {
            EventManager.AddListener<ObjectiveAdded>(OnObjectiveAdded);
            EventManager.AddListener<GameOverEvent>(OnGameOver);
            GameFlowManager.OnObjectiveProgress += GameManagerOnGameStateChanged;
        }

        private void GameManagerOnGameStateChanged(GameState state)
        {
            
            switch (state)
            {
                case GameState.DetectSurface:
                    break;
                case GameState.DetectImage:
                    StartCoroutine(NextObjective());
                    break;
                case GameState.SetupLevel:
                    StartCoroutine(NextObjective());
                    break;
                case GameState.PlayLevel:
                    break;
                case GameState.CollectedBricks:
                    StartCoroutine(NextObjective());
                    break;
                case GameState.Lose:
                    break;
                case GameState.Win:
                    break;
                default:
                    Debug.LogWarning("No canvas for this state: " + state.ToString());
                    break;
            }

            prevState = state;
        }

        private IEnumerator NextObjective()
        {
            if (objectivesCompleted == objectives.Count) 
            {
                GameFlowManager.Instance.PlaySound(1, -1);
                yield break;
            }

            if (prevState == GameState.ApproveImage)
                yield break;

            GameFlowManager.Instance.PlaySound(0, -1);

            yield return new WaitForSeconds(1.5f);

            // destroy completed obj
            GameObject obj = objectives[objectivesCompleted];

            
            objectives[objectivesCompleted].SetActive(false);

            objectivesCompleted++;

            //Move other objectives
            // Position the objective.

            objectives[objectivesCompleted].SetActive(true);
            var rectTransform = objectives[objectivesCompleted].GetComponent<RectTransform>();
            m_NextY = (m_ObjectivePanel.sizeDelta.y - s_Spacing);
            rectTransform.anchoredPosition = new Vector2(0, m_NextY - s_TopMargin);
        }

        void OnObjectiveAdded(ObjectiveAdded evt)
        {
            if (!evt.Objective.m_Hidden)
            {
                // Instantiate the UI element for the new objective.
                GameObject go = Instantiate(evt.Objective.m_Lose ? m_LoseObjectivePrefab : m_WinObjectivePrefab, m_ObjectivePanel.transform);

                // Setup Sticker
                Image[] children = go.GetComponentsInChildren<Image>();

                foreach (Image sticker in children)
                {
                    if (sticker.transform.name == "Sticker")
                    {
                        sticker.sprite = stickers[stickersAdded];
                    }
                }

                stickersAdded++;

                // Initialise the objective element.
                Objective objective = go.GetComponent<Objective>();
                objective.Initialize(evt.Objective.m_Title, evt.Objective.m_Description, evt.Objective.GetProgress());

                // Force layout rebuild to get height of objective UI.
                LayoutRebuilder.ForceRebuildLayoutImmediate(objective.GetComponent<RectTransform>());

                // Position the objective.
                var rectTransform = go.GetComponent<RectTransform>();
                m_NextY = (m_ObjectivePanel.sizeDelta.y - s_Spacing);
                rectTransform.anchoredPosition = new Vector2(0, m_NextY - s_TopMargin);
                m_NextY -= rectTransform.sizeDelta.y + s_Spacing;

                evt.Objective.OnProgress += objective.OnProgress;

                objectives.Add(go);

                for (int i = 0; i < objectives.Count; i++)
                {
                    if (i == 0)
                        continue;
                    else objectives[i].SetActive(false);
                }
            }
        }



        void OnGameOver(GameOverEvent evt)
        {
            EventManager.RemoveListener<ObjectiveAdded>(OnObjectiveAdded);
            EventManager.RemoveListener<GameOverEvent>(OnGameOver);
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<ObjectiveAdded>(OnObjectiveAdded);
            EventManager.RemoveListener<GameOverEvent>(OnGameOver);
        }
    }
}
