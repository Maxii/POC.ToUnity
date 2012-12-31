// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MouseCameraControl_Focus.cs
// COMMENT - one line to give desiredRotation brief idea of what this file does.
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
using CodeEnv.Master.UI;
using CodeEnv.Master.CommonUnity;

/// <summary>
/// COMMENT 
/// </summary>
public class MouseCameraControl_Focus : MonoBehaviour {

    // Mouse Camera Control default configurations

    // Move Camera by using Screen Scroll default configuration
    public MouseMovementConfiguration screenEdge = new MouseMovementConfiguration { sensitivity = 6F, activate = true };

    // Move up/down
    public MouseButtonConfiguration pedestal = new MouseButtonConfiguration { mouseButton = MouseButton.Middle, sensitivity = 1F };

    // Move up/down fast
    public MouseButtonConfiguration fastPedestal = new MouseButtonConfiguration { mouseButton = MouseButton.Middle, modifiers = new Modifiers { shiftKeyReqd = true }, sensitivity = 3F };

    // Move left/right
    public MouseButtonConfiguration truck = new MouseButtonConfiguration { mouseButton = MouseButton.Middle, sensitivity = 1F };

    // Move left/right fast
    public MouseButtonConfiguration fastTruck = new MouseButtonConfiguration { mouseButton = MouseButton.Middle, modifiers = new Modifiers { shiftKeyReqd = true }, sensitivity = 3F };

    // Roll around Z axis
    public MouseButtonConfiguration roll = new MouseButtonConfiguration { mouseButton = MouseButton.Right, modifiers = new Modifiers { altKeyReqd = true }, sensitivity = 3F };

    // Look around 
    public MouseButtonConfiguration panAndTilt = new MouseButtonConfiguration { mouseButton = MouseButton.Right, sensitivity = 3f, activate = true };

    // Move forward/backward
    public MouseScrollWheelConfiguration scrollZoom = new MouseScrollWheelConfiguration { sensitivity = 500F, activate = true };
    public SimultaneousMouseButtonConfiguration mouseZoom = new SimultaneousMouseButtonConfiguration { firstMouseButton = MouseButton.Left, secondMouseButton = MouseButton.Right, sensitivity = 10.0F };



    // Orbit around focus object
    public MouseButtonConfiguration orbitOnFocus = new MouseButtonConfiguration { mouseButton = MouseButton.Right, sensitivity = 0.1f, activate = true };

    // Zoom on focus object
    public MouseScrollWheelConfiguration scrollZoomOnFocus = new MouseScrollWheelConfiguration { sensitivity = 16F, activate = true };


    // TODO
    // Make the cursor disappear when dragging. Tried MouseButtonConfiguration mod but didn't work.
    // Add ability to assign and unassign a focus
    // Zoom should track in the direction of the cursor and optionally, out from the cursor. Default should be out along the Z axis.
    // Zoom should also not change it's facing when it moves in the direction of the cursor.
    // Implement 'optimalDistanceFromFocus'
    // Implement 360 degree orbit and pan/tilt. Currently limited by Gimbal lock.
    // Should Tilt/EdgePan have some Pedastal/Truck added like Star Ruler?
    // Add a follow moving object capability
    // How should zooming toward cursor combine with an object in focus? Does the zoom add an offset creating a new defacto focus point, ala Star Ruler?
    // Implement Reset, and call it upon a new focus selection

    EventManager eventManager;


    // Fields specific to camera controls with Game Object focus
    public Transform focus;
    public float minimumDistanceFromFocus = 3.0F;
    public float optimalDistanceFromFocus = 5.0F;
    public float maximumDistanceFromFocus = 1000.0F;    //  UNCLEAR what use this is

    private float requestedDistanceFromFocus = 0.0F;
    private float distanceFromFocus = 0.0F;


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

    void Start() { Initialize(); }
    void OnEnable() {
        eventManager = EventManager.Instance;
        eventManager.AddListener<FocusSelectedEvent>(OnFocusSelected);
    }


    private void OnFocusSelected(FocusSelectedEvent e) {
        Debug.Log("FocusSelectedEvent has arrived!");
        if (focus != null) {
            focus.GetComponent<ClickToFocus>().SetFocusLost();
        }
        focus = e.Focus;
        Reset();
    }

