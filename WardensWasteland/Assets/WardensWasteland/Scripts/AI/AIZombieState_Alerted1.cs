// ----------------------------------------------------------------------------
// Copyright (c) 2023 [Chewy551]. All rights reserved.
// ----------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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
    [SerializeField] float _directionChangeTime = 1.5f;

    // Private Fields
    float _timer = 0.0f;
    float _directionChangeTimer = 0.0f;

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

        // Reset timers for state duration and direction change
        _timer = _maxDuration;
        _directionChangeTimer = 0.0f;
    }

    // ------------------------------------------------------------------
    // Name : OnUpdate
    // Desc : Evaluates conditions to transition out of the Alerted state. 
    //        This includes evaluating potential threats and determining 
    //        the next course of action for the zombie.
    // ------------------------------------------------------------------
    public override AIStateType OnUpdate()
    {
        // Decrement timers for state duration and direction change
        _timer -= Time.deltaTime;
        _directionChangeTimer += Time.deltaTime;

        // Transition into a patrol state when timer has passed with its old waypoint destination
        if (_timer <= 0.0f)
        {
            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(false));
            _zombieStateMachine.navAgent.isStopped = false;
            _timer = _maxDuration;
        }

        // Evaluate threats based on sensory input and set appropriate targets and states
        // Priority is given to visual threats of the player type as they are more immediate
        // Other threats or interests (lights, audio cues, food) are also evaluated

        // Handle visual threat from player
        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Player)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }

        // Handle audio threat
        if (_zombieStateMachine.AudioThreat.type == AITargetType.Audio)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
            _timer = _maxDuration;
        }

        //Handle visual threat from light source
        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Light)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            _timer = _maxDuration;
        }

        // Handle visual threat from food, but only if there's no immediate audio threat
        if (_zombieStateMachine.AudioThreat.type == AITargetType.None && 
            _zombieStateMachine.VisualThreat.type == AITargetType.Visual_Food)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }

        
        float angle;

        // If the target is audio-based or a light source and hasn't been reached yet...
        if ((_zombieStateMachine.targetType == AITargetType.Audio || _zombieStateMachine.targetType == AITargetType.Visual_Light) && !_zombieStateMachine.isTargetReached)
        {
            // Handle the zombie's orientation relative to the target
            angle = AIState.FindSignedAngle(_zombieStateMachine.transform.forward,
                                            _zombieStateMachine.targetPosition- _zombieStateMachine.transform.position);

            // If the audio threat is immediate and within a specific angle threshold, pursue it
            if (_zombieStateMachine.targetType == AITargetType.Audio && Mathf.Abs(angle) < _threatAngleThreshold)
            {
                return AIStateType.Pursuit;
            }

            // Change the direction based on intelligence or randomly if a set time has passed
            if (_directionChangeTimer > _directionChangeTime)
            {
                if (Random.value < _zombieStateMachine.intelligence)
                {
                    _zombieStateMachine.seeking = (int)Mathf.Sign(angle);
                }
                else
                {
                    _zombieStateMachine.seeking = (int)Mathf.Sign(Random.Range(-1.0f, 1.0f));
                }
                _directionChangeTimer = 0.0f;
            }
            
        }

        // If the target is a waypoint and a path is already generated...
        else if (_zombieStateMachine.targetType == AITargetType.Waypoint && !_zombieStateMachine.navAgent.pathPending)
        {
            angle = AIState.FindSignedAngle(_zombieStateMachine.transform.forward,
                                            _zombieStateMachine.navAgent.steeringTarget- _zombieStateMachine.transform.position);

            // If the angle to the waypoint is within a specific threshold, transition to the Patrol state
            if (Mathf.Abs(angle) < _waypointAngleThreshold) return AIStateType.Patrol;

            _zombieStateMachine.seeking = (int)Mathf.Sign(angle);
        }

        // If none of the above conditions are met, stay in the Alerted state
        return AIStateType.Alerted;
    }
}
