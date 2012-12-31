// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MouseCameraControl_Normal.cs
// TODO - one line to give desiredRotation brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//namespace Trials.ToUnity.SunScripts {

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

/// <summary>
/// TODO 
/// </summary>
[AddComponentMenu("Camera-Control/Mouse Camera Control Normal")]
public class MouseCameraControl_Normal : MonoBehaviour {

    // Default unity names for mouse axes   // IMPROVE change to constants. Move to Unity Constants class.
    string mouseAxisName_Horizontal = "Mouse X";
    string mouseAxisName_Vertical = "Mouse Y";
    string mouseAxisName_ScrollWheel = "Mouse ScrollWheel";

    // TODO Add detection of Mouse type


    // Common Mouse Camera Control default configurations


    // Look left/right
    public MouseButtonConfiguration pan = new MouseButtonConfiguration { mouseButton = MouseButton.Right, sensitivity = 3f };

    // Look up/down
    public MouseButtonConfiguration tilt = new MouseButtonConfiguration { mouseButton = MouseButton.Right, sensitivity = 3f };

    // Look around
    public MouseButtonConfiguration panAndTilt = new MouseButtonConfiguration { mouseButton = MouseButton.Right, sensitivity = 3f };

    // Roll
    public MouseButtonConfiguration roll = new MouseButtonConfiguration { mouseButton = MouseButton.Right, modifiers = new Modifiers { altKeyReqd = true }, sensitivity = 3f };

    // Move Camera by using Screen Scroll default configuration
    public MouseMovementConfiguration moveViaScreenScroll = new MouseMovementConfiguration { sensitivity = 20f };



    //  3 Button Mouse Camera Control default configurations


    // Move up/down
    public MouseButtonConfiguration moveVertical = new MouseButtonConfiguration { mouseButton = MouseButton.Middle, sensitivity = 2f };

    // Move up/down fast
    public MouseButtonConfiguration moveVerticalFast = new MouseButtonConfiguration { mouseButton = MouseButton.Middle, modifiers = new Modifiers { shiftKeyReqd = true }, sensitivity = 4f };

    // Move left/right
    public MouseButtonConfiguration moveHorizontal = new MouseButtonConfiguration { mouseButton = MouseButton.Middle, sensitivity = 2f };

    // Move left/right fast
    public MouseButtonConfiguration moveHorizontalFast = new MouseButtonConfiguration { mouseButton = MouseButton.Middle, modifiers = new Modifiers { shiftKeyReqd = true }, sensitivity = 4f };

    // Move forward/backward
    public MouseScrollWheelConfiguration zoom = new MouseScrollWheelConfiguration { sensitivity = 16F };



    // 2 Button Mouse Camera Control default configurations.    Note: 2 Button Zoom is handled by the KeyBoard arrow keys

    // Move up/down
    //public MouseButtonConfiguration moveVertical2 = new MouseButtonConfiguration { mouseButton = MouseButton.Left, modifiers = new Modifiers { altKeyReqd = true }, sensitivity = 2f };

    // Move up/down fast
    //public MouseButtonConfiguration moveVertical2Fast = new MouseButtonConfiguration { mouseButton = MouseButton.Left, modifiers = new Modifiers { altKeyReqd = true, shiftKeyReqd = true }, sensitivity = 4f };

    // Move left/right
    //public MouseButtonConfiguration moveHorizontal2 = new MouseButtonConfiguration { mouseButton = MouseButton.Left, modifiers = new Modifiers { altKeyReqd = true }, sensitivity = 2f };

    // Move left/right fast 
    //public MouseButtonConfiguration moveHorizontal2Fast = new MouseButtonConfiguration { mouseButton = MouseButton.Left, modifiers = new Modifiers { altKeyReqd = true, shiftKeyReqd = true }, sensitivity = 4f };







    // Members yet to be implemented in support of Game Object focus camera controls

    //public Transform focus;
    //public float targetMinimumDistance;
    //public float targetOptimalDistance;

    //float requestedZoomDistance;
    //float adjustedZoomDistance;
    //float minZoomDistance;
    //float maxZoonDistance;

