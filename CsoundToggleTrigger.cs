using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CsoundToggleTrigger : MonoBehaviour
{
    public CsoundUnity csound;
    public string triggerChannelName;
    public bool triggerOnStart = false;
    private int triggerValue = 0;

    public void Start()
    {
        if (csound == null)
            csound = GetComponent<CsoundUnity>();

        if (triggerOnStart)
            Trigger();
    }

    public void Trigger()
    {
        triggerValue = 1 - triggerValue;
        csound.SetChannel(triggerChannelName, triggerValue);

        Debug.Log(gameObject + " csound trigger");
    }
}
