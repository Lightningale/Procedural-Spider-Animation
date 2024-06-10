using System;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class FABRIK : MonoBehaviour
{

    

    public Transform target;
    public Transform pole;

    public int chainSize = 3;

    public static int iterations = 10;

    [Range(0, 1)]
    public static float snappingFactor = 1f;
    public static float dt = 0.001f;

    private int poleSize = 1;


    private float[] segmentLengths; //Target to Origin
    private float totalLength;
    [SerializeField]private Transform[] bones;
    [SerializeField]private Vector3[] positions;
    private Vector3[] initialDirections;

    private float threshhold;
    private Quaternion[] initialRotations;
    private Quaternion initialTargetRotation;
    private Transform root;


    // Start is called before the first frame update
    void Awake()
    {
        Init();
    }

    void Init()
    {
        segmentLengths = new float[chainSize];
        bones = new Transform[chainSize + 1];
        positions = new Vector3[chainSize + 1];
        initialRotations = new Quaternion[chainSize + 1];
        initialDirections = new Vector3[chainSize + 1];
        
        //locate chain
        root = transform;
        for (int i = 0; i <= chainSize; ++i)
        {
            root = root.parent;
        }

        initialTargetRotation = LocalRotation(target);

        Transform current = transform;
        totalLength = 0;
        Vector3 currentPosition = LocalPosition(current);
        Vector3 nextPosition;

        for (int i = bones.Length - 1; i > -1; --i)
        {
            initialRotations[i] = LocalRotation(current);
            bones[i] = current;
            

            if (i == bones.Length - 1)
            {
                // Leaf bone: direction towards the target
                initialDirections[i] = LocalPosition(target) - currentPosition;
            }
            else
            {
                // Cache the next position to avoid multiple root space calculations
                nextPosition = LocalPosition(bones[i + 1]);
                initialDirections[i] = nextPosition - currentPosition;
                segmentLengths[i] = initialDirections[i].magnitude;
                totalLength += segmentLengths[i];
            }

            // Update current to the parent for the next iteration
            current = current.parent;
            if (current != null && i != 0)
            {
                currentPosition = LocalPosition(current);
            }
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        IKSolver();
    }
    public void setChainLength(int chainSize)
    {
        chainSize=Math.Clamp(chainSize,2,10);
        this.chainSize=chainSize;
    }
    public static void setIterations(int iterations)
    {
        iterations=Math.Clamp(iterations,1,1000);
        FABRIK.iterations=iterations;
    }
    public static void setDelta(float dt)
    {
        FABRIK.dt=dt;
    }
    public static void setSnapBackStrength(float snappingFactor)
    {
        snappingFactor=Math.Clamp(snappingFactor,0,1);
        FABRIK.snappingFactor=snappingFactor;
    }

    private void IKSolver()
    {
        //get position

        Quaternion targetRotation = LocalRotation(target);
        Vector3 targetPosition = LocalPosition(target);

        for (int i = 0; i < bones.Length; i++)
            positions[i] = LocalPosition(bones[i]);

        //If can't reach target, stretch chain
        if (Vector3.Distance(targetPosition,positions[0]) >= totalLength)
        {
            Vector3 direction = (targetPosition - positions[0]).normalized;
            for (int i = 1; i < positions.Length; i++)
                positions[i] = positions[i - 1] + direction * segmentLengths[i - 1];
        }
        else
        {
            // Initially adjust the positions towards the start direction with the given snap back strength
            int k = 0;
            for (int i = 0; i < positions.Length - 1; ++i) {
                positions[i + 1] = Vector3.Lerp(positions[i + 1], positions[i] + initialDirections[i], snappingFactor);
            }
            // Perform iterations of the FABRIK algorithm
            while( k < iterations) 
            {
                if (Vector3.Distance(positions[positions.Length - 1], targetPosition) < dt) 
                    break;
                // Backward pass: Adjust from end to start
                for (int i = positions.Length - 1; i >= 1; --i) {
                    if (i == positions.Length - 1) {
                        // Set end effector directly to the target
                        positions[i] = targetPosition;
                    } 
                    else
                    {
                        // Set each position to maintain the bone length from the next position
                        Vector3 direction = (positions[i] - positions[i + 1]).normalized;
                        positions[i] = positions[i + 1] + direction * segmentLengths[i];
                    }
                }
                // Forward pass: Adjust from start to end
                for (int i = 1; i < positions.Length; ++i) 
                {
                    Vector3 direction = (positions[i] - positions[i - 1]).normalized;
                    positions[i] = positions[i - 1] + direction * segmentLengths[i - 1];
                }
                k++;
                if(k==iterations&&pole)
                {
                    Vector3 polePosition = LocalPosition(pole);

                    for (int i = 1; i < positions.Length - 1; ++i)
                    {
                        Vector3 rootToNext = positions[i + 1] - positions[i - 1];
                        Plane segmentPlane = new Plane(rootToNext, positions[i - 1]);
                        Vector3 projectedBone = segmentPlane.ClosestPointOnPlane(positions[i]);
                        Vector3 projectedPole = segmentPlane.ClosestPointOnPlane(polePosition);

                        Vector3 toProjectedBone = projectedBone - positions[i - 1];
                        Vector3 toProjectedPole = projectedPole - positions[i - 1];
                        float angle = Vector3.SignedAngle(toProjectedBone, toProjectedPole, segmentPlane.normal);

                        positions[i] = Quaternion.AngleAxis(angle, segmentPlane.normal) * (positions[i] - positions[i - 1]) + positions[i - 1];
                    }
                }
            }
        }
        int j=0;
        while(j < positions.Length)
        {
            Quaternion newRotation;
            if (j == positions.Length - 1)
            {
                newRotation = Quaternion.Inverse(targetRotation) * initialTargetRotation * Quaternion.Inverse(initialRotations[j]);
            }
            else
            {
                Vector3 directionToNextBone = positions[j + 1] - positions[j];
                newRotation = Quaternion.FromToRotation(initialDirections[j], directionToNextBone) * Quaternion.Inverse(initialRotations[j]);
            }

            applyRotation(bones[j], newRotation);
            applyPosition(bones[j], positions[j]);
            j++;
        }
    }
    private Quaternion LocalRotation(Transform current)
    {
        return root ? Quaternion.Inverse(current.rotation) * root.rotation : current.rotation;
    }

    private Vector3 LocalPosition(Transform current)
    {
        return root ? Quaternion.Inverse(root.rotation) * (current.position - root.position) : current.position;
    }
    private void applyRotation(Transform current, Quaternion rotation)
    {
        current.rotation = root ? root.rotation * rotation : rotation;
    }
    private void applyPosition(Transform current, Vector3 position)
    {

        current.position = root ? root.rotation * position + root.position : position;
    }



}
