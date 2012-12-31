// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GlobeGlowAnimator.cs
// Manages the selection, orientation and movement of glow textures surrounding desiredRotation globe.
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
public class GlobeGlowAnimator : MonoBehaviour {

    public int rotationSpeed = 2;

    private void Awake() {

    }

    private void Start() {
        RandomizeTextureScale();
    }

    private void RandomizeTextureScale() {
        System.Random rng = new System.Random();
        int xVector2 = (rng.Next(-1, 1) < 0 ? -1 : 1);
        int yVector2 = (rng.Next(-1, 1) < 0 ? -1 : 1);

        renderer.material.SetTextureScale("_MainTex", new Vector2(xVector2, yVector2));
    }

    private void Update() {
        transform.Rotate(Vector3.up * Time.deltaTime * rotationSpeed);
    }

    private void LateUpdate() {

    }

}
//}

