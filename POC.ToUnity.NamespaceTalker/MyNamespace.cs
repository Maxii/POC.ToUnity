// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: POC.ToUnity.NamespaceTalker.cs
// TODO - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace POC.ToUnity.NamespaceTalker {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// TODO 
    /// </summary>
    public class MyNamespace : MonoBehaviour {

        private int privateInt = 0;

        public string publicMsg = "Message from MyNamespace indicating a Script with no namespace can access a MonoBehaviour inside a namespace.";

        public int IntProperty { get; set; }

        private GameObject myGameObject;


        private void Awake() {

        }

        private void Start() {
            myGameObject = GameObject.Find("MyGameObject");
            DefaultNamespace script1 = myGameObject.GetComponent<DefaultNamespace>();
            if (script1 != null) {
                string script1Msg = script1.publicMsg;
                if (script1Msg != null) {
                    Debug.Log(script1Msg);
                }
                else {
                    Debug.Log("DefaultNamespace is not null, but the message is.");
                }
            }
            else {
                Debug.Log("DefaultNamespace is null indicating that a MonoBehaviourClass inside a namespace cannot see a UnityScript outside of it.");
            }
        }

        private void Update() {

        }

        private void LateUpdate() {

        }

    }
}

