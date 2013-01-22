// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MouseCameraControl_Advanced_2.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Resources;
using CodeEnv.Master.Common.Unity;
using CodeEnv.Master.Common.UI;

/// <summary>
/// COMMENT 
/// </summary>
public class MouseCameraControl_Advanced_2 : MonoBehaviour {

    // Mouse Camera Control default configurations
    // Move Camera via Edge Scrolling
    public ScreenEdgeConfiguration focusEdgeZoom = new ScreenEdgeConfiguration { sensitivity = 5F, activate = true };
    public ScreenEdgeConfiguration freeEdgeZoom = new ScreenEdgeConfiguration { sensitivity = 20F, activate = true };
    public ScreenEdgeConfiguration freeEdgePan = new ScreenEdgeConfiguration { sensitivity = 6F, activate = true };
    public ScreenEdgeConfiguration focusEdgeOrbit = new ScreenEdgeConfiguration { sensitivity = 10F, activate = true };

    // Move up/down
    public MouseButtonConfiguration truckPedestal = new MouseButtonConfiguration { mouseButton = MouseButton.Middle, modifiers = new Modifiers { altKeyReqd = true }, sensitivity = 1F, activate = true };
    public MouseButtonConfiguration fastTruckPedestal = new MouseButtonConfiguration { mouseButton = MouseButton.Middle, modifiers = new Modifiers { altKeyReqd = true, shiftKeyReqd = true }, sensitivity = 3F, activate = true };

    // Roll around Z axis
    public MouseButtonConfiguration focusRoll = new MouseButtonConfiguration { mouseButton = MouseButton.Right, modifiers = new Modifiers { altKeyReqd = true }, sensitivity = 3F };
    public MouseButtonConfiguration freeRoll = new MouseButtonConfiguration { mouseButton = MouseButton.Right, modifiers = new Modifiers { altKeyReqd = true }, sensitivity = 3F };

    // Look around 
    public MouseButtonConfiguration orbit = new MouseButtonConfiguration { mouseButton = MouseButton.Right, sensitivity = 2f, activate = true };
    public MouseButtonConfiguration panTilt = new MouseButtonConfiguration { mouseButton = MouseButton.Right, sensitivity = 2f, activate = true };

    // Move forward/backward
    public MouseScrollWheelConfiguration focusZoom = new MouseScrollWheelConfiguration { sensitivity = 50F, activate = true };
    public MouseScrollWheelConfiguration freeZoom = new MouseScrollWheelConfiguration { sensitivity = 50F, activate = true };

    //public SimultaneousMouseButtonConfiguration mouseZoom = new SimultaneousMouseButtonConfiguration { firstMouseButton = MouseButton.Left, secondMouseButton = MouseButton.Right, reqdCameraState = CameraState.Freeform, sensitivity = 10.0F };

    // IMPROVE
    // Implement optional zoom OUT from cursor. Zoom IN currently tracks in the direction of the cursor. Current Zoom OUT is directly backwards.
    // I've implemented 360 degree orbit and pan/tilt under the thinking that Quaternions allow it. Keep an eye out for gimbal lock.
    // Should Tilt/EdgePan have some Pedastal/Truck added like Star Ruler?
    // Need more elegant rotation and translation functions when selecting a focus - aka Slerp, Mathf.SmoothDamp/Angle, etc.
    // Add a follow moving object capability
    // How should zooming toward cursor combine with an object in focus? Should the zoom add an offset creating a new defacto focus point, ala Star Ruler?
    // Dragging the mouse with any button held down works offscreen OK, but upon release offscreen, immediately enables edge scrolling and panning
    // Implement Camera controls such as clip planes, FieldOfView

    public bool IsResetOnFocus { get; set; }

    private bool isRollEnabled;
    public bool IsRollEnabled {
        get { return isRollEnabled; }
        set { isRollEnabled = value; focusRoll.activate = value; freeRoll.activate = value; }
    }
    // public bool IsZoomInOnCursor { get; set; } // Not implemented. ScrollWheel always zooms IN on cursor, Zoom OUT is directly backwards

