using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ------------------------------------------------------------------
// Name : AIStateMachineLink
// Desc : This class is responsible for linking Unity's Animator 
//        State Machine to our AI State Machine for more granular control.
// ------------------------------------------------------------------
public class AIStateMachineLink : StateMachineBehaviour
{
    // Protected
    protected AIStateMachine _stateMachine; // Reference to the AIStateMachine that this link corresponds to.


    // Public
    public AIStateMachine stateMachine
    {
        set { _stateMachine = value; } // Property to set the AIStateMachine for this link.
    }
}
