/*
 * Inspired by: https://github.com/mxgmn/WaveFunctionCollapse
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Linq;
using System.Xml.Linq;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEditor;

public class LegoTiledModelWFC
{
    public bool[][] wave;
    private int[][][] propagator;
    private int[][][] compatible;
    private int[] observed;

    private (int, int)[] stack;
    private int stacksize;

    private System.Random random;
    private int sizeX, sizeY, tilesCount;
    private bool periodic;

    private float[] weights;
    private float[] weightLogWeights;

    private float sumOfWeights, sumOfWeightLogWeights, startingEntropy;
    private float[] voxelsSumOfWeights, voxelsSumOfWeightLogWeights, voxelsEntropy;
    private int[] voxelsSumOfOnes;

    public List<GameObject> tiles;
    public List<string> tilenames;
    float tilesize;
    Transform parent;
    AssetBundle levelComponents;
    public List<GameObject> tilePrefabs = new List<GameObject>();
    public Dictionary<string, int> tileIndexes = new Dictionary<string, int>();
    bool asset_bundles_loaded = false;

    public LegoTiledModelWFC(XElement xroot, string subsetName, int width, int height, bool periodic, Transform parent, List<GameObject> tileObjects, Dictionary<string, int> objectIndexes)
    {
        //levelComponents = AssetBundleManager.Instance.levelComponents;

        tileIndexes = objectIndexes;
        tilePrefabs = tileObjects;
        sizeX = width;
        sizeY = height;
        this.periodic = periodic;
        this.parent = parent;

        tilesize = 8f;
        bool unique = xroot.Get("unique", false);

        List<string> subset = null;
        if (!string.IsNullOrEmpty(subsetName))
        {
            XElement xsubsets = xroot.Element("subsets");
            if (xsubsets != null)
            {
                XElement xsubset = xroot.Element("subsets").Elements("subset").FirstOrDefault(x => x.Get<string>("name") == subsetName);
                if (xsubset == null) Debug.LogError($"subset {subsetName} could not be found");
                else subset = xsubset.Elements("tile").Select(x => x.Get<string>("name")).ToList();
            }
            else
            {
                Debug.LogWarning("subsets tag not found");
            }
        }

        tiles = new List<GameObject>();
        tilenames = new List<string>();
        List<float> tempStationary = new List<float>();

        List<int[]> action = new List<int[]>();
        Dictionary<string, int> firstOccurrence = new Dictionary<string, int>();

        foreach (XElement xtile in xroot.Element("tiles").Elements("tile"))
        {
            string tilename = xtile.Get<string>("name");
            if (subset != null && !subset.Contains(tilename))
                continue;

            Func<int, int> a, b;
            int cardinality;

            char sym = xtile.Get("symmetry", 'X');
            if (sym == 'L')
            {
                cardinality = 4;
                a = i => (i + 1) % 4;
                b = i => i % 2 == 0 ? i + 1 : i - 1;
            }
            else if (sym == 'T')
            {
                cardinality = 4;
                a = i => (i + 1) % 4;
                b = i => i % 2 == 0 ? i : 4 - i;
            }
            else if (sym == 'I')
            {
                cardinality = 2;
                a = i => 1 - i;
                b = i => i;
            }
            else if (sym == 'D')
            {
                cardinality = 2;
                a = i => 1 - i;
                b = i => 1 - i;
            }
            else
            {
                cardinality = 1;
                a = i => i;
                b = i => i;
            }

            tilesCount = action.Count;
            firstOccurrence.Add(tilename, tilesCount);

            int[][] map = new int[cardinality][];
            for (int c = 0; c < cardinality; c++)
            {
                map[c] = new int[8];

                map[c][0] = c;
                map[c][1] = a(c);
                map[c][2] = a(a(c));
                map[c][3] = a(a(a(c)));
                map[c][4] = b(c);
                map[c][5] = b(a(c));
                map[c][6] = b(a(a(c)));
                map[c][7] = b(a(a(a(c))));

                for (int s = 0; s < 8; s++)
                    map[c][s] += tilesCount;

                action.Add(map[c]);
            }

            if (unique)
            {
                for (int c = 0; c < cardinality; c++)
                {
                    GameObject tilePrefab = tilePrefabs[tileIndexes[tilename]];
                    GameObject tileInstance = GameObject.Instantiate(tilePrefab, new Vector3(0, 0, 0), tilePrefab.transform.rotation);
                    tileInstance.SetActive(false);
                    tileInstance.transform.parent = parent;
                    tiles.Add(tileInstance);
                    tilenames.Add($"{tilename} {c}");
                }
            }
            else
            {
                GameObject tilePrefab = tilePrefabs[tileIndexes[tilename]];
                GameObject tileInstance = GameObject.Instantiate(tilePrefab, new Vector3(0, 0, 0), tilePrefab.transform.rotation);
                tileInstance.SetActive(false);
                tileInstance.transform.parent = parent;
                tiles.Add(tileInstance);
                tilenames.Add($"{tilename} 0");
                tilesCount++;

                for (int c = 1; c < cardinality; c++)
                {
                    tileInstance = GameObject.Instantiate(tiles[tilesCount - 1], new Vector3(0, 0, 0), Quaternion.identity);
                    Transform tileTransform = tileInstance.transform;
                    tileTransform.RotateAround(tileTransform.position, tileTransform.up, 360 - c * (c % 2 == 0 ? 0 : 90));
                    tileInstance.gameObject.name = $"{tilename} {c}";
                    tiles.Add(tileInstance);
                    tileInstance.SetActive(false);
                    tileTransform.parent = parent;
                    tilenames.Add($"{tilename} {c}");
                }
            }

            for (int c = 0; c < cardinality; c++)
                tempStationary.Add(xtile.GetFloat("weight", 1.0f));
        }

        tilesCount = action.Count;
        weights = tempStationary.ToArray();

        propagator = new int[4][][];
        bool[][][] tempPropagator = new bool[4][][];

        for (int face = 0; face < 4; face++)
        {
            tempPropagator[face] = new bool[tilesCount][];
            propagator[face] = new int[tilesCount][];
            for (int tile = 0; tile < tilesCount; tile++)
                tempPropagator[face][tile] = new bool[tilesCount];
        }

        foreach (XElement xneighbor in xroot.Element("neighbors").Elements("neighbor"))
        {
            string[] left = xneighbor.Get<string>("left").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] right = xneighbor.Get<string>("right").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (subset != null && (!subset.Contains(left[0]) || !subset.Contains(right[0])))
                continue;

            int L = action[firstOccurrence[string.Join(" ", left.Take(left.Length - 1).ToArray())]][left.Length == 1 ? 0 : int.Parse(left.Last())];
            int R = action[firstOccurrence[string.Join(" ", right.Take(right.Length - 1).ToArray())]][right.Length == 1 ? 0 : int.Parse(right.Last())];
            int D = action[L][1];
            int U = action[R][1];

            tempPropagator[0][R][L] = true;
            tempPropagator[0][action[R][6]][action[L][6]] = true;
            tempPropagator[0][action[L][4]][action[R][4]] = true;
            tempPropagator[0][action[L][2]][action[R][2]] = true;

            tempPropagator[1][U][D] = true;
            tempPropagator[1][action[D][6]][action[U][6]] = true;
            tempPropagator[1][action[U][4]][action[D][4]] = true;
            tempPropagator[1][action[D][2]][action[U][2]] = true;
        }

        for (int tile2 = 0; tile2 < tilesCount; tile2++)
        {
            for (int tile1 = 0; tile1 < tilesCount; tile1++)
            {
                tempPropagator[2][tile2][tile1] = tempPropagator[0][tile1][tile2];
                tempPropagator[3][tile2][tile1] = tempPropagator[1][tile1][tile2];
            }
        }

        List<int>[][] sparsePropagator = new List<int>[4][];
        for (int face = 0; face < 4; face++)
        {
            sparsePropagator[face] = new List<int>[tilesCount];
            for (int tile = 0; tile < tilesCount; tile++)
                sparsePropagator[face][tile] = new List<int>();
        }

        for (int face = 0; face < 4; face++)
        {
            for (int tile1 = 0; tile1 < tilesCount; tile1++)
            {
                List<int> curSpPropagator = sparsePropagator[face][tile1];

                for (int tile2 = 0; tile2 < tilesCount; tile2++)
                    if (tempPropagator[face][tile1][tile2])
                        curSpPropagator.Add(tile2);

                int sparseTilesCount = curSpPropagator.Count;
                propagator[face][tile1] = new int[sparseTilesCount];
                for (int sparseTile = 0; sparseTile < sparseTilesCount; sparseTile++)
                    propagator[face][tile1][sparseTile] = curSpPropagator[sparseTile];
            }
        }

        Init();
        Clear();
    }

    void Init()
    {
        wave = new bool[sizeX * sizeY][];
        compatible = new int[wave.Length][][];
        for (int voxel = 0; voxel < wave.Length; voxel++)
        {
            wave[voxel] = new bool[tilesCount];
            compatible[voxel] = new int[tilesCount][];
            for (int tile = 0; tile < tilesCount; tile++)
                compatible[voxel][tile] = new int[4];
        }

        weightLogWeights = new float[tilesCount];
        sumOfWeights = 0f;
        sumOfWeightLogWeights = 0f;

        for (int tile = 0; tile < tilesCount; tile++)
        {
            weightLogWeights[tile] = weights[tile] * Mathf.Log(weights[tile]);
            sumOfWeights += weights[tile];
            sumOfWeightLogWeights += weightLogWeights[tile];
        }

        startingEntropy = Mathf.Log(sumOfWeights) - sumOfWeightLogWeights / sumOfWeights;

        voxelsSumOfOnes = new int[sizeX * sizeY];
        voxelsSumOfWeights = new float[sizeX * sizeY];
        voxelsSumOfWeightLogWeights = new float[sizeX * sizeY];
        voxelsEntropy = new float[sizeX * sizeY];

        stack = new (int, int)[wave.Length * tilesCount];
        stacksize = 0;
    }

    void Clear()
    {
        for (int voxel = 0; voxel < wave.Length; voxel++)
        {
            for (int tile = 0; tile < tilesCount; tile++)
            {
                wave[voxel][tile] = true;
                for (int face = 0; face < 4; face++)
                    compatible[voxel][tile][face] = propagator[opposite[face]][tile].Length;
            }

            voxelsSumOfOnes[voxel] = weights.Length;
            voxelsSumOfWeights[voxel] = sumOfWeights;
            voxelsSumOfWeightLogWeights[voxel] = sumOfWeightLogWeights;
            voxelsEntropy[voxel] = startingEntropy;
        }
    }

    public void Ban(int voxel, int tile)
    {
        if (wave[voxel][tile])
        {
            wave[voxel][tile] = false;

            for (int face = 0; face < 4; face++)
                compatible[voxel][tile][face] = 0;
            stack[stacksize] = (voxel, tile);
            stacksize++;

            voxelsSumOfOnes[voxel] -= 1;
            voxelsSumOfWeights[voxel] -= weights[tile];
            voxelsSumOfWeightLogWeights[voxel] -= weightLogWeights[tile];

            float curSum = voxelsSumOfWeights[voxel];
            voxelsEntropy[voxel] = Mathf.Log(curSum) - voxelsSumOfWeightLogWeights[voxel] / curSum;
        }
    }

    public void BanExcept(int voxel, int tile)
    {
        for (int t = 0; t < tilesCount; t++)
            if (t != tile)
                Ban(voxel, t);
    }

    bool? Observe()
    {
        float minEntropy = 1000f;
        int minEntropyVoxel = -1;

        for (int voxel = 0; voxel < wave.Length; voxel++)
        {
            if (OnBoundary(voxel % sizeX, voxel / sizeX))
                continue;

            if (voxelsSumOfOnes[voxel] == 0)
                return false;

            if (voxelsSumOfOnes[voxel] > 1 && voxelsEntropy[voxel] <= minEntropy)
            {
                float noise = 0.000001f * (float)random.NextDouble();
                if (voxelsEntropy[voxel] + noise < minEntropy)
                {
                    minEntropy = voxelsEntropy[voxel] + noise;
                    minEntropyVoxel = voxel;
                }
            }
        }

        if (minEntropyVoxel == -1)
        {
            observed = new int[sizeX * sizeY];
            for (int voxel = 0; voxel < wave.Length; voxel++)
                for (int tile = 0; tile < tilesCount; tile++)
                    if (wave[voxel][tile])
                    {
                        observed[voxel] = tile;
                        break;
                    }
            return true;
        }

        float[] distribution = new float[tilesCount];
        for (int tile = 0; tile < tilesCount; tile++)
            distribution[tile] = wave[minEntropyVoxel][tile] ? weights[tile] : 0f;
        int selectedTile = distribution.Random((float)random.NextDouble());

        for (int tile = 0; tile < tilesCount; tile++)
            if (wave[minEntropyVoxel][tile] != (tile == selectedTile))
                Ban(minEntropyVoxel, tile);

        return null;
    }

    void Propagate()
    {
        while (stacksize > 0)
        {
            (int, int) voxelTile1 = stack[stacksize - 1];
            stacksize--;

            int voxel1 = voxelTile1.Item1;
            int voxel1X = voxel1 % sizeX;
            int voxel1Y = voxel1 / sizeX;

            for (int face = 0; face < 4; face++)
            {
                int faceX = DX[face];
                int faceY = DY[face];

                int voxel2X = voxel1X + faceX;
                int voxel2Y = voxel1Y + faceY;

                if (OnBoundary(voxel2X, voxel2Y))
                    continue;

                if (voxel2X < 0)
                    voxel2X += sizeX;
                else if (voxel2X >= sizeX)
                    voxel2X -= sizeX;

                if (voxel2Y < 0)
                    voxel2Y += sizeY;
                else if (voxel2Y >= sizeY)
                    voxel2Y -= sizeY;

                int voxel2 = voxel2X + voxel2Y * sizeX;
                int[] curPropagator = propagator[face][voxelTile1.Item2];

                for (int i = 0; i < curPropagator.Length; i++)
                {
                    int[] curCompatible = compatible[voxel2][curPropagator[i]];
                    curCompatible[face]--;
                    if (curCompatible[face] == 0)
                        Ban(voxel2, curPropagator[i]);
                }
            }
        }
    }

    bool OnBoundary(int x, int y) => !periodic && (x < 0 || y < 0 || x >= sizeX || y >= sizeY);

    public bool Run(int seed, int limit)
    {
        random = new System.Random(seed);

        for (int l = 0; l < limit || limit == 0; l++)
        {
            bool? result = Observe();
            if (result != null) return (bool)result;
            Propagate();
        }

        return true;
    }

    public Transform Draw()
    {
        if (observed != null)
        {
            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    Transform tile = GameObject.Instantiate(tiles[observed[x + y * sizeX]], parent.transform.position + new Vector3( + x * tilesize, 0, y * tilesize), tiles[observed[x + y * sizeX]].transform.rotation).transform;
                    tile.parent = parent;
                    tile.gameObject.SetActive(true);
                }
            }
        }

        return parent;
    }

    public int GetTileIndex(string tilename)
    {
        for (int t = 0; t < tilesCount; t++)
        {
            if (tilenames[t] == tilename)
                return t;
        }
        return -1;
    }

    public string TextOutput()
    {
        var result = new System.Text.StringBuilder();

        for (int y = 0; y < sizeY; y++)
        {
            for (int x = 0; x < sizeX; x++) result.Append($"{tilenames[observed[x + y * sizeX]]}, ");
            result.Append(Environment.NewLine);
        }

        return result.ToString();
    }

    public List<string> TextListOutput()
    {
        List<string> result = new List<string>();

        for (int x = 0; x < sizeY; x++)
        {
            for (int y = 0; y < sizeX; y++) result.Add($"{tilenames[observed[x + y * sizeX]]}");
        }

        return result;
    }

    

    static int[] DX = { -1, 0, 1, 0 };
    static int[] DY = { 0, 1, 0, -1 };
    static int[] opposite = { 2, 3, 0, 1 };
}