    // Fields visible in the Inspector so they can be tweaked
    public float minimumDistanceFromTarget = 3.0F;
    public float optimalDistanceFromTarget = 5.0F;
    public float universeDiameter = 2000.0F;
    public float mouseMotionSensitivity = 40F;
    public float activeScreenEdge = 10F;
    public float freeZoomSpeedDerater = 40.0F;

    // External Object references
    private Transform focusTransform;
    private Transform targetTransform;
    private Transform _transform;   // cached
    GameEventManager eventManager;  // cached
    GameObject dummyTargetGO;   // cached

    // Calculated Positional fields    
    private Vector3 cameraPosition;
    private Vector3 targetDirection;
    private float requestedDistanceFromTarget = 0.0F;
    private float distanceFromTarget = 0.0F;
    private float lerpDistanceDampening = 4.0F;

    // Calculated Rotational fields
    private Quaternion cameraRotation;
    private float lerpRotationDampening = 4.0F;
    // Continuously calculated, accurate EulerAngles
    private float xRotation = 0.0F;
    private float yRotation = 0.0F;
    private float zRotation = 0.0F;

    // State fields
    private static CameraState cameraState;
    private bool isInitialized = false;

    void Awake() {
        // the 1st method called, setting enabled to true
        Debug.Log("Awake() called. Enabled = " + enabled);
    }

    void OnEnable() {
        // called when enabled set to true, including by Awake()
        Debug.Log("OnEnable() called. Enabled = " + enabled);
        if (isInitialized) {
            eventManager.AddListener<FocusSelectedEvent>(OnFocusSelected);
        }
    }

    void Start() {
        // called after all components have been awoken and enabled
        Debug.Log("Start() called. Enabled = " + enabled);
        Initialize();
    }
    void OnApplicationFocus(bool isFocus) {
        if (isInitialized) {
            // ignore 1st false call after Awake(). Bug? as window clearly has focus without receiving a true call
            enabled = isFocus;
        }
        Debug.Log("OnApplicationFocus(" + isFocus + ") called. Enabled = " + enabled);
    }

    void OnApplicationPause(bool isPaused) {
        // called when minimized/resumed
        if (isInitialized) {
            // ignore 1st false call after Awake()       
            enabled = !isPaused;
        }
        Debug.LogWarning("OnApplicationPause(" + isPaused + ") called. Enabled = " + enabled);
    }

    void OnDisable() {
        // called when enabled set to false
        Debug.Log("OnDisable() called. Enabled = " + enabled);
        eventManager.RemoveListener<FocusSelectedEvent>(OnFocusSelected);
    }

    private void Initialize() {
        eventManager = GameEventManager.Instance;
        eventManager.AddListener<FocusSelectedEvent>(OnFocusSelected);

        _transform = transform;    // cache it as transform is actually GetComponent<Transform>()
        dummyTargetGO = GameObject.Find("Temp Camera Target");
        if (dummyTargetGO == null) {
            dummyTargetGO = new GameObject("Temp Camera Target");
        }
        IsResetOnFocus = true;
        IsRollEnabled = true;

        // TODO initial camera position should be focused on the player's starting planet, no rotation
        cameraPosition = _transform.position;

        // initialize to WorldSpace no rotation
        cameraRotation = Quaternion.identity;
        _transform.rotation = cameraRotation;
        xRotation = Vector3.Angle(Vector3.right, _transform.right);  // same as 0.0F
        yRotation = Vector3.Angle(Vector3.up, _transform.up);    // same as 0.0F
        zRotation = Vector3.Angle(Vector3.forward, _transform.forward);  // same as 0.0F

        Debug.Log("Camera initialized. Camera Rotation = " + cameraRotation);
        focusTransform = null;
        // for now, create a temporary target just in front of the camera 
        Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        SetTargetAtScreenPoint(screenCenter);
        ChangeState(CameraState.Freeform);
        isInitialized = true;
    }

    private void OnFocusSelected(FocusSelectedEvent e) {
        Debug.Log("FocusSelectedEvent received.");
        CleanupOldState();
        // changing state split like this to accomodate old condition where the previous focus needed
        // to be told it was no longer the focus before we lost the reference to it
        focusTransform = e.FocusTransform;
        InitializeNewState(CameraState.Focused);

        // IMPROVE Another approach to selecting a focus object: routinely ray or sphere cast to check for new (non dummy)
        // targets (perhaps if mouse is moving with no button pressed?), keeping target continually updated. If a 
        // middle button click occurs, and the current target is not a dummy target, then make the current target the focus.
    }

