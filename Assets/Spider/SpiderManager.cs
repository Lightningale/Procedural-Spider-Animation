using UnityEngine;
using System.Collections.Generic;

public class SpiderManager : MonoBehaviour
{
    public GameObject spiderPrefab; // Assign your spider prefab in the inspector
    private int poolSize = 500; // Maximum number of spiders
    [Range(0, 500)]
    public int spiderNum = 0; // Active number of spiders
    private List<GameObject> spiders = new List<GameObject>(); // The pool
    public float spawnRadius = 200f; // Radius within which to spawn spiders
    public float spawnHeight = 0.7f; // Height at which spiders are spawned
    private float checkRadius = 3f; // Radius to check for overlap (adjust based on spider size)
    public LayerMask spiderLayer; // Layer to identify spiders
    private int lastSpiderNum = 0; // Track the last spider number to avoid unnecessary calculations
    private float deltaTime = 0.0f;
    void Awake()
    {
        InitializePool();
    }
    void Start()
    {
        lastSpiderNum = spiderNum;
    }
    void Update ()
    {
        if (lastSpiderNum != spiderNum)
        {
            AdjustSpiderPopulation();
            lastSpiderNum = spiderNum;
        }
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        Debug.Log("FPS: "+fps);

    }
        // Initialize the pool with inactive spider instances
    void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject spider = Instantiate(spiderPrefab, Vector3.zero, Quaternion.identity, transform);
            spider.SetActive(false);
            spider.GetComponent<SpiderController>().AutoMode=true;
            spiders.Add(spider);
        }
        Debug.Log("Pool initialized with " + poolSize + " spiders");
    }


    void AdjustSpiderPopulation()
    {
        int currentActive = Mathf.Clamp(spiderNum, 0, poolSize); // Ensure spiderNum is within bounds
        for (int i = 0; i < spiders.Count; i++)
        {
            if (i < currentActive)
            {
                if (!spiders[i].activeSelf)
                {
                    spiders[i].SetActive(true);
                    spiders[i].transform.position = FindSpawnPosition();
                    SetFacingDirection(spiders[i], spiders[i].transform.position);
                    spiders[i].GetComponent<SpiderController>().ResetLegs();
                }
            }
            else
            {
                if (spiders[i].activeSelf)
                {
                    spiders[i].SetActive(false);
                }
            }
        }
    }

    Vector3 FindSpawnPosition()
    {
        for (int attempt = 0; attempt < 100; attempt++) // Limit attempts to prevent infinite loop
        {
            Vector2 randomPoint = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = new Vector3(randomPoint.x, spawnHeight, randomPoint.y);

            // Check if the position is too close to any existing spider
            if (!Physics.CheckSphere(spawnPosition, checkRadius, spiderLayer))
            {
                Debug.Log("Spawned at: " + spawnPosition);
                return spawnPosition; // Return this position if it's not overlapping
            }
        }
        return Vector3.zero; // Return a zero vector if no suitable position is found
    }
    void SetFacingDirection(GameObject spider, Vector3 spawnPosition)
    {
        Vector3 directionToCenter = (Vector3.zero - spawnPosition).normalized; // Direction from spider to the origin
        Vector3 clockwiseDirection = Quaternion.Euler(0, -90, 0) * directionToCenter; // Rotate to get clockwise direction

        spider.transform.rotation = Quaternion.LookRotation(clockwiseDirection, Vector3.up);
    }
}
