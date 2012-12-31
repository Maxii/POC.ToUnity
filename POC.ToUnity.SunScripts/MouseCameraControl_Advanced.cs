// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MouseCameraControl_Advanced.cs
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
using CodeEnv.Master.Resources;
using CodeEnv.Master.CommonUnity;
using CodeEnv.Master.UI;

/// <summary>
/// COMMENT 
/// </summary>
public class MouseCameraControl_Advanced : MonoBehaviour {

    // Mouse Camera Control default configurations

    // Move Camera by using Edge Scrolling
    public MouseMovementConfiguration edgeZoom = new MouseMovementConfiguration { sensitivity = 6F, command = CameraCommand.EdgeZoom, activate = true };
    public MouseMovementConfiguration edgePan = new MouseMovementConfiguration { sensitivity = 6F, command = CameraCommand.EdgePan, activate = true };

    // Move up/down
    public MouseButtonConfiguration truckAndPedestal = new MouseButtonConfiguration { mouseButton = MouseButton.Middle, modifiers = new Modifiers { altKeyReqd = true }, sensitivity = 1F, command = CameraCommand.TruckAndPedestal, };

    // Move up/down fast
    public MouseButtonConfiguration fastTruckAndPedestal = new MouseButtonConfiguration { mouseButton = MouseButton.Middle, modifiers = new Modifiers { altKeyReqd = true, shiftKeyReqd = true }, sensitivity = 3F, command = CameraCommand.FastTruckAndPedestal };

    // Roll around Z axis
    public MouseButtonConfiguration roll = new MouseButtonConfiguration { mouseButton = MouseButton.Right, modifiers = new Modifiers { altKeyReqd = true }, sensitivity = 3F, command = CameraCommand.Roll };

    // Look around 
    public MouseButtonConfiguration panTiltOrbit = new MouseButtonConfiguration { mouseButton = MouseButton.Right, sensitivity = 3f, command = CameraCommand.PanTiltOrbit, activate = true };

    // Move forward/backward
    public MouseScrollWheelConfiguration scrollZoom = new MouseScrollWheelConfiguration { sensitivity = 200F, command = CameraCommand.ScrollZoom, activate = true };
    public SimultaneousMouseButtonConfiguration mouseZoom = new SimultaneousMouseButtonConfiguration { firstMouseButton = MouseButton.Left, secondMouseButton = MouseButton.Right, sensitivity = 10.0F, command = CameraCommand.MouseZoom };

    // TODO
    // Try combining zoomTarget and focus calculation values to get rid of jerkiness when states change
    // Make the cursor disappear when dragging. Tried MouseButtonConfiguration mod but didn't work.
    // Zoom should track in the direction of the cursor and optionally, out from the cursor. Default should be out along the Z axis.
    // Zoom should also not change it's facing when it moves in the direction of the cursor.
    // Implement 360 degree orbit and pan/tilt. Currently limited by Gimbal lock.
    // Should Tilt/EdgePan have some Pedastal/Truck added like Star Ruler?
    // Add a follow moving object capability
    // How should zooming toward cursor combine with an object in focus? Does the zoom add an offset creating a new defacto focus point, ala Star Ruler?

    EventManager eventManager;

    public bool IsResetOnFocus { get; set; }

    private bool isRollEnabled;
    public bool IsRollEnabled {
        get { return isRollEnabled; }
        set { isRollEnabled = value; roll.activate = value; }
    }

    private CameraCommand cameraCommand;
    private CameraState cameraState;

    void Start() { Initialize(); }
    void OnEnable() {
        Initialize();
        eventManager.AddListener<FocusSelectedEvent>(OnFocusSelected);
    }
    private void Initialize() {
        eventManager = EventManager.Instance;
        IsResetOnFocus = false;
        IsRollEnabled = true;
        Reset();
        ChangeState(CameraState.Freeform);
    }

    void OnDisable() {
        eventManager.RemoveListener<FocusSelectedEvent>(OnFocusSelected);
    }

    private void OnFocusSelected(FocusSelectedEvent e) {
        if (focus != null) {
            focus.GetComponent<ClickToFocus>().SetFocusLost();
        }
        if (IsResetOnFocus) {
            Reset();
        }
        focus = e.Focus;
        ChangeState(CameraState.Focused);
        Debug.Log("FocusSelectedEvent received");
    }

