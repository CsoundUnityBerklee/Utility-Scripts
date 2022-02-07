using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CsoundSetChannels : MonoBehaviour
{
    [SerializeField] private CsoundUnity csound;
    [SerializeField] private ChannelData[] channelData;
    [SerializeField] private bool setChannelsOnStart;

    // Start is called before the first frame update
    void Start()
    {
        if (csound == null)
            csound = GetComponent<CsoundUnity>();

        if (setChannelsOnStart)
            SetChannels();
    }

    public void SetChannels()
    {
        foreach(ChannelData data in channelData)
        {
            if (!data.setRandom)
                csound.SetChannel(data.channelName, data.value);
            else
                csound.SetChannel(data.channelName, Random.Range(data.minRandomValue, data.maxRandomValue));
        }
    }
}

[System.Serializable]
public class ChannelData
{
    public string channelName;
    public float value;
    public bool setRandom;
    public float minRandomValue;
    public float maxRandomValue;
}