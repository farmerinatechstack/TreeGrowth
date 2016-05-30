using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TreeGrowth : MonoBehaviour {
    // Public Tree Variables
    public int MaxVertices = 2048;
    public float GrowthDelay = 0.2f;
    [Range(4, 20)]
    public int NumSides = 10;
    [Range(0.25f, 4f)]
    public float BaseRadius = 4f;
    [Range(0.75f, 1.0f)]
    public float RadiusFalloff = 0.98f;
    [Range(0.01f, 0.2f)]
    public float MinimumRadius = 0.25f;
    [Range(2, 5)]
    public int BranchAmount = 3;
    [Range(0.5f, 1f)]
    public float BranchRoundness = 1f;
    [Range(0.1f, 2f)]
    public float SegmentLength = 0.2f;
    [Range(0f, 40f)]
    public float Twisting = 8;
    [Range(0f, 0.3f)]
    public float BranchProbability = 0.1f;

    // Private Variables
    MeshFilter mFilter;
    MeshRenderer mRenderer;

    // Tree Parameters
    List<Vector3> vertexList; // Vertex list
    List<int> triangleList; // Triangle list
    Material treeMaterial;
    float[] ringShape;
    int lastRingVertexIndex;
    Vector3 lastPosition;
    int branchCallsForSprout;
    int branchCalls;

    public int getBranchCallsForSprout()
    {
        return branchCallsForSprout;
    }

    // Add a Mesh Filter/Renderer
    void OnEnable()
    {
        mFilter = gameObject.GetComponent<MeshFilter>();
        if (mFilter == null) mFilter = gameObject.AddComponent<MeshFilter>();
        mRenderer = gameObject.GetComponent<MeshRenderer>();
        if (mRenderer == null) mRenderer = gameObject.AddComponent<MeshRenderer>();

        TreeControl.IncrementGrowth += DecreaseGrowthDelay;
        TreeControl.DecrementGrowth += IncreaseGrowthDelay;
    }

    void OnDisable()
    {
        TreeControl.IncrementGrowth -= DecreaseGrowthDelay;
        TreeControl.DecrementGrowth -= IncreaseGrowthDelay;
    }

    void IncreaseGrowthDelay()
    {
        print("Slowing growth");
        GrowthDelay *= 2f;
    }

    void DecreaseGrowthDelay()
    {
        print("Speeding up growth");
        GrowthDelay *= 0.5f;
        GrowthDelay = (GrowthDelay > 0.01f) ? GrowthDelay : 0.01f;
    }

    // Use this for initialization
    void Start () {
        vertexList = new List<Vector3>();
        triangleList = new List<int>();
        lastPosition = Vector3.zero;
        lastRingVertexIndex = -1;
        treeMaterial = Resources.Load("TreeBark", typeof(Material)) as Material;

        SetBranchLimits();
        StartCoroutine("Branch");
    }

    void SetBranchLimits()
    {
        int verticesPerLevel = NumSides + 1;
        int maxBranchCallsByVertexCount = MaxVertices / verticesPerLevel;
        int maxBranchCallsByRadiusFalloff = (int)(Mathf.Log(MinimumRadius / BaseRadius) / Mathf.Log(RadiusFalloff));
        branchCalls = (maxBranchCallsByVertexCount > maxBranchCallsByRadiusFalloff) ? maxBranchCallsByRadiusFalloff : maxBranchCallsByVertexCount;
        branchCallsForSprout = branchCalls / 3;
    }

    // Update is called once per frame
    void Update () {

    }

    void SproutBranches(Vector3 position, float radius, int numChildren)
    {
        for (int i = 0; i < BranchAmount; i++)
        {
            GameObject branch = new GameObject();
            branch.transform.parent = gameObject.transform;
            branch.transform.localPosition = position;

            branch.AddComponent<TreeGrowth>();
            branch.GetComponent<TreeGrowth>().BranchAmount = numChildren;
            branch.GetComponent<TreeGrowth>().BaseRadius = radius;
            branch.GetComponent<TreeGrowth>().GrowthDelay = GrowthDelay * 2f;
            branch.GetComponent<TreeGrowth>().Twisting = 12;
        }
    }

    IEnumerator Branch()
    {
        Quaternion originalRotation = transform.localRotation;
        Quaternion q = new Quaternion();
        float radius = BaseRadius;
        int numBranchIters = 0;
        SetRingShape();

        while (numBranchIters < branchCalls)
        {
            numBranchIters++;


            if (vertexList.Count != 0) UncapBranch();
            if (Random.value > 0.9) SproutBranches(lastPosition, radius * 0.1f, 0); // Randomly sprout
            if (numBranchIters == branchCallsForSprout) SproutBranches(lastPosition, radius*0.9f, BranchAmount-1);

            AddRingVertices(q, radius);
            if (lastRingVertexIndex >= 0) AddTriangles(lastRingVertexIndex);
            radius *= RadiusFalloff;

            // Randomize the branch angle and extend the branch, with random offsets
            transform.rotation = q;
            float x = (Random.value - 0.5f) * Twisting;
            float z = (Random.value - 0.5f) * Twisting;
            if (Random.value > 0.7f)
            {
                x = x * 1.5f;
                z = z * 1.5f;
            }
            transform.Rotate(x, 0f, z);
            float extension = (Random.value < 0.9f) ? SegmentLength : SegmentLength * 2f;
            lastPosition += q * new Vector3(0f, extension, 0f);

            // Prep for next extension
            q = transform.rotation;
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
