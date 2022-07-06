using System.Collections;   
using System.Collections.Generic;
using UnityEngine;

//TODO
    //PACKAGING
        //Write summaries for all functions
        //Write tooltips for all variables
        //Thorough commenting of every line of code
        //Inspector scripting: make a drop down for each header of variables

//BACKLOG
    //Angular speed: makes it so it can calculate the angular speed from the transform
    //Add Velocity: pass data based on each individual velocity vector axis
    //Rotation: make it so it axis can act as  an endless encoder

/// <summary>
/// Provides general methods to pass transform and rigidbody data from Unity to Csound
/// </summary>
public class CsoundTransformAndPhysicsSender : MonoBehaviour
{
    [Header("REFERENCES")]
    [Tooltip("Reference to the CsoundUnity component to send information to. Will automatically get the component attached to the same object if left empty.")]
    public CsoundUnity csoundUnity;
    [Tooltip("Assign this field if you want to take the physics/transform data from another game object. Leave blank to use this same object for the physics/transform data.")]
    public GameObject gObject;
    private Rigidbody rigidbody;

    public enum SpeedSource { Rigidbody, Transform };
    [Header("SPEED CHANNELS")]
    [Tooltip("Defines if the object's speed is taken from its Rigidbody or calculated from its Transform position.")]
    [SerializeField] private SpeedSource speedSource = SpeedSource.Transform;
    [Tooltip("Array of channels that are gonna be modified by the object's speed.")]
    public CsoundChannelDataSO speedChannels;
    [Tooltip("Maximum speed value that will be mapped into a Csound value.")]
    public float maxSpeedValue;
    [Tooltip("If true, starts calculating speed and passing speed data into Csound on start.")]
    [SerializeField] private bool updateSpeedOnStart = false;
    private float speed;
    private Vector3 previousPosSpeed;
    private bool updateSpeed = false;

    public enum PositionVectorReference { Absolute, Relative, RelativeToCamera, None };
    [Header("TRANSFORM POSITION CHANNELS")]
    [SerializeField] private PositionVectorReference setXPositionTo = PositionVectorReference.None;
    [SerializeField] private PositionVectorReference setYPositionTo = PositionVectorReference.None;
    [SerializeField] private PositionVectorReference setZPositionTo = PositionVectorReference.None;
    [SerializeField] private Vector3 posVectorRangesMax, posVectorRangesMin;
    [SerializeField] private bool returnAbsoluteValuesPosX, returnAbsoluteValuesPosY, returnAbsoluteValuesPosZ;
    [Space]
    [SerializeField] private CsoundChannelDataSO csoundChannelsPosX;
    [SerializeField] private CsoundChannelDataSO csoundChannelsPosY;
    [SerializeField] private CsoundChannelDataSO csoundChannelsPosZ;
    [SerializeField] private bool updatePositionOnStart = false;

    //Position references
    private Transform camera;
    private Vector3 startPos, startPosCameraRelative;
    private Vector3 relativePos, relativeCameraPos;
    private bool calculateRelativeCameraPos;
    private bool updatePosition = false;

    [Header("ANGULAR SPEED CHANNELS")]
    [SerializeField] private CsoundChannelDataSO angularSpeedChannels;
    [SerializeField] private float maxAngularSpeedValue;
    [SerializeField] private bool updateAngularSpeedOnStart;
    private float rotationSpeed;
    private bool updateAngularSpeed = false;