    private void ChangeState(CameraState newState) {
        CleanupOldState();
        InitializeNewState(newState);
    }

    private void CleanupOldState() {
        CameraState oldState = cameraState;
        // if this is initialization, oldState is null
        switch (oldState) {
            case CameraState.Focused:
                if (focusTransform != null) {
                    // focusTransform.GetComponent<ClickToFocus>().SetFocusLost(); 
                    // no current need for Celestial object to know if it is or is not the focus 
                }
                break;
            case CameraState.Freeform:
                break;
            case CameraState.None:
            default:
                // throw Illegal state exception
                break;
        }
    }

    private void InitializeNewState(CameraState newState) {
        Arguments.ValidateNotNull(targetTransform);

        cameraState = newState;
        switch (newState) {
            case CameraState.Focused:
                Arguments.ValidateNotNull(focusTransform);

                targetTransform = focusTransform;

                distanceFromTarget = Vector3.Distance(targetTransform.position, cameraPosition);
                requestedDistanceFromTarget = optimalDistanceFromTarget;
                // face the selected focus
                xRotation = Vector3.Angle(_transform.right, targetTransform.right);
                yRotation = Vector3.Angle(_transform.up, targetTransform.up);
                zRotation = Vector3.Angle(_transform.forward, targetTransform.forward);

                if (IsResetOnFocus) {
                    ResetToWorldspace();
                }
                break;
            case CameraState.Freeform:
                focusTransform = null;
                distanceFromTarget = Vector3.Distance(targetTransform.position, cameraPosition);
                requestedDistanceFromTarget = distanceFromTarget;
                // no facing change
                break;
            case CameraState.None:
            default:
                // throw Illegal CameraState Exception
                break;
        }
        targetDirection = (targetTransform.position - _transform.position).normalized;
        Debug.Log("CameraState changed to " + cameraState);
    }

    public void ResetToWorldspace() {
        // current and requested distance to target already set

        // reset camera rotation to worldspace, no rotation
        cameraRotation = Quaternion.identity;
        _transform.rotation = cameraRotation;
        xRotation = Vector3.Angle(Vector3.right, _transform.right); // same as 0.0F
        yRotation = Vector3.Angle(Vector3.up, _transform.up);   // same as 0.0F
        zRotation = Vector3.Angle(Vector3.forward, _transform.forward); // same as 0.0F

        // set the desired direction the target should be located at relative to the camera and let LateUpdate() slerp the camera to the calculated position
        targetDirection = _transform.forward;

        Debug.Log("ResetToWorldSpace called. Worldspace Camera Rotation = " + cameraRotation);
        Debug.Log("Target Position = " + targetTransform.position);
    }

    int count = 0;
    void Update() {
        if (Input.GetMouseButtonDown((int)MouseButton.Middle)) {
            count++;
            Debug.Log("Middle Mouse Button has been pressed. Count is = " + count);
        }
    }