    //Quaternion cameraRotation;
    //Quaternion requestedCameraRotation;

    // General scaling control translating mouse motion into camera motion
    public float MouseMotionSensitivity = 40f;
    float timeSinceLastUpdate = 0.0f;

    private void LateUpdate() {
        timeSinceLastUpdate = Time.deltaTime;

        //if (focus == null) {
        //    ProcessNormalMouseState();
        //}
        //else {
        //    ProcessTargetedMouseState();
        //}

        // Common Mouse

        if (pan.isActivated()) {
            float xRotation = Input.GetAxis(mouseAxisName_Horizontal) * pan.sensitivity * MouseMotionSensitivity * timeSinceLastUpdate;
            transform.localEulerAngles += new Vector3(0, xRotation, 0);
            // focus.Rotate(0, xRotation, 0);    rotates Z axis too when combined with tilt
        }
        if (tilt.isActivated()) {
            float yRotation = Input.GetAxis(mouseAxisName_Vertical) * tilt.sensitivity * MouseMotionSensitivity * timeSinceLastUpdate;
            Mathf.Clamp(yRotation, -80, 80);    // avoids Gimbal Lock
            transform.localEulerAngles += new Vector3(-yRotation, 0, 0);
            // focus.Rotate(-yRotation, 0, 0);   rotates Z axis too when combined with pan
            //Debug.Log("Z Rotation = " + focus.resultingCameraRotation.eulerAngles.z);
        }
        if (panAndTilt.isActivated()) {
            float xRotation = Input.GetAxis(mouseAxisName_Horizontal) * panAndTilt.sensitivity * MouseMotionSensitivity * timeSinceLastUpdate;
            float yRotation = Input.GetAxis(mouseAxisName_Vertical) * panAndTilt.sensitivity * MouseMotionSensitivity * timeSinceLastUpdate;
            Mathf.Clamp(yRotation, -80, 80);    // avoids Gimbal Lock
            transform.localEulerAngles += new Vector3(-yRotation, xRotation, 0);
            //focus.Rotate(-yRotation, xRotation, 0);   rotates Z axis too when combined with pan
        }
        if (roll.isActivated()) {   // TODO allow natural circular motion to continue roll
            float zRotation = (Input.GetAxis(mouseAxisName_Horizontal) - Input.GetAxis(mouseAxisName_Vertical)) * roll.sensitivity * MouseMotionSensitivity * timeSinceLastUpdate;
            transform.Rotate(0, 0, zRotation);
        }
        if (moveViaScreenScroll.isActivated()) {
            checkCursorForEdgeScroll();
        }



        // 3 Button Mouse

        if (moveVertical.isActivated()) {
            float yTranslation = Input.GetAxis(mouseAxisName_Vertical) * moveVertical.sensitivity * MouseMotionSensitivity * timeSinceLastUpdate;
            transform.Translate(0, yTranslation, 0);
        }
        if (moveVerticalFast.isActivated()) {
            float yTranslation = Input.GetAxis(mouseAxisName_Vertical) * moveVerticalFast.sensitivity * MouseMotionSensitivity * timeSinceLastUpdate;
            transform.Translate(0, yTranslation, 0);
        }
        if (moveHorizontal.isActivated()) {
            float xTranslation = Input.GetAxis(mouseAxisName_Horizontal) * moveHorizontal.sensitivity * MouseMotionSensitivity * timeSinceLastUpdate;
            transform.Translate(xTranslation, 0, 0);
        }
        if (moveHorizontalFast.isActivated()) {
            float xTranslation = Input.GetAxis(mouseAxisName_Horizontal) * moveHorizontalFast.sensitivity * MouseMotionSensitivity * timeSinceLastUpdate;
            transform.Translate(xTranslation, 0, 0);
        }
        if (zoom.isActivated()) {       // UNCLEAR any need for mouseMotionSensitivity here with no Mouse motion. Doesn't hurt either
            float zTranslation = Input.GetAxis(mouseAxisName_ScrollWheel) * zoom.sensitivity * MouseMotionSensitivity * timeSinceLastUpdate;
            transform.Translate(0, 0, zTranslation);
        }



        // 2 Button Mouse

        //if (moveVertical2.isActivated()) {
        //    float requestedPedestalDistance = Input.GetAxis(mouseAxisName_Vertical) * moveVertical2.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
        //    focus.Translate(0, requestedPedestalDistance, 0);
        //}
        //if (moveVertical2Fast.isActivated()) {
        //    float requestedPedestalDistance = Input.GetAxis(mouseAxisName_Vertical) * moveVertical2Fast.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
        //    focus.Translate(0, requestedPedestalDistance, 0);
        //}
        //if (moveHorizontal2.isActivated()) {
        //    float requestedTruckDistance = Input.GetAxis(mouseAxisName_Horizontal) * moveHorizontal2.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
        //    focus.Translate(requestedTruckDistance, 0, 0);
        //}
        //if (moveHorizontal2Fast.isActivated()) {
        //    float requestedTruckDistance = Input.GetAxis(mouseAxisName_Horizontal) * moveHorizontal2Fast.sensitivity * mouseMotionSensitivity * timeSinceLastUpdate;
        //    focus.Translate(requestedTruckDistance, 0, 0);
        //}
    }