    public enum RotationVectorReference { Absolute, Relative, None };
    public enum RotationMode { Circular };
    [Header("ROTATION")]
    [SerializeField] private RotationMode rotationMode = RotationMode.Circular;
    [SerializeField] private RotationVectorReference setXRotationTo = RotationVectorReference.Relative;
    [SerializeField] private RotationVectorReference setYRotationTo = RotationVectorReference.Relative;
    [SerializeField] private RotationVectorReference setZRotationTo = RotationVectorReference.Relative;
    //[SerializeField] private Vector3 rotationVectorRangesMax, rotationVectorRangesMin;
    //[SerializeField] private bool returnAbsoluteValuesRotationX, returnAbsoluteValuesRotationY, returnAbsoluteValuesRotationZ;
    [Space]
    [SerializeField] private CsoundChannelDataSO csoundChannelsRotationX;
    [SerializeField] private CsoundChannelDataSO csoundChannelsRotationY;
    [SerializeField] private CsoundChannelDataSO csoundChannelsRotationZ;
    [SerializeField] private bool useLocalEulerAngles;
    [SerializeField] private bool updateRotationOnStart = false;

    private bool updateRotation;
    private Vector3 rotationStart, rotationRelative, localRotation;


    public enum ScaleVectorReference { Absolute, Relative, None };
    [Header("SCALE MAGNITUDE")]
    [SerializeField] private ScaleVectorReference setScaleMagnitudeTo = ScaleVectorReference.Relative;
    [SerializeField] private CsoundChannelDataSO scaleMagnitudeChannels;
    public float scaleMagnitudeMax;
    [SerializeField] private bool useLocalScaleMagnitude = true;
    [SerializeField] private bool updateScaleMagnitudeOnStart;
    private bool updateScaleMagnitude;
    private float scaleMagnitudeCurrent, scaleMagnitudeStart, scaleMagnitudeFinal;

    [Header("SCALE AXIS")]
    [SerializeField] private ScaleVectorReference setXScaleTo = ScaleVectorReference.None;
    [SerializeField] private ScaleVectorReference setYScaleTo = ScaleVectorReference.None;
    [SerializeField] private ScaleVectorReference setZScaleTo = ScaleVectorReference.None;
    [SerializeField] private Vector3 scaleVectorRangesMax, scaleVectorRangesMin;
    [Space]
    [SerializeField] private CsoundChannelDataSO csoundChannelsScaleX;
    [SerializeField] private CsoundChannelDataSO csoundChannelsScaleY;
    [SerializeField] private CsoundChannelDataSO csoundChannelsScaleZ;
    [SerializeField] private bool useLocalScale = true;
    [SerializeField] private bool updateScaleOnStart;
    private Vector3 startScale, relativeScale;
    private bool calculateRelativeScale;
    private bool updateScaleAxis = false;

    [Header("DEBUG")]
    [Tooltip("Prints the object's speed on Update.")]
    [SerializeField] private bool debugSpeed = false;
    [Tooltip("Prints the object's relative position vector on Update.")]
    [SerializeField] private bool debugPosition = false;
    [Tooltip("Prints the object's angular speed on Update.")]
    [SerializeField] private bool debugAngularSpeed = false;
    [SerializeField] private bool debugRotation = false;
    [SerializeField] private bool debugScale = false;

    #region UNITY LIFE CYCLE
    private void Awake()
    {
        if (gObject == null)
            gObject = gameObject;

        //Gets Rigidbody attached to object if Speed Source is set to Rigidbody.
        if ((rigidbody == null)) //&& (speedSource == SpeedSource.Rigidbody))
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
        //Send values to Csound based on object speed if updateSpeedOnStart is true.
        if (updateSpeedOnStart)
            UpdateSpeed(true);

        if (updatePositionOnStart)
            UpdatePosition(true);

        if (updateAngularSpeedOnStart)
            UpdateAngularSpeed(true);

        if (updateScaleMagnitudeOnStart)
            UpdateScaleMagnitude(true);

        if (updateScaleOnStart)
            UpdateScaleAxis(true);

        if (updateRotationOnStart)
            UpdateRotation(true);
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
            if (setXPositionTo == PositionVectorReference.Relative || setYPositionTo == PositionVectorReference.Relative || setZPositionTo == PositionVectorReference.Relative)
                CaculateRelativePos();

            if (calculateRelativeCameraPos)
                CalculateRelativeCameraPos();

            if (csoundChannelsPosX != null || setXPositionTo != PositionVectorReference.None)
                SetCsoundValuesPosX();
            if (csoundChannelsPosY != null || setYPositionTo != PositionVectorReference.None)
                SetCsoundValuesPosY();
            if (csoundChannelsPosZ != null || setZPositionTo != PositionVectorReference.None)
                SetCsoundValuesPosZ();

            if (debugPosition)
                Debug.Log("CSOUND " + gObject.name + " relative position: " + relativePos);

        }

