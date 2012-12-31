// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GlobeRotationAnimator.cs
// Manages the selection, orientation and movement of textures used to simulate the resultingCameraRotation of desiredRotation globe.
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
public class GlobeRotationAnimator : MonoBehaviour {

    public float xScrollSpeed = 0.015F;
    public float yScrollSpeed = 0.015F;
    // used only if desiredRotation second material is attached to the GO
    public float xScrollSpeedMtl2 = 0.015F;
    public float yScrollSpeedMtl2 = 0.015F;

    private void Awake() {

    }

    private void Start() {

    }

    private void Update() {
        Vector2 offset = new Vector2(Time.time * xScrollSpeed % 1, Time.time * yScrollSpeed % 1);
        renderer.material.SetTextureOffset("_MainTex", offset); // _MainTex = Main Diffuse Texture used by Unity's builtin shaders
        renderer.material.SetTextureOffset("_BumpMap", offset); // _BumpMap = Normal Map used by Unity's builtin shaders
        // Added for IOS compatibility? IMPROVE
        if (renderer.materials.Length > 1) {
            offset = new Vector2(Time.time * xScrollSpeedMtl2 % 1, Time.time * yScrollSpeedMtl2 % 1);
            renderer.materials[1].SetTextureOffset("_MainTex", offset);
            renderer.materials[1].SetTextureOffset("_BumpMap", offset);
        }
    }

    private void LateUpdate() {

    }

}
//}

