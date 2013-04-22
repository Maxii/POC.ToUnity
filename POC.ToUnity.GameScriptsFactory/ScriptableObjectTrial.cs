// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ScriptableObjectTrial.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using CodeEnv.Master.Common;
using UnityEngine;

[Serializable]
/// <summary>
/// Demo class in support of creating ScriptableObject asset files.
/// </summary>
public class ScriptableObjectTrial : ScriptableObject {

    public string trialName = "MyTrialName";
    public int trialValue = 1;


    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

