using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TreeGrowth : MonoBehaviour {
    // Tree Options
    public int MaxVertices = 1024;
    [Range(4, 20)]
    public int NumSides = 8;
    [Range(0.25f, 4f)]
    public float BaseRadius = 2f;
    [Range(0.75f, 0.95f)]
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
    MeshFilter tempFilter;
    MeshRenderer tempRenderer;
    public Material tempMaterial;
    Color treeColor;

    // Tree Parameters
    List<Vector3> vertexList; // Vertex list
    List<int> triangleList; // Triangle list

    float[] ringShape;

    private int branchCalls;
    private float alpha;
    [SerializeField]
    float treeLife;

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
        alpha = 0f;
        treeColor = new Color(treeMaterial.color.r, treeMaterial.color.g, treeMaterial.color.b, 0f);

        Color transparent = new Color(treeColor.r, treeColor.g, treeColor.b, 0f);
        treeMaterial.color = transparent;
        tempMaterial.color = transparent;

        BuildBranch(2, vertexList, triangleList);
        DrawBranch(vertexList, triangleList, treeMaterial, gameObject);
    }

    // Update is called once per frame
    void Update () {
        if (treeLife > 50 && treeLife < 80)
        {
            StartCoroutine("IncrementalGrowth");
        } else if (treeLife > 80)
        {
            ExtendBranch(15);
        }
        treeLife = 0;

    }

    IEnumerator IncrementalGrowth()
    {
        float timeStep = 0.1f;

        ExtendBranch(3);
        yield return new WaitForSeconds(timeStep);
        ExtendBranch(4);
        yield return new WaitForSeconds(timeStep);
        ExtendBranch(5);
        yield return new WaitForSeconds(timeStep);
        ExtendBranch(6);
        yield return new WaitForSeconds(timeStep);
        ExtendBranch(7);
    }

    void ExtendBranch(int branchSize)
    {
        BuildBranch(branchSize, vertexList, triangleList);

        GameObject treeCopy = new GameObject();
        treeCopy.transform.position = transform.position;
        mFilter = treeCopy.AddComponent<MeshFilter>();
        mRenderer = treeCopy.AddComponent<MeshRenderer>();

        DrawBranch(vertexList, triangleList, tempMaterial, treeCopy);
    }

    void BuildBranch(int branchDepthLimit, List<Vector3> vtxList, List<int> triList)
    {
        branchCalls = 0;
        Quaternion originalRotation = transform.localRotation;

        SetRingShape();

        // Main recursive call, starts creating the ring of vertices in the trunk's base
        Branch(new Quaternion(), Vector3.zero, -1, BaseRadius, branchDepthLimit, vtxList, triList);
        transform.localRotation = originalRotation; // Restore original object rotation
    }

    private void DrawBranch(List<Vector3> vtxList, List<int> triList, Material m, GameObject tree)
    {
        // Set the material as faded out
        Color transparent = new Color(treeColor.r, treeColor.g, treeColor.b, 0f);
        m.color = transparent;

        // Get mesh or create one
        Mesh mesh = mFilter.sharedMesh;
        if (mesh == null)
            mesh = mFilter.sharedMesh = new Mesh();
        else
            mesh.Clear();
        mRenderer.sharedMaterial = m;

        // Assign vertex data
        mesh.vertices = vtxList.ToArray();
        mesh.triangles = triList.ToArray();

        // Update mesh
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.Optimize(); // Do not call this if we are going to change the mesh dynamically!

        // Fade the material in
        alpha = 0;
        StartCoroutine(FadeBranchIn(m, tree));
    }


    IEnumerator FadeBranchIn(Material m, GameObject tree)
    {
        while(alpha < 1f)
        {
            alpha += Time.deltaTime * 2;

            float newAlpha = Mathf.Lerp(0f, 1f, alpha);
            Color newColor = new Color(treeColor.r, treeColor.g, treeColor.b, newAlpha);
            m.color = newColor;

            yield return null;
        }

        tree.GetComponent<MeshRenderer>().material = treeMaterial;

        Color newColor = new Color(treeColor.r, treeColor.g, treeColor.b, 1.0f);
        m.color = newColor;
        tree.GetComponent<MeshRenderer>().material = treeMaterial;

        yield break;
    }

    private void SetRingShape()
    {
        ringShape = new float[NumSides+1];
        
        for (int i = 0; i < NumSides; i++) ringShape[i] = 1f;
        ringShape[NumSides] = ringShape[0];
    }

    void Branch(Quaternion quaternion, Vector3 position, int lastRingVertexIndex, float radius, int branchLimit, List<Vector3> vtxList, List<int>triList)
    {
        AddRingVertices(quaternion, position, radius, vtxList);
        
        if (lastRingVertexIndex >= 0) AddTriangles(lastRingVertexIndex, vtxList, triList);

        radius *= RadiusFalloff;
        if (radius < MinimumRadius || vertexList.Count + NumSides >= MaxVertices || branchCalls >= branchLimit) // End branch if reached minimum radius, or ran out of vertices
        {
            EndBranch(position, vtxList, triList);
            return;
        }

        // Randomize the branch angle
        //transform.rotation = quaternion;
        float x = (Random.value - 0.5f) * Twisting;
        float z = (Random.value - 0.5f) * Twisting;
        transform.Rotate(x, 0f, z);

        lastRingVertexIndex = vertexList.Count - NumSides - 1;
        position += quaternion * new Vector3(0f, SegmentLength, 0f);
        branchCalls++;
        Branch(transform.rotation, position, lastRingVertexIndex, radius, branchLimit, vtxList, triList);
    }

    private void EndBranch(Vector3 position, List<Vector3> vtxList, List<int> triList)
    {
        // Create a cap for ending the branch
        vtxList.Add(position); // Add central vertex
        for (var n = vtxList.Count - NumSides - 2; n < vertexList.Count - 2; n++) // Add cap
        {
            triList.Add(n);
            triList.Add(vertexList.Count - 1);
            triList.Add(n + 1);
        }
    }

    private void AddRingVertices(Quaternion quaternion, Vector3 position, float radius, List<Vector3> vtxList)
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
            vtxList.Add(position + quaternion * offset); // Add Vertex position
        }
    }

    private void AddTriangles(int lastRingVertexIndex, List<Vector3> vtxList, List<int> triList)
    {
        // Create quads between the last two tree rings
        for (int currentRingVertexIndex = vtxList.Count - NumSides - 1; currentRingVertexIndex < vtxList.Count - 1; currentRingVertexIndex++, lastRingVertexIndex++)
        {
            triList.Add(lastRingVertexIndex + 1); // Triangle A
            triList.Add(lastRingVertexIndex);
            triList.Add(currentRingVertexIndex);

            triList.Add(currentRingVertexIndex); // Triangle B
            triList.Add(currentRingVertexIndex + 1);
            triList.Add(lastRingVertexIndex + 1);
        }
    }
}