    private void ChangeState(CameraState newState) {
        CleanupOldState();
        InitializeNewState(newState);
    }
    private void CleanupOldState() {
        CameraState oldState = cameraState;
        // Does nothing for now
    }
    private void InitializeNewState(CameraState newState) {
        cameraState = newState;
        switch (newState) {
            case CameraState.Focused:
                if (focus == null) {
                    throw new NullReferenceException("Illegal State: Focus should not be null.");
                }

                distanceFromFocus = Vector3.Distance(cameraPosition, focus.position);
                requestedDistanceFromFocus = optimalDistanceFromFocus;
                // face the selected focus
                xRotation = Vector3.Angle(transform.right, focus.right);
                yRotation = Vector3.Angle(transform.up, focus.up);
                zRotation = Vector3.Angle(transform.forward, focus.forward);
                zoomTarget = null;
                break;
            case CameraState.ZoomOnTarget:
                if (zoomTarget == null) {
                    throw new NullReferenceException("Illegal State: ZoomTarget should not be null.");
                }
                distanceFromTarget = Vector3.Distance(cameraPosition, zoomTarget.position);
                requestedDistanceFromTarget = optimalDistanceFromFocus;
                // no facing change
                focus = null;
                break;
            case CameraState.Freeform:
                focus = null;
                zoomTarget = null;
                break;
            default:
                // throw Illegal CameraState Exception
                break;
        }
        Debug.Log("CameraState changed to " + cameraState);
    }

    public void Reset() {
        cameraPosition = transform.position;
        cameraRotation = transform.rotation;

        cameraCommand = CameraCommand.None;
        cameraState = CameraState.Freeform;

        focus = null;
        zoomTarget = null;

        distanceFromFocus = 0.0F;
        requestedDistanceFromFocus = optimalDistanceFromFocus;
        distanceFromTarget = 0.0F;
        requestedDistanceFromTarget = optimalDistanceFromFocus;
        // initialize to current rotation
        xRotation = Vector3.Angle(Vector3.right, transform.right);
        yRotation = Vector3.Angle(Vector3.up, transform.up);
        zRotation = Vector3.Angle(Vector3.forward, transform.forward);
    }

    // General scaling control translating mouse motion into camera motion
    public float mouseMotionSensitivity = 40f;
    // The distance in pixels from screen edge that, when the cursor is present, causes the camera to pan or zoom 
    public float activeScreenEdge = 10f;
    public float lerpDampening = 4.0F;

    // Fields specific to camera controls with Game Object focus
    private Transform focus;
    private float minimumDistanceFromFocus = 3.0F;
    private float optimalDistanceFromFocus = 5.0F;
    private float maximumDistanceFromFocus = 1000.0F;    //  UNCLEAR what use this is

    private float requestedDistanceFromFocus = 0.0F;
    private float distanceFromFocus = 0.0F;

    private Transform zoomTarget;
    private float requestedDistanceFromTarget = 0.0F;
    private float distanceFromTarget = 0.0F;

    // Fields specific to camera controls without a focus
    private float dollyDistance = 0.0F;
    private float truckDistance = 0.0F;
    private float pedestalDistance = 0.0F;

    // General fields
    private float xRotation = 0.0F;
    private float yRotation = 0.0F;
    private float zRotation = 0.0F;

    private Quaternion cameraRotation;
    private Vector3 cameraPosition;

