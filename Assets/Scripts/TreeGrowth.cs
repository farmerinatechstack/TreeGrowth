using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TreeGrowth : MonoBehaviour {
    // Tree Options
    public int MaxVertices = 1024;
    public float GrowthDelay = 0.1f;
    [Range(4, 20)]
    public int NumSides = 8;
    [Range(0.25f, 4f)]
    public float BaseRadius = 2f;
    [Range(0.75f, 1.0f)]
    public float RadiusFalloff = 0.9f;
    [Range(0.01f, 0.2f)]
    public float MinimumRadius = 0.02f;
    [Range(0.5f, 1f)]
    public float BranchRoundness = 0.8f;
    [Range(0.1f, 2f)]
    public float SegmentLength = 0.5f;
    [Range(0f, 40f)]
    public float Twisting = 20f;
    [Range(0f, 0.3f)]
    public float BranchProbability = 0.1f;

    // Private Variables
    MeshFilter mFilter;
    MeshRenderer mRenderer;
    public Material treeMaterial;

    // Tree Parameters
    List<Vector3> vertexList; // Vertex list
    List<int> triangleList; // Triangle list

    float[] ringShape;

    private int depth;
    [SerializeField]
    float treeLife;
    Vector3 lastPosition;
    float lastRadius;

    // Counters for debugging
    int levelOneDepth;
    int levelTwoDepth;
    int levelThreeDepth;
    int levelFourDepth;
    
    int timesBranchCalled;

    // Add a Mesh Filter/Renderer if necessary, and store each
    void OnEnable()
    {
        mFilter = gameObject.GetComponent<MeshFilter>();
        if (mFilter == null) mFilter = gameObject.AddComponent<MeshFilter>();
        mRenderer = gameObject.GetComponent<MeshRenderer>();
        if (mRenderer == null) mRenderer = gameObject.AddComponent<MeshRenderer>();
    }

    // Use this for initialization
    void Start () {
        vertexList = new List<Vector3>();
        triangleList = new List<int>();
        lastPosition = Vector3.zero;
        timesBranchCalled = 0;
        depth = 0;

        int verticesPerLevel = NumSides + 1;
        int maxBranchCallsByVertexCount = MaxVertices / verticesPerLevel;
        int maxBranchCallsByRadiusFalloff = (int) (Mathf.Log(MinimumRadius / BaseRadius) / Mathf.Log(RadiusFalloff));

        levelFourDepth = (maxBranchCallsByVertexCount > maxBranchCallsByRadiusFalloff) ? maxBranchCallsByRadiusFalloff : maxBranchCallsByVertexCount;
        levelThreeDepth = (int)(levelFourDepth * 3f / 4f);
        levelTwoDepth = (int)(levelFourDepth / 2f);
        levelOneDepth = (int)(levelFourDepth / 4f);

        print("level four depth: " + levelFourDepth);

        ExtendBranch(levelOneDepth);
    }

    // Update is called once per frame
    void Update () {
        if (treeLife > 25)
        {
            ExtendBranch(levelTwoDepth);
        } else if (treeLife > 50) {
            ExtendBranch(levelThreeDepth);
        } else if (treeLife > 75) {
            ExtendBranch(levelFourDepth);
        }
        treeLife = 0;
    }

    void ExtendBranch(int depthLimit)
    {
        depth = 0;
        SetRingShape();

        StartCoroutine(Branch(new Quaternion(), -1, BaseRadius, 100));
        /*
        if (vertexList.Count == 0) // the branch is empty
        {
            StartCoroutine(Branch(new Quaternion(), -1, BaseRadius, depthLimit));
        } else {
            UncapBranch();
            StartCoroutine(Branch(new Quaternion(), vertexList.Count - NumSides - 1, lastRadius * 1 / RadiusFalloff, depthLimit));
        }
        */
    }

    IEnumerator Branch(Quaternion quaternion, int lastRingVertexIndex, float radius, int depthLimit)
    {
        Quaternion originalRotation = transform.localRotation;
        
        while (depth < depthLimit)
        {
            if (vertexList.Count != 0) UncapBranch();
            depth++;

            AddRingVertices(quaternion, radius);

            if (lastRingVertexIndex >= 0) AddTriangles(lastRingVertexIndex);

            radius *= RadiusFalloff;
            lastRadius = radius;

            // Randomize the branch angle
            transform.rotation = quaternion;
            float x = (Random.value - 0.5f) * Twisting;
            float z = (Random.value - 0.5f) * Twisting;
            if (Random.value > 0.7f) // Randomly apply extra twisting
            {
                x = x * 1.5f;
                z = z * 1.5f;
            }
            transform.Rotate(x, 0f, z);

            // Extend the branch
            if (depth >= depthLimit) CapBranch(lastPosition);
            float extension = (Random.value < 0.9f) ? SegmentLength : SegmentLength * 2f;
            lastPosition += quaternion * new Vector3(0f, extension, 0f);

            // Prep for next extension
            quaternion = transform.rotation;
            transform.localRotation = originalRotation;
            CapBranch(lastPosition);
            DrawBranch();

            lastRingVertexIndex = vertexList.Count - NumSides - 1;
            yield return new WaitForSeconds(GrowthDelay);
        }

        yield break;
    }

    private void DrawBranch()
    {
        // Get mesh or create one
        Mesh mesh = mFilter.sharedMesh;
        if (mesh == null)
            mesh = mFilter.sharedMesh = new Mesh();
        else
            mesh.Clear();
        mRenderer.sharedMaterial = treeMaterial;

        // Assign vertex data
        mesh.vertices = vertexList.ToArray();
        mesh.triangles = triangleList.ToArray();

        // Update mesh
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void SetRingShape()
    {
        ringShape = new float[NumSides+1];
        
        for (int i = 0; i < NumSides; i++) ringShape[i] = 1f;
        ringShape[NumSides] = ringShape[0];
    }

    private void UncapBranch()
    {
        int numToRemove = NumSides * 3;
        triangleList.RemoveRange(triangleList.Count - numToRemove, numToRemove);
        vertexList.RemoveAt(vertexList.Count - 1); // Add central vertex
    }

    private void CapBranch(Vector3 position)
    {
        // Create a cap for ending the branch
        vertexList.Add(position); // Add central vertex
        for (var n = vertexList.Count - NumSides - 2; n < vertexList.Count - 2; n++) // Add cap
        {
            triangleList.Add(n);
            triangleList.Add(vertexList.Count - 1);
            triangleList.Add(n + 1);
        }
    }

    private void AddRingVertices(Quaternion quaternion, float radius)
    {
        Vector3 offset = Vector3.zero;
        float textureStepU = 1f / NumSides;
        float angInc = 2f * Mathf.PI * textureStepU;
        float ang = 0f;

        // Add ring vertices
        for (int n = 0; n <= NumSides; n++, ang += angInc)
        {
            float r = ringShape[n] * radius;
            offset.x = r * Mathf.Cos(ang); // Get X, Z vertex offsets
            offset.z = r * Mathf.Sin(ang);
            vertexList.Add(lastPosition + quaternion * offset); // Add Vertex position
        }
    }

    private void AddTriangles(int lastRingVertexIndex)
    {
        // Create quads between the last two tree rings
        for (int currentRingVertexIndex = vertexList.Count - NumSides - 1; currentRingVertexIndex < vertexList.Count - 1; currentRingVertexIndex++, lastRingVertexIndex++)
        {
            triangleList.Add(lastRingVertexIndex + 1); // Triangle A
            triangleList.Add(lastRingVertexIndex);
            triangleList.Add(currentRingVertexIndex);

            triangleList.Add(currentRingVertexIndex); // Triangle B
            triangleList.Add(currentRingVertexIndex + 1);
            triangleList.Add(lastRingVertexIndex + 1);
        }
    }
}
