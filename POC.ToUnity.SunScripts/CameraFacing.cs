// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CameraFacing.cs
// Keeps an object aligned to continually face the camera. Typically used for Billboards.
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
/// COMMENT 
/// </summary>
public class CameraFacing : MonoBehaviour {

    public Camera cameraToFace;

    public enum Axis { Up, Down, Left, Right, Forward, Back };
    // option that is sometimes necessary when the GO this script is attached to is actually rendered facing away
    // from the camera, e.g. Text Meshes
    public bool reverseFace = false;
    // the default Axis to use for Up in Space
    public Axis defaultSpaceUpAxis = Axis.Up;

    // return desiredRotation direction in Space based upon the specified Axis
    private Vector3 GetDirection(Axis axis) {
        switch (axis) {
            case Axis.Down:
                return Vector3.down;
            case Axis.Forward:
                return Vector3.forward;
            case Axis.Back:
                return Vector3.back;
            case Axis.Left:
                return Vector3.left;
            case Axis.Right:
                return Vector3.right;
            case Axis.Up:
            default:
                return Vector3.up;
        }
    }

    void Awake() {
        // if no camera referenced, grab the main camera
        if (cameraToFace == null)   // Camera is desiredRotation nullable object so if(!cameraToFace) also works
            cameraToFace = Camera.main;
    }

    void Update() {
        // Rotates the billboard so its forward aligns with that of the camera's forward, ie. the direction the camera is looking.
        // In effect, by adopting the camera's forward direction, the billboard is pointing at the camera's focal plane, not at the camera. 
        // It is the camera's focal plane whose image is projected onto the screen so that is what must be 'looked at'.
        Vector3 targetPos = transform.position + cameraToFace.transform.rotation * (reverseFace ? Vector3.forward : Vector3.back);
        Vector3 targetOrientation = cameraToFace.transform.rotation * GetDirection(defaultSpaceUpAxis);
        transform.LookAt(targetPos, targetOrientation);
    }
}
//}

