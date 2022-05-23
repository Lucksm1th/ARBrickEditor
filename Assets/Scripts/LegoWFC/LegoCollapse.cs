using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEditor;
using System.Xml.Linq;
using TMPro;
using Unity.LEGO.Minifig;


// Permission to use for my thesis was granted by Alberto Giudice & Fabio Scita
public class LegoCollapse : MonoBehaviour
{
    private LegoSpelunky spelunky;
    [SerializeField] private TextAsset set = null;
    [SerializeField] private string subset = "";
    [SerializeField] private bool periodic = false;
    [SerializeField] private int attempts = 10;
    [SerializeField] private int limit = 0;
    public Transform player;
    public Transform coin;
    LegoTiledModelWFC model;
    [SerializeField] private bool enforceBoundaries = true;
    //[SerializeField] private bool openEntryAndExit = true;
    public TextMeshProUGUI tmp;
    public Transform spawnPosition;
    public GameObject Fog;

    public List<GameObject> tilePrefabs = new List<GameObject>();
    private Dictionary<string, int> tileIndexes = new Dictionary<string, int>();
    public int MaxPoints = 0;

    public static LegoCollapse Instance;



    private List<string> tilesWithDoorLeft = new List<string> { "room_I 1", "room_U 3", "room_L 0", "room_L 3", "room_T 0", "room_T 1", "room_T 2", "room_X 0" };
    private List<string> tilesWithDoorRight = new List<string> { "room_I 1", "room_U 1", "room_L 1", "room_L 2", "room_T 0", "room_T 2", "room_T 3", "room_X 0" };
    private List<string> tilesWithDoorBack = new List<string> { "room_I 0", "room_U 2", "room_L 2", "room_L 3", "room_T 0", "room_T 1", "room_T 3", "room_X 0" };
    private List<string> tilesWithDoorForward = new List<string> { "room_I 0", "room_U 0", "room_L 0", "room_L 1", "room_T 1", "room_T 2", "room_T 3", "room_X 0" };
    private List<string> tilesWithoutDoorLeft = new List<string> { "room_I 0", "room_U 0", "room_U 1", "room_U 2", "room_L 1", "room_L 2", "room_T 3", "room_O 0" };
    private List<string> tilesWithoutDoorRight = new List<string> { "room_I 0", "room_U 0", "room_U 2", "room_U 3", "room_L 0", "room_L 3", "room_T 1", "room_O 0" };
    private List<string> tilesWithoutDoorBack = new List<string> { "room_I 1", "room_U 0", "room_U 1", "room_U 3", "room_L 0", "room_L 1", "room_T 2", "room_O 0" };
    private List<string> tilesWithoutDoorForward = new List<string> { "room_I 1", "room_U 1", "room_U 2", "room_U 3", "room_L 2", "room_L 3", "room_T 0", "room_O 0" };
    private string UForward = "room_U 0";
    private string Uright = "room_U 1";
    private string ULeft = "room_U 3";
    private string UBack = "room_U 2";

    private Vector3 playerYOffset = new Vector3(0, 2.5f, 0);

    AssetBundle levelComponents;
    bool asset_bundles_loaded = false;

    public void Reset()
    {
        tileIndexes.Clear();
    }

    private void Awake()
    {
        Unity.LEGO.Game.GameFlowManager.OnGameStateChanged += GameManagerOnGameStateChanged;
    }

    private void Start()
    {
        if (Instance == null) Instance = this;
        Fog.SetActive(false);
    }

    private void GameManagerOnGameStateChanged(GameState state)
    {
        if (state == GameState.SetupPlayer)
        {
            ResetExplorePosition();
            Explore();
            DebugManager.Instance.DebugText.text = "changing to playlevel";
            Unity.LEGO.Game.GameFlowManager.Instance.UpdateGameState(GameState.PlayLevel);
        }
        else if (state == GameState.SetupLevel)
        {
            Run();
            Unity.LEGO.Game.GameFlowManager.Instance.UpdateGameState(GameState.SetupPlayer);
        }

    }

