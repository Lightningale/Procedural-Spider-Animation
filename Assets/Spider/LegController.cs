using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegController : MonoBehaviour {
    public SpiderController spiderBody; // Reference to the spider body to track its rotation
    public Transform ikTarget; // The target where the foot aims to step
    public Transform footTip; // The tip of the foot
    public float moveThreshold = 0.8f; // Max distance for a step
    public float stepHeight = 0.8f; // How high the foot lifts when stepping
    public float rotationThreshold = 30f; // Degrees of rotation to trigger a step
    public float legSpeed = 4000f; // Speed of the leg movement
    public LayerMask groundLayer; // To identify the ground
    
    private List<Vector3> allHitPoints = new List<Vector3>(); // Store all hit points for drawing
    [HideInInspector]public Vector3 bestHitPoint{get;private set;}// Store the best hit point for drawing
    [HideInInspector]public bool isMoving{get;private set;} // Flag to check if the foot is currently moving
    [HideInInspector]public Vector3 targetPosition{get;private set;} // Store the calculated target position for drawing gizmos

    [HideInInspector]public Vector3 groundNormal{get;private set;}
    private Vector3 fixedPosition;
    private bool fixFoot;
    void Start() {
        isMoving = false;
        fixFoot=true;
        ikTarget.position = footTip.position; // Initialize the target position to the foot tip
        fixedPosition=ikTarget.position;
        spiderBody=GetComponentInParent<SpiderController>();
        groundNormal=transform.up;
        bestHitPoint = footTip.position; 
        //footTip=transform.Find("Root/Joint1/Joint2/Tip");
    }

    void Update() {
        //Step();
        targetPosition=CalculateTargetPosition();
        if(fixFoot)
        {
            ikTarget.position=fixedPosition;
        }
    }
    public void Step()
    {
        if(!isMoving)
        {
            if(Vector3.Distance(ikTarget.position,targetPosition)>moveThreshold)
            {
                isMoving=true;
                fixFoot=false;
                StartCoroutine(MoveFootToPosition(targetPosition));
            }

        }
    }
    public void ResetLeg()
    {
        ikTarget.position = footTip.position;
        fixedPosition=ikTarget.position;
        fixFoot=true;
        isMoving=false;
    }
    Vector3 CalculateTargetPosition() 
    {
        allHitPoints.Clear(); // Clear previous frame hit points
        bestHitPoint = footTip.position; // Default to current foot position
        float closestDistance = Mathf.Infinity; // Initialize with the highest possible value
            // Calculate directions for the 4 surrounding rays, 30 degrees tilted
        float tiltAngle = 30f; // Angle to tilt from the directly downward direction
        Vector3[] directions = new Vector3[5];
        Vector3 down=(-spiderBody.transform.up-groundNormal).normalized;
        directions[0] = Quaternion.AngleAxis(30, down)*(Quaternion.AngleAxis(tiltAngle, transform.right) * down); // Forward tilt
        directions[1] = Quaternion.AngleAxis(30, down)*(Quaternion.AngleAxis(-tiltAngle, transform.right) * down); // Backward tilt
        directions[2] = Quaternion.AngleAxis(30, down)*(Quaternion.AngleAxis(tiltAngle, transform.forward) * down); // Right tilt
        directions[3] = Quaternion.AngleAxis(30, down)*(Quaternion.AngleAxis(-tiltAngle, transform.forward) * down); // Left tilt
        directions[4] = down; // Directly downward
        Vector3 rayStart = transform.position + transform.forward * 2f+groundNormal*1.5f;
        // Cast the 4 tilted rays
        foreach (var dir in directions) {
            // Calculate the ray's starting point

            Debug.DrawLine(rayStart,rayStart+dir*5f,Color.green,0.1f);
            // Raycast
            if (Physics.Raycast(rayStart, dir, out RaycastHit hit, 10f, groundLayer)) {
                allHitPoints.Add(hit.point); // Add hit point to the list for drawing
                // Check if this hit is closer than the previous closest
                float distance = Vector3.Distance(rayStart, hit.point);//!
                if (distance < closestDistance) {
                    closestDistance = distance;
                    bestHitPoint = hit.point;
                    groundNormal=hit.normal;
                }
            }
        }
        // Optionally adjust for step height here if needed
        return bestHitPoint;
    }

    IEnumerator MoveFootToPosition(Vector3 initialTargetPosition) {
        Vector3 startPosition = ikTarget.position;
        float startTime = Time.time;
        float journeyLength = Vector3.Distance(startPosition, initialTargetPosition);

        while (Vector3.Distance(ikTarget.position, targetPosition) > 0.02f) {
            // Calculate the current time since the coroutine started
            float timeSinceStarted = Time.time - startTime;
            // Determine the fraction of the journey completed
            float fractionOfJourney = timeSinceStarted * (legSpeed*Time.deltaTime / journeyLength);

            // Lerp position without height
            Vector3 newPosition = Vector3.Lerp(startPosition, new Vector3(targetPosition.x, startPosition.y, targetPosition.z), fractionOfJourney);

            // Calculate height offset based on a parabola
            float heightDifference = targetPosition.y - startPosition.y;
            float parabolicHeight = Mathf.Sin(fractionOfJourney * Mathf.PI) * stepHeight;

            // Apply additional height based on the height difference between start and target, ensuring smooth ascent/descent
            float heightOffset = Mathf.Lerp(0, heightDifference, fractionOfJourney) + parabolicHeight;

            // Combine Lerp position with height offset
            ikTarget.position = new Vector3(newPosition.x, startPosition.y + heightOffset, newPosition.z);

            // Exit loop if the movement is nearly complete
            if (fractionOfJourney >= 1) break;

            yield return null;
        }

        // Directly set to the final position to ensure it exactly matches the target position
        ikTarget.position = targetPosition;
        fixedPosition=ikTarget.position;
        fixFoot=true;
        yield return new WaitForSeconds(0.05f); // Adjust the delay (0.5s here) as needed for realism
        isMoving = false;
    }
    // Draw Gizmos to visualize the target position
    void OnDrawGizmos() {

        // Draw unselected hit points as blue spheres
        Gizmos.color = Color.blue;
        foreach (Vector3 hitPoint in allHitPoints) {
            Gizmos.DrawWireSphere(hitPoint, 0.1f);
        }
 
        // Draw the best hit point as a red sphere
        if (bestHitPoint != Vector3.zero) { // Ensure there's a best hit point to draw
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(bestHitPoint, 0.1f);
        }
    }
}
