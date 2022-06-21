using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO
    //Angular speed: makes it so it can calculate the angular speed from the transform
    //Rotation: makes it so it passes Csound data based on the object's transform rotation vector
    //Scale: makes it so it passes data based on the object's transform scale vector

/// <summary>
/// Provides general methods to pass data from Unity to Csound.
/// </summary>
public class CsoundSender : MonoBehaviour
{

    [Header("REFERENCES")]
        [Tooltip("Reference to the CsoundUnity component to send information to. Will automatically get the component attached to the same object if left empty.")]
        public CsoundUnity csoundUnity;
        [Tooltip("Assign this field if you want to take the physics/transform data from another game object. Leave blank to use this same object for the physics/transform data.")]
        public GameObject gObject;
        private Rigidbody rigidbody;

    [Header("INSTRUMENT PRESETS")]
        [Tooltip("Array containing instrument presets to be set by calling the SetPreset function")]
        public List<CsoundChannelDataSO> presetList = new List<CsoundChannelDataSO>();
        [Tooltip("If true, sets the defined preset value on start")]
        [SerializeField] private bool setPresetOnStart;
        [Tooltip("Defined which preset to be set on start")]
        [SerializeField] private int presetIndexOnStart;
        private int currentPresetIndex;

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

    public enum SpeedSource { Rigidbody, Transform };
    [Header("SPEED CHANNELS")]
        [Tooltip("Defines if the object's speed is taken from its Rigidbody or calculated from its Transform position.")]
        [SerializeField] private SpeedSource speedSource = SpeedSource.Transform;
        [Tooltip("Array of channels that are gonna be modified by the object's speed.")]
        public CsoundChannelDataSO speedChannels;
        [Tooltip("Maximum speed value that will be mapped into a Csound value.")]
        [SerializeField] private float maxSpeedValue;
        [Tooltip("If true, starts calculating speed and passing speed data into Csound on start.")]
        [SerializeField] private bool updateSpeedOnStart = false;
        private float speed;
        private Vector3 previousPosSpeed;
        private bool updateSpeed = false;

    public enum PositionReference { Absolute, Relative, None };
    [Header("TRANSFORM POSITION CHANNELS")]
        [SerializeField] private PositionReference setXPositionTo = PositionReference.None;
        [SerializeField] private PositionReference setYPositionTo = PositionReference.None;
        [SerializeField] private PositionReference setZPositionTo = PositionReference.None;
        [SerializeField] private Vector3 vectorRangesMax, vectorRangesMin;
        [SerializeField] private bool returnAbsoluteValuesX, returnAbsoluteValuesY, returnAbsoluteValuesZ;
        [SerializeField] private bool relativePositionToCamera;
        [Space]
        [SerializeField] private CsoundChannelDataSO csoundChannelsPosX;
        [SerializeField] private CsoundChannelDataSO csoundChannelsPosY;
        [SerializeField] private CsoundChannelDataSO csoundChannelsPosZ;
        [SerializeField] private bool updatePositionOnStart = false;

        //Position references
        private Transform camera;
        private Vector3 startPos;
        private Vector3 relativePos;
        private bool updatePosition = false;

    [Header("ANGULAR SPEED CHANNELS")]
        [SerializeField] private CsoundChannelDataSO angularSpeedChannels;
        [SerializeField] private float maxAngularSpeedValue;
        [SerializeField] private bool updateAngularSpeedOnStart;
        private float rotationSpeed;
        private bool updateAngularSpeed = false;


    [Header("DEBUG")]
        [Tooltip("Prints channel names and values when calling SetPreset.")]
        [SerializeField] private bool debugPresets = false;
        [Tooltip("Prints trigger channel value when calling ToggleTrigger and SetTrigger.")]
        [SerializeField] private bool debugTrigger = false;
        [Tooltip("Prints channel names and values when calling SetChannelsToRandomValue.")]
        [SerializeField] private bool debugSetRandomChannels = false;
        [Tooltip("Prints the object's speed on Update.")]
        [SerializeField] private bool debugSpeed = false;
        [Tooltip("Prints the object's relative position vector on Update.")]
        [SerializeField] private bool debugPosition = false;
        [Tooltip("Prints the object's angular speed on Update.")]
        [SerializeField] private bool debugAngularSpeed= false;


    #region UNITY LIFE CYCLE
    private void Awake()
    {
        if (gObject == null)
            gObject = gameObject;

        //Gets Rigidbody attached to object if Speed Source is set to Rigidbody.
        if ((rigidbody == null) && (speedSource == SpeedSource.Rigidbody))
        {
            rigidbody = gObject.GetComponent<Rigidbody>();

            if (rigidbody == null)
                Debug.LogError("No Rigidbody component attached to " + gameObject.name);
        }

        //Gets the CsoundUnity component attached to the object.
        if (csoundUnity == null)
        {
            csoundUnity = GetComponent<CsoundUnity>();

            if (csoundUnity == null)
                Debug.LogError("No CsoundUnity component attached to " + gameObject.name);
        }

        //Gets reference to the main camera object
        camera = Camera.main.transform;
    }