    private void Initialize() {
        eventManager = EventManager.Instance;
        Reset();
    }

    void OnDisable() {
        eventManager.RemoveListener<FocusSelectedEvent>(OnFocusSelected);
    }

    // General scaling control translating mouse motion into camera motion
    public float mouseMotionSensitivity = 40f;
    // The distance in pixels from screen edge that, when the cursor is present, causes the camera to pan or zoom 
    public float activeScreenEdge = 10f;
    public float lerpDampening = 4.0F;

    /// <summary>
    /// Activates/de-activates the Z axis roll feature.
    /// </summary>
    /// <param name="toActivate">if set to <c>true</c> [to activate].</param>
    public void ActivateRoll(bool toActivate) {
        roll.activate = toActivate;
    }

    public void Reset() {
        cameraPosition = transform.position;
        cameraRotation = transform.rotation;

        if (focus != null) {
            distanceFromFocus = Vector3.Distance(cameraPosition, focus.position);
            // face the selected focus
            xRotation = Vector3.Angle(transform.right, focus.right);
            yRotation = Vector3.Angle(transform.up, focus.up);
            zRotation = Vector3.Angle(transform.forward, focus.forward);
            requestedDistanceFromFocus = optimalDistanceFromFocus;
        }
        else {
            xRotation = Vector3.Angle(Vector3.right, transform.right);
            yRotation = Vector3.Angle(Vector3.up, transform.up);
            zRotation = Vector3.Angle(Vector3.forward, transform.forward);
        }

    }


