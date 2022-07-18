using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides methods to send data in the CsoundChannelDataSO and CsoundScoreEventSO assets through a CsoundUnity component.
/// </summary>
public class CsoundSender : MonoBehaviour
{

    [Header("REFERENCES")]
        [Tooltip("Reference to the CsoundUnity component to send information to. Will automatically get the component attached to the same object if left empty.")]
        public CsoundUnity csoundUnity;

    [Header("INSTRUMENT PRESETS")]
        [Tooltip("Array containing ChannelData asssets to be used as instrument presets")]
        [SerializeField] private List<CsoundChannelDataSO> presetList = new List<CsoundChannelDataSO>();
        [Tooltip("Defined which preset to be set on start")]
        [SerializeField] private int presetIndexOnStart;
        [Tooltip("If true, sets the defined preset value on start")]
        [SerializeField] private bool setPresetOnStart;

        [HideInInspector] public int presetCurrentIndex = 0;

    [Header("SCORE EVENTS")]
        [Tooltip("Array containing ScoreEvent asssets")]
        [SerializeField] private List <CsoundScoreEventSO> scoreEvents = new List<CsoundScoreEventSO>();
        [Tooltip("Defined which score event to send on start")]
        [SerializeField] private bool sendScoreEventOnStart;
        [Tooltip("If true, sends the defined score event on start")]
        [SerializeField] public int scoreEventIndexOnStart;

        [HideInInspector] public int scoreEventCurrentIndex = 0;

    [Header("INSTRUMENT TRIGGER")]
        [Tooltip("Defines the channel name that is used to start and stop the Csound instrument")]
        [SerializeField] private string triggerChannelName;
        [Tooltip("If true, sets the trigger channel to a value of 1")]
        [SerializeField] private bool triggerOnStart = false;

        private int triggerValue = 0;

    [Header("SET CHANNELS TO RANDOM VALUES")]
        [Tooltip("Array of ChannelData assets to be used to randomize channel values.")]
        public CsoundChannelDataSO[] randomValueChannels;
        [SerializeField] private int randomValueIndexOnStart;
        [Tooltip("If true, ignores the randomValueChannels field and uses the current preset minValues and maxValues to generate random values instead.")]
        public bool useCurrentPresetRandomValues = false;
        [SerializeField] private bool setChannelRandomValuesOnStart = false;

        [HideInInspector] public int randomValueCurrentIndex = 0;

    [Header("DEBUG")]
        [Tooltip("Prints channel names and values when changing presets.")]
        [SerializeField] private bool debugPresets = false;
        [Tooltip("Prints score events.")]
        [SerializeField] private bool debugScoreEvents = false;
        [Tooltip("Prints trigger channel values.")]
        [SerializeField] private bool debugTrigger = false;
        [Tooltip("Prints channel names and values when randomizing values.")]
        [SerializeField] private bool debugSetRandomChannels = false;

