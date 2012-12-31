// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ClickToFocus.cs
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
using CodeEnv.Master.UI;

/// <summary>
/// COMMENT 
/// </summary>
public class ClickToFocus : MonoBehaviour {

    EventManager eventManager;

    private bool isSelectedFocus = false;

    public void SetFocusLost() {    // Not needed unless using ZoomTargetChangeEvents
        if (!isSelectedFocus) {
            Debug.LogWarning("SetFocusLost() called without being the selected focus.");
        }
        isSelectedFocus = false;
    }

    private void Awake() {

    }

    private void Start() {
        // Keep at a minimum, an empty Start method so that instances receive the OnDestroy event
        eventManager = EventManager.Instance;
    }

    private void Update() {

    }

    private void LateUpdate() {

    }

    void OnMouseOver() {
        if (!isSelectedFocus && Input.GetMouseButtonDown((int)MouseButton.Middle)) {
            isSelectedFocus = true;
            Debug.Log("FocusSelectedEvent has been Raised.");
            eventManager.Raise<FocusSelectedEvent>(new FocusSelectedEvent(transform));
        }
    }

    //// Left MouseButton clicks are reliably detected by OnMouseDown()
    //// Recommended way to detect Right and Middle MouseButton clicks:
    //void OnMouseOver() {
    //    if (Input.GetMouseButtonDown((int)MouseButton.Right)) {
    //        // Do work on Right or Middle button click on this GameObject
    //    }
    //}


    //void OnMouseEnter() {
    //    if (!isSelectedFocus && !Input.anyKey) {
    //        eventManager.Raise<ZoomTargetChangeEvent>(new ZoomTargetChangeEvent(transform));
    //        Debug.Log("OnMouseEnter() raises a ZoomTargetChangeEvent.");
    //    }
    //}

    //void OnMouseExit() {
    //    if (!isSelectedFocus) {
    //        eventManager.Raise<ZoomTargetChangeEvent>(new ZoomTargetChangeEvent(null));
    //        Debug.Log("OnMouseExit() raises a ZoomTargetChangeEvent with a null transform.");

    //    }
    //}



    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