    // The distance in pixels from screen edge that, when the cursor is present, causes the camera to scroll (aka - translate)
    public float screenEdgeScrollActiveSpace = 10f;

    /// <summary>
    /// Checks if the cursor is positioned on the edge of the screen thereby instructing the camera to move
    /// </summary>
    private void checkCursorForEdgeScroll() {
        float xMousePosition = Input.mousePosition.x;
        float yMousePosition = Input.mousePosition.y;

        // Move the camera based on Mouse location
        if (xMousePosition < screenEdgeScrollActiveSpace) {
            transform.Translate(-moveViaScreenScroll.sensitivity * timeSinceLastUpdate, 0, 0);
        }
        else if (xMousePosition >= Screen.width - screenEdgeScrollActiveSpace) {
            transform.Translate(moveViaScreenScroll.sensitivity * timeSinceLastUpdate, 0, 0);
        }
        else if (yMousePosition < screenEdgeScrollActiveSpace) {
            transform.Translate(0, -moveViaScreenScroll.sensitivity * timeSinceLastUpdate, 0);
        }
        else if (yMousePosition >= Screen.height - screenEdgeScrollActiveSpace) {
            transform.Translate(0, moveViaScreenScroll.sensitivity * timeSinceLastUpdate, 0);
        }
    }

    // Mouse buttons in the same order as Unity
    public enum MouseButton { Left = 0, Right = 1, Middle = 2, None = 3 }

    [Serializable]
    // Handles left modifiers keys (Alt, Ctrl, Shift)
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
    // Defines Mouse Button Camera controls
    public class MouseButtonConfiguration {

        public bool activate;
        public MouseButton mouseButton;
        public Modifiers modifiers;
        public float sensitivity;

        public bool isActivated() {
            return activate && Input.GetMouseButton((int)mouseButton) && modifiers.confirmModifierKeyState();
        }
    }

    [Serializable]
    // Defines Mouse Scroll Wheel Camera controls
    public class MouseScrollWheelConfiguration {

        public bool activate;
        public Modifiers modifiers;
        public float sensitivity;

        public bool isActivated() {
            return activate && modifiers.confirmModifierKeyState();
        }
    }

    [Serializable]
    // Defines Mouse Movement (no Button) Camera controls
    public class MouseMovementConfiguration {

        public bool activate;
        public Modifiers modifiers;
        public float sensitivity;

        public bool isActivated() {
            bool isAnyMouseButtonDown = false;
            foreach (MouseButton button in Enum.GetValues(typeof(MouseButton))) {
                isAnyMouseButtonDown = isAnyMouseButtonDown || Input.GetMouseButton((int)button);
            }
            return activate && !isAnyMouseButtonDown && modifiers.confirmModifierKeyState();

        }
    }

    //public override string ToString() {
    //    return new ObjectAnalyzer.ToString(this);
    //}



}
//}