    void LateUpdate() {
        float timeSinceLastUpdate = Time.deltaTime;
        bool isTranslationMove = false;
        bool toLockCursor = false;

        // Fields specific to translations without a target
        float dollyDistance = 0.0F;
        float truckDistance = 0.0F;
        float pedestalDistance = 0.0F;


        switch (cameraState) {
            case CameraState.Focused:
                if (truckPedestal.isActivated() || fastTruckPedestal.isActivated()) {
                    ChangeState(CameraState.Freeform);
                    return;
                }
                if (orbit.isActivated()) {
                    toLockCursor = true;
                    xRotation += Input.GetAxis(UnityConstants.MouseAxisName_Horizontal) * orbit.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                    yRotation -= Input.GetAxis(UnityConstants.MouseAxisName_Vertical) * orbit.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                    lerpRotationDampening = orbit.lerpDampening;
                }
                if (focusEdgeOrbit.isActivated()) {
                    float xMousePosition = Input.mousePosition.x;
                    if (xMousePosition <= activeScreenEdge) {
                        xRotation -= focusEdgeOrbit.sensitivity * timeSinceLastUpdate;
                        lerpRotationDampening = focusEdgeOrbit.lerpDampening;
                    }
                    else if (xMousePosition >= Screen.width - activeScreenEdge) {
                        xRotation += focusEdgeOrbit.sensitivity * timeSinceLastUpdate;
                        lerpRotationDampening = focusEdgeOrbit.lerpDampening;
                    }
                }
                if (focusRoll.isActivated()) {
                    toLockCursor = true;
                    zRotation -= Input.GetAxis(UnityConstants.MouseAxisName_Horizontal) * focusRoll.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                    lerpRotationDampening = focusRoll.lerpDampening;
                }
                if (focusZoom.isActivated()) {
                    float mouseScrollWheelValue = Input.GetAxis(UnityConstants.MouseAxisName_ScrollWheel);
                    if (mouseScrollWheelValue > 0) {
                        // Scroll ZoomIN Command
                        SetTargetAtScreenPoint(Input.mousePosition);
                        if (targetTransform != focusTransform) {
                            ChangeState(CameraState.Freeform);
                            return;
                        }
                    }
                    requestedDistanceFromTarget -= mouseScrollWheelValue * Mathf.Abs(requestedDistanceFromTarget) * focusZoom.sensitivity * timeSinceLastUpdate;
                    requestedDistanceFromTarget = Mathf.Clamp(requestedDistanceFromTarget, minimumDistanceFromTarget, universeDiameter);
                    lerpDistanceDampening = focusZoom.lerpDampening;
                }
                if (focusEdgeZoom.isActivated()) {
                    float yMousePosition = Input.mousePosition.y;
                    if (yMousePosition <= activeScreenEdge) {
                        requestedDistanceFromTarget += Mathf.Abs(requestedDistanceFromTarget) * focusEdgeZoom.sensitivity * timeSinceLastUpdate;
                        requestedDistanceFromTarget = Mathf.Clamp(requestedDistanceFromTarget, minimumDistanceFromTarget, universeDiameter);
                        lerpDistanceDampening = focusEdgeZoom.lerpDampening;
                    }
                    else if (yMousePosition >= Screen.height - activeScreenEdge) {
                        requestedDistanceFromTarget -= Mathf.Abs(requestedDistanceFromTarget) * focusEdgeZoom.sensitivity * timeSinceLastUpdate;
                        requestedDistanceFromTarget = Mathf.Clamp(requestedDistanceFromTarget, minimumDistanceFromTarget, universeDiameter);
                        lerpDistanceDampening = focusEdgeZoom.lerpDampening;
                    }
                }
                // transform.forward is the camera's current definition of 'forward', ie. WorldSpace's absolute forward adjusted by the camera's rotation (Vector.forward * cameraRotation )   
                targetDirection = _transform.forward;
                break;

            case CameraState.Freeform:
                if (freeEdgeZoom.isActivated()) {
                    float yMousePosition = Input.mousePosition.y;
                    if (yMousePosition <= activeScreenEdge) {
                        isTranslationMove = true;
                        lerpDistanceDampening = freeEdgeZoom.lerpDampening;
                        float requestedDollyDistance = -freeEdgeZoom.sensitivity * timeSinceLastUpdate;
                        //Debug.Log("Below Screen FreeEdgeZoom RequestedDollyDistance = " + requestedDollyDistance);
                        dollyDistance = Mathf.Lerp(dollyDistance, requestedDollyDistance, lerpDistanceDampening * timeSinceLastUpdate);
                        truckDistance = 0.0F;
                        pedestalDistance = 0.0F;
                    }
                    else if (yMousePosition >= Screen.height - activeScreenEdge) {
                        isTranslationMove = true;
                        lerpDistanceDampening = freeEdgeZoom.lerpDampening;
                        float requestedDollyDistance = freeEdgeZoom.sensitivity * timeSinceLastUpdate;
                        //Debug.Log("Above Screen FreeEdgeZoom RequestedDollyDistance = " + requestedDollyDistance);
                        dollyDistance = Mathf.Lerp(dollyDistance, requestedDollyDistance, lerpDistanceDampening * timeSinceLastUpdate);
                        truckDistance = 0.0F;
                        pedestalDistance = 0.0F;
                    }
                }
                if (freeEdgePan.isActivated()) {
                    float xMousePosition = Input.mousePosition.x;
                    if (xMousePosition <= activeScreenEdge) {
                        xRotation -= freeEdgePan.sensitivity * timeSinceLastUpdate;
                        lerpRotationDampening = freeEdgePan.lerpDampening;
                    }
                    else if (xMousePosition >= Screen.width - activeScreenEdge) {
                        xRotation += freeEdgePan.sensitivity * timeSinceLastUpdate;
                        lerpRotationDampening = freeEdgePan.lerpDampening;
                    }
                }
                if (truckPedestal.isActivated()) {
                    isTranslationMove = true;
                    toLockCursor = true;
                    lerpDistanceDampening = truckPedestal.lerpDampening;
                    float requestedPedestalDistance = Input.GetAxis(UnityConstants.MouseAxisName_Vertical) * truckPedestal.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                    pedestalDistance = Mathf.Lerp(pedestalDistance, requestedPedestalDistance, lerpDistanceDampening * timeSinceLastUpdate);
                    float requestedTruckDistance = Input.GetAxis(UnityConstants.MouseAxisName_Horizontal) * truckPedestal.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                    truckDistance = Mathf.Lerp(truckDistance, requestedTruckDistance, lerpDistanceDampening * timeSinceLastUpdate);
                    dollyDistance = 0.0F;
                }
                if (fastTruckPedestal.isActivated()) {
                    isTranslationMove = true;
                    toLockCursor = true;
                    lerpDistanceDampening = fastTruckPedestal.lerpDampening;
                    float requestedPedestalDistance = Input.GetAxis(UnityConstants.MouseAxisName_Vertical) * fastTruckPedestal.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                    pedestalDistance = Mathf.Lerp(pedestalDistance, requestedPedestalDistance, lerpDistanceDampening * timeSinceLastUpdate);
                    float requestedTruckDistance = Input.GetAxis(UnityConstants.MouseAxisName_Horizontal) * fastTruckPedestal.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                    truckDistance = Mathf.Lerp(truckDistance, requestedTruckDistance, lerpDistanceDampening * timeSinceLastUpdate);
                    dollyDistance = 0.0F;
                }
                if (freeRoll.isActivated()) {
                    toLockCursor = true;
                    zRotation -= Input.GetAxis(UnityConstants.MouseAxisName_Horizontal) * freeRoll.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                    lerpRotationDampening = freeRoll.lerpDampening;
                }
                if (panTilt.isActivated()) {
                    toLockCursor = true;
                    xRotation += Input.GetAxis(UnityConstants.MouseAxisName_Horizontal) * panTilt.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                    yRotation -= Input.GetAxis(UnityConstants.MouseAxisName_Vertical) * panTilt.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                    lerpRotationDampening = panTilt.lerpDampening;
                }
                if (freeZoom.isActivated()) {
                    float mouseScrollWheelValue = Input.GetAxis(UnityConstants.MouseAxisName_ScrollWheel);
                    if (mouseScrollWheelValue > 0) {
                        // Scroll ZoomIN command
                        SetTargetAtScreenPoint(Input.mousePosition);
                    }
                    if (mouseScrollWheelValue < 0) {
                        // find (or create) a target directly ahead of camera so zooming OUT always goes backward from facing
                        Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
                        SetTargetAtScreenPoint(screenCenter);
                    }
                    if (mouseScrollWheelValue != 0) {
                        lerpDistanceDampening = freeZoom.lerpDampening;
                        requestedDistanceFromTarget -= mouseScrollWheelValue * Mathf.Abs(requestedDistanceFromTarget) * freeZoom.sensitivity * timeSinceLastUpdate;
                        requestedDistanceFromTarget = Mathf.Clamp(requestedDistanceFromTarget, minimumDistanceFromTarget, universeDiameter);
                        // Debug.Log("FreeZoom RequestedDistanceFromTarget = " + requestedDistanceFromTarget);
                    }
                }

                targetDirection = (targetTransform.position - _transform.position).normalized;
                //Debug.Log("Target Direction is: " + targetDirection);
                break;
            case CameraState.None:
            default:
                // throw Illegal State Exception
                break;
        }

        if (isTranslationMove) {
            //Debug.Log("Translation Move instruction received: ({0}, {1}, {2})".Inject(truckDistance, pedestalDistance, dollyDistance));
            cameraPosition += _transform.right * truckDistance + _transform.up * pedestalDistance + _transform.forward * dollyDistance;
            // keep target values up to date to prevent sudden movements on transition back to target mode
            requestedDistanceFromTarget = (targetTransform.position - cameraPosition).magnitude;
            distanceFromTarget = requestedDistanceFromTarget;
        }
        else {
            cameraRotation = CalculateCameraRotation(xRotation, yRotation, zRotation, timeSinceLastUpdate);

            distanceFromTarget = Mathf.Lerp(distanceFromTarget, requestedDistanceFromTarget, lerpDistanceDampening * timeSinceLastUpdate);
            // Alternative Smoothing approach: float _velocity = 0.0F;
            //distanceFromTarget = Mathf.SmoothDamp(distanceFromTarget, requestedDistanceFromTarget, ref _velocity, 5.0F, Mathf.Infinity, timeSinceLastUpdate);
            //Debug.Log("Actual DistanceFromTarget = " + distanceFromTarget);

            cameraPosition = targetTransform.position - (targetDirection * distanceFromTarget);
            //Debug.Log("Resulting Camera Position = " + cameraPosition);

            // keep translation values zero'd so there are no sudden movements when back in translation mode
            //truckDistance = 0.0F;
            //pedestalDistance = 0.0F;
            //dollyDistance = 0.0F;
        }
        _transform.rotation = cameraRotation;
        _transform.position = cameraPosition;

        ManageCursorDisplay(toLockCursor);
    }

