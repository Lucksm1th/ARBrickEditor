
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;

public enum Direction
{
    back = 0,
    left = 1,
    forward = 2,
    right = 3
}
//Permission to use for my thesis granted by Alberto Giudice & Fabio Scita
public class LegoSpelunky : MonoBehaviour
{
    [SerializeField] public Texture2D legoTexture;
    [SerializeField] public Vector2Int spaceSize = new Vector2Int(6, 6);
    [SerializeField] public Vector3 tileSize = new Vector3(1, 1, 1);
    [SerializeField] private float stepDelay = 0;
    private Vector2Int currTile = Vector2Int.one * -1;
    private Vector2Int entryTile = Vector2Int.one * -1;
    private Vector2Int exitTile = Vector2Int.one * -1;
    [SerializeField] [Range(0, 1)] private float forwardTendency = 0;
    private int iterations = 0;
    [SerializeField] public List<Vector2Int> pathTiles = new List<Vector2Int>();
    public List<Vector2Int> legoCoords = new List<Vector2Int>();
    public Dictionary<Vector2Int, int[]> pathColors = new Dictionary<Vector2Int, int[]>();
    private bool allowNextStep = true;
    private bool shouldCollapse = true;
    public TextMeshProUGUI tmp;
    public List<int> textureColors = new List<int>();


    void Start()
    {
        //Run();
    }

    public void Run()
    {
        Reset();

        List<Vector2Int> tiles = LegoTiles(legoTexture);
        pathTiles = ConnectTiles(tiles);

        int counter = 0;
        for (int i = 0; i < pathTiles.Count; i++)
        {
            counter++;
        }
    }

    // Returns the coordinates of the non-exit/entry lego tiles from the texture
    // coordinates are in sorted order, where B is nearest A, C is nearest B etc
    // entry tile variable is also assigned here
    private List<Vector2Int> LegoTiles(Texture2D texture)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();

        // find the tiles in the texture and mark entry tile
        int textureHalfWidth = texture.width / 2;
        int textureHalfHeight = texture.height / 2;
        spaceSize.x = textureHalfWidth - 1;
        spaceSize.y = textureHalfHeight - 1;

        int horizontalCoord = 0;
        int verticalCoord = 0;
        for (int i = 1; i < texture.width - 1; i += 2)
        {
           
            for (int j = 1; j < texture.width - 1; j += 2)
            {
                int[] fourColors = new int[4] { 0, 0, 0, 0 };
                float[] tempColors = new float[4] { 0, 0, 0, 0 };

                tempColors[0] = LevelGenerator.Instance.predefinedColors.IndexOf(texture.GetPixel(i, j));
                tempColors[1] = LevelGenerator.Instance.predefinedColors.IndexOf(texture.GetPixel(i + 1, j));
                tempColors[2] = LevelGenerator.Instance.predefinedColors.IndexOf(texture.GetPixel(i, j + 1));
                tempColors[3] = LevelGenerator.Instance.predefinedColors.IndexOf(texture.GetPixel(i + 1, j + 1));

                bool entry = false;
                bool exit = false;
                bool tile = false;

                for (int k = 0; k < 4; k++)
                {
                    switch (tempColors[k])
                    {
                        case 0:
                            fourColors[k] = 1; // RED
                            tile = true;
                            break;
                        case 1:
                            fourColors[k] = 3; // BLUE
                            tile = true;
                            break;
                        case 2:
                            fourColors[k] = 2; // GREEN
                            tile = true;
                            break;
                        case 3:
                            fourColors[k] = 0;
                            break;
                        case 4:
                            fourColors[k] = 0;
                            break;
                        case 5:
                            fourColors[k] = 2; // GREEN
                            tile = true;
                            break;
                    }
                    
                    // entry/exit stuff
                    if (i == 1 && j == 1)
                    {
                        entry = true;
                        fourColors[k] = 4;
                    }
                    if (i == texture.width - 3 && j == texture.height - 3)
                    {
                        exit = true;
                        fourColors[k] = 5;
                    }
                }

                if (tile)
                    tiles.Add(new Vector2Int(horizontalCoord, verticalCoord));
                //tiles.Add(new Vector2Int(horizontalCoord, verticalCoord));
                legoCoords.Add(new Vector2Int(horizontalCoord, verticalCoord));
                pathColors.Add(new Vector2Int(horizontalCoord, verticalCoord), fourColors);
                if (entry)
                {
                    entryTile = tiles[tiles.Count - 1];
                }
                if (exit)
                {
                    exitTile = tiles[tiles.Count - 1];
                }

                verticalCoord += 1;
            }
            verticalCoord = 0;
            horizontalCoord += 1;
        }

