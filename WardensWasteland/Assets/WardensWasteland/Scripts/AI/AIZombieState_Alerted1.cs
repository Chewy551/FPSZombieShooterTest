// ----------------------------------------------------------------------------
// Copyright (c) 2023 [Chewy551]. All rights reserved.
// ----------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// ------------------------------------------------------------------------
// Name : AIZombieState_Alerted1
// Desc : Represents the Alerted state for the zombie AI. This state is 
//        triggered when the zombie perceives a potential threat. It dictates 
//        how the zombie reacts to threats and conditions to transition to 
//        other states.
// ------------------------------------------------------------------------

public class AIZombieState_Alerted1 : AIZombieState
{
    // Inspector Assigned
    [SerializeField][Range(1, 60)] float _maxDuration = 10.0f;
    [SerializeField] float _waypointAngleThreshold = 90.0f;
    [SerializeField] float _threatAngleThreshold = 10.0f;

    // Private Fields
    float _timer = 0.0f;

    // ------------------------------------------------------------------
    // Name : GetStateType
    // Desc : Returns the type of the state
    // ------------------------------------------------------------------
    public override AIStateType GetStateType()
    {
        return AIStateType.Alerted;
    }

    // ------------------------------------------------------------------
    // Name : OnEnterState
    // Desc : Configurations and actions to take when entering the Alerted state
    // ------------------------------------------------------------------
    public override void OnEnterState()
    {
        Debug.Log("Entering Alerted State");
        base.OnEnterState();
        if (_zombieStateMachine == null) return;

        // Configure State Machine
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.speed = 0;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;

        _timer = _maxDuration;
    }

    // ------------------------------------------------------------------
    // Name : OnUpdate
    // Desc : Evaluates conditions to transition out of the Alerted state
    // ------------------------------------------------------------------
    public override AIStateType OnUpdate()
    {
        // Reduce Timer 
        _timer -= Time.deltaTime;

        // Transition into a patrol state if available
        if (_timer <= 0.0f) return AIStateType.Patrol;

        // Do we have a visual threat that is the player. These take priority over audio threats
        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Player)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }

        if (_zombieStateMachine.AudioThreat.type == AITargetType.Audio)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
            _timer = _maxDuration;
        }

        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Light)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            _timer = _maxDuration;
        }

        if (_zombieStateMachine.AudioThreat.type == AITargetType.None && 
            _zombieStateMachine.VisualThreat.type == AITargetType.Visual_Food)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }

        float angle;

        if (_zombieStateMachine.targetType == AITargetType.Audio || _zombieStateMachine.targetType == AITargetType.Visual_Light)
        {
            angle = AIState.FindSignedAngle(_zombieStateMachine.transform.forward,
                                            _zombieStateMachine.targetPosition- _zombieStateMachine.transform.position);

            if (_zombieStateMachine.targetType == AITargetType.Audio && Mathf.Abs(angle) < _threatAngleThreshold)
            {
                return AIStateType.Pursuit;
            }
            if (Random.value < _zombieStateMachine.intelligence)
            {
                _zombieStateMachine.seeking = (int)Mathf.Sign(angle);
            }
            else
            {
                _zombieStateMachine.seeking = (int)Mathf.Sign(Random.Range(-1.0f, 1.0f));
            }
        }
        else if (_zombieStateMachine.targetType == AITargetType.Waypoint)
        {
            angle = AIState.FindSignedAngle(_zombieStateMachine.transform.forward,
                                            _zombieStateMachine.navAgent.steeringTarget- _zombieStateMachine.transform.position);

            if (Mathf.Abs(angle) < _waypointAngleThreshold) return AIStateType.Patrol;

            _zombieStateMachine.seeking = (int)Mathf.Sign(angle);
        }

        return AIStateType.Alerted;
    }
}
