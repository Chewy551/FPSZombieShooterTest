// ----------------------------------------------------------------------------
// Copyright (c) 2023 [Chewy551]. All rights reserved.
// ----------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ------------------------------------------------------------------
// Name : RootMotionConfigurator
// Desc : Inherits from AIStateMachineLink. Used to configure root
//        motion based on the AI's current state and animator behavior.
// ------------------------------------------------------------------
public class RootMotionConfigurator : AIStateMachineLink
{
    // Serialized fields for root position and rotation motion adjustments.
    [SerializeField] private int _rootPosition = 0;
    [SerializeField] private int _rootRotation = 0;

    // ------------------------------------------------------------------
    // Name : OnStateEnter
    // Desc : Called by Unity when the AI enters a state. Adjusts the 
    //        root motion based on the AI's current behavior.
    // ------------------------------------------------------------------
    override public void OnStateEnter(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex)
    {
        if (_stateMachine)
        {
            _stateMachine.AddRootMotionRequest(_rootPosition, _rootRotation);
        }
    }

    // ------------------------------------------------------------------
    // Name : OnStateExit
    // Desc : Called by Unity when the AI exits a state. Reverses the 
    //        root motion adjustment made during state entry.
    // ------------------------------------------------------------------
    override public void OnStateExit(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex)
    {
        if (_stateMachine)
        {
            _stateMachine.AddRootMotionRequest(-_rootPosition, -_rootRotation);
        }
    }
}