        if (updateRotation)
        {
            CalculateRelativeRotation();

            if (csoundChannelsRotationX != null || setXRotationTo != RotationVectorReference.None)
                SetCsoundValuesXRotation();

            if (csoundChannelsRotationY != null || setYRotationTo != RotationVectorReference.None)
                SetCsoundValuesYRotation();

            if (csoundChannelsRotationZ != null || setZRotationTo != RotationVectorReference.None)
                SetCsoundValuesZRotation();
        }

        if (updateScaleAxis)
        {
            if (calculateRelativeScale)
                CalculateRelativeScale();

            if (csoundChannelsScaleX != null || setXScaleTo != ScaleVectorReference.None)
                SetCsoundValuesScaleX();
            if (csoundChannelsScaleY != null || setYScaleTo != ScaleVectorReference.None)
                SetCsoundValuesScaleY();
            if (csoundChannelsScaleZ != null || setZScaleTo != ScaleVectorReference.None)
                SetCsoundValuesScaleZ();
        }

        if (updateScaleMagnitude && setScaleMagnitudeTo != ScaleVectorReference.None)
            SendCsoundDataBasedOnScaleMagnitude();
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

        if (setXPositionTo == PositionVectorReference.RelativeToCamera || setYPositionTo == PositionVectorReference.RelativeToCamera || setZPositionTo == PositionVectorReference.RelativeToCamera)
            calculateRelativeCameraPos = true;

