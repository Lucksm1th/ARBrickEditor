using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;
using TMPro;

public class TrackImagesInfo : MonoBehaviour
{
    public Camera worldSpaceCanvasCamera;
    public GameObject levelMesh;
    public GameObject objParent;
    public Vector3 imagePosition;
    public TextMeshProUGUI debugText;

    ARTrackedImageManager trackedImageManager;

    public static TrackImagesInfo Instance;
    public Transform imageTrans;
    public TrackingState imageTrackingState; 

    private bool first = false;
    private Vector3[] fourCorners = new Vector3[4];
    private Quaternion[] fourRotations = new Quaternion[4];

    Texture2D m_DefaultTexture;
    public Texture2D defaultTexture
    {
        get { return m_DefaultTexture; }
        set { m_DefaultTexture = value; }
    }


    public float scale = 1;

    [SerializeField]
    [Tooltip("A transform which should be made to appear to be at the touch point.")]
    Transform m_Content;

    public GameObject playableLevel;
    private Texture2D candidateImage;

    /// <summary>
    /// A transform which should be made to appear to be at the touch point.
    /// </summary>
    public Transform content
    {
        get { return m_Content; }
        set { m_Content = value; }
    }

    [SerializeField]
    [Tooltip("The rotation the content should appear to have.")]
    Quaternion m_Rotation;

    public bool FoundLEGO = false;
    private bool startTimer = false;
    private float timer = 0;
    private string[] imageNames = { "first", "second", "third", "fourth" };
    private int imageCount = 0;
    public Quaternion rotation
    {
        get { return m_Rotation; }
        set
        {
            m_Rotation = value;
            if (m_SessionOrigin != null)
                m_SessionOrigin.MakeContentAppearAt(content, content.transform.position, m_Rotation);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (Unity.LEGO.Game.GameFlowManager.Instance.State == GameState.DetectImage)
            {
                Unity.LEGO.Game.GameFlowManager.Instance.UpdateGameState(GameState.SetupLevel);
            }
        }
        
    }

    private void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
        m_SessionOrigin = GetComponent<ARSessionOrigin>();
        for (int i = 0; i < fourCorners.Length; i++)
        {
            fourCorners[i] = Vector3.zero;
        }

        if (Instance == null) Instance = this;

    }

    void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    public void AddImage(Texture2D imageToAdd)
    {
        candidateImage = imageToAdd;
        
    }

    public void ActuallyAddImage()
    {
        DebugManager.Instance.DebugText.text = "GOT HERE";
        if (trackedImageManager.referenceLibrary is MutableRuntimeReferenceImageLibrary mutableLibrary)
        {
            DebugManager.Instance.DebugText.text = "ADDED IMAGE";
            mutableLibrary.ScheduleAddImageWithValidationJob(
                candidateImage,
                "LEGOPLADE_" + imageCount,
                0.11f /* 50 cm */);
            imageCount++;
        }
        else
        {
            DebugManager.Instance.DebugText.text = "COULD NOT ADD IMAGE";
        }
        FoundLEGO = true;
        Unity.LEGO.Game.GameFlowManager.Instance.UpdateGameState(GameState.DetectImage);
    }

    public void TakeNewPhoto()
    {
        imageCount++;
    }

    public void FoundLego()
    {
        ActuallyAddImage();
        
    }

    void UpdateInfo(ARTrackedImage trackedImage)
    {
        if (!FoundLEGO)
            return;

        imageTrackingState = trackedImage.trackingState;

        // Disable the visual plane if it is not being tracked
        if (trackedImage.trackingState == TrackingState.Tracking)
        {

            imagePosition = trackedImage.transform.position;
            if (m_SessionOrigin.transform.localScale != Vector3.one * scale)
                m_SessionOrigin.transform.localScale = Vector3.one * scale;

            // The image extents is only valid when the image is being tracked
            trackedImage.transform.localScale = new Vector3(trackedImage.size.x, 1f, trackedImage.size.y);
            
            int foundFourCorners = 0;


            if (foundFourCorners == 0)
            {
                Vector3 center = trackedImage.transform.position;
                Quaternion newRot = trackedImage.transform.rotation;

                imageTrans = trackedImage.transform;

                if (Unity.LEGO.Game.GameFlowManager.Instance.State == GameState.DetectImage)
                {
                    Unity.LEGO.Game.GameFlowManager.Instance.UpdateGameState(GameState.SetupLevel);
                }

                if (!first)
                {
                    m_SessionOrigin.MakeContentAppearAt(content, center);

                }
            }
        }
        else
        {
            //Unity.LEGO.Minifig.MinifigController.Instance.SetInputEnabled(false);
            //levelMesh.SetActive(false);
        }
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
        {
            // Give the initial image a reasonable default scale
            trackedImage.transform.localScale = new Vector3(0.01f, 1f, 0.01f);

            UpdateInfo(trackedImage);
        }

        foreach (var trackedImage in eventArgs.updated)
        {
            UpdateInfo(trackedImage);
            //DebugManager.Instance.DebugText.text = "Updating Image - " + Time.time;
        }
    }

    ARSessionOrigin m_SessionOrigin;
}
