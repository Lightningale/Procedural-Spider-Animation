using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class IKParameters : MonoBehaviour
{
    [Range(0, 1000)]
    public int iterations = 10; 
    private int lastIterations;
    [Range(0, 0.1f)]
    public float delta = 0.01f;
    private float lastDelta;
    [Range(0, 1)]
    public float snapBackStrength = 1f;
    private float lastSnapBackStrength;
    // Start is called before the first frame update
    void Start()
    {
        lastIterations = iterations;
        lastDelta = delta;
        lastSnapBackStrength = snapBackStrength;
        
    }
    void changeParameters()
    {
        if(lastIterations != iterations)
        {
            FABRIK.setIterations(iterations);
            lastIterations = iterations;
        }
        if(lastDelta != delta)
        {
            FABRIK.setDelta(delta);
            lastDelta = delta;
        }
        if(lastSnapBackStrength != snapBackStrength)
        {
            FABRIK.setSnapBackStrength(snapBackStrength);
            lastSnapBackStrength = snapBackStrength;
        }
    }
    // Update is called once per frame
    void Update()
    {
        changeParameters();
    }
}
