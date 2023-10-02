// ----------------------------------------------------------------------------
// Copyright (c) 2023 [Chewy551]. All rights reserved.
// ----------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIZombieState_Pursuit1 : AIZombieState
{
    
    // Inspector Assigned
    [SerializeField][Range(0, 10)] private float _speed = 1.0f;
    [SerializeField] private float _slerpSpeed = 5.0f;
    [SerializeField] private float _repathDistanceMultiplier = 0.035f;
    [SerializeField] private float _repathVisualMinDuration = 0.05f;
    [SerializeField] private float _repathVisualMaxDuration = 5.0f;
    [SerializeField] private float _repathAudioMinDuration = 0.25f;
    [SerializeField] private float _repathAudioMaxDuration = 5.0f;
    [SerializeField] private float _maxDuration = 40.0f;

    // Private Fields
    private float _timer = 0.0f;
    private float _repathTimer = 0.0f;


    public override AIStateType GetStateType()
    {
        return AIStateType.Pursuit;
    }

    public override void OnEnterState()
    {
        Debug.Log("Entering Pursuit State");
        base.OnEnterState();
        if (_zombieStateMachine == null) return;

        // Configure State Machine
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.speed = _speed;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;

        // Zombie will only pursue for so long before breaking off
        _timer = 0.0f  ;
        _repathTimer = 0.0f;

        // Set Path
        _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.targetPosition);
        _zombieStateMachine.navAgent.isStopped = false; 
    }

    public override AIStateType OnUpdate()
    {
        _timer += Time.deltaTime;
        _repathTimer += Time.deltaTime;

        // If we are in Pursuit for longer than our MaxDuration then switch to Patrol State
        if (_timer > _maxDuration)
        {
            return AIStateType.Patrol;
        }

        // If we are chasing the player and have entered the melee trigger then switch to Attack State
        if (_zombieStateMachine.targetType == AITargetType.Visual_Player && _zombieStateMachine.inMeleeRange)
        {
            return AIStateType.Attack;
        }

        // Otherwise this is navigation to areas of interest so use the standard target threshold
        if (_zombieStateMachine.isTargetReached)
        {
            switch (_zombieStateMachine.targetType)
            {
                case AITargetType.Visual_Light:
                case AITargetType.Audio:
                    _zombieStateMachine.ClearTarget(); // Clear the threat
                    return AIStateType.Alerted; // Become alert and scan for targets

                case AITargetType.Visual_Food:
                    return AIStateType.Feeding;
            }
        }

        // If for any reason the nav agent has lost its path then call the event to say we are not longer pursuing
        if (_zombieStateMachine.navAgent.isPathStale ||
            !_zombieStateMachine.navAgent.hasPath ||
            _zombieStateMachine.navAgent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            _zombieStateMachine.ClearTarget();
            return AIStateType.Alerted;
        }

        // If we are close to the target then turn off the nav agent
        if (!_zombieStateMachine.useRootRotation && 
            _zombieStateMachine.targetType == AITargetType.Visual_Light && 
            _zombieStateMachine.VisualThreat.type == AITargetType.Visual_Player &&
            _zombieStateMachine.isTargetReached)
        {
            Vector3 targetPos = _zombieStateMachine.targetPosition;
            targetPos.y = _zombieStateMachine.transform.position.y;
            Quaternion newRot = Quaternion.LookRotation(targetPos - _zombieStateMachine.transform.position);
            _zombieStateMachine.transform.rotation = newRot;
        }
        // Slowly update our rotation to match the nav agents desired rotation BUT only if we are not persuing the player and are not in melee range
        else if (!_zombieStateMachine.useRootRotation && !_zombieStateMachine.isTargetReached)
        {
            // Generate a new Quaternion representing the rotation we should have
            Quaternion newRot = Quaternion.LookRotation(_zombieStateMachine.navAgent.desiredVelocity);
            // Smoothly rotate to that new rotation over time
            _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot, Time.deltaTime * _slerpSpeed);
        }
        // If we have reached the target location and there is nothing there, drop to alerted state and look for targets.
        else if(_zombieStateMachine.isTargetReached)
        {
            return AIStateType.Alerted;
        }

        // Do we have a visual threat that is the player
        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Player)
        {
            // The position is different - maybe same threat but it has moved so repath periodically
            if (_zombieStateMachine.targetPosition != _zombieStateMachine.VisualThreat.position)
            {
                // Repath more frequently as we get closer to the target - helps with slime mould effect
                if (Mathf.Clamp(_zombieStateMachine.VisualThreat.distance * _repathDistanceMultiplier, _repathVisualMinDuration, _repathVisualMaxDuration) < _repathTimer)
                {
                    // Repath the agent
                    _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.VisualThreat.position);
                    _repathTimer = 0.0f;
                }
            }

            // Make sure this is the current target
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);

            // Remain in pursuit state
            return AIStateType.Pursuit;
        }

        // If our target is the last sighting of a player then remain
        // in pursuit mode until we have the player again
        if (_zombieStateMachine.targetType == AITargetType.Visual_Player)
        {
            return AIStateType.Pursuit;
        }

        // If we have a visual threat that is the player's light
        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Light)
        {
            // and we currently have a lower priority target tjem drop into alerted
            // mode and try to find source of light
            if (_zombieStateMachine.targetType == AITargetType.Audio || _zombieStateMachine.targetType == AITargetType.Visual_Food)
            {
                _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                return AIStateType.Alerted;
            }
            else if (_zombieStateMachine.targetType == AITargetType.Visual_Food)
            {
                // Get unique ID of the collider of our target
                int currentID = _zombieStateMachine.targetColliderID;

                // If this is the same light that we are currently targeting
                if (currentID == _zombieStateMachine.VisualThreat.collider.GetInstanceID())
                {
                    // The position is different - maybe same threat but it has moved so repath periodically
                    if (_zombieStateMachine.targetPosition != _zombieStateMachine.VisualThreat.position)
                    {
                        // Repath more frequently as we get closer to the target - helps with slime mould effect
                        if (Mathf.Clamp(_zombieStateMachine.VisualThreat.distance * _repathDistanceMultiplier, _repathVisualMinDuration, _repathVisualMaxDuration) < _repathTimer)
                        {
                            // Repath the agent
                            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.VisualThreat.position);
                            _repathTimer = 0.0f;
                        }
                    }

                    // Make sure this is the current target
                    _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);

                    // Remain in pursuit state
                    return AIStateType.Pursuit;
                }
                else
                {
                    // Switch to new light
                    _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                    return AIStateType.Alerted;
                }
            }

        }
        else if (_zombieStateMachine.AudioThreat.type == AITargetType.Audio)
        {
            if (_zombieStateMachine.targetType == AITargetType.Visual_Food)
            {
                _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
                return AIStateType.Alerted;
            }
            else if (_zombieStateMachine.targetType == AITargetType.Audio)
            {
                // Get unique ID of the collider of our target
                int currentID = _zombieStateMachine.targetColliderID;

                // If this is the same light that we are currently targeting
                if (currentID == _zombieStateMachine.AudioThreat.collider.GetInstanceID())
                {
                    // The position is different - maybe same threat but it has moved so repath periodically
                    if (_zombieStateMachine.targetPosition != _zombieStateMachine.AudioThreat.position)
                    {
                        // Repath more frequently as we get closer to the target - helps with slime mould effect
                        if (Mathf.Clamp(_zombieStateMachine.AudioThreat.distance * _repathDistanceMultiplier, _repathAudioMinDuration, _repathAudioMaxDuration) < _repathTimer)
                        {
                            // Repath the agent
                            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.AudioThreat.position);
                            _repathTimer = 0.0f;
                        }
                    }

                    // Make sure this is the current target
                    _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);

                    // Remain in pursuit state
                    return AIStateType.Pursuit;
                }
                else
                {
                    // Switch to new audio source
                    _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
                    return AIStateType.Alerted;
                }
            }
        }

        return AIStateType.Pursuit;
    }

}