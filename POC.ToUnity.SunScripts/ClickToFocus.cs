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
using CodeEnv.Master.Common.Resources;
using CodeEnv.Master.Common.UI;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// COMMENT 
/// </summary>
public class ClickToFocus : MonoBehaviour {

    GameEventManager eventManager;

    private bool isSelectedFocus = false;

    [Obsolete]
    public void SetFocusLost() {    // Not needed unless using ZoomTargetChangeEvents
        Debug.LogWarning(transform.gameObject.name + ".SetFocusLost() call received.");
        if (!isSelectedFocus) {
            Debug.LogError("SetFocusLost() called without being the selected focus.");
        }
        isSelectedFocus = false;
    }

    private void Awake() {

    }

    private void Start() {
        // Keep at a minimum, an empty Start method so that instances receive the OnDestroy event
        eventManager = GameEventManager.Instance;
    }

    private void Update() {

    }

    private void LateUpdate() {

    }

    void OnMouseOver() {
        //Debug.Log(transform.gameObject.name + ".OnMouseOver() called. IsSelectedFocus = " + isSelectedFocus);
        if (!isSelectedFocus && Input.GetMouseButtonDown((int)MouseButton.Middle)) {
            //isSelectedFocus = true;   // knowledge of being the focus no longer needed
            Debug.LogWarning("FocusSelectedEvent has been Raised.");
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

