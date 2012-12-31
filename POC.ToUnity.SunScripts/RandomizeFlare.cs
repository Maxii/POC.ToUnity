// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RandomizeFlare.cs
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
public class RandomizeFlare : MonoBehaviour {

    private static System.Random rng = new System.Random();

    public Flare flare1;
    public Flare flare2;

    private void Awake() {

    }

    private void Start() {
        Light light = gameObject.GetComponentInChildren<Light>();
        if (light == null) {
            Debug.LogWarning("RandomizeFlare cannot find light.");
            // attached to GO without a light or the light has been disabled
            return;
        }

        if (light.flare == null) {
            // if there is no flare already assigned, it is because I don't want flares right now
            Debug.Log("There is no flare attached to randomize.");
            return;
        }
        light.flare = (rng.Next(-1, 1) < 0) ? flare1 : flare2;
    }

    private void Update() {

    }

    private void LateUpdate() {

    }

}
//}