    private static void ManageCursorDisplay(bool toLockCursor) {
        if (Input.GetKeyDown(UnityConstants.Key_Escape)) {
            Screen.lockCursor = false;
        }

        if (toLockCursor && !Screen.lockCursor) {
            Screen.lockCursor = true;
        }
        else if (Screen.lockCursor && !toLockCursor) {
            Screen.lockCursor = false;
        }
    }

    /// <summary>
    /// Assigns the object under the provided point on the screen as the target. If no object is present, the  
    /// DummyTarget is moved to a distant location along the ray to the screenPoint and assigned as the target.
    /// </summary>
    /// <param name="screenPoint">The X, Y screen point as a Vector3. The Z value is ignored.</param>
    private void SetTargetAtScreenPoint(Vector3 screenPoint) {
        Ray ray = camera.ScreenPointToRay(screenPoint);
        RaycastHit targetHit;
        if (Physics.Raycast(ray, out targetHit)) {
            Debug.DrawRay(ray.origin, targetHit.point, Color.yellow);
            if (targetHit.transform != targetTransform) {
                targetTransform = targetHit.transform;
            }
            // Note: distanceToTarget = targetHit.distance + sphereColliderRadius as targetHit is the sphere collider, not the sphere's transform
            requestedDistanceFromTarget = Vector3.Distance(targetTransform.position, _transform.position);
            distanceFromTarget = requestedDistanceFromTarget;
        }
        else {
            // no hit so set target a long way away in the direction of screenPoint
            Vector3 distantPointAlongLineToLocation = ray.GetPoint(universeDiameter / freeZoomSpeedDerater);
            PositionDummyTargetAt(distantPointAlongLineToLocation);
        }
    }

