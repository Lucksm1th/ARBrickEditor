using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using TMPro;

public class LevelGenerator : MonoBehaviour
{
    // Singleton stuff
    private static LevelGenerator _instance;

    public static LevelGenerator Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    // End singleton stuff
    public List<Color32> predefinedColors;

    public Texture2D paperScan;
    public LegoSpelunky legoSpelunky;
    private Texture2D outputTex;

    public int xSize, ySize;

    public RawImage originalImg;
    public RawImage croppedArea;
    public RawImage nearestColorImg;
    public RawImage smallNonNearColorImg;
    

    private Vector3[] vertices;

    public Mesh mesh;

    public TextMeshProUGUI text;


    private GCHandle pixelHandle;
    private IntPtr pixelPtr;

    private int imageSize = 14;


    private GCHandle outputHandle;
    private IntPtr outputPtr;

    [DllImport("OpenCVUnity")]
    private static extern float ProcessImage(IntPtr texData, int width, int height, IntPtr output);

    private void Start()
    {
        // Generate X by Y white texture
        outputTex = new Texture2D(imageSize, imageSize, TextureFormat.BGRA32, false);
        Color32 whiteCol = Color.white;
        Color32[] pixels = outputTex.GetPixels32();

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = whiteCol;
        }

        outputTex.SetPixels32(pixels);
        outputTex.Apply();
    }

    public Mesh GenerateMesh()
    {
		GetComponent<MeshFilter>().mesh = mesh = new Mesh();
		mesh.name = "Procedural Grid";

		vertices = new Vector3[(xSize + 1) * (ySize + 1)];
		for (int i = 0, y = 0; y <= ySize; y++)
		{
			for (int x = 0; x <= xSize; x++, i++)
			{
				vertices[i] = new Vector3(x, y);
			}
		}
		mesh.vertices = vertices;

		int[] triangles = new int[xSize * ySize * 6];
		for (int ti = 0, vi = 0, y = 0; y < ySize; y++, vi++)
		{
			for (int x = 0; x < xSize; x++, ti += 6, vi++)
			{
				triangles[ti] = vi;
				triangles[ti + 3] = triangles[ti + 2] = vi + 1;
				triangles[ti + 4] = triangles[ti + 1] = vi + xSize + 1;
				triangles[ti + 5] = vi + xSize + 2;
			}
		}
		mesh.triangles = triangles;

        return mesh;
	}

    private Texture2D GetSquareCenteredTexture(Texture2D sourceTexture)
    {
        int squareSize;
        int xPos = 0;
        int yPos = 0;
        if (sourceTexture.height < sourceTexture.width)
        {
            squareSize = sourceTexture.height;
            xPos = (sourceTexture.width - sourceTexture.height) / 2;
        }
        else
        {
            squareSize = sourceTexture.width;
            yPos = (sourceTexture.height - sourceTexture.width) / 2;
        }

        Color[] c = ((Texture2D)sourceTexture).GetPixels(xPos, yPos, squareSize, squareSize);
        Texture2D croppedTexture = new Texture2D(squareSize, squareSize);
        croppedTexture.SetPixels(c);
        croppedTexture.Apply();
        return croppedTexture;
    }

    public unsafe Texture2D TurnGray(Texture2D texture2D, int targetX, int targetY)
    {
        Texture2D squaredTexture = GetSquareCenteredTexture(texture2D);
        Color32[] rawTexture = squaredTexture.GetPixels32();
        Color32[] whiteTexture = outputTex.GetPixels32();
        List<int> colorIndices = new List<int>();

        pixelHandle = GCHandle.Alloc(rawTexture, GCHandleType.Pinned);
        pixelPtr = pixelHandle.AddrOfPinnedObject();

        outputHandle = GCHandle.Alloc(whiteTexture, GCHandleType.Pinned);
        outputPtr = outputHandle.AddrOfPinnedObject();

        float info;

        try
        {
            info = ProcessImage(pixelPtr, squaredTexture.width, squaredTexture.height, outputPtr);
        }
        finally
        {
            if (pixelHandle.IsAllocated)
            {
                pixelHandle.Free();
            }
            if (outputHandle.IsAllocated)
            {
                outputHandle.Free();
            }
        }

        Texture2D nonNearColor = new Texture2D(imageSize, imageSize, outputTex.format, false);
        nonNearColor.SetPixels32(whiteTexture);
        nonNearColor.filterMode = FilterMode.Point;
        nonNearColor.Apply();
        smallNonNearColorImg.texture = nonNearColor;

        for (int i = 0; i < whiteTexture.Length; i++)
        {
            whiteTexture[i] = NearestColor(whiteTexture[i]);
            colorIndices.Add(predefinedColors.IndexOf(whiteTexture[i]));
        }

        Texture2D result = new Texture2D(imageSize, imageSize, outputTex.format, false);
        result.SetPixels32(whiteTexture);
        result.filterMode = FilterMode.Point;
        result.Apply();

        Texture2D croppedResult = new Texture2D(squaredTexture.width, squaredTexture.height, texture2D.format, false);
        croppedResult.SetPixels32(rawTexture);
        croppedResult.Apply();
        croppedArea.texture = croppedResult;

        DebugManager.Instance.DebugText.text = "Adding Image";
        TrackImagesInfo.Instance.AddImage(croppedResult);
        legoSpelunky.textureColors = colorIndices;

        return result;
    }

    private Color32 NearestColor(Color32 input)
    {
        float difference = float.MaxValue;
        int colorIndex = 0;

        for (int i = 0; i < predefinedColors.Count; i++)
        {
           float candidateDiffence = Mathf.Sqrt(
                Mathf.Pow(input[0] - predefinedColors[i][0], 2) + 
                Mathf.Pow(input[1] - predefinedColors[i][1], 2) +
                Mathf.Pow(input[2] - predefinedColors[i][2], 2));

            if (candidateDiffence < difference)
            {
                difference = candidateDiffence;
                colorIndex = i;
            }
        }
        return predefinedColors[colorIndex];
    }

    public void GenerateTexture()
    {
        originalImg.texture = paperScan;
        Texture2D grayScan = TurnGray(paperScan, paperScan.width, paperScan.height);
        nearestColorImg.texture = grayScan;
        originalImg.texture = paperScan;
        legoSpelunky.legoTexture = grayScan;
        //wfc.Run();
    }
}