    void LateUpdate() {
        CheckForZoomTargetHit();
        TryAdjustCameraState();

        float timeSinceLastUpdate = Time.deltaTime;

        switch (cameraState) {
            case CameraState.Focused:
                if (cameraCommand == panTiltOrbit.command) {
                    xRotation += Input.GetAxis(UnityConstants.MouseAxisName_Horizontal) * panTiltOrbit.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                    yRotation -= Input.GetAxis(UnityConstants.MouseAxisName_Vertical) * panTiltOrbit.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                    cameraRotation = CalculateCameraRotation(xRotation, yRotation, 0, timeSinceLastUpdate);
                }
                if (cameraCommand == roll.command) {
                    zRotation -= Input.GetAxis(UnityConstants.MouseAxisName_Horizontal) * roll.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                    cameraRotation = CalculateCameraRotation(xRotation, yRotation, zRotation, timeSinceLastUpdate);
                }
                if (cameraCommand == scrollZoom.command) {
                    requestedDistanceFromFocus -= Input.GetAxis(UnityConstants.MouseAxisName_ScrollWheel) * Mathf.Abs(requestedDistanceFromFocus) * scrollZoom.sensitivity * timeSinceLastUpdate;
                    requestedDistanceFromFocus = Mathf.Clamp(requestedDistanceFromFocus, minimumDistanceFromFocus, maximumDistanceFromFocus);
                    // For smoothing of the zoom, lerp distance
                }
                distanceFromFocus = Mathf.Lerp(distanceFromFocus, requestedDistanceFromFocus, lerpDampening * timeSinceLastUpdate);

                // transform.forward is the camera's current definition of 'forward', ie. WorldSpace's absolute forward adjusted by the camera's rotation (Vector.forward * cameraRotation )   
                if (focus != null) {
                    cameraPosition = focus.position - (transform.forward * distanceFromFocus);
                }
                break;
            case CameraState.ZoomOnTarget:
                if (cameraCommand == scrollZoom.command) {
                    requestedDistanceFromTarget -= Input.GetAxis(UnityConstants.MouseAxisName_ScrollWheel) * Mathf.Abs(requestedDistanceFromTarget) * scrollZoom.sensitivity * timeSinceLastUpdate;
                    requestedDistanceFromTarget = Mathf.Clamp(requestedDistanceFromTarget, minimumDistanceFromFocus, maximumDistanceFromFocus); // FIXME
                    //Debug.Log("RequestedDistanceFromTarget = " + requestedDistanceFromTarget);
                }
                distanceFromTarget = Mathf.Lerp(distanceFromTarget, requestedDistanceFromTarget, lerpDampening * timeSinceLastUpdate);
                //Debug.Log("DistanceFromTarget = " + distanceFromTarget);

                // transform.forward is the camera's current definition of 'forward', ie. WorldSpace's absolute forward adjusted by the camera's rotation (Vector.forward * cameraRotation )   
                if (zoomTarget != null) {   // protect against timing issues where zoomTarget is reset to null before the states can change
                    Vector3 zoomTargetDirection = (zoomTarget.position - transform.position).normalized;
                    cameraPosition = zoomTarget.position - (zoomTargetDirection * distanceFromTarget);
                }
                break;
            case CameraState.Freeform:
                #region ScreenEdge
                if (cameraCommand == edgeZoom.command) {
                    float yMousePosition = Input.mousePosition.y;
                    if (yMousePosition <= activeScreenEdge) {
                        float requestedDollyDistance = -edgeZoom.sensitivity * timeSinceLastUpdate;
                        dollyDistance = Mathf.Lerp(dollyDistance, requestedDollyDistance, lerpDampening * timeSinceLastUpdate);
                        cameraPosition += transform.forward * dollyDistance;
                    }
                    else if (yMousePosition >= Screen.height - activeScreenEdge) {
                        float requestedDollyDistance = edgeZoom.sensitivity * timeSinceLastUpdate;
                        dollyDistance = Mathf.Lerp(dollyDistance, requestedDollyDistance, lerpDampening * timeSinceLastUpdate);
                        cameraPosition += transform.forward * dollyDistance;
                    }
                }
                if (cameraCommand == edgePan.command) {
                    float xMousePosition = Input.mousePosition.x;
                    if (xMousePosition <= activeScreenEdge) {
                        xRotation -= edgePan.sensitivity * timeSinceLastUpdate;
                        cameraRotation = CalculateCameraRotation(xRotation, 0, 0, timeSinceLastUpdate);
                    }
                    else if (xMousePosition >= Screen.width - activeScreenEdge) {
                        xRotation += edgePan.sensitivity * timeSinceLastUpdate;
                        cameraRotation = CalculateCameraRotation(xRotation, 0, 0, timeSinceLastUpdate);
                    }
                }
                #endregion

                if (cameraCommand == truckAndPedestal.command) {
                    float requestedPedestalDistance = Input.GetAxis(UnityConstants.MouseAxisName_Vertical) * truckAndPedestal.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                    pedestalDistance = Mathf.Lerp(pedestalDistance, requestedPedestalDistance, lerpDampening * timeSinceLastUpdate);
                    float requestedTruckDistance = Input.GetAxis(UnityConstants.MouseAxisName_Horizontal) * truckAndPedestal.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                    truckDistance = Mathf.Lerp(truckDistance, requestedTruckDistance, lerpDampening * timeSinceLastUpdate);

                    cameraPosition += transform.up * pedestalDistance + transform.right * truckDistance;
                }
                if (cameraCommand == fastTruckAndPedestal.command) {
                    float requestedPedestalDistance = Input.GetAxis(UnityConstants.MouseAxisName_Vertical) * fastTruckAndPedestal.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                    pedestalDistance = Mathf.Lerp(pedestalDistance, requestedPedestalDistance, lerpDampening * timeSinceLastUpdate);
                    float requestedTruckDistance = Input.GetAxis(UnityConstants.MouseAxisName_Horizontal) * fastTruckAndPedestal.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                    truckDistance = Mathf.Lerp(truckDistance, requestedTruckDistance, lerpDampening * timeSinceLastUpdate);

                    cameraPosition += transform.up * pedestalDistance + transform.right * truckDistance;
                }
                if (cameraCommand == roll.command) {
                    zRotation -= Input.GetAxis(UnityConstants.MouseAxisName_Horizontal) * roll.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                    cameraRotation = CalculateCameraRotation(xRotation, yRotation, zRotation, timeSinceLastUpdate);
                }
                if (cameraCommand == panTiltOrbit.command) {
                    xRotation += Input.GetAxis(UnityConstants.MouseAxisName_Horizontal) * panTiltOrbit.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                    yRotation -= Input.GetAxis(UnityConstants.MouseAxisName_Vertical) * panTiltOrbit.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                    cameraRotation = CalculateCameraRotation(xRotation, yRotation, zRotation, timeSinceLastUpdate);
                }
                if (cameraCommand == scrollZoom.command) {
                    float requestedDollyDistance = Input.GetAxis(UnityConstants.MouseAxisName_ScrollWheel) * scrollZoom.sensitivity * timeSinceLastUpdate;
                    dollyDistance = Mathf.Lerp(dollyDistance, requestedDollyDistance, lerpDampening * timeSinceLastUpdate);
                    cameraPosition += transform.forward * dollyDistance;
                }
                if (cameraCommand == mouseZoom.command) {
                    float requestedDollyDistance = Input.GetAxis(UnityConstants.MouseAxisName_Vertical) * mouseZoom.sensitivity * timeSinceLastUpdate;
                    dollyDistance = Mathf.Lerp(dollyDistance, requestedDollyDistance, lerpDampening * timeSinceLastUpdate);
                    cameraPosition += transform.forward * dollyDistance;
                }
                // Cannot adjust cameraPosition here as below as truckDistance and pedestalDistance retain their values when their configuration is no longer active 
                // causing continued momentun-like movement. dollyDistance lerps to zero as zoom, unlike the others, is always active. 
                //cameraPosition += focus.forward * dollyDistance + focus.right * truckDistance + focus.up * pedestalDistance;
                break;
            default:
                // throw Illegal State Exception
                break;
        }

        transform.rotation = cameraRotation;
        transform.position = cameraPosition;
    }

