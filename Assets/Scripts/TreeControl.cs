using UnityEngine;
using System.Collections;

public class TreeControl : MonoBehaviour {
    public delegate void GrowthAction();
    public static event GrowthAction IncrementGrowth;
    public static event GrowthAction DecrementGrowth;

    GameObject baseBranch;

	// Use this for initialization
	void Start () {
        // Create the base branch
        baseBranch = new GameObject();
        baseBranch.name = "Base";
        baseBranch.transform.parent = gameObject.transform;
        baseBranch.transform.localPosition = transform.position;

        baseBranch.AddComponent<TreeGrowth>();
        baseBranch.GetComponent<TreeGrowth>().Twisting = 8;

        InvokeRepeating("IncreaseGrowth", 5f, 10f);
    }
	
	// Update is called once per frame
	void Update () {
        
    }

    void OnCollisionEnter(Collision col)
    {
        print("Collision");
        if (col.gameObject.tag == "Enemy")
        {
            if (DecrementGrowth != null) DecrementGrowth();
            Destroy(col.gameObject);
        }
    }

    void IncreaseGrowth()
    {
        if (IncrementGrowth != null) IncrementGrowth();
    }
}
