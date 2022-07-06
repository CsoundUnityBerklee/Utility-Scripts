using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//TODO
    //Make Random Value Channel an array

/// <summary>
/// Provides methods to send data in the CsoundChannelDataSO and CsoundScoreEventSO scriptable object assets into Csound.
/// </summary>
public class CsoundSender : MonoBehaviour
{

    [Header("REFERENCES")]
        [Tooltip("Reference to the CsoundUnity component to send information to. Will automatically get the component attached to the same object if left empty.")]
        public CsoundUnity csoundUnity;
        [Tooltip("Assign this field if you want to take the physics/transform data from another game object. Leave blank to use this same object for the physics/transform data.")]
        public GameObject gObject;

    [Header("INSTRUMENT PRESETS")]
        [Tooltip("Array containing instrument presets to be set by calling the SetPreset function")]
        public List<CsoundChannelDataSO> presetList = new List<CsoundChannelDataSO>();
        [Tooltip("If true, sets the defined preset value on start")]
        [SerializeField] private bool setPresetOnStart;
        [Tooltip("Defined which preset to be set on start")]
        [SerializeField] private int presetIndexOnStart;
        private int currentPresetIndex;

    [Header("SCORE EVENTS")]
        [SerializeField] private CsoundScoreEventSO[] scoreEvents;
        [SerializeField] private bool sendScoreEventOnStart;
        [SerializeField] public int scoreEventIndexOnStart;
        private int currentScoreEvent;

    [Header("INSTRUMENT TRIGGER")]
        [Tooltip("Defines the channel name that is used to start and stop the Csound instrument")]
        [SerializeField] private string triggerChannelName;
        [Tooltip("If true, sets the trigger channel to a value of 1")]
        [SerializeField] private bool triggerOnStart = false;
        private int triggerValue = 0;

    [Header("SET CHANNELS RANDOM VALUE")]
        [Tooltip("Array of channels that can be set to random values either on start or by calling the SetChannelsToRandomValue function.")]
        public CsoundChannelDataSO randomValueChannels;
        [Tooltip("If true, ignores the randomValueChannels field and uses the current preset minValues and maxValues to generate a random value instead.")]
        public bool useCurrentPresetRandomValues = false;
        [SerializeField] private bool setChannelRandomValuesOnStart = false;

    [Header("DEBUG")]
        [Tooltip("Prints channel names and values when calling SetPreset.")]
        [SerializeField] private bool debugPresets = false;
        [Tooltip("Prints trigger channel value when calling ToggleTrigger and SetTrigger.")]
        [SerializeField] private bool debugTrigger = false;
        [Tooltip("Prints channel names and values when calling SetChannelsToRandomValue.")]
        [SerializeField] private bool debugSetRandomChannels = false;

    #region UNITY LIFE CYCLE
    private void Awake()
    {
        if (gObject == null)
            gObject = gameObject;


        //Gets the CsoundUnity component attached to the object.
        if (csoundUnity == null)
        {
            csoundUnity = GetComponent<CsoundUnity>();

            if (csoundUnity == null)
                Debug.LogError("No CsoundUnity component attached to " + gameObject.name);
        }
    }

    void Start()
    {
        //Calls SetPreset if setPresetOnStart is true.
        if (setPresetOnStart)
            SetPreset(presetIndexOnStart);

        if (sendScoreEventOnStart)
            SendScoreEvent(scoreEventIndexOnStart);

        //Set defined trigger channel to 1 if triggerOnStart is true.
        if (triggerOnStart)
            SetTrigger(1);

        //Calls SetChannelsToRandomValue if setChannelRandomValuesOnStart is true.
        if (setChannelRandomValuesOnStart)
            SetChannelsToRandomValue();
    }

    #endregion

    #region PRESETS
    /// <summary>
    /// Set channel fixed values to Csound by using an index from the preset list.
    /// </summary>
    /// <param name="index"></param>
    public void SetPreset(int index)
    {
        if (debugPresets)
            Debug.Log("CSOUND " + gameObject.name + " set preset: " + presetList[index]);

        foreach (CsoundChannelDataSO.CsoundChannelData channelData in presetList[index].channelData)
        {
            //Passes values to Csound.
            csoundUnity.SetChannel(channelData.name, channelData.fixedValue);

            if (debugPresets)
                Debug.Log("CSOUND " + gameObject.name + " set preset: " + channelData.name + " , " + channelData.fixedValue);
        }

        //Set current preset index to the passed in index.
        currentPresetIndex = index;

    }

    /// <summary>
    /// Adds a channel data element to the preset list and sets its fixed values to the Csound instrument
    /// </summary>
    /// <param name="channelData"></param>
    public void SetPreset(CsoundChannelDataSO channelData)
    {
        //Adds new channel data to the preset list as the last item.
        presetList.Add(channelData);
        //Calls SetPreset passing in the last item as the index.
        SetPreset(presetList.Count - 1);
    }

    #endregion

    #region TRIGGER
    /// <summary>
    /// Toggles the defined Csound trigger channel value between 0 and 1.
    /// </summary>
    public void ToggleTrigger()
    {
        //Toggles the value of the trigger chanel between 0 and 1.
        triggerValue = 1 - triggerValue;
        //Passes value to Csound.
        csoundUnity.SetChannel(triggerChannelName, triggerValue);

        if(debugTrigger)
            Debug.Log("CSOUND " + gameObject.name + " trigger: " + triggerValue);
    }