    /// <summary>
    /// Positions the dummy target at the provided location in Worldspace and assigns it as the target.
    /// </summary>
    /// <param name="location">The location in Worldspace.</param>
    private void PositionDummyTargetAt(Vector3 location) {
        float cameraToLocationDistance = Vector3.Distance(location, _transform.position);
        // Debug.Log("Dummy Target Location: " + screenPoint);
        dummyTargetGO.transform.position = location;
        targetTransform = dummyTargetGO.transform;
        requestedDistanceFromTarget = cameraToLocationDistance;
        distanceFromTarget = requestedDistanceFromTarget;
        targetDirection = (targetTransform.position - _transform.position).normalized;
    }

    /// <summary>
    /// Calculates and sets cameraRotation from the provided EulerAngle arguments.
    /// </summary>
    /// <param name="xDeg">The x deg.</param>
    /// <param name="yDeg">The y deg.</param>
    /// <param name="zDeg">The z deg.</param>
    /// <param name="adjustedTime">The elapsed time.</param>
    /// <returns></returns>
    private Quaternion CalculateCameraRotation(float xDeg, float yDeg, float zDeg, float elapsedTime) {
        // keep rotation values exact as a substitute for the unreliable accuracy that comes from reading EulerAngles from the Quaternion
        xRotation = xDeg % 360;
        yRotation = yDeg % 360; //        ClampAngle(yDeg % 360, -80, 80);
        zRotation = zDeg % 360;
        Quaternion desiredRotation = Quaternion.Euler(yRotation, xRotation, zRotation);
        cameraRotation = Quaternion.Slerp(cameraRotation, desiredRotation, lerpRotationDampening * elapsedTime);
        // OPTIMIZE Lerp is faster but not as pretty when the rotation changes are far apart
        return cameraRotation;
    }

