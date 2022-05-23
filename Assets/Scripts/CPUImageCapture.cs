using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


public class CPUImageCapture : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The ARCameraManager which will produce frame events.")]
    ARCameraManager m_CameraManager;


    /// <summary>
    /// Get or set the <c>ARCameraManager</c>.
    /// </summary>
    public ARCameraManager cameraManager
    {
        get => m_CameraManager;
        set => m_CameraManager = value;
    }

    Texture2D m_CameraTexture;

    public unsafe bool SnapShot()
    {
        if (!m_CameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            return false;

        // Consider each image plane.
        for (int planeIndex = 0; planeIndex < image.planeCount; ++planeIndex)
        {
            // Log information about the image plane.
            var plane = image.GetPlane(planeIndex); 
            Debug.LogFormat("Plane {0}:\n\tsize: {1}\n\trowStride: {2}\n\tpixelStride: {3}",
                planeIndex, plane.data.Length, plane.rowStride, plane.pixelStride);

            // Do something with the data.
            //MyComputerVisionAlgorithm(plane.data);
        }

        // Dispose the XRCpuImage to avoid resource leaks.
        var format = TextureFormat.BGRA32;

        if (m_CameraTexture == null || m_CameraTexture.width != image.width || m_CameraTexture.height != image.height)
        {
            m_CameraTexture = new Texture2D(image.width, image.height, format, false);
        }

        // Convert the image to format, flipping the image across the Y axis.
        // We can also get a sub rectangle, but we'll get the full image here.
        var conversionParams = new XRCpuImage.ConversionParams(image, format, XRCpuImage.Transformation.MirrorY);

        // Texture2D allows us write directly to the raw texture data
        // This allows us to do the conversion in-place without making any copies.
        var rawTextureData = m_CameraTexture.GetRawTextureData<byte>();
        try
        {
            image.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
        }
        finally
        {
            // We must dispose of the XRCpuImage after we're finished
            // with it to avoid leaking native resources.
            image.Dispose();
        }

        // Apply the updated texture data to our texture
        m_CameraTexture.Apply();

        // Set the RawImage's texture so we can visualize it.
        LevelGenerator.Instance.paperScan = m_CameraTexture;
        LevelGenerator.Instance.GenerateTexture();
        Unity.LEGO.Game.GameFlowManager.Instance.UpdateGameState(GameState.ApproveImage);
        return true;
    }

    public void TakeSnapshot()
    {
        SnapShot();
    }
}
