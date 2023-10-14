// ----------------------------------------------------------------------------
// Copyright (c) 2023 [Chewy551]. All rights reserved.
// ----------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ------------------------------------------------------------------
// Name : AIZombieState_Idle1
// Desc : Class for handling the 'Idle' state of the Zombie AI. This 
//        state represents when the zombie is not actively pursuing a 
//        target or responding to a threat. The zombie remains in this 
//        state until a specific event or timer prompts a state change.
// ------------------------------------------------------------------
public class AIZombieState_Idle1 : AIZombieState
{
    // Inspector Assigned
    [SerializeField] Vector2 _idleTimeRange = new Vector2(10.0f, 60.0f); // Range for random idle duration

    // Private
    float _idleTime = 0.0f; // Duration the zombie will remain idle for
    float _timer = 0.0f; // Tracks the time zombie has been idle

    // ------------------------------------------------------------------
    // Name : GetStateType
    // Desc : Returns the type of the state
    // ------------------------------------------------------------------
    public override AIStateType GetStateType()
    {
        return AIStateType.Idle;
    }

    // ------------------------------------------------------------------
    // Name : OnEnterState
    // Desc : Configurations and actions to take when entering the idle state
    // ------------------------------------------------------------------
    public override void OnEnterState()
    {
        Debug.Log("Entering Idle State");
        base.OnEnterState();
        if (_zombieStateMachine == null) return;

        // Set how long this idle state will last for
        _idleTime = Random.Range(_idleTimeRange.x, _idleTimeRange.y);
        _timer    = 0.0f;

        // Configure State Machine
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.speed = 0;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;
        _zombieStateMachine.ClearTarget(); // Clears any previous target
    }

    // ------------------------------------------------------------------
    // Name : OnUpdate
    // Desc : Evaluates conditions to transition out of the idle state
    // ------------------------------------------------------------------
    public override AIStateType OnUpdate()
    {

        if (_zombieStateMachine == null) return AIStateType.Idle;

        // If we detect a player, enter pursuit state
        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Player)
        {
            Debug.Log("Entering Pursuit State");
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }

        // If we detect a significant light source, become alerted
        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Light)
        {
            Debug.Log("Entering Alerted State");
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Alerted;
        }

        // If we hear a noise, become alerted
        if (_zombieStateMachine.AudioThreat.type == AITargetType.Audio)
        {
            Debug.Log("Entering Alerted State");
            _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
            return AIStateType.Alerted;
        }

        // If we detect food, enter feeding state
        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Food)
        {
            Debug.Log("Entering Feeding State");
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Feeding;
        }

        // If we've been idle for longer than the specified time, start patrolling
        _timer += Time.deltaTime;
        if (_timer > _idleTime)
        {
            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(false));
            _zombieStateMachine.navAgent.isStopped = false;
            return AIStateType.Alerted;
        }

        return AIStateType.Idle; // Default to staying in idle state
    }

}