    /// <summary>
    /// Clamps the angle.
    /// </summary>
    /// <param name="angle">The angle.</param>
    /// <param name="min">The min.</param>
    /// <param name="max">The max.</param>
    /// <returns></returns>
    private float ClampAngle(float angle, float min, float max) {
        if (angle < -360F) {
            angle += 360F;
        }
        if (angle > 360F) {
            angle -= 360F;
        }
        return Mathf.Clamp(angle, min, max);
    }


    public enum CameraState { None = 0, Focused = 1, Freeform = 2 }

    [Serializable]
    // Handles modifiers keys (Alt, Ctrl, Shift and Apple)
    public class Modifiers {
        public bool altKeyReqd;
        public bool ctrlKeyReqd;
        public bool shiftKeyReqd;
        public bool appleKeyReqd;

        public bool confirmModifierKeyState() {
            return (!altKeyReqd ^ (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))) &&
                (!ctrlKeyReqd ^ (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) &&
                (!shiftKeyReqd ^ (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) &&
                (!appleKeyReqd ^ (Input.GetKey(KeyCode.LeftApple) || Input.GetKey(KeyCode.RightApple)));
        }
    }

    [Serializable]
    // Defines Camera Controls using 1Mouse Button
    public class MouseButtonConfiguration {
        public bool activate;
        public MouseButton mouseButton;
        public Modifiers modifiers;
        public float sensitivity;
        public float lerpDampening = 4.0F;

        public bool isActivated() {
            bool isAnotherButtonDown = false;
            foreach (MouseButton button in Enums<MouseButton>.GetValues()) {
                if (button != mouseButton) {
                    isAnotherButtonDown = isAnotherButtonDown || Input.GetMouseButton((int)button);
                }
            }
            return activate && Input.GetMouseButton((int)mouseButton) && !isAnotherButtonDown && modifiers.confirmModifierKeyState();
        }
    }

    [Serializable]
    // Defines Camera Controls using 2 simultaneous Mouse Buttons
    public class SimultaneousMouseButtonConfiguration {
        public bool activate;
        public MouseButton firstMouseButton;
        public MouseButton secondMouseButton;
        public Modifiers modifiers;
        public float sensitivity;
        public float lerpDampening = 4.0F;

        public bool isActivated() {
            return activate && Input.GetMouseButton((int)firstMouseButton) && Input.GetMouseButton((int)secondMouseButton) && modifiers.confirmModifierKeyState();
        }
    }

    [Serializable]
    // Defines Camera Controls using the Mouse Scroll Wheel
    public class MouseScrollWheelConfiguration {
        public bool activate;
        public Modifiers modifiers;
        public float sensitivity;
        public float lerpDampening = 4.0F;

        public bool isActivated() {
            bool isAnyMouseButtonDown = Input.anyKey;
            return activate && !isAnyMouseButtonDown && modifiers.confirmModifierKeyState();
        }
    }

    [Serializable]
    // Defines Mouse Movement (no Button) Camera controls
    public class ScreenEdgeConfiguration {
        public bool activate;
        public Modifiers modifiers;
        public float sensitivity;
        public float lerpDampening = 4.0F;

        public bool isActivated() {
            bool isAnyMouseButtonDown = Input.anyKey;
            return activate && !isAnyMouseButtonDown && modifiers.confirmModifierKeyState();
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

