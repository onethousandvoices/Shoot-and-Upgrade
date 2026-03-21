using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Utilities;

public sealed class WallGenerator
{
    private const float BLOCK_WIDTH = 1f;
    private const float BLOCK_HEIGHT = 4f;
    private const float BLOCK_DEPTH = 1f;
    private const float WALL_Y = 0f;
    private const float MIN_WALL_GAP = 3f;
    private const float CENTER_EXCLUSION_RADIUS_SQR = 20f * 20f;
    private const int MAX_ATTEMPTS = 200;
    private const float DARKNESS_THRESHOLD = 0.5f;
    
    private readonly Transform _parent;
    private readonly Material _material;
    private readonly List<Rect> _placedRects = new();
    
    private static Mesh _blockMesh;
    private static Vector2Int[][] _parsedPatterns;
    private static int _maxPatternBlocks;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        if (_blockMesh) Object.Destroy(_blockMesh);
        _blockMesh = null;
        _parsedPatterns = null;
        _maxPatternBlocks = 0;
    }

    public WallGenerator(Transform parent)
    {
        _parent = parent;
        _material = ResourcesLoader.GetWhiteMaterial();
    }

    public void Generate(float locationSize)
    {
        var halfSize = locationSize * 0.5f;
        GenerateBorderWalls(halfSize);
        
        var patterns = GetParsedPatterns();
        GenerateInnerWalls(halfSize, patterns);
    }

    private static Vector2Int[][] GetParsedPatterns()
    {
        if (_parsedPatterns != null)
            return _parsedPatterns;
        
        var textures = ResourcesLoader.GetWallPatterns();
        _parsedPatterns = new Vector2Int[textures.Length][];
        
        for (var i = 0; i < textures.Length; i++)
        {
            _parsedPatterns[i] = ParsePattern(textures[i]);
            if (_parsedPatterns[i].Length > _maxPatternBlocks)
                _maxPatternBlocks = _parsedPatterns[i].Length;
        }
        
        return _parsedPatterns;
    }

    private static Vector2Int[] ParsePattern(Texture2D texture)
    {
        var width = texture.width;
        var height = texture.height;
        var pixels = texture.GetPixels32();
        List<Vector2Int> blocks = new(width * height);
        
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var pixel = pixels[y * width + x];
            var brightness = (pixel.r + pixel.g + pixel.b) / (3f * 255f);
            if (brightness < DARKNESS_THRESHOLD)
                blocks.Add(new(x, y));
        }
        
        return blocks.ToArray();
    }

    private void GenerateBorderWalls(float halfSize)
    {
        var length = halfSize * 2f;
        var horizontalMesh = CreateBoxMesh(length, BLOCK_DEPTH);
        var verticalMesh = CreateBoxMesh(BLOCK_DEPTH, length);
        
        CreateBorderWall(new(-halfSize, WALL_Y, halfSize - BLOCK_DEPTH), horizontalMesh, length, BLOCK_DEPTH);
        CreateBorderWall(new(-halfSize, WALL_Y, -halfSize), horizontalMesh, length, BLOCK_DEPTH);
        CreateBorderWall(new(halfSize - BLOCK_DEPTH, WALL_Y, -halfSize), verticalMesh, BLOCK_DEPTH, length);
        CreateBorderWall(new(-halfSize, WALL_Y, -halfSize), verticalMesh, BLOCK_DEPTH, length);
    }

    private void CreateBorderWall(Vector3 origin, Mesh mesh, float sizeX, float sizeZ)
    {
        var wallGo = CreateWallObject("BorderWall", origin, mesh);
        
        var collider = wallGo.AddComponent<BoxCollider>();
        collider.center = new(sizeX * 0.5f, BLOCK_HEIGHT * 0.5f, sizeZ * 0.5f);
        collider.size = new(sizeX, BLOCK_HEIGHT, sizeZ);
    }

    private void GenerateInnerWalls(float halfSize, Vector2Int[][] patterns)
    {
        const float margin = BLOCK_DEPTH + 1f;
        var minPos = -halfSize + margin;
        var maxPos = halfSize - margin;
        var attempts = 0;
        var patternCount = patterns.Length;
        var usageCounts = new int[patternCount];
        var rotation = 0;
        var rotatedBuffer = new Vector3[_maxPatternBlocks];
        while (attempts < MAX_ATTEMPTS)
        {
            var patternIndex = GetLeastUsedPattern(usageCounts, patternCount);
            var pattern = patterns[patternIndex];
            var blockCount = pattern.Length;
            
            if (blockCount == 0)
            {
                attempts++;
                continue;
            }
            
            rotation = (rotation + 90) % 360;
            CalculateRotatedFootprint(pattern, rotation, rotatedBuffer, out var fpMin, out var fpMax);
            
            var footprintX = fpMax.x - fpMin.x;
            var footprintZ = fpMax.y - fpMin.y;
            
            var spawnMinX = minPos - fpMin.x;
            var spawnMaxX = maxPos - (fpMin.x + footprintX);
            var spawnMinZ = minPos - fpMin.y;
            var spawnMaxZ = maxPos - (fpMin.y + footprintZ);
            
            if (spawnMinX > spawnMaxX || spawnMinZ > spawnMaxZ)
            {
                attempts++;
                continue;
            }
            
            var posX = Random.Range(spawnMinX, spawnMaxX);
            var posZ = Random.Range(spawnMinZ, spawnMaxZ);
            
            var wallRect = new Rect(
                posX + fpMin.x - MIN_WALL_GAP * 0.5f,
                posZ + fpMin.y - MIN_WALL_GAP * 0.5f,
                footprintX + MIN_WALL_GAP,
                footprintZ + MIN_WALL_GAP
            );
            
            var centerX = posX + fpMin.x + footprintX * 0.5f;
            var centerZ = posZ + fpMin.y + footprintZ * 0.5f;
            
            if (centerX * centerX + centerZ * centerZ < CENTER_EXCLUSION_RADIUS_SQR)
            {
                attempts++;
                continue;
            }
            
            if (OverlapsAny(wallRect))
            {
                attempts++;
                continue;
            }
            
            CreatePatternWall(rotatedBuffer, blockCount, new(posX, WALL_Y, posZ));
            _placedRects.Add(wallRect);
            usageCounts[patternIndex]++;
            attempts = 0;
        }
    }

    private static int GetLeastUsedPattern(int[] usageCounts, int count)
    {
        var minUsage = usageCounts[0];
        var candidateCount = 1;
        
        for (var i = 1; i < count; i++)
            if (usageCounts[i] < minUsage)
            {
                minUsage = usageCounts[i];
                candidateCount = 1;
            }
            else if (usageCounts[i] == minUsage)
                candidateCount++;
            
        var chosen = Random.Range(0, candidateCount);
        var seen = 0;
        
        for (var i = 0; i < count; i++)
        {
            if (usageCounts[i] != minUsage)
                continue;
            
            if (seen == chosen)
                return i;
            
            seen++;
        }
        
        return 0;
    }

    private static void CalculateRotatedFootprint(
        Vector2Int[] pattern,
        int angleDeg,
        Vector3[] rotatedBuffer,
        out Vector2 min,
        out Vector2 max)
    {
        var sin = Mathf.RoundToInt(Mathf.Sin(angleDeg * Mathf.Deg2Rad));
        var cos = Mathf.RoundToInt(Mathf.Cos(angleDeg * Mathf.Deg2Rad));
        var blockCount = pattern.Length;
        
        var firstX = pattern[0].x * cos - pattern[0].y * sin;
        var firstZ = pattern[0].x * sin + pattern[0].y * cos;
        rotatedBuffer[0] = new(firstX * BLOCK_WIDTH, 0f, firstZ * BLOCK_DEPTH);
        
        min = new(firstX * BLOCK_WIDTH, firstZ * BLOCK_DEPTH);
        max = new(firstX * BLOCK_WIDTH + BLOCK_WIDTH, firstZ * BLOCK_DEPTH + BLOCK_DEPTH);
        
        for (var i = 1; i < blockCount; i++)
        {
            var rx = pattern[i].x * cos - pattern[i].y * sin;
            var rz = pattern[i].x * sin + pattern[i].y * cos;
            var wx = rx * BLOCK_WIDTH;
            var wz = rz * BLOCK_DEPTH;
            rotatedBuffer[i] = new(wx, 0f, wz);
            
            if (wx < min.x) min.x = wx;
            if (wz < min.y) min.y = wz;
            if (wx + BLOCK_WIDTH > max.x) max.x = wx + BLOCK_WIDTH;
            if (wz + BLOCK_DEPTH > max.y) max.y = wz + BLOCK_DEPTH;
        }
    }

    private void CreatePatternWall(Vector3[] blockPositions, int blockCount, Vector3 origin)
    {
        var blockMesh = GetBlockMesh();
        var combineBuffer = new CombineInstance[blockCount];
        
        for (var i = 0; i < blockCount; i++)
        {
            combineBuffer[i].mesh = blockMesh;
            combineBuffer[i].transform = Matrix4x4.Translate(blockPositions[i]);
        }
        
        var combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combineBuffer, true, true);
        combinedMesh.RecalculateBounds();
        
        var wallGo = CreateWallObject("Wall", origin, combinedMesh);
        var collider = wallGo.AddComponent<MeshCollider>();
        collider.sharedMesh = combinedMesh;
        combinedMesh.UploadMeshData(true);
    }

    private GameObject CreateWallObject(string name, Vector3 origin, Mesh mesh)
    {
        var wallGo = new GameObject(name);
        wallGo.transform.SetParent(_parent, false);
        wallGo.transform.position = origin;
        wallGo.layer = Layers.OBSTACLE;
        
        var filter = wallGo.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        
        var renderer = wallGo.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = _material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        
        var destroyer = wallGo.AddComponent<MeshDestroyer>();
        destroyer.Mesh = mesh;
        
        return wallGo;
    }

    private bool OverlapsAny(Rect rect)
    {
        for (var i = 0; i < _placedRects.Count; i++)
            if (_placedRects[i].Overlaps(rect))
                return true;
            
        return false;
    }

    private static Mesh CreateBoxMesh(float sizeX, float sizeZ)
    {
        var mesh = new Mesh
        {
            vertices = new[]
            {
                new Vector3(0, 0, 0), new Vector3(sizeX, 0, 0), new Vector3(sizeX, BLOCK_HEIGHT, 0), new Vector3(0, BLOCK_HEIGHT, 0),
                new Vector3(sizeX, 0, sizeZ), new Vector3(0, 0, sizeZ), new Vector3(0, BLOCK_HEIGHT, sizeZ), new Vector3(sizeX, BLOCK_HEIGHT, sizeZ),
                new Vector3(0, BLOCK_HEIGHT, 0), new Vector3(sizeX, BLOCK_HEIGHT, 0), new Vector3(sizeX, BLOCK_HEIGHT, sizeZ), new Vector3(0, BLOCK_HEIGHT, sizeZ),
                new Vector3(0, 0, sizeZ), new Vector3(sizeX, 0, sizeZ), new Vector3(sizeX, 0, 0), new Vector3(0, 0, 0),
                new Vector3(0, 0, sizeZ), new Vector3(0, 0, 0), new Vector3(0, BLOCK_HEIGHT, 0), new Vector3(0, BLOCK_HEIGHT, sizeZ),
                new Vector3(sizeX, 0, 0), new Vector3(sizeX, 0, sizeZ), new Vector3(sizeX, BLOCK_HEIGHT, sizeZ), new Vector3(sizeX, BLOCK_HEIGHT, 0)
            },
            triangles = new[]
            {
                0, 2, 1, 0, 3, 2,
                4, 6, 5, 4, 7, 6,
                8, 10, 9, 8, 11, 10,
                12, 14, 13, 12, 15, 14,
                16, 18, 17, 16, 19, 18,
                20, 22, 21, 20, 23, 22
            }
        };
        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh GetBlockMesh()
    {
        if (_blockMesh) return _blockMesh;
        _blockMesh = CreateBoxMesh(BLOCK_WIDTH, BLOCK_DEPTH);
        return _blockMesh;
    }
}