    private void CheckForZoomTargetHit() {
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit zoomTargetHit;
        if (Physics.Raycast(ray, out zoomTargetHit)) {
            Debug.DrawRay(ray.origin, zoomTargetHit.point, Color.yellow);
            zoomTarget = zoomTargetHit.transform;
        }
        else {
            zoomTarget = null;
        }
    }

    private CameraCommand checkForCameraCommand() {
        if (scrollZoom.isActivated() && Input.GetAxis(UnityConstants.MouseAxisName_ScrollWheel) != 0) {
            return scrollZoom.command;
        }
        if (edgePan.isActivated()) {
            float xMousePosition = Input.mousePosition.x;
            if (xMousePosition <= activeScreenEdge || xMousePosition >= Screen.width - activeScreenEdge) {
                return edgePan.command;
            }
        }
        if (roll.isActivated()) {
            return roll.command;
        }
        if (panTiltOrbit.isActivated()) {
            return panTiltOrbit.command;
        }
        if (edgeZoom.isActivated()) {
            float yMousePosition = Input.mousePosition.y;
            if (yMousePosition <= activeScreenEdge || yMousePosition >= Screen.height - activeScreenEdge) {
                return edgeZoom.command;
            }
        }
        if (truckAndPedestal.isActivated()) {
            return truckAndPedestal.command;
        }
        if (fastTruckAndPedestal.isActivated()) {
            return fastTruckAndPedestal.command;
        }
        if (mouseZoom.isActivated()) {
            return mouseZoom.command;
        }
        return CameraCommand.None;
    }