    void OnDestroy()
    {
        Unity.LEGO.Game.GameFlowManager.OnGameStateChanged -= GameManagerOnGameStateChanged;
    }

    public void Run()
    {
        Reset();
        GetComponent<LegoSpelunky>().Run();

        for (int i = 0; i < tilePrefabs.Count; i++)
        {
            tileIndexes.Add(tilePrefabs[i].name, i);
        }

        System.Random random = new System.Random();

        int children = transform.childCount;
        for (int i = children - 1; i > 0; i--)
        {
            GameObject.DestroyImmediate(transform.GetChild(i).gameObject);
        }

        XElement xroot = XDocument.Parse(set.text).Root;
        LegoSpelunky spelunky = GetComponent<LegoSpelunky>();
        model = new LegoTiledModelWFC(
            xroot, subset, spelunky.spaceSize.x, spelunky.spaceSize.y, periodic, transform, tilePrefabs, tileIndexes);
        #region DeterminePath

        for (int i = 0; i < spelunky.pathTiles.Count; i++)
        {
            // go through pathTiles, check where the two doors must be, ban anything incompatible
            Vector2Int curr = spelunky.pathTiles[i];
            Direction? prevDoor = null;
            if (i > 0)
            {
                Vector2Int prev = spelunky.pathTiles[i - 1];
                if (prev.x < curr.x)
                {
                    prevDoor = Direction.left;
                }
                else if (prev.x > curr.x)
                {
                    prevDoor = Direction.right;
                }
                else if (prev.y < curr.y)
                {
                    prevDoor = Direction.back;
                }
                else
                {
                    prevDoor = Direction.forward; // Add compatibility for going back
                }
            }
            Direction? nextDoor = null;
            if (i < spelunky.pathTiles.Count - 1)
            {
                Vector2Int next = spelunky.pathTiles[i + 1];
                if (next.x < curr.x)
                {
                    nextDoor = Direction.left;
                }
                else if (next.x > curr.x)
                {
                    nextDoor = Direction.right;
                }
                else if (next.y > curr.y)
                {
                    nextDoor = Direction.forward;
                }
                else
                {
                    nextDoor = Direction.back;
                }
            }
            // Enforce entry to be U shaped
            //if (i == 0)
            //{
            //    switch (nextDoor)
            //    {
            //        case Direction.forward:
            //            int tileIndexF = model.GetTileIndex(UForward);
            //            if (tileIndexF > -1)
            //            {
            //                model.BanExcept(curr.y * spelunky.spaceSize.x + curr.x, tileIndexF);
            //            }
            //            else
            //            {
            //                Debug.LogWarning($"tilename {UForward} not found");
            //            }
            //            break;
            //        case Direction.left:
            //            int tileIndexL = model.GetTileIndex(ULeft);
            //            if (tileIndexL > -1)
            //            {
            //                model.BanExcept(curr.y * spelunky.spaceSize.x + curr.x, tileIndexL);
            //            }
            //            else
            //            {
            //                Debug.LogWarning($"tilename {ULeft} not found");
            //            }
            //            break;
            //        case Direction.right:
            //            int tileIndexR = model.GetTileIndex(Uright);
            //            if (tileIndexR > -1)
            //            {
            //                model.BanExcept(curr.y * spelunky.spaceSize.x + curr.x, tileIndexR);
            //            }
            //            else
            //            {
            //                Debug.LogWarning($"tilename {Uright} not found");
            //            }
            //            break;
            //        case Direction.back:
            //            int tileIndexB = model.GetTileIndex(UBack);
            //            if (tileIndexB > -1)
            //            {
            //                model.BanExcept(curr.y * spelunky.spaceSize.x + curr.x, tileIndexB);
            //            }
            //            else
            //            {
            //                Debug.LogWarning($"tilename {UBack} not found");
            //            }
            //            break;
            //    }
            //}
            //if (i == spelunky.pathTiles.Count - 1)
            //{
            //    switch (prevDoor)
            //    {
            //        case Direction.forward:
            //            int tileIndexF = model.GetTileIndex(UForward);
            //            if (tileIndexF > -1)
            //            {
            //                model.BanExcept(curr.y * spelunky.spaceSize.x + curr.x, tileIndexF);
            //            }
            //            else
            //            {
            //                Debug.LogWarning($"tilename {UForward} not found");
            //            }
            //            break;
            //        case Direction.left:
            //            int tileIndexL = model.GetTileIndex(ULeft);
            //            if (tileIndexL > -1)
            //            {
            //                model.BanExcept(curr.y * spelunky.spaceSize.x + curr.x, tileIndexL);
            //            }
            //            else
            //            {
            //                Debug.LogWarning($"tilename {ULeft} not found");
            //            }
            //            break;
            //        case Direction.right:
            //            int tileIndexR = model.GetTileIndex(Uright);
            //            if (tileIndexR > -1)
            //            {
            //                model.BanExcept(curr.y * spelunky.spaceSize.x + curr.x, tileIndexR);
            //            }
            //            else
            //            {
            //                Debug.LogWarning($"tilename {Uright} not found");
            //            }
            //            break;
            //        case Direction.back:
            //            int tileIndexB = model.GetTileIndex(UBack);
            //            if (tileIndexB > -1)
            //            {
            //                model.BanExcept(curr.y * spelunky.spaceSize.x + curr.x, tileIndexB);
            //            }
            //            else
            //            {
            //                Debug.LogWarning($"tilename {UBack} not found");
            //            }
            //            break;
            //    }
            //    break;
            //}
            if (prevDoor != null)
            {
                switch (prevDoor)
                {
                    case Direction.back:
                        foreach (string tilename in tilesWithoutDoorBack)
                        {
                            int tileIndex = model.GetTileIndex(tilename);
                            if (tileIndex > -1)
                            {
                                model.Ban(curr.y * spelunky.spaceSize.x + curr.x, tileIndex);
                            }
                            else
                            {
                                Debug.LogWarning($"tilename {tilename} not found");
                            }
                        }
                        break;
                    case Direction.left:
                        foreach (string tilename in tilesWithoutDoorLeft)
                        {
                            int tileIndex = model.GetTileIndex(tilename);
                            if (tileIndex > -1)
                            {
                                model.Ban(curr.y * spelunky.spaceSize.x + curr.x, tileIndex);
                            }
                            else
                            {
                                Debug.LogWarning($"tilename {tilename} not found");
                            }
                        }
                        break;
                    case Direction.right:
                        foreach (string tilename in tilesWithoutDoorRight)
                        {
                            int tileIndex = model.GetTileIndex(tilename);
                            if (tileIndex > -1)
                            {
                                model.Ban(curr.y * spelunky.spaceSize.x + curr.x, tileIndex);
                            }
                            else
                            {
                                Debug.LogWarning($"tilename {tilename} not found");
                            }
                        }
                        break;
                    case Direction.forward:
                        foreach (string tilename in tilesWithoutDoorForward)
                        {
                            int tileIndex = model.GetTileIndex(tilename);
                            if (tileIndex > -1)
                            {
                                model.Ban(curr.y * spelunky.spaceSize.x + curr.x, tileIndex);
                            }
                            else
                            {
                                Debug.LogWarning($"tilename {tilename} not found");
                            }
                        }
                        break;
                }
            }
            if (nextDoor != null)
            {
                switch (nextDoor)
                {
                    case Direction.forward:
                        foreach (string tilename in tilesWithoutDoorForward)
                        {
                            int tileIndex = model.GetTileIndex(tilename);
                            if (tileIndex > -1)
                            {
                                model.Ban(curr.y * spelunky.spaceSize.x + curr.x, tileIndex);
                            }
                            else
                            {
                                Debug.LogWarning($"tilename {tilename} not found");
                            }
                        }
                        break;
                    case Direction.left:
                        foreach (string tilename in tilesWithoutDoorLeft)
                        {
                            int tileIndex = model.GetTileIndex(tilename);
                            if (tileIndex > -1)
                            {
                                model.Ban(curr.y * spelunky.spaceSize.x + curr.x, tileIndex);
                            }
                            else
                            {
                                Debug.LogWarning($"tilename {tilename} not found");
                            }
                        }
                        break;
                    case Direction.right:
                        foreach (string tilename in tilesWithoutDoorRight)
                        {
                            int tileIndex = model.GetTileIndex(tilename);
                            if (tileIndex > -1)
                            {
                                model.Ban(curr.y * spelunky.spaceSize.x + curr.x, tileIndex);
                            }
                            else
                            {
                                Debug.LogWarning($"tilename {tilename} not found");
                            }
                        }
                        break;
                    case Direction.back:
                        foreach (string tilename in tilesWithoutDoorBack)
                        {
                            int tileIndex = model.GetTileIndex(tilename);
                            if (tileIndex > -1)
                            {
                                model.Ban(curr.y * spelunky.spaceSize.x + curr.x, tileIndex);
                            }
                            else
                            {
                                Debug.LogWarning($"tilename {tilename} not found");
                            }
                        }
                        break;
                }
            }
        }

        // enforce boundaries
        if (enforceBoundaries)
        {
            for (int x = 0; x < spelunky.spaceSize.x; x++)
            {
                for (int y = 0; y < spelunky.spaceSize.y; y++)
                {
                    if (x == 0)
                    {
                        foreach (string tilename in tilesWithDoorLeft)
                        {
                            int tileIndex = model.GetTileIndex(tilename);
                            if (tileIndex > -1)
                            {
                                model.Ban(y * spelunky.spaceSize.x + x, tileIndex);
                            }
                            else
                            {
                                Debug.LogWarning($"tilename {tilename} not found");
                            }
                        }
                    }
                    else if (x == spelunky.spaceSize.x - 1)
                    {
                        foreach (string tilename in tilesWithDoorRight)
                        {
                            int tileIndex = model.GetTileIndex(tilename);
                            if (tileIndex > -1)
                            {
                                model.Ban(y * spelunky.spaceSize.x + x, tileIndex);
                            }
                            else
                            {
                                Debug.LogWarning($"tilename {tilename} not found");
                            }
                        }
                    }

                    if (y == 0/* && spelunky.GetEntryCoordinates().x != x*/)
                    {
                        foreach (string tilename in tilesWithDoorBack)
                        {
                            int tileIndex = model.GetTileIndex(tilename);
                            if (tileIndex > -1)
                            {
                                model.Ban(y * spelunky.spaceSize.x + x, tileIndex);
                            }
                            else
                            {
                                Debug.LogWarning($"tilename {tilename} not found");
                            }
                        }
                    }
                    else if (y == spelunky.spaceSize.y - 1/* && spelunky.GetExitCoordinates().x != x*/)
                    {
                        foreach (string tilename in tilesWithDoorForward)
                        {
                            int tileIndex = model.GetTileIndex(tilename);
                            if (tileIndex > -1)
                            {
                                model.Ban(y * spelunky.spaceSize.x + x, tileIndex);
                            }
                            else
                            {
                                Debug.LogWarning($"tilename {tilename} not found");
                            }
                        }
                    }
                }
            }
        }

        // force entry and exit connection
        //if (openEntryAndExit)
        //{
        //    foreach (string tilename in tilesWithoutDoorBack)
        //    {
        //        int tileIndex = model.GetTileIndex(tilename);
        //        if (tileIndex > -1)
        //        {
        //            model.Ban(spelunky.GetEntryCoordinates().y * spelunky.spaceSize.x + spelunky.GetEntryCoordinates().x, tileIndex);
        //        }
        //        else
        //        {
        //            Debug.LogWarning($"tilename {tilename} not found");
        //        }
        //    }
        //    foreach (string tilename in tilesWithoutDoorForward)
        //    {
        //        int tileIndex = model.GetTileIndex(tilename);
        //        if (tileIndex > -1)
        //        {
        //            model.Ban(spelunky.GetExitCoordinates().y * spelunky.spaceSize.x + spelunky.GetExitCoordinates().x, tileIndex);
        //        }
        //        else
        //        {
        //            Debug.LogWarning($"tilename {tilename} not found");
        //        }
        //    }
        //}

        #endregion

        // run WFC
        for (int k = 0; k < attempts; k++)
        {
            int seed = random.Next();
            bool finished = model.Run(seed, limit);
            if (finished)
            {
                Transform result = model.Draw();
                result.parent = transform;

                break;
            }
            else
            {
                Debug.LogWarning("CONTRADICTION");
            }
        }

        // Function added by Nikolaj Lyhne. Removes tiles that are not accessible from main path.
        #region RemoveUselessTiles
        // mark tiles not connected to main path for removal
        // list to mark tiles safe
        List<int> tilesForRemoval = new List<int>();
        List<int[]> tileColors = new List<int[]>();

        // list containing names of rooms
        List<string> tilesRoomNames = model.TextListOutput();

        // mark all tiles as -1 for removal
        for (int i = 0; i < (spelunky.spaceSize.x * spelunky.spaceSize.y); i++)
        {
            tilesForRemoval.Add(-1);
            tileColors.Add(new int[4] { -1, -1, -1, -1 });
        }

        // Queue for breadth first search
        Queue<Vector2Int> mainTiles = new Queue<Vector2Int>();

        // enqueue entry tile
        mainTiles.Enqueue(spelunky.GetEntryCoordinates());

        while (mainTiles.Count > 0)
        {
            // get current tile
            Vector2Int current = mainTiles.Dequeue();

            if (current == null)
                continue;

            // mark tile safe in removal list
            tilesForRemoval[(current.x * spelunky.spaceSize.y) + current.y] = 1;

            // add neighbours on path to queue
            string currentRoom = tilesRoomNames[(current.x * spelunky.spaceSize.y) + current.y];

            // check neighbours in order up, down, right, left only if they are within bounds
            if (current.y + 1 < spelunky.spaceSize.y)
            {
                bool safe = false;
                if (tilesForRemoval[(current.x * spelunky.spaceSize.y) + current.y + 1] > 0)
                    safe = true;
                string forwardRoom = tilesRoomNames[(current.x * spelunky.spaceSize.y) + current.y + 1]; 
                if (ConnectedRooms(currentRoom, forwardRoom, 0) && !safe)
                {
                    mainTiles.Enqueue(new Vector2Int(current.x, current.y + 1));
                }
            }
            if (current.y - 1 >= 0)
            {
                bool safe = false;
                if (tilesForRemoval[(current.x * spelunky.spaceSize.y) + current.y - 1] > 0)
                    safe = true;
                string backRoom = tilesRoomNames[(current.x * spelunky.spaceSize.y) + current.y - 1];
                if (ConnectedRooms(currentRoom, backRoom, 1) && !safe)
                {
                    mainTiles.Enqueue(new Vector2Int(current.x, current.y - 1));
                }
            }
            if (current.x + 1 < spelunky.spaceSize.x)
            {
                bool safe = false;
                if (tilesForRemoval[((current.x + 1) * spelunky.spaceSize.y) + current.y] > 0)
                    safe = true;
                string rightRoom = tilesRoomNames[((current.x + 1) * spelunky.spaceSize.y) + current.y];
                if (ConnectedRooms(currentRoom, rightRoom, 3) && !safe)
                {
                    mainTiles.Enqueue(new Vector2Int(current.x + 1, current.y));
                }
            }
            if (current.x - 1 >= 0)
            {
                bool safe = false;
                if (tilesForRemoval[((current.x - 1) * spelunky.spaceSize.y) + current.y] > 0)
                    safe = true;
                string leftRoom = tilesRoomNames[((current.x - 1) * spelunky.spaceSize.y) + current.y];
                if (ConnectedRooms(currentRoom, leftRoom, 2) && !safe)
                {
                    mainTiles.Enqueue(new Vector2Int(current.x - 1, current.y));
                }
            }

            // mark color of tile according to nearest path tile
            float closestDist = 9999999;
            float dist = 0;
            int closestCoordIndex = 0;

            for (int i = 0; i < spelunky.legoCoords.Count; i++)
            {
                dist = Manhattan(current, spelunky.legoCoords[i]);

                if (dist < closestDist)
                {
                    closestCoordIndex = i;
                    closestDist = dist;
                }
            }

            tileColors[(current.x * spelunky.spaceSize.y) + current.y] = spelunky.pathColors[spelunky.legoCoords[closestCoordIndex]];

        }

        // offset because of inactive gameobjects in children
        int offSet = 17;

        for (int i = tilesForRemoval.Count - 1; i >= 0; i--)
        {
            if (tilesForRemoval[i] < 0)
            {
                GameObject.DestroyImmediate(transform.GetChild(offSet + i).gameObject);
            }
            else
            {
                // Determine colors to be used when creating this room
                bool useFireTrap = false;
                bool useBladeTrap = false;
                bool blueRoom = false;
                bool entrance = false;
                bool exit = false;
                for (int j = 0; j < 4; j++)
                {
                    float curCol = tileColors[i][j];
                    
                    switch (curCol)
                    {
                        case 0:
                            break;
                        case 1:
                            useFireTrap = true;
                            break;
                        case 2:
                            useBladeTrap = true;
                            break;
                        case 3:
                            blueRoom = true;
                            break;
                        case 4:
                            entrance = true;
                            break;
                        case 5:
                            exit = true;
                            break;

                    }
                }

                int coinChance = Random.Range(0, 3);
                int coinColor = Random.Range(0, 3);
                
                Transform parent = transform.GetChild(offSet + i);
                string roomName = blueRoom && !(exit || entrance || useBladeTrap) ? parent.name.Substring(0, 6) + "_blue" : parent.name.Substring(0, 6) + "_white";
                GameObject tilePrefab = tilePrefabs[tileIndexes[roomName]];
                if (coinChance == 0 && !(exit || entrance))
                {
                    string color = "";
                    if (coinColor == 0)
                        color = "_blue";
                    else if (coinColor == 1)
                        color = "_green";
                    else
                        color = "_red";

                    MaxPoints++;
                    GameObject coin = tilePrefabs[tileIndexes["Coin" + color]];
                    parent.gameObject.GetComponent<Room>().SetupTrap(coin, 3);
                }
                if (useFireTrap && !(exit || entrance || useBladeTrap))
                {
                    GameObject trap = tilePrefabs[tileIndexes["FireTrap"]];
                    parent.gameObject.GetComponent<Room>().SetupTrap(trap, 0);
                }
                if (useBladeTrap && !(exit || entrance ||useFireTrap))
                {
                    string wallType = parent.name.Contains("room_L") || parent.name.Contains("room_T") || parent.name.Contains("room_X") ? "_L" : "_I";
                    GameObject trap = tilePrefabs[tileIndexes["RotatingWall" + wallType]];
                    parent.gameObject.GetComponent<Room>().SetupTrap(trap, 1);
                }
                if (exit)
                {
                    GameObject exitObj = tilePrefabs[tileIndexes["Exit"]];
                    parent.gameObject.GetComponent<Room>().SetupTrap(exitObj, 2);
                    
                }
                if (!useFireTrap && !useBladeTrap && !blueRoom)
                {
                    parent.gameObject.GetComponent<Room>().EnableCheckpoint();
                }
                if (entrance)
                {
                    Unity.LEGO.Game.GameFlowManager.Instance.CurrentRoom = parent.gameObject;
                    spawnPosition.position = Unity.LEGO.Game.GameFlowManager.Instance.CurrentRoom.GetComponent<Room>().SpawnPoint.position;
                }

                parent.gameObject.GetComponent<Room>().SetupRoom(tilePrefab, 0);

            }


        }

        #endregion

        Fog.SetActive(true);

        ResetExplorePosition();
        Explore();
    }