    void Start()
    {
        //Set defined trigger channel to 1 if triggerOnStart is true.
        if (triggerOnStart)
            SetTrigger(1);

        //Calls SetPreset if setPresetOnStart is true.
        if (setPresetOnStart)
            SetPreset(presetIndexOnStart);

        //Calls SetChannelsToRandomValue if setChannelRandomValuesOnStart is true.
        if (setChannelRandomValuesOnStart)
            SetChannelsToRandomValue();

        //Send values to Csound based on object speed if updateSpeedOnStart is true.
        if (updateSpeedOnStart)
            UpdateSpeed(true);

        if (updatePositionOnStart)
            UpdatePosition(true);

        if (updateAngularSpeedOnStart)
            UpdateAngularSpeed(true);
    }

    void Update()
    {
        
    }

    void FixedUpdate()
    {
        //If the updateSpeed bool is true, calculate speed and send values to Csound.
        if (updateSpeed)
            SendCsoundDataBasedOnSpeed();

        if (updateAngularSpeed)
            SendCsoundDataBasedOnAngularSpeed();
    }

    private void LateUpdate()
    {
        if (updatePosition)
        {
            if (setXPositionTo == PositionReference.Relative || setYPositionTo == PositionReference.Relative || setZPositionTo == PositionReference.Relative)
                CaculateRelativePos();

            if (csoundChannelsPosX != null && setXPositionTo != PositionReference.None)
                SetCsoundValuesPosX();
            if (csoundChannelsPosY != null && setYPositionTo != PositionReference.None)
                SetCsoundValuesPosY();
            if (csoundChannelsPosZ != null && setZPositionTo != PositionReference.None)
                SetCsoundValuesPosZ();

            if (debugPosition)
                Debug.Log("CSOUND " + gObject.name + " relative position: " + relativePos);

        }
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

    #region PRESETS
    /// <summary>
    /// Set channel fixed values to Csound by using an index from the preset list.
    /// </summary>
    /// <param name="index"></param>
    public void SetPreset(int index)
    {
        if (debugPresets)
            Debug.Log("CSOUND " + gameObject.name + " set preset: " + presetList[index]);

        foreach(CsoundChannelDataSO.CsoundChannelData channelData in presetList[index].channelData)
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

    #region SPEED
    private void SendCsoundDataBasedOnSpeed()
    {
        //Checks if speed should be calculated from the objects Rigidbody or from the Transform. 
        if (speedSource == SpeedSource.Rigidbody)
        {
            //Gets speed from the Rigidbody's velocity.
            speed = rigidbody.velocity.magnitude;
        }
        else if (speedSource == SpeedSource.Transform)
        {
            //Calculates speed based on the transform
            speed = (gObject.transform.position - previousPosSpeed).magnitude / Time.deltaTime;
            previousPosSpeed = gObject.transform.position;
        }

        //Assign values to Csound channels based on the object's speed.
        foreach (CsoundChannelDataSO.CsoundChannelData channelData in speedChannels.channelData)
        {
            //Scales the value passed to Csound based on the minValue and maxValue defined for each channel.
            float scaledSpeedValue =
                Mathf.Clamp(ScaleFloat(0, maxSpeedValue, channelData.minValue, channelData.maxValue, speed), channelData.minValue, channelData.maxValue);
            //Passes values to Csound.
            csoundUnity.SetChannel(channelData.name, scaledSpeedValue);
        }

        if (debugSpeed)
            Debug.Log(speed);
    }

    /// <summary>
    /// Starts calcualting the object speed and passing that value into the defined Csound if the bool is true and stops it if false.
    /// </summary>
    /// <param name="update"></param>
    public void UpdateSpeed(bool update)
    {
        updateSpeed = update;

        if (debugSpeed)
            Debug.Log("CSOUND " + gameObject.name + " update speed = " + updateSpeed);
    }

    /// <summary>
    /// Toggles the update speed bool between true and false. Starts calcualting the object speed and passing that value into the defined Csound if the bool is true and stops it if false.
    /// </summary>
    public void UpdateSpeedToggle()
    {
        if (updateSpeed)
            updateSpeed = false;
        else if (!updateSpeed)
            updateSpeed = true;

        if (debugSpeed)
            Debug.Log("CSOUND " + gameObject.name + " update speed = " + updateSpeed);
    }
    #endregion

    #region POSITION

    public void UpdatePosition(bool update)
    {
        updatePosition = update;

        if (update)
            GetRelativeStartingPosition();

        if (debugPosition)
            Debug.Log("CSOUND " + gameObject.name + " update speed = " + updatePosition);
    }

    public void UpdatePositionToggle()
    {
        if (updatePosition)
            updatePosition = false;
        else
           updatePosition = true;

        UpdatePosition(updatePosition);

        if (debugPosition)
            Debug.Log("CSOUND " + gameObject.name + " update speed = " + updatePosition);

    }

    private void GetRelativeStartingPosition()
    {
        if (relativePositionToCamera)
            startPos = camera.transform.InverseTransformPoint(gObject.transform.position);
        else
            startPos = gObject.transform.position;
    }

    private void CaculateRelativePos()
    {
        Vector3 currentTransform = new Vector3();

        if (relativePositionToCamera)
        {
            currentTransform = camera.transform.InverseTransformPoint(transform.position);
        }
        else
        {
            currentTransform = gObject.transform.position;
        }

        relativePos.x = currentTransform.x - startPos.x;
        relativePos.y = currentTransform.y - startPos.y;
        relativePos.z = currentTransform.z - startPos.z;
    }

    private void SetCsoundChannelBasedOnPosition(CsoundChannelDataSO csoundChannels, float minVectorRange, float maxVectorRange, float transformAxis, bool returnAbsoluteValue)
    {
        foreach (CsoundChannelDataSO.CsoundChannelData data in csoundChannels.channelData)
        {
            float value =
                Mathf.Clamp(ScaleFloat(minVectorRange, maxVectorRange, data.minValue, data.maxValue, transformAxis), data.minValue, data.maxValue);

            if (!returnAbsoluteValue)
            {
                csoundUnity.SetChannel(data.name, value);
            }
            else
            {
                csoundUnity.SetChannel(data.name, Mathf.Abs(value));
            }
        }
    }

    private void SetCsoundValuesPosX()
    {
        if (setXPositionTo == PositionReference.Absolute)
            SetCsoundChannelBasedOnPosition(csoundChannelsPosX, vectorRangesMin.x, vectorRangesMax.x, transform.position.x, returnAbsoluteValuesX);
        else
            SetCsoundChannelBasedOnPosition(csoundChannelsPosX, vectorRangesMin.x, vectorRangesMax.x, relativePos.x, returnAbsoluteValuesX);
    }

    private void SetCsoundValuesPosY()
    {
        if (setYPositionTo == PositionReference.Absolute)
            SetCsoundChannelBasedOnPosition(csoundChannelsPosY, vectorRangesMin.y, vectorRangesMax.y, transform.position.y, returnAbsoluteValuesY);
        else
            SetCsoundChannelBasedOnPosition(csoundChannelsPosY, vectorRangesMin.y, vectorRangesMax.y, relativePos.y, returnAbsoluteValuesY);
    }

    private void SetCsoundValuesPosZ()
    {
        if (setZPositionTo == PositionReference.Absolute)
            SetCsoundChannelBasedOnPosition(csoundChannelsPosZ, vectorRangesMin.z, vectorRangesMax.z, transform.position.z, returnAbsoluteValuesZ);
        else
            SetCsoundChannelBasedOnPosition(csoundChannelsPosZ, vectorRangesMin.z, vectorRangesMax.z, relativePos.z, returnAbsoluteValuesZ);
    }
    #endregion

    #region ANGULAR VELOCITY/TORQUE

    public void UpdateAngularSpeed(bool update)
    {
        updateAngularSpeed = update;

        if (debugAngularSpeed)
            Debug.Log("CSOUND " + gameObject.name + " update andgular speed = " + updateAngularSpeed);
    }

    public void UpdateAngularSpeedToggle()
    {
        if (updateAngularSpeed)
            updateAngularSpeed = false;
        else
            updateAngularSpeed = true;

        if (debugAngularSpeed)
            Debug.Log("CSOUND " + gameObject.name + " update andgular speed = " + updateAngularSpeed);
    }

    private void SendCsoundDataBasedOnAngularSpeed()
    {
        rotationSpeed = rigidbody.angularVelocity.magnitude;

        foreach (CsoundChannelDataSO.CsoundChannelData data in angularSpeedChannels.channelData)
        {
            float value =
                Mathf.Clamp(ScaleFloat(0, maxAngularSpeedValue, data.minValue, data.maxValue, rigidbody.angularVelocity.magnitude), data.minValue, data.maxValue);

            csoundUnity.SetChannel(data.name, value);
        }

        if (debugAngularSpeed)
            Debug.Log("CSOUND " + gObject.name + " angular speed: " + rotationSpeed);
    }
    #endregion

    #region ROTATION

    #endregion

    #region UTILITIES
    private float ScaleFloat(float OldMin, float OldMax, float NewMin, float NewMax, float OldValue)
    {
        float OldRange = (OldMax - OldMin);
        float NewRange = (NewMax - NewMin);
        float NewValue = (((OldValue - OldMin) * NewRange) / OldRange) + NewMin;

        return (NewValue);
    }
    #endregion
}