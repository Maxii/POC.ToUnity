// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ScriptableObjectTrialAsset.cs
// Demo class that enables creation of ScriptableObject asset files.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using UnityEditor;

/// <summary>
/// Demo class that establishes an Assets/Create/&lt;YourScriptableObjectAssetClassName&gt; 
/// MenuItem that enables creation of YourScriptableObjectAssetClassName asset files.
/// </summary>
public class ScriptableObjectTrialAsset {

    [MenuItem(UnityConstants.AssetsCreateMenuItem + "ScriptableObjectTrial")]
    public static void CreateAsset() {
        UnityEditorUtility.CreateScriptableObjectAsset<ScriptableObjectTrial>();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