    public void Clear()
    {
        int children = transform.childCount;
        for (int i = children - 1; i > 0; i--)
        {
            GameObject.DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    public void TakeNewPicture()
    {
        Clear();
        TrackImagesInfo.Instance.TakeNewPhoto();
        Unity.LEGO.Game.GameFlowManager.Instance.UpdateGameState(GameState.DetectImage);
    }

    private float Manhattan(Vector2Int start, Vector2Int goal)
    {
        float xd = start.x - goal.x;
        float yd = start.y - goal.y;

        return Mathf.Abs(xd) + Mathf.Abs(yd);
    }

    private bool ConnectedRooms(string current, string other, int direction)
    {
        if (tilesWithDoorForward.Contains(current) && direction == 0)
        {
            if (tilesWithDoorBack.Contains(other))
                return true;
        }
        if (tilesWithDoorBack.Contains(current) && direction == 1)
        {
            if (tilesWithDoorForward.Contains(other))
                return true;
        }
        if (tilesWithDoorLeft.Contains(current) && direction == 2)
        {
            if (tilesWithDoorRight.Contains(other))
                return true;
        }
        if (tilesWithDoorRight.Contains(current) && direction == 3)
        {
            if (tilesWithDoorLeft.Contains(other))
                return true;
        }

        // if none were true, return false
        return false;
    }

    public void ResetExplorePosition()
    {
        LegoSpelunky spelunky = GetComponent<LegoSpelunky>();
        float maxGridSize = spelunky.spaceSize.x > spelunky.spaceSize.y ? spelunky.spaceSize.x : spelunky.spaceSize.y;
        player.position = spelunky.GetEntryPosition() + playerYOffset + new Vector3((maxGridSize - 1) * .5f, maxGridSize, (maxGridSize - 1) * .5f);
        //player.forward = Vector3.down;
        //coin.position = new Vector3(0f, 100f, 0f);
    }

    public void Explore()
    {
        LegoSpelunky spelunky = GetComponent<LegoSpelunky>();
        Vector3 entryPosition = spelunky.GetEntryPosition();
        //spawnPosition = GameObject.FindGameObjectWithTag("Spawn").GetComponent<Transform>();
        spawnPosition.position = Unity.LEGO.Game.GameFlowManager.Instance.CurrentRoom.GetComponent<Room>().SpawnPoint.position;
        MinifigController.Instance.TeleportTo(spawnPosition.position);
        MinifigController.Instance.gravity = 50;
        Unity.LEGO.Game.GameFlowManager.Instance.isPlacedAtSpawn = true;
        //if (entryPosition.x != -1 && entryPosition.z != -1)
        //{
        //    ResetExplorePosition();
        //    player.position = entryPosition;
        //    player.forward = Vector3.forward;
        //}
        //Vector3 exitPosition = spelunky.GetExitPosition();
        //if (exitPosition.x != -1 && exitPosition.z != -1)
        //{
        //    //coin.position = exitPosition;
        //    //coin.position += AdjustPosition(spelunky.GetExitCoordinates());
        //}
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(LegoCollapse))]
public class CollapseEditor : Editor
{
    LegoCollapse me;

    void OnEnable()
    {
        me = (LegoCollapse)target;
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUI.BeginChangeCheck();
        if (GUILayout.Button("Run"))
        {
            Undo.RecordObject(me, "Run");
            me.Run();
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("Player Explore"))
        {
            me.Explore();
        }
        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }
    }
}
#endif
