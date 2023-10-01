// ----------------------------------------------------------------------------
// Copyright (c) 2023 [Chewy551]. All rights reserved.
// ----------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// ------------------------------------------------------------------------
// Name : AIZombieState_Patrol1
// Desc : Represents the Patrol state for the zombie AI. In this state, the 
//        zombie will move between designated waypoints. The zombie will 
//        also detect and respond to threats during its patrol.
// ------------------------------------------------------------------------
public class AIZombieState_Patrol1 : AIZombieState
{
    // Inspector Assigned
    [SerializeField] AIWaypointNetwork _waypointNetwork = null;
    [SerializeField] bool              _randomPatrol    = false;
    [SerializeField] int               _currentWaypoint = 0;
    [SerializeField] float             _turnOnSpotThreshold = 80.0f;
    [SerializeField] float             _slerpSpeed = 5.0f;
    [SerializeField] [Range(0.0f, 3.0f)] float             _speed = 1.0f;

    // ------------------------------------------------------------------------
    // Name : GetStateType
    // Desc : Returns the type of the state - Patrol.
    // ------------------------------------------------------------------------
    public override AIStateType GetStateType()
    {
        return AIStateType.Patrol;
    }

    // ------------------------------------------------------------------------
    // Name : OnEnterState
    // Desc : Initialization logic when the Patrol state is entered.
    // ------------------------------------------------------------------------
    public override void OnEnterState()
    {
        Debug.Log("Entering Patrol State");
        base.OnEnterState();
        if (_zombieStateMachine == null) return;

        // Configure State Machine
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.speed = _speed;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;

        // Logic to set the destination based on waypoints
        if (_zombieStateMachine.targetType != AITargetType.Waypoint)
        {
            _zombieStateMachine.ClearTarget();

            if (_waypointNetwork != null && _waypointNetwork.Waypoints.Count > 0)
            {
                if (_randomPatrol)
                {
                    _currentWaypoint = Random.Range(0, _waypointNetwork.Waypoints.Count);
                }

                if (_currentWaypoint < _waypointNetwork.Waypoints.Count)
                {
                    Transform waypoint = _waypointNetwork.Waypoints[_currentWaypoint];
                    if (waypoint != null)
                    {
                        _zombieStateMachine.SetTarget(AITargetType.Waypoint,
                                                      null,
                                                      waypoint.position,
                                                      Vector3.Distance(_zombieStateMachine.transform.position, waypoint.position));

                        _zombieStateMachine.navAgent.SetDestination(waypoint.position);
                    }
                }

            }
        }

        
        _zombieStateMachine.navAgent.isStopped = false;
    }

    // ------------------------------------------------------------------------
    // Name : OnUpdate
    // Desc : Handles the update logic for the Patrol state, including threat
    //        detection and transitions.
    // ------------------------------------------------------------------------
    public override AIStateType OnUpdate()
    {
        // Do we have a visual threat that is the player
        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Player)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }

        // Do we have a visual threat that is a light
        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Light)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Alerted;
        }

        // Do we have an audio threat (i.e. player weapon)
        if (_zombieStateMachine.AudioThreat.type == AITargetType.Audio)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
            return AIStateType.Alerted;
        }

        // Check if the detected object is a food source
        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Food)
        {
            // If the hunger level of the zombie (represented as (1.0 - satisfaction)) 
            // is greater than the normalized distance to the food source (distance to food divided by sensor radius), 
            // then set the food as the target and enter the 'Pursuit' state.
            // This means the zombie is hungry enough to consider the food source worth pursuing. 
            if ((1.0f- _zombieStateMachine.satisfaction) > (_zombieStateMachine.VisualThreat.distance / _zombieStateMachine.sensorRadius))
            {
                _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                return AIStateType.Pursuit;
            }
        }

        float angle = Vector3.Angle(_zombieStateMachine.transform.forward, (_zombieStateMachine.navAgent.steeringTarget - _zombieStateMachine.transform.position));
        if (angle > _turnOnSpotThreshold)
        {
            return AIStateType.Alerted;
        }
        
        if (!_zombieStateMachine.useRootRotation)
        {
            // Keep zombie facing in direction of travel
            Quaternion newRot = Quaternion.LookRotation(_zombieStateMachine.navAgent.desiredVelocity);
            _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot, Time.deltaTime * _slerpSpeed);
        }

        if (_zombieStateMachine.navAgent.isPathStale || 
            !_zombieStateMachine.navAgent.hasPath || 
            _zombieStateMachine.navAgent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            NextWaypoint();
        }

        // Stay in Patrol State
        return AIStateType.Patrol;
    }

    // ------------------------------------------------------------------------
    // Name : NextWaypoint
    // Desc : Transitions the zombie to the next waypoint in the patrol route.
    // ------------------------------------------------------------------------
    private void NextWaypoint()
    {
        // Increase the current waypoint with wrap-around to zerp (or choose a random waypoint)
        if (_randomPatrol && _waypointNetwork.Waypoints.Count > 1)
        {
            int oldWaypoint = _currentWaypoint;
            while (_currentWaypoint == oldWaypoint)
            {
                _currentWaypoint = Random.Range(0, _waypointNetwork.Waypoints.Count);
            }
        }
        else
        {
            _currentWaypoint = _currentWaypoint == _waypointNetwork.Waypoints.Count - 1 ? 0 : _currentWaypoint + 1;
            Debug.Log("OnDestinationReached choose next waypoint: " +_currentWaypoint);
        }

        if (_waypointNetwork.Waypoints[_currentWaypoint] != null)
        {
            Transform newWaypoint = _waypointNetwork.Waypoints[_currentWaypoint];

            // This is our new target position
            _zombieStateMachine.SetTarget(AITargetType.Waypoint, 
                                          null, 
                                          newWaypoint.transform.position, 
                                          Vector3.Distance(newWaypoint.transform.position, _zombieStateMachine.transform.position));

            // Set new Path
            _zombieStateMachine.navAgent.SetDestination(newWaypoint.transform.position);
        }
    }

    // ------------------------------------------------------------------------
    // Name : OnDestinationReached
    // Desc : Logic that's triggered when the zombie reaches its patrol destination.
    // ------------------------------------------------------------------------
    public override void OnDestinationReached(bool isReached)
    {
        if (_zombieStateMachine == null || !isReached) return;

        if (_zombieStateMachine.targetType == AITargetType.Waypoint)
        {
            
            NextWaypoint();
        }
    }

    // ------------------------------------------------------------------------
    // Name : OnAnimatorIKUpdated
    // Desc : Updates the IK logic for the zombie's animation.
    // ------------------------------------------------------------------------
    /*public override void OnAnimatorIKUpdated()
    {
        if (_zombieStateMachine == null) return;

        _zombieStateMachine.animator.SetLookAtPosition(_zombieStateMachine.targetPosition + Vector3.up);
        _zombieStateMachine.animator.SetLookAtWeight(0.55f);
    }
    */
}