        if (debugPosition)
            Debug.Log("CSOUND " + gameObject.name + " update position = " + updatePosition);
    }

    public void UpdatePositionToggle()
    {
        if (updatePosition)
            updatePosition = false;
        else
            updatePosition = true;

        UpdatePosition(updatePosition);

    }

    private void GetRelativeStartingPosition()
    {
        startPos = gObject.transform.position;

        if (calculateRelativeCameraPos)
            startPosCameraRelative = camera.transform.InverseTransformPoint(gObject.transform.position);
    }

    private void CaculateRelativePos()
    {
        relativePos.x = gObject.transform.position.x - startPos.x;
        relativePos.y = gObject.transform.position.y - startPos.y;
        relativePos.z = gObject.transform.position.z - startPos.z;
    }

    private void CalculateRelativeCameraPos()
    {
        Vector3 currentTransform = camera.transform.InverseTransformPoint(transform.position);

        relativeCameraPos.x = currentTransform.x - startPosCameraRelative.x;
        relativeCameraPos.y = currentTransform.x - startPosCameraRelative.y;
        relativeCameraPos.z = currentTransform.x - startPosCameraRelative.z;
    }


    private void SetCsoundValuesPosX()
    {
        if (setXPositionTo == PositionVectorReference.Absolute)
            SetCsoundChannelBasedOnAxis(csoundChannelsPosX, posVectorRangesMin.x, posVectorRangesMax.x, transform.position.x, returnAbsoluteValuesPosX);
        else if (setXPositionTo == PositionVectorReference.RelativeToCamera)
            SetCsoundChannelBasedOnAxis(csoundChannelsPosX, posVectorRangesMin.x, posVectorRangesMax.x, relativeCameraPos.x, returnAbsoluteValuesPosX);
        else
            SetCsoundChannelBasedOnAxis(csoundChannelsPosX, posVectorRangesMin.x, posVectorRangesMax.x, relativePos.x, returnAbsoluteValuesPosX);
    }

    private void SetCsoundValuesPosY()
    {
        if (setYPositionTo == PositionVectorReference.Absolute)
            SetCsoundChannelBasedOnAxis(csoundChannelsPosY, posVectorRangesMin.y, posVectorRangesMax.y, transform.position.y, returnAbsoluteValuesPosY);
        else if (setXPositionTo == PositionVectorReference.RelativeToCamera)
            SetCsoundChannelBasedOnAxis(csoundChannelsPosY, posVectorRangesMin.y, posVectorRangesMax.y, relativeCameraPos.y, returnAbsoluteValuesPosY);
        else
            SetCsoundChannelBasedOnAxis(csoundChannelsPosY, posVectorRangesMin.y, posVectorRangesMax.y, relativePos.y, returnAbsoluteValuesPosY);
    }

    private void SetCsoundValuesPosZ()
    {
        if (setZPositionTo == PositionVectorReference.Absolute)
            SetCsoundChannelBasedOnAxis(csoundChannelsPosZ, posVectorRangesMin.z, posVectorRangesMax.z, transform.position.z, returnAbsoluteValuesPosZ);
        else if (setZPositionTo == PositionVectorReference.RelativeToCamera)
            SetCsoundChannelBasedOnAxis(csoundChannelsPosZ, posVectorRangesMin.z, posVectorRangesMax.z, relativeCameraPos.z, returnAbsoluteValuesPosZ);
        else
            SetCsoundChannelBasedOnAxis(csoundChannelsPosZ, posVectorRangesMin.z, posVectorRangesMax.z, relativePos.z, returnAbsoluteValuesPosZ);
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
    public void UpdateRotation(bool update)
    {
        updateRotation = update;

        GetInitialRotation();

        if (debugRotation)
            Debug.Log("CSOUND " + gameObject.name + " update rotation  = " + updateRotation);
    }

    public void UpdateRotationToggle()
    {
        if (updateRotation)
            updateRotation = false;
        else
            updateRotation = true;

        UpdateRotation(updateRotation);
    }

    private void GetInitialRotation()
    {
        if (!useLocalEulerAngles)
            rotationStart = transform.eulerAngles;
        else
            rotationStart = transform.localEulerAngles;
    }

    private void CalculateRelativeRotation()
    {
        if (!useLocalEulerAngles)
            localRotation = transform.eulerAngles;
        else
            localRotation = transform.localEulerAngles;

        rotationRelative = localRotation - rotationStart;

        if (debugRotation)
            Debug.Log("CSOUND " + gameObject.name + " relative rotation  = " + rotationRelative);
    }

    private float CircularAxisValue(float rotationAxis, float wrapAroundValue)
    {
        float value;

        if (rotationAxis >= wrapAroundValue)
            value = ((wrapAroundValue * 2) - rotationAxis) * 2;
        else
            value = rotationAxis * 2;

        if (debugRotation)
            Debug.Log("CSOUND " + gameObject.name + " circular rotation value  = " + value);

        return value;
    }


    private void SetCsoundValuesXRotation()
    {
        if (rotationMode == RotationMode.Circular)
        {
            if (setXRotationTo == RotationVectorReference.Absolute)
                SetCsoundChannelBasedOnAxis(csoundChannelsRotationX, 0, 180, CircularAxisValue(localRotation.x, 90));
            else if (setXRotationTo == RotationVectorReference.Relative)
                SetCsoundChannelBasedOnAxis(csoundChannelsRotationX, 0, 180, CircularAxisValue(rotationRelative.x, 90));
        }
    }

    private void SetCsoundValuesYRotation()
    {
        if (rotationMode == RotationMode.Circular)
        {
            if (setYRotationTo == RotationVectorReference.Absolute)
                SetCsoundChannelBasedOnAxis(csoundChannelsRotationY, 0, 360, CircularAxisValue(localRotation.y, 180));
            else if (setYRotationTo == RotationVectorReference.Relative)
                SetCsoundChannelBasedOnAxis(csoundChannelsRotationY, 0, 360, CircularAxisValue(rotationRelative.y, 180));
        }
    }

    private void SetCsoundValuesZRotation()
    {
        if (rotationMode == RotationMode.Circular)
        {
            if (setZRotationTo == RotationVectorReference.Absolute)
                SetCsoundChannelBasedOnAxis(csoundChannelsRotationZ, 0, 360, CircularAxisValue(localRotation.z, 180));
            else if (setZRotationTo == RotationVectorReference.Relative)
                SetCsoundChannelBasedOnAxis(csoundChannelsRotationZ, 0, 360, CircularAxisValue(rotationRelative.z, 180));
        }
    }

    #endregion

    #region SCALE MAGNITUDE

    public void UpdateScaleMagnitude(bool update)
    {
        updateScaleMagnitude = update;

        if (updateScaleMagnitude)
        {
            if (useLocalScaleMagnitude)
                scaleMagnitudeStart = gObject.transform.localScale.magnitude;
            else
                scaleMagnitudeStart = gObject.transform.lossyScale.magnitude;
        }


        if (debugScale)
            Debug.Log("CSOUND " + gameObject.name + " update scale magnitude = " + updateScaleMagnitude);
    }

    public void UpdateScaleMagnitudeToggle()
    {
        if (updateScaleMagnitude)
            updateScaleMagnitude = false;
        else
            updateScaleMagnitude = true;

        UpdateScaleMagnitude(updateScaleMagnitude);
    }

    private void SendCsoundDataBasedOnScaleMagnitude()
    {
        if (useLocalScaleMagnitude)
            scaleMagnitudeCurrent = gObject.transform.localScale.magnitude;
        else
            scaleMagnitudeCurrent = gObject.transform.lossyScale.magnitude;

        if (setScaleMagnitudeTo == ScaleVectorReference.Relative)
        {
            scaleMagnitudeFinal = scaleMagnitudeCurrent - scaleMagnitudeStart;
        }
        else if (setScaleMagnitudeTo == ScaleVectorReference.Absolute)
        {
            scaleMagnitudeFinal = scaleMagnitudeCurrent;
        }

        foreach (CsoundChannelDataSO.CsoundChannelData channelData in scaleMagnitudeChannels.channelData)
        {
            //Scales the value passed to Csound based on the minValue and maxValue defined for each channel.
            float scaledValue =
                Mathf.Clamp(ScaleFloat(0, scaleMagnitudeMax, channelData.minValue, channelData.maxValue, scaleMagnitudeFinal), channelData.minValue, channelData.maxValue);
            //Passes values to Csound.
            csoundUnity.SetChannel(channelData.name, scaledValue);
        }

        if (debugScale)
            Debug.Log("CSOUND " + gameObject.name + " scale magnitude = " + scaleMagnitudeFinal);
    }

    #endregion

    #region SCALE AXIS
    public void UpdateScaleAxis(bool update)
    {
        updateScaleAxis = update;

        if (update)
            GetRelativeStartingScale();

        if (setXScaleTo == ScaleVectorReference.Relative || setYScaleTo == ScaleVectorReference.Relative || setZScaleTo == ScaleVectorReference.Relative)
            calculateRelativeScale = true;

        if (debugScale)
            Debug.Log("CSOUND " + gameObject.name + " update scale axis = " + updateScaleAxis);
    }

    public void UpdateScaleAxisToggle()
    {
        if (updateScaleAxis)
            updateScaleAxis = false;
        else
            updateScaleAxis = true;

        UpdateScaleAxis(updateScaleAxis);
    }

    private void GetRelativeStartingScale()
    {
        if (useLocalScale)
            startScale = gObject.transform.localScale;
        else
            startScale = gObject.transform.lossyScale;
    }

    private void CalculateRelativeScale()
    {
        if (useLocalScale)
        {
            relativeScale.x = gObject.transform.localScale.x - startScale.x;
            relativeScale.y = gObject.transform.localScale.y - startScale.y;
            relativeScale.z = gObject.transform.localScale.z - startScale.z;
        }
        else
        {
            relativeScale.x = gObject.transform.lossyScale.x - startScale.x;
            relativeScale.y = gObject.transform.lossyScale.y - startScale.y;
            relativeScale.z = gObject.transform.lossyScale.z - startScale.z;
        }

        if (debugScale)
            Debug.Log("CSOUND " + gObject.name + " relative scale: " + relativeScale);
    }

    private void SetCsoundValuesScaleX()
    {
        if (setXScaleTo == ScaleVectorReference.Absolute)
        {
            if (useLocalScale)
                SetCsoundChannelBasedOnAxis(csoundChannelsScaleX, scaleVectorRangesMin.x, scaleVectorRangesMax.x, gObject.transform.localScale.x);
            else
                SetCsoundChannelBasedOnAxis(csoundChannelsScaleX, scaleVectorRangesMin.x, scaleVectorRangesMax.x, gObject.transform.lossyScale.x);
        }
        else
            SetCsoundChannelBasedOnAxis(csoundChannelsScaleX, scaleVectorRangesMin.x, scaleVectorRangesMax.x, relativeScale.x);
    }

    private void SetCsoundValuesScaleY()
    {
        if (setYScaleTo == ScaleVectorReference.Absolute)
        {
            if (useLocalScale)
                SetCsoundChannelBasedOnAxis(csoundChannelsScaleY, scaleVectorRangesMin.y, scaleVectorRangesMax.y, gObject.transform.localScale.y);
            else
                SetCsoundChannelBasedOnAxis(csoundChannelsScaleY, scaleVectorRangesMin.y, scaleVectorRangesMax.y, gObject.transform.lossyScale.y);
        }
        else
            SetCsoundChannelBasedOnAxis(csoundChannelsScaleY, scaleVectorRangesMin.y, scaleVectorRangesMax.y, relativeScale.y);
    }

    private void SetCsoundValuesScaleZ()
    {
        if (setZScaleTo == ScaleVectorReference.Absolute)
        {
            if (useLocalScale)
                SetCsoundChannelBasedOnAxis(csoundChannelsScaleZ, scaleVectorRangesMin.z, scaleVectorRangesMax.z, gObject.transform.localScale.z);
            else
                SetCsoundChannelBasedOnAxis(csoundChannelsScaleZ, scaleVectorRangesMin.z, scaleVectorRangesMax.z, gObject.transform.lossyScale.z);
        }
        else
            SetCsoundChannelBasedOnAxis(csoundChannelsScaleZ, scaleVectorRangesMin.z, scaleVectorRangesMax.z, relativeScale.z);
    }
    #endregion

    #region UTILITIES
    private float ScaleFloat(float OldMin, float OldMax, float NewMin, float NewMax, float OldValue)
    {
        float OldRange = (OldMax - OldMin);
        float NewRange = (NewMax - NewMin);
        float NewValue = (((OldValue - OldMin) * NewRange) / OldRange) + NewMin;

        return (NewValue);
    }


    private void SetCsoundChannelBasedOnAxis(CsoundChannelDataSO csoundChannels, float minVectorRange, float maxVectorRange, float vectorAxis, bool returnAbsoluteValue)
    {
        foreach (CsoundChannelDataSO.CsoundChannelData data in csoundChannels.channelData)
        {
            float value =
                Mathf.Clamp(ScaleFloat(minVectorRange, maxVectorRange, data.minValue, data.maxValue, vectorAxis), data.minValue, data.maxValue);

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

    private void SetCsoundChannelBasedOnAxis(CsoundChannelDataSO csoundChannels, float minVectorRange, float maxVectorRange, float vectorAxis)
    {
        foreach (CsoundChannelDataSO.CsoundChannelData data in csoundChannels.channelData)
        {
            float value =
                Mathf.Clamp(ScaleFloat(minVectorRange, maxVectorRange, data.minValue, data.maxValue, vectorAxis), data.minValue, data.maxValue);

            csoundUnity.SetChannel(data.name, value);
        }
    }
    #endregion
}
