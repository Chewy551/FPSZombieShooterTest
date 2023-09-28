// ----------------------------------------------------------------------------
// Copyright (c) 2023 [Chewy551]. All rights reserved.
// ----------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ------------------------------------------------------------------
// Name : AIState
// Desc : Abstract base class representing the general structure and 
//        functionality of an AI state. This class provides the interface
//        and default implementations for behaviors that all AI states should have.
// ------------------------------------------------------------------
public abstract class AIState : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Name : SetStateMachine
    // Desc : Allows external classes, like AIStateMachine, to associate 
    //        themselves with this state.
    // ------------------------------------------------------------------
    public void SetStateMachine(AIStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    // ------------------------------------------------------------------
    // Name : OnEnterState
    // Desc : Virtual method called when transitioning into this state. 
    //        Can be overridden by derived state classes.
    // ------------------------------------------------------------------
    public virtual void OnEnterState() { }

    // ------------------------------------------------------------------
    // Name : OnExitState
    // Desc : Virtual method called when transitioning out of this state. 
    //        Can be overridden by derived state classes.
    // ------------------------------------------------------------------
    public virtual void OnExitState() { }

    // ------------------------------------------------------------------
    // Name : OnAnimatorUpdated
    // Desc : Called each frame when the Animator's animations are updated.
    //        Adjusts the AI's navigation and rotation based on the Animator's 
    //        root motion if root motion is enabled in the state machine.
    // ------------------------------------------------------------------
    public virtual void OnAnimatorUpdated()
    {
        if (_stateMachine.useRootPosition)
        {
            _stateMachine.NavAgent.velocity = _stateMachine.Animator.deltaPosition / Time.deltaTime;
        }

        if (_stateMachine.useRootRotation)
        {
            _stateMachine.transform.rotation = _stateMachine.Animator.rootRotation;
        }
    }

    // ------------------------------------------------------------------
    // Name : OnAnimatorIKUpdated
    // Desc : Virtual method called when the associated AI's animator updates 
    //        its inverse kinematics. Can be overridden by derived state classes.
    // ------------------------------------------------------------------
    public virtual void OnAnimatorIKUpdated() { }

    // ------------------------------------------------------------------
    // Name : OnTriggerEvent
    // Desc : Virtual method called when a trigger event occurs. Responds based 
    //        on the type of trigger event and the collider involved.
    // ------------------------------------------------------------------
    public virtual void OnTriggerEvent(AITriggerEventType eventType, Collider other) { }

    // ------------------------------------------------------------------
    // Name : OnDestinationReached
    // Desc : Virtual method called when the AI reaches its destination.
    // ------------------------------------------------------------------
    public virtual void OnDestinationReached(bool isReached) { }

    // ------------------------------------------------------------------
    // Name : GetStateType
    // Desc : Abstract method that returns the type of this AI state.
    //        Must be implemented by derived state classes.
    // ------------------------------------------------------------------
    public abstract AIStateType GetStateType();

    // ------------------------------------------------------------------
    // Name : OnUpdate
    // Desc : Abstract method called every frame to update this state's logic. 
    //        Determines if a state transition is needed and returns the 
    //        new state type. Must be implemented by derived state classes.
    // ------------------------------------------------------------------
    public abstract AIStateType OnUpdate();

    // Protected Member Variables
    protected AIStateMachine _stateMachine; // Reference to the controlling AI State Machine.
}
