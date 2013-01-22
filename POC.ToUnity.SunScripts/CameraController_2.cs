// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CameraController_2.cs
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
using CodeEnv.Master.Common.Game;

[Serializable]
/// <summary>
/// COMMENT 
/// </summary>
public class CameraController_2 : MonoBehaviour {

    // Focused Zooming: When focused, top and bottom Edge zooming and arrow key zooming cause camera movement in and out from the focused object that is centered on the screen. 
    // ScrollWheel zooming normally does the same if the cursor is pointed at the focused object. If the cursor is pointed somewhere else, scrolling IN moves toward the cursor resulting 
    // in a change to Freeform scrolling. By default, Freeform scrolling OUT is directly opposite the camera's facing. However, there is an option to scroll OUT from the cursor instead. 
    // If this is selected, then scrolling OUT while the cursor is not pointed at the focused object will also result in Freeform scrolling.
    public ScreenEdgeConfiguration edgeFocusZoom = new ScreenEdgeConfiguration { sensitivity = 0.03F, activate = true };
    public MouseScrollWheelConfiguration scrollFocusZoom = new MouseScrollWheelConfiguration { sensitivity = 0.5F, activate = true };
    public ArrowKeyboardConfiguration keyFocusZoom = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, sensitivity = 0.1F, activate = true };

    // Freeform Zooming: When not focused, top and bottom Edge zooming and arrow key zooming cause camera movement forward or backward along the camera's facing.
    // ScrollWheel zooming on the other hand always moves toward the cursor when scrolling IN. By default, scrolling OUT is directly opposite
    // the camera's facing. However, there is an option to scroll OUT from the cursor instead. 
    public ScreenEdgeConfiguration edgeFreeZoom = new ScreenEdgeConfiguration { activate = true };
    public ArrowKeyboardConfiguration keyFreeZoom = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, activate = true };
    public MouseScrollWheelConfiguration scrollFreeZoom = new MouseScrollWheelConfiguration { activate = true };

    // Panning, Tilting and Orbiting: When focused, side edge actuation, arrow key pan and tilting and mouse button/movement results in orbiting of the focused object that is centered on the screen. 
    // When not focused the same arrow keys, edge actuation and mouse button/movement results in the camera panning (looking left or right) and tilting (looking up or down) in place.
    public ScreenEdgeConfiguration edgeFreePan = new ScreenEdgeConfiguration { sensitivity = 10F, activate = true };
    public ScreenEdgeConfiguration edgeFocusOrbit = new ScreenEdgeConfiguration { sensitivity = 10F, activate = true };
    public ArrowKeyboardConfiguration keyAllPan = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, sensitivity = 0.5F, activate = true };
    public ArrowKeyboardConfiguration keyAllTilt = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, modifiers = new Modifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, sensitivity = 0.5F, activate = true };
    public MouseButtonConfiguration dragFocusOrbit = new MouseButtonConfiguration { mouseButton = MouseButton.Right, sensitivity = 40.0F, activate = true };
    public MouseButtonConfiguration dragFreePanTilt = new MouseButtonConfiguration { mouseButton = MouseButton.Right, sensitivity = 28.0F, activate = true };

    // Truck and Pedestal: Trucking (moving left and right) and Pedestalling (moving up and down) occurs only in Freeform space, repositioning the camera along it's current horizontal and vertical
    // axis'.
    public MouseButtonConfiguration dragFreeTruck = new MouseButtonConfiguration { mouseButton = MouseButton.Middle, modifiers = new Modifiers { altKeyReqd = true }, sensitivity = 0.3F, activate = true };
    public MouseButtonConfiguration dragFreePedestal = new MouseButtonConfiguration { mouseButton = MouseButton.Middle, modifiers = new Modifiers { shiftKeyReqd = true }, sensitivity = 0.3F, activate = true };
    public ArrowKeyboardConfiguration keyFreePedestal = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, modifiers = new Modifiers { ctrlKeyReqd = true }, activate = true };
    public ArrowKeyboardConfiguration keyFreeTruck = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, modifiers = new Modifiers { ctrlKeyReqd = true }, activate = true };

    // Rolling: Focused and freeform rolling results in the same behaviour, rolling around the camera's current forward axis.
    public MouseButtonConfiguration dragFocusRoll = new MouseButtonConfiguration { mouseButton = MouseButton.Right, modifiers = new Modifiers { altKeyReqd = true }, sensitivity = 40.0F, activate = true };
    public MouseButtonConfiguration dragFreeRoll = new MouseButtonConfiguration { mouseButton = MouseButton.Right, modifiers = new Modifiers { altKeyReqd = true }, sensitivity = 40.0F, activate = true };
    public ArrowKeyboardConfiguration keyAllRoll = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, modifiers = new Modifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, activate = true };

    // TODO
    public SimultaneousMouseButtonConfiguration dragFocusZoom = new SimultaneousMouseButtonConfiguration { firstMouseButton = MouseButton.Left, secondMouseButton = MouseButton.Right, sensitivity = 0.2F, activate = true };
    public SimultaneousMouseButtonConfiguration dragFreeZoom = new SimultaneousMouseButtonConfiguration { firstMouseButton = MouseButton.Left, secondMouseButton = MouseButton.Right, sensitivity = 0.2F, activate = true };

    // LEARNINGS
    // Edge-based requested values need to be normalized for framerate using timeSinceLastUpdate as the change per second is the framerate * sensitivity.
    // Key-based requested values DONOT need to be normalized for framerate using timeSinceLastUpdate as Input.GetAxis() is not framerate dependant.
    // Using +/- Mathf.Abs(requestedDistanceToTarget) accelerates/decelerates movement over time.

    // IMPROVE
    // Should Tilt/EdgePan have some Pedastal/Truck added like Star Ruler?
    // Need more elegant rotation and translation functions when selecting a focus - aka Slerp, Mathf.SmoothDamp/Angle, etc.
    // How should zooming toward cursor combine with an object in focus? Should the zoom add an offset creating a new defacto focus point, ala Star Ruler?
    // Dragging the mouse with any button held down works offscreen OK, but upon release offscreen, immediately enables edge scrolling and panning
    // Implement Camera controls such as clip planes, FieldOfView

    public bool IsResetOnFocusEnabled { get; set; }
    // ScrollWheel always zooms IN on cursor, zooming OUT with the ScrollWheel is directly backwards by default
    public bool IsScrollZoomOutOnCursorEnabled { get; set; }

    private bool isRollEnabled;
    public bool IsRollEnabled {
        get { return isRollEnabled; }
        set { isRollEnabled = value; dragFocusRoll.activate = value; dragFreeRoll.activate = value; }
    }

    public Settings settings = new Settings();

    // Values held outside LateUpdate() so they retain the last mouseInputValue set when the movement
    // instruction that was setting it is no longer being called
    private float positionSmoothingDampener = 4.0F;
    private float rotationSmoothingDampener = 4.0F;

    // External Object references
    private Transform focusTransform;
    private Transform targetTransform;

    // Cached for performance
    private Transform cameraTransform;
    private GameEventManager eventManager;
    private GameObject dummyTargetGO;
    private string[] keyboardAxesNames = new string[] { UnityConstants.KeyboardAxisName_Horizontal, UnityConstants.KeyboardAxisName_Vertical };
    private int collideWithUniverseEdgeLayerOnlyBitMask = 1 << (int)Layers.UniverseEdge;
    private int collideWithDummyTargetLayerOnlyBitMask = 1 << (int)Layers.DummyTarget;


    // Calculated Positional fields    
    private float requestedDistanceFromTarget = 0.0F;
    private float distanceFromTarget = 0.0F;

    // Continuously calculated, accurate EulerAngles
    private float xRotation = 0.0F;
    private float yRotation = 0.0F;
    private float zRotation = 0.0F;

    private enum CameraState { None = 0, Focused = 1, Freeform = 2 }

    // State fields
    private CameraState cameraState;
    [SerializeField]
    private bool isInitialized = false;

    /// <summary>
    /// The 1st method called, setting enabled to true
    /// </summary>
    void Awake() {
        Debug.Log("Awake() called. Enabled = " + enabled);
    }

    /// <summary>
    /// Called when enabled set to true, including by Awake()
    /// </summary>
    void OnEnable() {
        Debug.Log("OnEnable() called. Enabled = " + enabled + ", isInitialized = " + isInitialized);
        if (isInitialized) {
            eventManager.AddListener<FocusSelectedEvent>(OnFocusSelected);
        }
    }

    /// <summary>
    /// Called after all components have been awoken and enabled.
    /// </summary>
    void Start() {
        Debug.Log("Start() called. Enabled = " + enabled);
        Initialize();
    }

    /// <summary>
    /// Called when application goes in/out of focus, this method controls the
    /// enabled state of the camera.
    /// </summary>
    /// <param name="isFocus">if set to <c>true</c> [is focus].</param>
    void OnApplicationFocus(bool isFocus) {
        if (isInitialized) {
            // ignores 1st false call after Awake(). Bug? as window clearly has focus without receiving a true call
            enabled = isFocus;
        }
        Debug.Log("OnApplicationFocus(" + isFocus + ") called. Enabled = " + enabled);
    }

    /// <summary>
    /// Called when the application is minimized/resumed, this method controls the enabled
    /// state of the camera.
    /// </summary>
    /// <param name="isPaused">if set to <c>true</c> [is paused].</param>
    void OnApplicationPause(bool isPaused) {
        // called when minimized/resumed
        if (isInitialized) {
            // ignore 1st false call after Awake()       
            enabled = !isPaused;
        }
        Debug.Log("OnApplicationPause(" + isPaused + ") called. Enabled = " + enabled);
    }

    /// <summary>
    /// Called when enabled set to false.
    /// </summary>
    void OnDisable() {
        Debug.Log("OnDisable() called. Enabled = " + enabled);
        eventManager.RemoveListener<FocusSelectedEvent>(OnFocusSelected);
    }

    private void Initialize() {
        eventManager = GameEventManager.Instance;
        eventManager.AddListener<FocusSelectedEvent>(OnFocusSelected);

        cameraTransform = transform;    // cache it! transform is actually GetComponent<Transform>()

        dummyTargetGO = GameObject.Find(MiscellaneousPhrases.DummyCameraTargetGameObject);
        if (dummyTargetGO == null) {
            dummyTargetGO = new GameObject(MiscellaneousPhrases.DummyCameraTargetGameObject);
        }
        PlaceDummyTargetAtUniverseEdgeInDirection(cameraTransform.forward);
        // Add the collider after placing the DummyTarget so the placement algorithm doesn't accidently find it already in front of the camera
        dummyTargetGO.AddComponent<SphereCollider>().enabled = true;
        dummyTargetGO.layer = (int)Layers.DummyTarget;

        IsResetOnFocusEnabled = true;
        IsRollEnabled = true;
        IsScrollZoomOutOnCursorEnabled = true;

        // UNDONE initial camera position should be focused on the player's starting planet, no rotation
        // initialize to WorldSpace no rotation
        cameraTransform.rotation = Quaternion.identity;
        xRotation = Vector3.Angle(Vector3.right, cameraTransform.right);  // same as 0.0F
        yRotation = Vector3.Angle(Vector3.up, cameraTransform.up);    // same as 0.0F
        zRotation = Vector3.Angle(Vector3.forward, cameraTransform.forward);  // same as 0.0F

        Debug.Log("Camera initialized. Camera Rotation = " + cameraTransform.rotation);
        focusTransform = null;

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
    }

    private void ChangeState(CameraState newState) {
        CleanupOldState();
        InitializeNewState(newState);
    }

    private void CleanupOldState() {
        CameraState oldState = cameraState;
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
                // do nothing. Must be initializing
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(oldState));
        }
    }

    private void InitializeNewState(CameraState newState) {
        Arguments.ValidateNotNull(targetTransform);

        cameraState = newState;
        switch (newState) {
            case CameraState.Focused:
                Arguments.ValidateNotNull(focusTransform);

                targetTransform = focusTransform;

                distanceFromTarget = Vector3.Distance(targetTransform.position, cameraTransform.position);
                requestedDistanceFromTarget = settings.optimalDistanceFromTarget;
                // face the selected focus
                xRotation = Vector3.Angle(cameraTransform.right, targetTransform.right);
                yRotation = Vector3.Angle(cameraTransform.up, targetTransform.up);
                zRotation = Vector3.Angle(cameraTransform.forward, targetTransform.forward);

                if (IsResetOnFocusEnabled) {
                    ResetToWorldspace();
                }
                break;
            case CameraState.Freeform:
                focusTransform = null;
                distanceFromTarget = Vector3.Distance(targetTransform.position, cameraTransform.position);
                requestedDistanceFromTarget = distanceFromTarget;
                // no facing change
                break;
            case CameraState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(newState));
        }
        Debug.Log("CameraState changed to " + cameraState);
    }

    /// <summary>
    /// Resets the camera rotation to that of worldspace, no rotation.
    /// </summary>
    public void ResetToWorldspace() {
        // current and requested distance to target already set
        cameraTransform.rotation = Quaternion.identity;
        xRotation = Vector3.Angle(Vector3.right, cameraTransform.right); // same as 0.0F
        yRotation = Vector3.Angle(Vector3.up, cameraTransform.up);   // same as 0.0F
        zRotation = Vector3.Angle(Vector3.forward, cameraTransform.forward); // same as 0.0F

        Debug.Log("ResetToWorldSpace called. Worldspace Camera Rotation = " + cameraTransform.rotation);
        //Debug.Log("Target Position = " + targetTransform.position);
    }

    void LateUpdate() {
        //Debug.Log("___________________New Frame______________________");
        float timeSinceLastUpdate = Time.deltaTime;
        bool toLockCursor = false;
        float mouseInputValue = 0F;
        Vector3 targetDirection = (targetTransform.position - cameraTransform.position).normalized;

        switch (cameraState) {
            case CameraState.Focused:
                if (dragFreeTruck.IsActivated() || dragFreePedestal.IsActivated()) {
                    ChangeState(CameraState.Freeform);
                    return;
                }
                if (dragFocusOrbit.IsActivated()) {
                    toLockCursor = true;
                    if (GameInput.IsHorizontalMouseMovement(out mouseInputValue)) {
                        xRotation += mouseInputValue * dragFocusOrbit.sensitivity * timeSinceLastUpdate;
                    }
                    if (GameInput.IsVerticalMouseMovement(out mouseInputValue)) {
                        yRotation -= mouseInputValue * dragFocusOrbit.sensitivity * timeSinceLastUpdate;
                    }
                    rotationSmoothingDampener = dragFocusOrbit.dampener;
                }
                if (edgeFocusOrbit.IsActivated()) {
                    float xMousePosition = Input.mousePosition.x;
                    if (xMousePosition <= settings.activeScreenEdge) {
                        xRotation -= edgeFocusOrbit.sensitivity * timeSinceLastUpdate;
                        rotationSmoothingDampener = edgeFocusOrbit.dampener;
                    }
                    else if (xMousePosition >= Screen.width - settings.activeScreenEdge) {
                        xRotation += edgeFocusOrbit.sensitivity * timeSinceLastUpdate;
                        rotationSmoothingDampener = edgeFocusOrbit.dampener;
                    }
                }
                if (dragFocusRoll.IsActivated()) {
                    toLockCursor = true;
                    if (GameInput.IsHorizontalMouseMovement(out mouseInputValue)) {
                        zRotation -= mouseInputValue * dragFocusRoll.sensitivity * timeSinceLastUpdate;
                    }
                    rotationSmoothingDampener = dragFocusRoll.dampener;
                }
                if (scrollFocusZoom.IsActivated()) {
                    if (GameInput.IsScrollWheelMovement(out mouseInputValue)) {
                        if (mouseInputValue > 0 || (mouseInputValue < 0 && IsScrollZoomOutOnCursorEnabled)) {
                            // Scroll ZoomIN Command or ZoomOUT with ZoomOutOnCursorEnabled
                            TrySetNewTargetAtCursor();
                            if (targetTransform != focusTransform) {
                                // new target was selected so change state and startover to reset values
                                ChangeState(CameraState.Freeform);
                                return;
                            }
                        }
                        float positionAccelerationFactor = Mathf.Clamp(Mathf.Abs(requestedDistanceFromTarget), 0F, settings.maximumSpeedControl);
                        requestedDistanceFromTarget -= mouseInputValue * positionAccelerationFactor * scrollFocusZoom.sensitivity * scrollFocusZoom.translationSpeedNormalizer * timeSinceLastUpdate;
                        positionSmoothingDampener = scrollFocusZoom.dampener;
                    }
                }
                if (edgeFocusZoom.IsActivated()) {
                    float yMousePosition = Input.mousePosition.y;
                    if (yMousePosition <= settings.activeScreenEdge) {
                        float positionAccelerationFactor = Mathf.Clamp(Mathf.Abs(requestedDistanceFromTarget), 0F, settings.maximumSpeedControl);
                        requestedDistanceFromTarget += positionAccelerationFactor * edgeFocusZoom.sensitivity * edgeFocusZoom.translationSpeedNormalizer * timeSinceLastUpdate;
                        positionSmoothingDampener = edgeFocusZoom.dampener;
                    }
                    else if (yMousePosition >= Screen.height - settings.activeScreenEdge) {
                        float positionAccelerationFactor = Mathf.Clamp(Mathf.Abs(requestedDistanceFromTarget), 0F, settings.maximumSpeedControl);
                        requestedDistanceFromTarget -= positionAccelerationFactor * edgeFocusZoom.sensitivity * edgeFocusZoom.translationSpeedNormalizer * timeSinceLastUpdate;
                        positionSmoothingDampener = edgeFocusZoom.dampener;
                    }
                }
                if (keyFocusZoom.IsActivated()) {
                    float positionAccelerationFactor = Mathf.Clamp(Mathf.Abs(requestedDistanceFromTarget), 0F, settings.maximumSpeedControl);
                    requestedDistanceFromTarget -= Input.GetAxis(keyboardAxesNames[(int)keyFocusZoom.keyboardAxis]) * positionAccelerationFactor * keyFocusZoom.translationSpeedNormalizer * keyFocusZoom.sensitivity;
                    positionSmoothingDampener = keyFocusZoom.dampener;
                }
                if (dragFocusZoom.IsActivated()) {
                    toLockCursor = true;
                    if (GameInput.IsVerticalMouseMovement(out mouseInputValue)) {
                        // Debug.LogWarning("MouseFocusZoom Vertical Mouse Movement detected.");
                        float positionAccelerationFactor = Mathf.Clamp(Mathf.Abs(requestedDistanceFromTarget), 0F, settings.maximumSpeedControl);
                        requestedDistanceFromTarget -= mouseInputValue * positionAccelerationFactor * dragFocusZoom.sensitivity * dragFocusZoom.translationSpeedNormalizer * timeSinceLastUpdate;
                    }
                    positionSmoothingDampener = dragFocusZoom.dampener;
                }

                // transform.forward is the camera's current definition of 'forward', ie. WorldSpace's absolute forward adjusted by the camera's rotation (Vector.forward * cameraRotation )   
                targetDirection = cameraTransform.forward;   // this is the key that keeps the camera pointed at the target when focused
                break;

            case CameraState.Freeform:
                if (edgeFreePan.IsActivated()) {
                    float xMousePosition = Input.mousePosition.x;
                    if (xMousePosition <= settings.activeScreenEdge) {
                        xRotation -= edgeFreePan.sensitivity * timeSinceLastUpdate;
                        rotationSmoothingDampener = edgeFreePan.dampener;
                    }
                    else if (xMousePosition >= Screen.width - settings.activeScreenEdge) {
                        xRotation += edgeFreePan.sensitivity * timeSinceLastUpdate;
                        rotationSmoothingDampener = edgeFreePan.dampener;
                    }
                }
                if (dragFreeTruck.IsActivated()) {
                    toLockCursor = true;
                    if (GameInput.IsHorizontalMouseMovement(out mouseInputValue)) {
                        PlaceDummyTargetAtUniverseEdgeInDirection(cameraTransform.right);
                        requestedDistanceFromTarget -= mouseInputValue * dragFreeTruck.sensitivity * dragFreeTruck.translationSpeedNormalizer * timeSinceLastUpdate;
                    }
                    positionSmoothingDampener = dragFreeTruck.dampener;
                }
                if (dragFreePedestal.IsActivated()) {
                    toLockCursor = true;
                    if (GameInput.IsVerticalMouseMovement(out mouseInputValue)) {
                        PlaceDummyTargetAtUniverseEdgeInDirection(cameraTransform.up);
                        requestedDistanceFromTarget -= mouseInputValue * dragFreePedestal.sensitivity * dragFreePedestal.translationSpeedNormalizer * timeSinceLastUpdate;
                    }
                    positionSmoothingDampener = dragFreePedestal.dampener;
                }

                if (dragFreeRoll.IsActivated()) {
                    toLockCursor = true;
                    if (GameInput.IsHorizontalMouseMovement(out mouseInputValue)) {
                        zRotation -= mouseInputValue * dragFreeRoll.sensitivity * timeSinceLastUpdate;
                    }
                    rotationSmoothingDampener = dragFreeRoll.dampener;
                }
                if (dragFreePanTilt.IsActivated()) {
                    toLockCursor = true;
                    if (GameInput.IsHorizontalMouseMovement(out mouseInputValue)) {
                        xRotation += mouseInputValue * dragFreePanTilt.sensitivity * timeSinceLastUpdate;
                    }
                    if (GameInput.IsVerticalMouseMovement(out mouseInputValue)) {
                        yRotation -= mouseInputValue * dragFreePanTilt.sensitivity * timeSinceLastUpdate;
                    }
                    rotationSmoothingDampener = dragFreePanTilt.dampener;
                }
                if (scrollFreeZoom.IsActivated()) {
                    if (GameInput.IsScrollWheelMovement(out mouseInputValue)) {
                        if (mouseInputValue > 0) {
                            // Scroll ZoomIN command
                            TrySetNewTargetAtCursor();
                        }
                        if (mouseInputValue < 0) {
                            // Scroll ZoomOUT command
                            if (IsScrollZoomOutOnCursorEnabled) {
                                TrySetNewTargetAtCursor();
                            }
                            else {
                                PlaceDummyTargetAtUniverseEdgeInDirection(-cameraTransform.forward);
                            }
                        }
                        float positionAccelerationFactor = Mathf.Clamp(Mathf.Abs(requestedDistanceFromTarget), 0F, settings.maximumSpeedControl);
                        requestedDistanceFromTarget -= mouseInputValue * positionAccelerationFactor * scrollFreeZoom.sensitivity * scrollFreeZoom.translationSpeedNormalizer * timeSinceLastUpdate;
                        //Debug.Log("ScrollFreeZoom RequestedDistanceFromTarget = " + requestedDistanceFromTarget); 
                        positionSmoothingDampener = scrollFreeZoom.dampener;
                    }
                }
                if (edgeFreeZoom.IsActivated()) {
                    float yMousePosition = Input.mousePosition.y;
                    if (yMousePosition <= settings.activeScreenEdge) {
                        PlaceDummyTargetAtUniverseEdgeInDirection(-cameraTransform.forward);
                        requestedDistanceFromTarget -= edgeFreeZoom.sensitivity * edgeFreeZoom.translationSpeedNormalizer * timeSinceLastUpdate;
                        //Debug.Log("EdgeFreeZoom requestedDistanceFromTarget = " + requestedDistanceFromTarget);
                        positionSmoothingDampener = edgeFreeZoom.dampener;
                    }
                    else if (yMousePosition >= Screen.height - settings.activeScreenEdge) {
                        PlaceDummyTargetAtUniverseEdgeInDirection(cameraTransform.forward);
                        requestedDistanceFromTarget -= edgeFreeZoom.sensitivity * edgeFreeZoom.translationSpeedNormalizer * timeSinceLastUpdate;
                        positionSmoothingDampener = edgeFreeZoom.dampener;
                    }
                }
                if (dragFreeZoom.IsActivated()) {
                    toLockCursor = true;
                    if (GameInput.IsVerticalMouseMovement(out mouseInputValue)) {
                        PlaceDummyTargetAtUniverseEdgeInDirection(cameraTransform.forward);
                        requestedDistanceFromTarget -= mouseInputValue * dragFreeZoom.sensitivity * dragFreeZoom.translationSpeedNormalizer * timeSinceLastUpdate;
                    }
                    positionSmoothingDampener = dragFreeZoom.dampener;
                }

                // Freeform Arrow Keyboard Configurations. Mouse Buttons supercede Arrow Keys. Only Arrow Keys are used as IsActivated() must be governed by 
                //whether the appropriate key is down to keep the configurations from interfering with each other. 
                if (keyFreeZoom.IsActivated()) {
                    PlaceDummyTargetAtUniverseEdgeInDirection(cameraTransform.forward);
                    requestedDistanceFromTarget -= Input.GetAxis(keyboardAxesNames[(int)keyFreeZoom.keyboardAxis]) * keyFreeZoom.sensitivity * keyFreeZoom.translationSpeedNormalizer;
                    positionSmoothingDampener = keyFreeZoom.dampener;
                }
                if (keyFreeTruck.IsActivated()) {
                    PlaceDummyTargetAtUniverseEdgeInDirection(cameraTransform.right);
                    requestedDistanceFromTarget -= Input.GetAxis(keyboardAxesNames[(int)keyFreeTruck.keyboardAxis]) * keyFreeTruck.sensitivity * keyFreeTruck.translationSpeedNormalizer;
                    positionSmoothingDampener = keyFreeTruck.dampener;
                }
                if (keyFreePedestal.IsActivated()) {
                    PlaceDummyTargetAtUniverseEdgeInDirection(cameraTransform.up);
                    requestedDistanceFromTarget -= Input.GetAxis(keyboardAxesNames[(int)keyFreePedestal.keyboardAxis]) * keyFreePedestal.sensitivity * keyFreePedestal.translationSpeedNormalizer;
                    positionSmoothingDampener = keyFreePedestal.dampener;
                }

                targetDirection = (targetTransform.position - cameraTransform.position).normalized;  // needed when another target is picked when in scrollFreeZoom
                //Debug.Log("Target Direction is: " + targetDirection);
                break;
            case CameraState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cameraState));
        }
        // These Arrow Key configurations apply to both freeform and focused states, and at this stage, don't need seperate sensitivity values
        if (keyAllPan.IsActivated()) {
            xRotation += Input.GetAxis(keyboardAxesNames[(int)keyAllPan.keyboardAxis]) * keyAllPan.sensitivity;
            rotationSmoothingDampener = keyAllPan.dampener;
        }
        if (keyAllTilt.IsActivated()) {
            yRotation -= Input.GetAxis(keyboardAxesNames[(int)keyAllTilt.keyboardAxis]) * keyAllTilt.sensitivity;
            rotationSmoothingDampener = keyAllTilt.dampener;
        }
        if (keyAllRoll.IsActivated()) {
            zRotation -= Input.GetAxis(keyboardAxesNames[(int)keyAllRoll.keyboardAxis]) * keyAllRoll.sensitivity;
            rotationSmoothingDampener = keyAllRoll.dampener;
        }

        cameraTransform.rotation = CalculateCameraRotation(xRotation, yRotation, zRotation, rotationSmoothingDampener * timeSinceLastUpdate);

        // Alternative Smoothing approach: float _velocity = 0.0F;
        //distanceFromTarget = Mathf.SmoothDamp(distanceFromTarget, requestedDistanceFromTarget, ref _velocity, 5.0F, Mathf.Infinity, timeSinceLastUpdate);
        requestedDistanceFromTarget = Mathf.Clamp(requestedDistanceFromTarget, settings.minimumDistanceFromTarget, Mathf.Infinity);
        distanceFromTarget = Mathf.Lerp(distanceFromTarget, requestedDistanceFromTarget, positionSmoothingDampener * timeSinceLastUpdate);
        //Debug.Log("Actual DistanceFromTarget = " + distanceFromTarget);
        Vector3 proposedPosition = targetTransform.position - (targetDirection * distanceFromTarget);
        //Debug.Log("Resulting Camera Position = " + cameraTransform.position);

        cameraTransform.position = ValidatePosition(proposedPosition);

        ManageCursorDisplay(toLockCursor);
    }

    /// <summary>
    /// Validates the proposed new position of the camera to be within the universe.
    /// </summary>
    /// <param name="newPosition">The new position.</param>
    /// <returns>if validated, returns newPosition. If not, return the current position.</returns>
    private Vector3 ValidatePosition(Vector3 newPosition) {
        if ((newPosition - UnityConstants.UniverseOrigin).magnitude >= GameValues.UniverseRadius) {
            Debug.LogError("Proposed new position not valid at " + newPosition);
            return cameraTransform.position;
        }
        return newPosition;
    }

    /// <summary>
    /// Manages the display of the cursor during certain movement actions.
    /// </summary>
    /// <param name="toLockCursor">if set to <c>true</c> [to lock cursor].</param>
    private void ManageCursorDisplay(bool toLockCursor) {
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
    /// Attempts to assign any object found under the cursor as the new target. If the existing target is the object encountered, 
    /// then no change is made. If no object is encountered, the DummyTarget is moved to the edge of the universe along the line
    /// to the cursor and assigned as the new target.
    /// </summary>
    private void TrySetNewTargetAtCursor() {
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        int collideWithAllLayersExceptUniverseEdgeBitMask = ~collideWithUniverseEdgeLayerOnlyBitMask;   // not really needed as no collision possible with inside of UniverseEdge
        RaycastHit targetHit;
        if (Physics.Raycast(ray, out targetHit, Mathf.Infinity, collideWithAllLayersExceptUniverseEdgeBitMask)) {
            if (targetHit.transform == targetTransform) {
                // the target under the cursor is the current target object so do nothing
                Debug.Log("Existing Target under cursor found. Name = " + targetTransform.name);
                return;
            }
            // else I've got a new target object
            targetTransform = targetHit.transform;
            Debug.Log("New non-DummyTarget acquired. Name = " + targetTransform.name);
            requestedDistanceFromTarget = Vector3.Distance(targetTransform.position, cameraTransform.position);
            distanceFromTarget = requestedDistanceFromTarget;
        }
        else {
            // no target under cursor so move the dummy to the edge of the universe
            PlaceDummyTargetAtUniverseEdgeInDirection(ray.direction);
        }
    }


    /// <summary>
    /// Places the dummy target at the edge of the universe in the direction provided.
    /// </summary>
    /// <param name="direction">The direction.</param>
    private void PlaceDummyTargetAtUniverseEdgeInDirection(Vector3 direction) {
        if (direction.magnitude == 0F) {
            Debug.LogWarning("Direction Vector to place DummyTarget is " + direction);
            return;
        }
        Ray ray = new Ray(cameraTransform.position, direction.normalized);
        RaycastHit targetHit;
        if (Physics.Raycast(ray, out targetHit, Mathf.Infinity, collideWithDummyTargetLayerOnlyBitMask)) {
            if (dummyTargetGO.transform != targetHit.transform) {
                Debug.LogError("Should find DummyTarget, but it is: " + targetHit.transform.name);
                return;
            }
            else {
                float distanceToUniverseOrigin = (dummyTargetGO.transform.position - UnityConstants.UniverseOrigin).magnitude;
                if (!distanceToUniverseOrigin.CheckRange(GameValues.UniverseRadius, 1.0F)) {
                    Debug.LogError("Dummy Target is not located on UniverseEdge! Position = " + dummyTargetGO.transform.position);
                    return;
                }
                // the dummy target is already there
                //Debug.Log("DummyTarget already present at " + dummyTargetGO.transform.position + ". TargetHit at " + targetHit.transform.position);
                return;
            }
        }
        Vector3 pointOutsideUniverse = ray.GetPoint(GameValues.UniverseRadius * 2);
        if (Physics.Raycast(pointOutsideUniverse, -ray.direction, out targetHit, Mathf.Infinity, collideWithUniverseEdgeLayerOnlyBitMask)) {
            Vector3 universeEdgePoint = targetHit.point;
            dummyTargetGO.transform.position = universeEdgePoint;
            targetTransform = dummyTargetGO.transform;
            //Debug.Log("New DummyTarget location = " + universeEdgePoint);
            requestedDistanceFromTarget = Vector3.Distance(targetTransform.position, cameraTransform.position);
            distanceFromTarget = requestedDistanceFromTarget;
        }
        else {
            Debug.LogError("No Universe Edge point hit! PointOutsideUniverse = " + pointOutsideUniverse + "ReturnDirection = " + -ray.direction);

        }
    }

    /// <summary>
    /// Calculates a new rotation derived from the current rotation and the provided EulerAngle arguments.
    /// </summary>
    /// <param name="xDeg">The x deg.</param>
    /// <param name="yDeg">The y deg.</param>
    /// <param name="zDeg">The z deg.</param>
    /// <param name="adjustedTime">The  elapsed time to use with the Slerp function. Can be adjusted for effect.</param>
    /// <returns></returns>
    private Quaternion CalculateCameraRotation(float xDeg, float yDeg, float zDeg, float adjustedTime) {
        // keep rotation values exact as a substitute for the unreliable accuracy that comes from reading EulerAngles from the Quaternion
        xRotation = xDeg % 360;
        yRotation = yDeg % 360; //        ClampAngle(yDeg % 360, -80, 80);
        zRotation = zDeg % 360;
        Quaternion desiredRotation = Quaternion.Euler(yRotation, xRotation, zRotation);

        return Quaternion.Slerp(cameraTransform.rotation, desiredRotation, adjustedTime);
        // OPTIMIZE Lerp is faster but not as pretty when the rotation changes are far apart
    }

    [Serializable]
    // Settings visible in the Inspector so they can be tweaked
    public class Settings {
        public float minimumDistanceFromTarget = 3.0F;
        public float optimalDistanceFromTarget = 5.0F;
        public float activeScreenEdge = 10F;
        public float maximumSpeedControl = GameValues.UniverseRadius / 20;
    }

    [Serializable]
    // Handles modifiers keys (Alt, Ctrl, Shift and Apple)
    public class Modifiers {
        public bool altKeyReqd;
        public bool ctrlKeyReqd;
        public bool shiftKeyReqd;
        public bool appleKeyReqd;

        internal bool confirmModifierKeyState() { // ^ = Exclusive OR
            return (!altKeyReqd ^ (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))) &&
                (!ctrlKeyReqd ^ (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) &&
                (!shiftKeyReqd ^ (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) &&
                (!appleKeyReqd ^ (Input.GetKey(KeyCode.LeftApple) || Input.GetKey(KeyCode.RightApple)));
        }
    }

    [Serializable]
    // Defines Camera Controls using 1Mouse Button
    public class MouseButtonConfiguration : ConfigurationBase {
        public MouseButton mouseButton;
        // internal float translationSpeedNormalizer = 4.0F * GameValues.UniverseRadius;

        internal override float translationSpeedNormalizer {
            get { return 4.0F * GameValues.UniverseRadius; }
            set { }
        }


        internal override bool IsActivated() {
            return base.IsActivated() && GameInput.IsMouseButtonDown(mouseButton) && !GameInput.IsAnyMouseButtonDownBesides(mouseButton);
        }
    }

    [Serializable]
    // Defines Camera Controls using 2 simultaneous Mouse Buttons
    public class SimultaneousMouseButtonConfiguration : ConfigurationBase {
        public MouseButton firstMouseButton;
        public MouseButton secondMouseButton;
        //internal float translationSpeedNormalizer = 4.0F * GameValues.UniverseRadius;

        internal override float translationSpeedNormalizer {
            get { return 4.0F * GameValues.UniverseRadius; }
            set { }
        }


        internal override bool IsActivated() {
            return base.IsActivated() && GameInput.IsMouseButtonDown(firstMouseButton) && GameInput.IsMouseButtonDown(secondMouseButton);
        }
    }

    [Serializable]
    // Defines Screen Edge Camera controls
    public class ScreenEdgeConfiguration : ConfigurationBase {
        //internal float translationSpeedNormalizer = 0.02F * GameValues.UniverseRadius;

        internal override float translationSpeedNormalizer {
            get { return 0.02F * GameValues.UniverseRadius; }
            set { }
        }


        internal override bool IsActivated() {
            return base.IsActivated() && !GameInput.IsAnyKeyOrMouseButtonDown();
        }
    }

    [Serializable]
    // Defines Mouse Scroll Wheel Camera Controls
    public class MouseScrollWheelConfiguration : ConfigurationBase {
        //internal float translationSpeedNormalizer = 0.1F * GameValues.UniverseRadius;

        internal override float translationSpeedNormalizer {
            get { return 0.1F * GameValues.UniverseRadius; }
            set { }
        }


        internal override bool IsActivated() {
            return base.IsActivated() && !GameInput.IsAnyKeyOrMouseButtonDown();
        }
    }

    [Serializable]
    // Defines the movement associated with the Arrow Keys on the Keyboard
    public class ArrowKeyboardConfiguration : ConfigurationBase {
        public KeyboardAxis keyboardAxis;
        /// <summary>
        /// This factor is used to normalize the translation speed of different input mechanisms (keys, screen edge and mouse dragging)
        /// so that roughly the same distance is covered in a set period of time. The current implementation is a function of
        /// the size of the universe. At this time, this factor is not used to normalize rotation speed.
        /// </summary>
        //internal float translationSpeedNormalizer = 0.0002F * GameValues.UniverseRadius;

        internal override float translationSpeedNormalizer {
            get { return 0.0002F * GameValues.UniverseRadius; }
            set { }
        }

        private bool IsAxisKeyInUse() {
            switch (keyboardAxis) {
                case KeyboardAxis.Horizontal:
                    return Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);
                case KeyboardAxis.Vertical:
                    return Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow);
                case KeyboardAxis.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(keyboardAxis));
            }
        }

        internal override bool IsActivated() {
            return base.IsActivated() && !GameInput.IsAnyMouseButtonDown() && IsAxisKeyInUse();
        }
    }

    [Serializable]
    [HideInInspector]
    public abstract class ConfigurationBase {
        public bool activate;
        public Modifiers modifiers = new Modifiers();
        public float sensitivity = 1.0F;
        public float dampener = 4.0F;

        internal abstract float translationSpeedNormalizer { get; set; }

        internal virtual bool IsActivated() {
            return activate && modifiers.confirmModifierKeyState();
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