        List<Vector2Int> sortedTiles = new List<Vector2Int>();
        List<int> sortedIndices = new List<int>();
        int iteration = 0;

        // sort the list
        while (tiles.Count > 0)
        {
            float closestDist = 9999999;
            float dist = 0;
            int cloestsCoordIndex = 0;

            for (int i = 0; i < tiles.Count; i++)
            {
                // finding the tile closest to entry tile
                if (iteration == 0)
                {
                    dist = Manhattan(entryTile, tiles[i]);
                }
                else
                {
                    dist = Manhattan(sortedTiles[sortedTiles.Count - 1], tiles[i]);
                }

                if (dist < closestDist)
                {
                    cloestsCoordIndex = i;
                    closestDist = dist;
                }
            }

            iteration++;

            // add the closest tile to sorted list
            sortedTiles.Add(tiles[cloestsCoordIndex]);
            tiles.RemoveAt(cloestsCoordIndex);
        }

        return sortedTiles;
    }

    // returns a path from entrytile to tiles[0], tiles[1] ... tiles[last]
    private List<Vector2Int> ConnectTiles(List<Vector2Int> tiles)
    {
        List<Vector2Int> connectedTiles = new List<Vector2Int>();
        connectedTiles.Add(entryTile);

        //connect all the tiles
        for (int i = 0; i < tiles.Count; i++)
        {
            // connect entry tile to first tile
            if (i == 0)
            {
                connectedTiles.AddRange(PathBetweenTIles(entryTile, tiles[i]));
            }
            else
            {
                connectedTiles.AddRange(PathBetweenTIles(tiles[i - 1], tiles[i]));
            }
        }

        return connectedTiles;
    }

    // Function added by me to find path that goes through all placed tiles
    // finds path between two tiles
    private List<Vector2Int> PathBetweenTIles(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();

        // manhattan distance is the same as how many tiles the path will be
        int dist = (int)Manhattan(start, end);

        int newX = start.x;
        int newY = start.y;

        for (int i = 0; i < dist; i++)
        {
            // check if in same col
            if (newX == end.x)
            {
                newY += start.y < end.y ? 1 : -1;
            }
            else if (newY == end.y)
            {
                newX += start.x < end.x ? 1 : -1;
            }
            else
            {
                int direction = Random.Range(0, 1); // Consider removing
                if (direction == 0)
                    newX += start.x < end.x ? 1 : -1;
                else
                    newY += start.y < end.y ? 1 : -1;
            }

            path.Add(new Vector2Int(newX, newY));
        }

        return path;
    }

    private float Manhattan(Vector2Int start, Vector2Int goal)
    {
        float xd = start.x - goal.x;
        float yd = start.y - goal.y;

        return Mathf.Abs(xd) + Mathf.Abs(yd);
    }

    public Vector3 GetEntryPosition()
    {
        return new Vector3(entryTile.x, -0.35f, entryTile.y);
    }

    public Vector2Int GetEntryCoordinates()
    {
        return entryTile;
    }

    public Vector3 GetExitPosition()
    {
        return new Vector3(exitTile.x, -0.35f, exitTile.y);
    }

    public Vector2Int GetExitCoordinates()
    {
        return exitTile;
    }

    public void Reset()
    {
        currTile = Vector2Int.one * -1;
        entryTile = Vector2Int.one * -1;
        exitTile = Vector2Int.one * -1;
        iterations = 0;
        pathTiles.Clear();
        pathColors.Clear();
        legoCoords.Clear();
        allowNextStep = true;
        shouldCollapse = true;
        //GetComponent<LegoCollapse>().ResetExplorePosition();
    }

    void Update()
    {
        //// pick random tile on the -Z face
        //// repeat:
        ////   pick random direction except -Z, move there
        ////   memorize path along the way
        //// stop when trying to go beyond +Z face

        //if (exitTile.x == -1 && allowNextStep)
        //{
        //    if (Iterate())
        //    {
        //        StartCoroutine("ApplyStepDelay");
        //    }
        //}
        //else if (exitTile.x != -1 && shouldCollapse)
        //{
        //    StartCoroutine("RunCollapse");
        //}
    }

    //public void Run(int upToNSteps)
    //{
    //    while (exitTile.x == -1 && (upToNSteps <= 0 || iterations < upToNSteps))
    //    {
    //        Iterate();
    //    }
    //}

    bool Iterate()
    {
        if (entryTile.x == -1)
        {
            // set entrytile here
            currTile = new Vector2Int(Random.Range(0, (int)spaceSize.x), 0);
            entryTile = currTile;
            pathTiles.Add(currTile);
            iterations++;
            return true;
        }
        else
        {
            Direction nextDir = (Direction)Random.Range(1, 6); // lots of conversions when using an enum...
            if (Random.Range(0.0f, 1.0f) < forwardTendency)
            {
                nextDir = Direction.forward;
            }

            if (currTile.y == spaceSize.y - 1 && nextDir == Direction.forward)
            {
                exitTile = currTile;
                currTile = Vector2Int.one * -1;
                iterations++;
            }
            else
            {
                Vector2Int nextTile = currTile + new Vector2Int(((int)nextDir) % 2 == 1 ? (((int)nextDir) < 2 ? -1 : 1) : 0, ((int)nextDir) % 2 == 0 ? (((int)nextDir) < 2 ? -1 : 1) : 0);
                if (!pathTiles.Contains(nextTile) && !IsOutOfBounds(nextTile))
                {
                    currTile = nextTile;
                    pathTiles.Add(currTile);
                    iterations++;
                    return true;
                }
            }
        }
        return false;
    }

    //IEnumerator ApplyStepDelay()
    //{
    //    allowNextStep = false;
    //    yield return new WaitForSecondsRealtime(stepDelay);
    //    allowNextStep = true;
    //}

    //IEnumerator RunCollapse()
    //{
    //    shouldCollapse = false;
    //    LegoCollapse collapse = GetComponent<LegoCollapse>();
    //    yield return new WaitForSecondsRealtime(stepDelay * 2f);
    //    collapse.Run();
    //    SceneView.RepaintAll();
    //    yield return new WaitForSecondsRealtime(stepDelay * 4f);
    //    collapse.Explore();
    //}

    bool IsOutOfBounds(Vector2Int coords)
    {
        return coords.x < 0 || coords.x >= spaceSize.x || coords.y < 0 || coords.y >= spaceSize.y;
    }

    bool IsOutOfBounds(int x, int y)
    {
        return IsOutOfBounds(new Vector2Int(x, y));
    }

    void OnDrawGizmos()
    {
        if (this.enabled)
        {
            Gizmos.color = Color.grey;
            for (int x = 0; x < spaceSize.x; x++)
            {
                for (int y = 0; y < spaceSize.y; y++)
                {
                    Gizmos.DrawWireCube(GetPos(x, y), tileSize);
                }
            }
            Gizmos.color = Color.blue;
            foreach (Vector2Int tile in pathTiles)
            {
                Gizmos.DrawWireCube(GetPos(tile), tileSize);
            }
            Gizmos.color = Color.green;
            if (!IsOutOfBounds(entryTile.x, entryTile.y)) Gizmos.DrawWireCube(GetPos(entryTile), tileSize);
            Gizmos.color = Color.red;
            if (!IsOutOfBounds(exitTile.x, exitTile.y)) Gizmos.DrawWireCube(GetPos(exitTile), tileSize);
            Gizmos.color = Color.yellow;
            if (!IsOutOfBounds(currTile.x, currTile.y)) Gizmos.DrawWireCube(GetPos(currTile), tileSize);
        }
    }

    Vector3 GetPos(Vector2Int coords)
    {
        return transform.position + transform.right * tileSize.x * coords.x + transform.forward * tileSize.z * coords.y;
    }

    Vector3 GetPos(int x, int y)
    {
        return GetPos(new Vector2Int(x, y));
    }
}
#if UNITY_EDITOR
[CustomEditor(typeof(LegoSpelunky))]
public class SpelunkyEditor : Editor
{
    LegoSpelunky me;

    void OnEnable()
    {
        me = (LegoSpelunky)target;
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUI.BeginChangeCheck();
        if (GUILayout.Button("Reset"))
        {
            Undo.RecordObject(me, "Reset");
            me.Reset();
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("Run"))
        {
            Undo.RecordObject(me, "Run");
            me.Run();
            SceneView.RepaintAll();
        }
        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }
    }
}
#endif