    /// <summary>
    /// Passes in a trigger channel and its current value as parameters to toggle it between 0 and 1.
    /// </summary>
    /// <param name="channelName"></param>
    /// <param name="value"></param>
    public void SetTrigger(string channelName, int value)
    {
        //Toggle the passed in value between 1 and 0.
        int toggledValue = 1 - value;
        //Passes value to Csound.
        csoundUnity.SetChannel(channelName, toggledValue);

        if (debugTrigger)
            Debug.Log("CSOUND " + gameObject.name + " trigger: " + channelName + " , " + toggledValue);
    }

    /// <summary>
    /// Passes the parameter value to the defined Csound trigger channel.
    /// </summary>
    public void SetTrigger(int value)
    {
        //Sets the trigger channel value to the value passed as an argument.
        triggerValue = value;
        //Passes value to Csound.
        csoundUnity.SetChannel(triggerChannelName, triggerValue);

        if (debugTrigger)
            Debug.Log("CSOUND " + gameObject.name + " csound trigger: " + triggerValue);
    }
    #endregion

    #region SCORE EVENTS

    public void SendScoreEvent(int index)
    {
        currentScoreEvent = index;
        csoundUnity.SendScoreEvent(scoreEvents[currentScoreEvent].ConcatenateScoreEventString());
    }

    public void SendScoreEvent(string scoreEvent)
    {
        csoundUnity.SendScoreEvent(scoreEvent);
    }

    public void SendScoreEvent(string scorechar, string instrument, float delay, float duration)
    {

        csoundUnity.SendScoreEvent(scorechar + " " + instrument + " " + delay + " " + duration);
    }

    public void SendScoreEvent(string scorechar, string instrument, float delay, float duration, float[] extraPFields)
    {
        string concatenatedPFields = string.Join(" ", extraPFields);

        csoundUnity.SendScoreEvent(scorechar + " " + instrument + " " + delay + " " + duration + " " + concatenatedPFields);
    }

    #endregion

    #region SET CHANNEL FIXED VALUE
    /// <summary>
    /// Pass in a value to assign to a Csound channel.
    /// </summary>
    /// <param name="channelName"></param>
    /// <param name="channelValue"></param>
    public void SetChannelToFixedValue(string channelName, float channelValue)
    {
        //Passes values to Csound.
        csoundUnity.SetChannel(channelName, channelValue);

        if (debugSetRandomChannels)
            Debug.Log("CSOUND " + gameObject.name + " set channel: " + channelName + " , " + channelValue);
    }

    /// <summary>
    /// Set an array of Csound channels to the same value.
    /// </summary>
    /// <param name="channelNames"></param>
    /// <param name="channelValue"></param>
    public void SetChannelToFixedValue(string[] channelNames, float channelValue)
    {
        //Passes values to Csound.
        foreach(string name in channelNames)
        {
            csoundUnity.SetChannel(name, channelValue);

            if (debugSetRandomChannels)
                Debug.Log("CSOUND " + gameObject.name + " set channel: " + name + " , " + channelValue);
        }

    }
    #endregion

    #region SET CHANNELS RANDOM VALUE
    /// <summary>
    /// Takes a random number between the minValue and maxValue fileds for each element of the randomValueChannels scriptable object and assigns it to their Csound channel.
    /// </summary>
    public void SetChannelsToRandomValue()
    {
        if (!useCurrentPresetRandomValues)
        {
            for (int i = 0; i < randomValueChannels.channelData.Length; i++)
            {
                //Get the random value from the scriptable object and passes it to Csound.
                csoundUnity.SetChannel(randomValueChannels.channelData[i].name, randomValueChannels.GetRandomValue(i, debugSetRandomChannels));
            }
        }
        else
        {
            for (int i = 0; i < presetList[currentPresetIndex].channelData.Length; i++)
            {
                //Get the random value from the scriptable object and passes it to Csound.
                csoundUnity.SetChannel(presetList[currentPresetIndex].channelData[i].name, presetList[currentPresetIndex].GetRandomValue(i, debugSetRandomChannels));
            }
        }
    }

    /// <summary>
    /// Takes a random number between the minValue and maxValue fileds for each element of the randomValueChannels scriptable object and assigns it to their Csound channel.
    /// </summary>
    public void SetChannelsToRandomValue(CsoundChannelDataSO newChannelData)
    {
        for (int i = 0; i < newChannelData.channelData.Length; i++)
        {
            //Get the random value from the scriptable object and passes it to Csound.
            csoundUnity.SetChannel(randomValueChannels.channelData[i].name, randomValueChannels.GetRandomValue(i, debugSetRandomChannels));
        }
    }

    /// <summary>
    /// Generates a random value between a minimum and maximum range and assigns that to a Csound channel.
    /// </summary>
    /// <param name="channelName"></param>
    /// <param name="minValue"></param>
    /// <param name="maxValue"></param>
    public void SetChannelsToRandomValue(string channelName, float minValue, float maxValue)
    {
        //Generates random value.
        float randomValue = Random.Range(minValue, maxValue);
        //Passes value to Csound.
        csoundUnity.SetChannel(channelName, randomValue);

        if (debugSetRandomChannels)
            Debug.Log(gameObject.name + " channel value: " + channelName + " , " + randomValue);
    }

    /// <summary>
    /// Passes an array of string as channel names and generates an unique rnadom value for each channel within the same min and max range.
    /// </summary>
    /// <param name="channelNames"></param>
    /// <param name="minValue"></param>
    /// <param name="maxValue"></param>
    public void SetChannelsToRandomValue(string[] channelNames, float minValue, float maxValue)
    {
        //Passes value to Csound and generates a random value for each individual channel.
        foreach(string name in channelNames)
        {
            float randomValue = Random.Range(minValue, maxValue);
            csoundUnity.SetChannel(name, randomValue);

            if (debugSetRandomChannels)
                Debug.Log(gameObject.name + " channel value: " + name + " , " + randomValue);
        }
    }
    #endregion
}