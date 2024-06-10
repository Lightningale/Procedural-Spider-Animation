using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SpiderController : MonoBehaviour
{
    protected Rigidbody rb;
    public float speed=1000f;
    public float rotationSpeed;
    private Vector3 moveDirection = Vector3.zero;
    public LegController[] legs; // Array of all leg controllers
    private List<int> legOrder; // List to keep track of the leg movement order
    public LayerMask groundLayer; // To identify the ground
    // Define groups of legs that move together in the tetrapod gait pattern
    private int[][] legGroups = new int[][] {
        new int[] { 1, 2}, 
        new int[] { 0, 3},
        new int[] { 0, 3, 4},
        new int[] { 1, 2, 5},
        new int[] { 2, 5, 6},
        new int[] { 3, 4, 7},
        new int[] { 4, 7},
        new int[] { 5, 6}
    }; 
    public float heightOffset = 1f; // Offset from the ground
    public float heightAdjustmentSpeed = 10f; // Speed of height adjustment
    public float rotationAdjustmentSpeed = 15f; // Speed of rotation adjustment
    Quaternion lastRotation;
    float rotationChangeThreshold = 5f; // Degrees; adjust as needed
    public bool AutoMode=false;
    private Vector3 centerPoint = new Vector3(0,0.75f,0);
    // Start is called before the first frame update
    void Start()
    {
        rb=gameObject.GetComponent<Rigidbody>();
        lastRotation = transform.rotation;
        legOrder = new List<int>();
        for (int i = 0; i < legs.Length; i++)
        {
            legOrder.Add(i);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(AutoMode)
        {
            MoveInCircle();
        }
        else{
            moveDirection.x=Input.GetAxis("Horizontal");
            moveDirection.z=Input.GetAxis("Vertical");
            moveDirection.y=0;
            if(moveDirection.z!=0)
            {
                rb.velocity=transform.forward*moveDirection.z*speed*Time.deltaTime;
            }
            transform.Rotate(0,moveDirection.x*rotationSpeed*Time.deltaTime,0);
        }
        GaitWalk();
        AdjustSpiderBody();
    }
    public Vector3 getMoveDirection() 
    {
        return moveDirection;
    }
    void MoveInCircle()
    {
        Vector3 directionToCenter = (centerPoint - transform.position).normalized;
        // Calculate a clockwise perpendicular direction
        moveDirection = Vector3.Cross(directionToCenter, Vector3.up).normalized;
        
        // Apply movement
        rb.velocity = moveDirection * speed*0.4f*Time.deltaTime;

        // Optional: Rotate the spider to face the direction of movement
        Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
    }
    void GaitWalk() {
       for(int i=0;i<legs.Length;i++)
        {
            if (clearToMove(legGroups[i])) 
            {
                legs[i].Step();
            }

        }          
    }
    
    bool clearToMove(int[] group) {
        foreach (var legIndex in group) {
            if (legs[legIndex].isMoving) {
                return false;
            }
        }
        return true;
    }
    void AdjustSpiderBody()
    {
        Vector3 averagePosition = Vector3.zero;
        Vector3 averageNormal = Vector3.zero;
        int count = 0;

        foreach (LegController leg in legs)
        {
            if (leg.footTip != null)
            {
                averagePosition += leg.bestHitPoint;   
                averageNormal += leg.groundNormal;
                count++;
            }
        }

        if (count > 0)
        {
            averagePosition /= count;
            averageNormal /= count;
            // Adjust Rotation
            AdjustRotation(averageNormal);
            // Adjust Height
            AdjustHeight(averagePosition);

            
        }
        Debug.DrawLine(averagePosition, averagePosition + averageNormal*heightOffset, Color.red);
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -transform.up, out hit, Mathf.Infinity, groundLayer))
        {
            Debug.DrawLine(hit.point,hit.point+transform.up*heightOffset, Color.blue);
        }
        //Debug.DrawLine(transform.position, transform.position + transform.up*2, Color.green);
    }
    void AdjustHeight(Vector3 averagePosition)
    {
        /*
        // Determine the target height based on the average foot tip position
        // Here, you might want to define a desired offset from the ground
        Vector3 targetPosition = averagePosition-transform.forward*0.29f + averageNormal * heightOffset;
        Vector3 newPosition = transform.position + (targetPosition - transform.position).normalized * heightAdjustmentSpeed * Time.deltaTime;

        // Smoothly interpolate to the new position
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * heightAdjustmentSpeed);*/
        Vector3 targetPosition = transform.position;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -transform.up, out hit, Mathf.Infinity, groundLayer))
        {
            //
            float bodyToFootAverageDistance=Vector3.Dot(averagePosition+transform.up*heightOffset-transform.position,transform.up);
            if(Math.Abs(bodyToFootAverageDistance)>0.01f)
            {
                targetPosition=transform.position + transform.up * bodyToFootAverageDistance;
            }


            //
            float distanceToGround = hit.distance;
            float groundAdjustmentDistance=heightOffset-distanceToGround;
            if (groundAdjustmentDistance > 0.01f)
            {
                float adjustmentAmount = heightOffset - distanceToGround;
                targetPosition = transform.position + transform.up * adjustmentAmount;
            }
            /*
            float footAverageDistance=Vector3.Dot(averagePosition-hit.point,transform.up)-Vector3.Dot(targetPosition-hit.point,transform.up);
            
            if(footAverageDistance>0.01f)
            {
                targetPosition=transform.position + transform.up * footAverageDistance;
            }*/


            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * heightAdjustmentSpeed);
        }
    }
    void AdjustRotation(Vector3 averageNormal)
    {
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, averageNormal) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationAdjustmentSpeed);
    }
    public void ResetLegs()
    {
        foreach (LegController leg in legs)
        {
            leg.ResetLeg();
        }
    }
    /*
    void AdjustRotation() 
    {
        float smoothingFactor = 0.1f;
        // Assuming legs are paired front-to-back along the spider's body (0 & 1 are front, 6 & 7 are back, etc.)
        // Calculate the average height of front and back legs as an example
        float frontLegsHeight = (legs[0].footTip.position.y + legs[1].footTip.position.y + legs[2].footTip.position.y + legs[3].footTip.position.y) / 4;
        float backLegsHeight = (legs[6].footTip.position.y + legs[7].footTip.position.y + legs[4].footTip.position.y + legs[5].footTip.position.y) / 4;

        // Determine the pitch based on the height difference between front and back legs
        float heightDifference = backLegsHeight - frontLegsHeight;
        // Assuming the distance between the sets of legs is known or can be measured
        float distanceBetweenLegsSets = Vector3.Distance(legs[0].footTip.position, legs[6].footTip.position);
        float pitchAngle = Mathf.Atan2(heightDifference, distanceBetweenLegsSets) * Mathf.Rad2Deg;

        // Calculate the average height of left and right legs for roll adjustment
        float leftLegsHeight = (legs[0].footTip.position.y + legs[2].footTip.position.y + legs[4].footTip.position.y + legs[6].footTip.position.y) / 4;
        float rightLegsHeight = (legs[1].footTip.position.y + legs[3].footTip.position.y + legs[5].footTip.position.y + legs[7].footTip.position.y) / 4;

        // Determine the roll based on the height difference between left and right legs
        heightDifference = rightLegsHeight - leftLegsHeight;
        // Assuming the distance between the sides of legs is known or can be measured
        float distanceBetweenLegsSides = Vector3.Distance(legs[0].footTip.position, legs[1].footTip.position); // Example for one pair
        float rollAngle = Mathf.Atan2(heightDifference, distanceBetweenLegsSides) * Mathf.Rad2Deg;

        // Create a target rotation from the calculated pitch and roll angles
        Quaternion targetRotation = Quaternion.Euler(pitchAngle, transform.eulerAngles.y, rollAngle);
        if (Quaternion.Angle(transform.rotation, targetRotation) > rotationChangeThreshold) {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationAdjustmentSpeed);
        }
    // Update lastRotation for the next frame
    }*/
    
}