    #region UNITY LIFE CYCLE
    private void Awake()
    {
        //Gets the CsoundUnity component attached to the object if the inspector field is empty.
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

        //Call SendCoreEvent is scoreEventIndexOnStart istrue.
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
    /// Reset values for the currently indexed ChannelData preset.
    /// </summary>
    /// <param name="index"></param>
    public void ResetPreset()
    {
        if (debugPresets)
            Debug.Log("CSOUND " + gameObject.name + " set preset: " + presetList[presetCurrentIndex]);

        //Passes each channel fixed value to Csound.
        foreach (CsoundChannelDataSO.CsoundChannelData channelData in presetList[presetCurrentIndex].channelData)
        {
            csoundUnity.SetChannel(channelData.name, channelData.fixedValue);

            if (debugPresets)
                Debug.Log("CSOUND " + gameObject.name + " set preset: " + channelData.name + " , " + channelData.fixedValue);
        }
    }

    /// <summary>
    /// Uses the indexed ChannelData asset fixed values to set instrument presets.
    /// </summary>
    /// <param name="index"></param>
    public void SetPreset(int index)
    {
        if (debugPresets)
            Debug.Log("CSOUND " + gameObject.name + " set preset: " + presetList[index]);

        //Passes each channel fixed value to Csound.
        foreach (CsoundChannelDataSO.CsoundChannelData channelData in presetList[index].channelData)
        {
            csoundUnity.SetChannel(channelData.name, channelData.fixedValue);

            if (debugPresets)
                Debug.Log("CSOUND " + gameObject.name + " set preset: " + channelData.name + " , " + channelData.fixedValue);
        }

        //Set current preset index to the passed index.
        presetCurrentIndex = index;
    }

    /// <summary>
    /// Adds a ChannelsData asset to the preset list and sets it as a preset.
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
    /// Toggles the defined trigger channel value between 0 and 1.
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
    /// Passes in a trigger channel and its current value to toggle it between 0 and 1.
    /// </summary>
    /// <param name="channelName"></param>
    /// <param name="value"></param>
    public void ToggleTrigger(string channelName, int value)
    {
        //Toggle the passed in value between 1 and 0.
        int toggledValue = 1 - value;
        //Passes value to Csound.
        csoundUnity.SetChannel(channelName, toggledValue);

        if (debugTrigger)
            Debug.Log("CSOUND " + gameObject.name + " trigger: " + channelName + " , " + toggledValue);
    }

    /// <summary>
    /// Passes a value to the defined Csound trigger channel.
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

    /// <summary>
    /// Passes a value to the a Csound channel.
    /// </summary>
    public void SetTrigger(string channelName, int value)
    {
        //Sets the trigger channel value to the value passed as an argument.
        triggerValue = value;
        //Passes value to Csound.
        csoundUnity.SetChannel(channelName, triggerValue);

        if (debugTrigger)
            Debug.Log("CSOUND " + gameObject.name + " csound trigger: " + triggerValue);
    }
    #endregion

    #region SCORE EVENTS
    /// <summary>
    /// Uses the ScoreEvent asset currently indexed to send a score event.
    /// </summary>
    public void SendScoreEvent()
    {
        csoundUnity.SendScoreEvent(scoreEvents[scoreEventCurrentIndex].ConcatenateScoreEventString());

        if (debugScoreEvents)
            Debug.Log("CSOUND" + gameObject.name + " score event: " + scoreEvents[scoreEventCurrentIndex].ConcatenateScoreEventString());
    }

    /// <summary>
    /// Uses the indexed ScoreEvent asset to send a score event.
    /// </summary>
    /// <param name="index"></param>
    public void SendScoreEvent(int index)
    {
        scoreEventCurrentIndex = index;
        csoundUnity.SendScoreEvent(scoreEvents[scoreEventCurrentIndex].ConcatenateScoreEventString());

        if (debugScoreEvents)
            Debug.Log("CSOUND" + gameObject.name + " score event: " + scoreEvents[scoreEventCurrentIndex].ConcatenateScoreEventString());
    }

    /// <summary>
    /// Adds a ScoreEvent asset to the list and sends it as a score event.
    /// </summary>
    /// <param name="scoreEvent"></param>
    public void SendScoreEvent(CsoundScoreEventSO scoreEvent)
    {
        //Adds new ScoreEvent asset to the list as the last item.
        scoreEvents.Add(scoreEvent);
        //Calls SendScoreEvent passing in the last item as the index.
        SendScoreEvent(scoreEvents.Count - 1);
    }

    /// <summary>
    /// Passes in a string to be sent as a score event.
    /// </summary>
    /// <param name="scoreEvent"></param>
    public void SendScoreEvent(string scoreEvent)
    {
        csoundUnity.SendScoreEvent(scoreEvent);

        if (debugScoreEvents)
            Debug.Log("CSOUND" + gameObject.name + " score event: " + scoreEvent);
    }

    /// <summary>
    /// Passes in separate values for each p field to be sent as a score event.
    /// </summary>
    /// <param name="scorechar"></param>
    /// <param name="instrument"></param>
    /// <param name="delay"></param>
    /// <param name="duration"></param>
    public void SendScoreEvent(string scorechar, string instrument, float delay, float duration)
    {
        csoundUnity.SendScoreEvent(scorechar + " " + instrument + " " + delay + " " + duration);

        if (debugScoreEvents)
            Debug.Log("CSOUND" + gameObject.name + " score event: " + scorechar + " " + instrument + " " + delay + " " + duration);
    }

    /// <summary>
    /// Passes in separate values for each p field to be sent as a score event.
    /// </summary>
    public void SendScoreEvent(string scorechar, string instrument, float delay, float duration, float[] extraPFields)
    {
        string concatenatedPFields = string.Join(" ", extraPFields);
        csoundUnity.SendScoreEvent(scorechar + " " + instrument + " " + delay + " " + duration + " " + concatenatedPFields);

        if (debugScoreEvents)
            Debug.Log("CSOUND" + gameObject.name + " score event: " + scorechar + " " + instrument + " " + delay + " " + duration + " " + concatenatedPFields);

    }

    #endregion

    #region SET CHANNELS TO RANDOM VALUE
    /// <summary>
    /// Uses the currently indexed ChannelData asset in the randomValueChannels array to randomize values between the defined minValue and maxValue.
    /// </summary>
    public void SetChannelsToRandomValue()
    {
        if (!useCurrentPresetRandomValues)
        {
            for (int i = 0; i < randomValueChannels[randomValueCurrentIndex].channelData.Length; i++)
            {
                //Get the random value from the scriptable object and passes it to Csound.
                csoundUnity.SetChannel(randomValueChannels[randomValueCurrentIndex].channelData[i].name, randomValueChannels[randomValueCurrentIndex].GetRandomValue(i, debugSetRandomChannels));
            }
        }
        else
        {
            for (int i = 0; i < presetList[presetCurrentIndex].channelData.Length; i++)
            {
                //Get the random value from the scriptable object and passes it to Csound.
                csoundUnity.SetChannel(presetList[presetCurrentIndex].channelData[i].name, presetList[presetCurrentIndex].GetRandomValue(i, debugSetRandomChannels));
            }
        }
    }

    /// <summary>
    /// Changes the indexed ChannelData asset in the randomValueChannels array and randomizes values between the defined minValue and maxValue.
    /// </summary>
    public void SetChannelsToRandomValue(int index)
    {
        randomValueCurrentIndex = index;

        if (!useCurrentPresetRandomValues)
        {
            for (int i = 0; i < randomValueChannels[randomValueCurrentIndex].channelData.Length; i++)
            {
                //Get the random value from the scriptable object and passes it to Csound.
                csoundUnity.SetChannel(randomValueChannels[randomValueCurrentIndex].channelData[i].name, randomValueChannels[randomValueCurrentIndex].GetRandomValue(i, debugSetRandomChannels));
            }
        }
        else
        {
            for (int i = 0; i < presetList[presetCurrentIndex].channelData.Length; i++)
            {
                //Get the random value from the scriptable object and passes it to Csound.
                csoundUnity.SetChannel(presetList[presetCurrentIndex].channelData[i].name, presetList[presetCurrentIndex].GetRandomValue(i, debugSetRandomChannels));
            }
        }
    }

    /// <summary>
    /// Passses in a ChannelData asset to randomize channel values between the defined minValue and maxValue.
    /// </summary>
    public void SetChannelsToRandomValue(CsoundChannelDataSO newChannelData)
    {
        for (int i = 0; i < newChannelData.channelData.Length; i++)
        {
            //Get the random value from the scriptable object and passes it to Csound.
            csoundUnity.SetChannel(newChannelData.channelData[i].name, newChannelData.GetRandomValue(i, debugSetRandomChannels));
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