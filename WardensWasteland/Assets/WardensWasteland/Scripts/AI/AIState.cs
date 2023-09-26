using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The abstract base class for all AI states. This class defines the common interface and default implementations 
// for behaviors that all AI states should have, such as entering/exiting a state, responding to triggers, etc.
public abstract class AIState : MonoBehaviour
{
    // Allows external classes, like the AIStateMachine, to associate themselves with this state.
    public void SetStateMachine(AIStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    // Virtual methods that provide default implementations for state behaviors. 
    // Derived state classes can override these methods to provide state-specific behavior.

    // Called when transitioning into this state.
    public virtual void OnEnterState() { }

    // Called when transitioning out of this state.
    public virtual void OnExitState() { }

    // Called when the associated AI's animator updates its animations.
    public virtual void OnAnimatorUpdated() { }

    // Called when the associated AI's animator updates its inverse kinematics.
    public virtual void OnAnimatorIKUpdated() { }

    // Called when a trigger event (defined by the AITriggerEventType enum) occurs.
    public virtual void OnTriggerEvent(AITriggerEventType eventType, Collider other) { }

    // Called when the AI reaches its destination.
    public virtual void OnDestinationReached(bool isReached) { }

    // Abstract methods that must be implemented by derived state classes.

    // Must return the type of this state (e.g., Idle, Attack, etc.).
    public abstract AIStateType GetStateType();

    // Called every frame to update this state's logic and possibly request a state transition.
    // Must return the type of state the AI should transition to after this update.
    public abstract AIStateType OnUpdate();

    // Protected variables.

    // Reference to the AIStateMachine that's controlling this state.
    protected AIStateMachine _stateMachine;
}