    private void TryAdjustCameraState() {
        cameraCommand = checkForCameraCommand();
        switch (cameraState) {
            case CameraState.Focused:
                switch (cameraCommand) {
                    case CameraCommand.ScrollZoom:
                        if (zoomTarget != null) {
                            ChangeState(CameraState.ZoomOnTarget);
                        }
                        break;
                    case CameraCommand.EdgeZoom:
                    case CameraCommand.MouseZoom:
                    case CameraCommand.EdgePan:
                    case CameraCommand.TruckAndPedestal:
                    case CameraCommand.FastTruckAndPedestal:
                        ChangeState(CameraState.Freeform);
                        break;
                    case CameraCommand.Roll:
                    case CameraCommand.PanTiltOrbit:
                    case CameraCommand.None:
                        // do nothing
                        break;
                    default:
                        // TODO
                        break;
                }
                break;
            case CameraState.Freeform:
                switch (cameraCommand) {
                    case CameraCommand.ScrollZoom:
                        if (zoomTarget != null) {
                            ChangeState(CameraState.ZoomOnTarget);
                        }
                        break;
                    case CameraCommand.EdgePan:
                    case CameraCommand.EdgeZoom:
                    case CameraCommand.FastTruckAndPedestal:
                    case CameraCommand.MouseZoom:
                    case CameraCommand.PanTiltOrbit:
                    case CameraCommand.Roll:
                    case CameraCommand.TruckAndPedestal:
                    case CameraCommand.None:
                        // do nothing
                        break;
                    default:
                        // TODO
                        break;
                }
                break;
            case CameraState.ZoomOnTarget:
                switch (cameraCommand) {
                    case CameraCommand.EdgePan:
                    case CameraCommand.EdgeZoom:
                    case CameraCommand.FastTruckAndPedestal:
                    case CameraCommand.MouseZoom:
                    case CameraCommand.PanTiltOrbit:
                    case CameraCommand.Roll:
                    case CameraCommand.TruckAndPedestal:
                        ChangeState(CameraState.Freeform);
                        break;
                    case CameraCommand.ScrollZoom:
                        if (zoomTarget == null) {
                            ChangeState(CameraState.Freeform);
                        }
                        break;
                    case CameraCommand.None:
                        // do nothing
                        break;
                    default:
                        // TODO throw unknown state exception
                        break;
                }
                break;
            default:
                // TODO throw Illegal State Exception
                break;
        }
    }

    /// <summary>
    /// Calculates the camera rotation.
    /// </summary>
    /// <param name="xDeg">The x deg.</param>
    /// <param name="yDeg">The y deg.</param>
    /// <param name="zDeg">The z deg.</param>
    /// <param name="elapsedTime">The elapsed time.</param>
    /// <returns></returns>
    private Quaternion CalculateCameraRotation(float xDeg, float yDeg, float zDeg, float elapsedTime) {
        float yDegClamped = (yDeg == 0.0F) ? yDeg : ClampAngle(yDeg, -80, 80);
        Quaternion desiredRotation = Quaternion.Euler(yDegClamped, xDeg, zDeg);
        return Quaternion.Lerp(transform.rotation, desiredRotation, lerpDampening * elapsedTime);
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

    public enum CameraCommand {
        None = 0,
        ScrollZoom = 1,
        EdgeZoom = 2,
        MouseZoom = 3,
        EdgePan = 4,
        PanTiltOrbit = 5,
        TruckAndPedestal = 6,
        FastTruckAndPedestal = 7,
        Roll = 8
    }

    private enum CameraState { None = 0, Focused = 1, ZoomOnTarget = 2, Freeform = 3 }

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
        public CameraCommand command;

        public bool isActivated() {
            bool isOtherButtonDown = false;
            foreach (MouseButton button in Enums<MouseButton>.GetValues()) {
                if (button != mouseButton) {
                    isOtherButtonDown = isOtherButtonDown || Input.GetMouseButton((int)button);
                }
            }
            bool isActivated = activate && Input.GetMouseButton((int)mouseButton) && !isOtherButtonDown && modifiers.confirmModifierKeyState();
            // Screen.showCursor = !isActivated; Does not work
            return isActivated;
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
        public CameraCommand command;

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
        public CameraCommand command;

        public bool isActivated() {
            bool isAnyMouseButtonDown = Input.anyKey;
            return activate && !isAnyMouseButtonDown && modifiers.confirmModifierKeyState();
        }
    }

    [Serializable]
    // Defines Mouse Movement (no Button) Camera controls
    public class MouseMovementConfiguration {

        public bool activate;
        public Modifiers modifiers;
        public float sensitivity;
        public CameraCommand command;

        public bool isActivated() {
            bool isAnyMouseButtonDown = Input.anyKey;
            return activate && !isAnyMouseButtonDown && modifiers.confirmModifierKeyState();
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

