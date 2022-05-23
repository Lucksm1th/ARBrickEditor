using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using UnityEngine.Networking;

public class AssetBundleManager : MonoBehaviour
{
    public static AssetBundleManager Instance;
    public AssetBundle levelComponents;
    public bool asset_bundles_loaded = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(load_asset_bundles());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator load_asset_bundles()
    {
        // Reuse this line to load different asset bundles of different names
        StartCoroutine(load_sub_asset_bundle("levelComponents"));

        // The delay here has been added to make sure it will load.
        // Im looking into a solution to make it as quick as possible
        // but this is another problem and is not required here
        // 

        yield return new WaitForSeconds(2F);
        asset_bundles_loaded = true;
        yield return true;
    }

    private IEnumerator load_sub_asset_bundle(string bundle_name)
    {
        // This coroutine loads a single asset bundle at a time
        string uri;
        string path_to_use;

#if UNITY_ANDROID && !UNITY_EDITOR
             // This is the path to require an asset bundle in Assets/StreamingAssets on Android
             path_to_use = Path.Combine("jar:file://" + Application.dataPath + "!assets/", bundle_name);
             uri = path_to_use;
#else
        // This is the same path but for your computer to recognize
        path_to_use = Application.dataPath;
        uri = path_to_use + "/" + bundle_name;
#endif

        // Ask for the bundle
        UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(uri, 0);

        request.SendWebRequest();
        switch (bundle_name)
        {
            case "levelComponents":
                // Get the bundle data and store it in the AssetBundle variable created at the begining.
                levelComponents = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "levelComponents"));
                print("BUNDLE FOUND");
                break;
            default:
                print("BUNDLE  _NOT_ FOUND");
                break;
        }
        // Delay for now is just to make sure it loads properly before its use.
        yield return new WaitForSeconds(1F);
    }
}
