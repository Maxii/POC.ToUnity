// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: HighliteOnMouseOver.cs
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
public class HighliteOnMouseOver : MonoBehaviour {

    public Light sunLight;

    private Color startingColor;

    void Start() {
        if (sunLight == null) {
            GameObject parentSunContainer = GameObject.Find("Sun");
            // Debug.LogWarning("Parent Sun Container GO Name = " + parentSunContainer.name);
            Transform sunBillboardContainer = parentSunContainer.transform.Find("Sun Billboard Container");
            // Debug.LogWarning("Sun Billboard Container Transform Name = " + sunBillboardContainer.name);
            Transform pointLight = sunBillboardContainer.Find("Point light");
            // Debug.LogWarning("Point light Transform Name = " + pointLight.name);
            sunLight = pointLight.light;
        }
    }

    void OnMouseEnter() {
        startingColor = sunLight.color;
        sunLight.color = Color.red;
    }

    void OnMouseExit() {
        sunLight.color = startingColor;
    }

}
//}

