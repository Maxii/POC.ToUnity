// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DefaultNamespace.cs
// TODO - one line to give a brief idea of what this Unity Script does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using POC.ToUnity.NamespaceTalker;

/// <summary>
/// TODO: Update summary.
/// </summary>
public class DefaultNamespace : MonoBehaviour {

    public string publicMsg = "Message from DefaultNamespace indicating a MonoBehaviour inside a namespace can access a Script without a namespace.";

    public int IntProperty { get; set; }

    private GameObject myGameObject;


    private void Awake() {

    }

    private void Start() {
        myGameObject = GameObject.Find("MyGameObject");
        MyNamespace mono1 = myGameObject.GetComponent<MyNamespace>();
        if (mono1 != null) {
            string mono1Msg = mono1.publicMsg;
            if (mono1Msg != null) {
                Debug.Log(mono1Msg);
            }
            else {
                Debug.Log("Mono1 is not null, but the message is.");
            }
        }
        else {
            Debug.Log("Mono1 is null indicating that a MonoBehaviour inside a namespace cannot be accessed.");
        }

    }

    private void Update() {

    }

    private void LateUpdate() {

    }

}

