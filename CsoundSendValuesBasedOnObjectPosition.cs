using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CsoundSendValuesBasedOnObjectPosition : MonoBehaviour
{
    public enum PositionReference { Absolute, Relative };

    public bool debug;
    [Space]
    [Header("VECTOR RANGES")]
    [SerializeField] private Vector3 vectorRanges;
    [SerializeField] private PositionReference setXPositionTo = PositionReference.Relative;
    [SerializeField] private PositionReference setYPositionTo = PositionReference.Absolute;
    [SerializeField] private PositionReference setZPositionTo = PositionReference.Relative;
    [SerializeField] private bool updatePositionOnStart = false;
    [Space]
    [Header("CSOUND CHANNELS")]
    [SerializeField] private CsoundUnity csound;
    [SerializeField] private CsoundChannelValueBasedOnPosition[] csoundChannelsX;
    [SerializeField] private CsoundChannelValueBasedOnPosition[] csoundChannelsY;
    [SerializeField] private CsoundChannelValueBasedOnPosition[] csoundChannelsZ;

    //Position references
    private Transform camera;
    private Vector3 startPos;
    private Vector3 relativePos;
    private bool updatePosition = false;

    private void Awake()
    {
        camera = Camera.main.transform;

        if (csound == null)
            csound = GetComponent<CsoundUnity>();
    }

    private void Start()
    {
        if (updatePositionOnStart)
            updatePosition = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!updatePosition) { return; }

        CaculateRelativePos();
        SetCsoundValuesY();
        SetCsoundValuesX();
        SetCsoundValuesZ();
    }

    #region Relative Position Calculation
    public void UpdatePosition()
    {
        startPos = camera.transform.InverseTransformPoint(transform.position);
        updatePosition = true;
    }

    private void CaculateRelativePos()
    {
        Vector3 inverseTransform = camera.transform.InverseTransformPoint(transform.position);

        relativePos.x = inverseTransform.x - startPos.x;
        relativePos.y = inverseTransform.y - startPos.y;
        relativePos.z = inverseTransform.z - startPos.z;
    }

    public void StopUpdatingPosition()
    {
        updatePosition = false;
    }
    #endregion

    #region Csound Communication
    private void SetCsoundChannelBasedOnPosition(CsoundChannelValueBasedOnPosition[] csoundChannels, float minVectorRange, float maxVectorRange, float transformAxis)
    {
        foreach (CsoundChannelValueBasedOnPosition data in csoundChannels)
        {
            float value =
                Mathf.Clamp(ScaleFloat(minVectorRange, maxVectorRange, data.minValue, data.maxValue, transformAxis), data.minValue, data.maxValue);

            if (!data.returnAbsoluteValue)
            {
                csound.SetChannel(data.channelName, value);
            }
            else
            {
                csound.SetChannel(data.channelName, Mathf.Abs(value));
            }

            if (debug)
                Debug.Log(data.channelName + ": " + value);
        }
    }

    private void SetCsoundValuesX()
    {
        if (setXPositionTo == PositionReference.Absolute)
            SetCsoundChannelBasedOnPosition(csoundChannelsX, -vectorRanges.x, vectorRanges.x, transform.position.x);
        else
            SetCsoundChannelBasedOnPosition(csoundChannelsX, -vectorRanges.x, vectorRanges.x, relativePos.x);
    }

    private void SetCsoundValuesY()
    {
        if(setYPositionTo == PositionReference.Absolute)
            SetCsoundChannelBasedOnPosition(csoundChannelsY, 0, vectorRanges.y, transform.position.y);
        else
            SetCsoundChannelBasedOnPosition(csoundChannelsY, -vectorRanges.y, vectorRanges.y, relativePos.y);
    }

    private void SetCsoundValuesZ()
    {
        if (setZPositionTo == PositionReference.Absolute)
            SetCsoundChannelBasedOnPosition(csoundChannelsZ, -vectorRanges.z, vectorRanges.z, transform.position.z);
        else
            SetCsoundChannelBasedOnPosition(csoundChannelsZ, -vectorRanges.z, vectorRanges.z, relativePos.z);
    }
    #endregion

    private float ScaleFloat(float OldMin, float OldMax, float NewMin, float NewMax, float OldValue)
    {
        float OldRange = (OldMax - OldMin);
        float NewRange = (NewMax - NewMin);
        float NewValue = (((OldValue - OldMin) * NewRange) / OldRange) + NewMin;

        return (NewValue);
    }
}

[System.Serializable]
public class CsoundChannelValueBasedOnPosition
{
    public string channelName;
    public float minValue;
    public float maxValue;
    public bool returnAbsoluteValue = false;
}