    void LateUpdate() {
        float timeSinceLastUpdate = Time.deltaTime;

        if (focus) {
            if (orbitOnFocus.isActivated()) {
                xRotation += Input.GetAxis(UnityConstants.MouseAxisName_Horizontal) * orbitOnFocus.sensitivity * mouseMotionSensitivity;
                yRotation -= Input.GetAxis(UnityConstants.MouseAxisName_Vertical) * orbitOnFocus.sensitivity * mouseMotionSensitivity;
                cameraRotation = CalculateCameraRotation(xRotation, yRotation, 0, timeSinceLastUpdate);
                // IDEA should I allow rolling when orbiting?
            }
            if (scrollZoomOnFocus.isActivated()) {
                requestedDistanceFromFocus -= Input.GetAxis(UnityConstants.MouseAxisName_ScrollWheel) * Mathf.Abs(requestedDistanceFromFocus) * scrollZoomOnFocus.sensitivity * timeSinceLastUpdate;
                //control the distance from the focus so that it doesn't exceed the minimum and maximum allowed
                requestedDistanceFromFocus = Mathf.Clamp(requestedDistanceFromFocus, minimumDistanceFromFocus, maximumDistanceFromFocus);
                // For smoothing of the zoom, lerp distance
                distanceFromFocus = Mathf.Lerp(distanceFromFocus, requestedDistanceFromFocus, lerpDampening * timeSinceLastUpdate);
            }
            // focus.forward is the camera's current definition of 'forward', ie. WorldSpace's absolute forward adjusted by the camera's rotation (Vector.forward * cameraRotation )   
            cameraPosition = focus.position - (transform.forward * distanceFromFocus);
        }
        else {

            if (screenEdge.isActivated()) {
                #region ScreenEdge
                float xMousePosition = Input.mousePosition.x;
                float yMousePosition = Input.mousePosition.y;

                // Left and Right edge of the screen Pans
                if (xMousePosition <= activeScreenEdge) {
                    xRotation -= screenEdge.sensitivity * timeSinceLastUpdate;
                    cameraRotation = CalculateCameraRotation(xRotation, 0, 0, timeSinceLastUpdate);
                }
                else if (xMousePosition >= Screen.width - activeScreenEdge) {
                    xRotation += screenEdge.sensitivity * timeSinceLastUpdate;
                    cameraRotation = CalculateCameraRotation(xRotation, 0, 0, timeSinceLastUpdate);
                }
                // Top and bottom of the screen Zooms
                else if (yMousePosition <= activeScreenEdge) {
                    float requestedDollyDistance = -screenEdge.sensitivity * timeSinceLastUpdate;
                    dollyDistance = Mathf.Lerp(dollyDistance, requestedDollyDistance, lerpDampening * timeSinceLastUpdate);
                    cameraPosition += transform.forward * dollyDistance;
                }
                else if (yMousePosition >= Screen.height - activeScreenEdge) {
                    float requestedDollyDistance = screenEdge.sensitivity * timeSinceLastUpdate;
                    dollyDistance = Mathf.Lerp(dollyDistance, requestedDollyDistance, lerpDampening * timeSinceLastUpdate);
                    cameraPosition += transform.forward * dollyDistance;
                }
                #endregion
            }

            if (pedestal.isActivated()) {
                float requestedPedestalDistance = Input.GetAxis(UnityConstants.MouseAxisName_Vertical) * pedestal.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                pedestalDistance = Mathf.Lerp(pedestalDistance, requestedPedestalDistance, lerpDampening * timeSinceLastUpdate);
                cameraPosition += transform.up * pedestalDistance;
            }
            if (fastPedestal.isActivated()) {
                float requestedPedestalDistance = Input.GetAxis(UnityConstants.MouseAxisName_Vertical) * fastPedestal.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                pedestalDistance = Mathf.Lerp(pedestalDistance, requestedPedestalDistance, lerpDampening * timeSinceLastUpdate);
                cameraPosition += transform.up * pedestalDistance;
            }
            if (truck.isActivated()) {
                float requestedTruckDistance = Input.GetAxis(UnityConstants.MouseAxisName_Horizontal) * truck.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                truckDistance = Mathf.Lerp(truckDistance, requestedTruckDistance, lerpDampening * timeSinceLastUpdate);
                cameraPosition += transform.right * truckDistance;
            }
            if (fastTruck.isActivated()) {
                float requestedTruckDistance = Input.GetAxis(UnityConstants.MouseAxisName_Horizontal) * fastTruck.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                truckDistance = Mathf.Lerp(truckDistance, requestedTruckDistance, lerpDampening * timeSinceLastUpdate);
                cameraPosition += transform.right * truckDistance;
            }
            if (roll.isActivated()) {
                zRotation -= Input.GetAxis(UnityConstants.MouseAxisName_Horizontal) * roll.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                cameraRotation = CalculateCameraRotation(xRotation, yRotation, zRotation, timeSinceLastUpdate);
            }
            if (panAndTilt.isActivated()) {
                xRotation += Input.GetAxis(UnityConstants.MouseAxisName_Horizontal) * panAndTilt.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                yRotation -= Input.GetAxis(UnityConstants.MouseAxisName_Vertical) * panAndTilt.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
                cameraRotation = CalculateCameraRotation(xRotation, yRotation, zRotation, timeSinceLastUpdate);
            }
            if (scrollZoom.isActivated()) {
                float requestedDollyDistance = Input.GetAxis(UnityConstants.MouseAxisName_ScrollWheel) * scrollZoom.sensitivity * timeSinceLastUpdate;
                dollyDistance = Mathf.Lerp(dollyDistance, requestedDollyDistance, lerpDampening * timeSinceLastUpdate);
                cameraPosition += transform.forward * dollyDistance;
            }
            if (mouseZoom.isActivated()) {
                float requestedDollyDistance = Input.GetAxis(UnityConstants.MouseAxisName_Vertical) * mouseZoom.sensitivity * timeSinceLastUpdate;
                dollyDistance = Mathf.Lerp(dollyDistance, requestedDollyDistance, lerpDampening * timeSinceLastUpdate);
                cameraPosition += transform.forward * dollyDistance;
            }
            // Cannot adjust cameraPosition here as below as truckDistance and pedestalDistance retain their values when their configuration is no longer active 
            // causing continued momentun-like movement. dollyDistance lerps to zero as zoom, unlike the others, is always active. 
            //cameraPosition += focus.forward * dollyDistance + focus.right * truckDistance + focus.up * pedestalDistance;
        }
        transform.rotation = cameraRotation;
        transform.position = cameraPosition;
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

        public bool isActivated() {
            bool isAnyMouseButtonDown = Input.anyKey;
            //foreach (MouseButton button in Enum.GetValues(typeof(MouseButton))) {
            //    isAnyMouseButtonDown = isAnyMouseButtonDown || Input.GetMouseButton((int)button);
            //}
            return activate && !isAnyMouseButtonDown && modifiers.confirmModifierKeyState();

        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

