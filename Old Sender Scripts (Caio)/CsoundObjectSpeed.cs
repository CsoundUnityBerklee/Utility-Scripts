using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CsoundObjectSpeed : MonoBehaviour
{
    public enum VelocityMethod { Rigidbody, Transform };

    [Header("CSOUND")]
    [SerializeField] private CsoundUnity csound;
    [SerializeField] private ChannelSpeedData[] channelData;

    [Header("VELOCITY")]
    [SerializeField] private VelocityMethod velocityMethod = VelocityMethod.Rigidbody;
    public float maxSpeedValue;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private bool runOnStart;
    [SerializeField] private bool debugSpeed = false;

    private float speed;
    private Vector3 previousPos;
    private bool update;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (csound == null)
            csound = GetComponent<CsoundUnity>();
    }

    private void Start()
    {
        if (runOnStart)
            Run();
    }

    // Update is called once per frame
    void Update()
    {
        if(!update) { return; }
        SendCsoundDataBasedOnSpeed();
    }

    private void SendCsoundDataBasedOnSpeed()
    {
        if (velocityMethod == VelocityMethod.Rigidbody)
        {
            speed = rb.velocity.magnitude;
        }
        else if (velocityMethod == VelocityMethod.Transform)
        {
            speed = ((transform.position - previousPos).magnitude) / Time.deltaTime;
            previousPos = transform.position;
        }

        foreach (ChannelSpeedData data in channelData)
        {
            float value =
                Mathf.Clamp(ScaleFloat(0, maxSpeedValue, data.minValue, data.maxValue, rb.velocity.magnitude), data.minValue, data.maxValue);

            csound.SetChannel(data.channelName, value);
        }

        if (debugSpeed)
            print(speed);
    }

    public void Run()
    {
        update = true;
    }

    public void Stop()
    {
        update = false;
    }
    private float ScaleFloat(float OldMin, float OldMax, float NewMin, float NewMax, float OldValue)
    {
        float OldRange = (OldMax - OldMin);
        float NewRange = (NewMax - NewMin);
        float NewValue = (((OldValue - OldMin) * NewRange) / OldRange) + NewMin;

        return (NewValue);
    }
}

[System.Serializable]
public class ChannelSpeedData
{
    public string channelName;
    public float minValue, maxValue;
}