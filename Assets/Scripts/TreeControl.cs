using UnityEngine;
using System.Collections;

public class TreeControl : MonoBehaviour {
    public int growthTime;

    GameObject baseBranch;
    TreeGrowth baseGrowthScript;

	// Use this for initialization
	void Start () {
        // Create the base branch
        baseBranch = new GameObject();
        baseBranch.name = "Base";
        baseBranch.transform.parent = gameObject.transform;
        baseBranch.transform.localPosition = transform.position;

        baseBranch.AddComponent<TreeGrowth>();
        baseBranch.GetComponent<TreeGrowth>().Twisting = 8;

        baseGrowthScript = baseBranch.GetComponent<TreeGrowth>();

        EstimateGrowthTime();
    }
	
	// Update is called once per frame
	void Update () {

    }

    // A rough approximation of how long the tree would take to grow, if left unperturbed
    void EstimateGrowthTime()
    {
        int baseRingsBeforeSprout = baseGrowthScript.getBranchCallsForSprout();
        float growthDelay = baseGrowthScript.GrowthDelay;
        int numSproutCalls = baseGrowthScript.BranchAmount;

        baseGrowthScript.StoreBranchLimits(
                baseGrowthScript.NumSides + 1,
                baseGrowthScript.MaxVertices,
                baseGrowthScript.BaseRadius,
                ref baseRingsBeforeSprout,
                ref baseRingsBeforeSprout);

        float growthTime = baseRingsBeforeSprout * growthDelay;
        print("BaseGrowth: " + growthTime);
        int branchCalls = 0;
        int branchCallsForSprout = 0;

        for (int i = 0; i <= numSproutCalls; i++)
        {
            baseGrowthScript.StoreBranchLimits(
                baseGrowthScript.NumSides + 1,
                baseGrowthScript.MaxVertices,
                baseGrowthScript.BaseRadius * Mathf.Pow(0.2f, i + 1),
                ref branchCalls, 
                ref branchCallsForSprout);

            float delayFactor = baseGrowthScript.GrowthDelay * Mathf.Pow(2f, i + 1);
            float additive;

            if (i == numSproutCalls) {
                additive = branchCalls * delayFactor;
            } else {
                additive = branchCallsForSprout * delayFactor;
            }
            print("Branch " + i + " calls: " + branchCallsForSprout + ". Additive: " + additive);
            growthTime += additive;
        }
        print(growthTime);
    }

}
