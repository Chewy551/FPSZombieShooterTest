﻿// ----------------------------------------------------------------------------
// Copyright (c) 2023 [Chewy551]. All rights reserved.
// ----------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Ensure that the GameObject this script is attached to also has a NavMeshAgent component.
[RequireComponent(typeof(NavMeshAgent))]
public class NavAgentRootMotion : MonoBehaviour
{
    // Variables settable in the Unity Inspector
    public AIWaypointNetwork WaypointNetwork = null; // Reference to the waypoint network this agent should follow.
    public int CurrentIndex = 0;                      // Current waypoint index the agent is targeting.
    public bool HasPath = false;                      // Does the agent currently have a path?
    public bool PathPending = false;                  // Is there a path computation that is pending?
    public bool PathStale = false;                    // Is the current path stale and needs recalculating?
    public NavMeshPathStatus PathStatus = NavMeshPathStatus.PathInvalid; // Current path status.
    public AnimationCurve JumpCurve = new AnimationCurve(); // Curve to define the jump's trajectory.
    public bool MixedMode = true;

    // Private members
    private NavMeshAgent _navAgent = null;            // Reference to the NavMeshAgent component.
    private Animator _animator = null;
    private float _smoothAngle = 0f;

    void Start()
    {
        // Initialize the NavMeshAgent.
        _navAgent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();

        _navAgent.updateRotation = false;

        // If there's no defined waypoint network, exit the method.
        if (WaypointNetwork == null) return;

        // Set the initial destination for the NavMeshAgent.
        SetNextDestination(false);
    }

    // Define the next destination point for the agent to navigate to.
    void SetNextDestination(bool increment)
    {
        // Exit if no waypoint network is defined.
        if (!WaypointNetwork) return;

        // Determine next waypoint index.
        int incStep = increment ? 1 : 0;
        Transform nextWaypointTransform = null;

        int nextWaypoint = (CurrentIndex + incStep >= WaypointNetwork.Waypoints.Count) ? 0 : CurrentIndex + incStep;

        // Fetch the transform of the next waypoint.
        nextWaypointTransform = WaypointNetwork.Waypoints[nextWaypoint];

        // If a valid waypoint is found, set the agent's destination.
        if (nextWaypointTransform != null)
        {
            CurrentIndex = nextWaypoint;
            _navAgent.destination = nextWaypointTransform.position;
            return;
        }

        // Increment the current waypoint index if no valid waypoint was found.
        CurrentIndex=nextWaypoint;
    }

    void Update()
    {

        // Update navigation status properties.
        HasPath = _navAgent.hasPath;
        PathPending = _navAgent.pathPending;
        PathStale = _navAgent.isPathStale;
        PathStatus = _navAgent.pathStatus;

        Vector3 localDesiredVelocity = transform.InverseTransformVector(_navAgent.desiredVelocity);
        float angle = Mathf.Atan2(localDesiredVelocity.x, localDesiredVelocity.z) * Mathf.Rad2Deg;
        _smoothAngle = Mathf.MoveTowardsAngle(_smoothAngle, angle, 80.0f * Time.deltaTime);

        float speed = localDesiredVelocity.z;

        _animator.SetFloat("Angle", _smoothAngle);
        _animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);

        if (_navAgent.desiredVelocity.sqrMagnitude > Mathf.Epsilon)
        {
            if(!MixedMode || (MixedMode && Mathf.Abs(angle) < 80.0f && _animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Locomotion")))
            {
                Quaternion lookRotation = Quaternion.LookRotation(_navAgent.desiredVelocity, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 5.0f * Time.deltaTime);
            }
            
        }

        // If the agent is on an OffMeshLink (jumping between areas), perform the jump.
        /*if (_navAgent.isOnOffMeshLink)
        {
            StartCoroutine(Jump(1.0f));
            return;
        }*/

        // Decide on the next destination based on the agent's status.
        if ((_navAgent.remainingDistance <= _navAgent.stoppingDistance && !PathPending) || PathStatus == NavMeshPathStatus.PathInvalid)
        {
            SetNextDestination(true);
        }
        else if (_navAgent.isPathStale)
        {
            SetNextDestination(false);
        }
    }

    void OnAnimatorMove()
    {
        if (MixedMode && !_animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Locomotion"))
        {
            transform.rotation = _animator.rootRotation;
        }
        _navAgent.velocity = _animator.deltaPosition / Time.deltaTime;
    }

    // Coroutine to animate the agent jumping between different mesh areas.
    IEnumerator Jump(float duration)
    {
        OffMeshLinkData data = _navAgent.currentOffMeshLinkData;
        Vector3 startPos = _navAgent.transform.position;
        Vector3 endPos = data.endPos + (_navAgent.baseOffset * Vector3.up);
        float time = 0.0f;

        // Animate the agent's position over the duration using the JumpCurve.
        while (time <= duration)
        {
            float t = time / duration;
            _navAgent.transform.position = Vector3.Lerp(startPos, endPos, t) + (JumpCurve.Evaluate(t) * Vector3.up);
            time += Time.deltaTime;
            yield return null;
        }
        _navAgent.CompleteOffMeshLink();
    }
}
