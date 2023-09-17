using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Ensure that the GameObject this script is attached to also has a NavMeshAgent component.
[RequireComponent(typeof(NavMeshAgent))]
public class NavAgentExample : MonoBehaviour
{
    // Variables settable in the Unity Inspector
    public AIWaypointNetwork WaypointNetwork = null; // Reference to the waypoint network this agent should follow.
    public int CurrentIndex = 0;                      // Current waypoint index the agent is targeting.
    public bool HasPath = false;                      // Does the agent currently have a path?
    public bool PathPending = false;                  // Is there a path computation that is pending?
    public bool PathStale = false;                    // Is the current path stale and needs recalculating?
    public NavMeshPathStatus PathStatus = NavMeshPathStatus.PathInvalid; // Current path status.

    // Private members
    private NavMeshAgent _navAgent = null;            // Reference to the NavMeshAgent component.

    void Start()
    {
        // Get reference to the NavMeshAgent component.
        _navAgent = GetComponent<NavMeshAgent>();

        // If no waypoint network is set, do nothing.
        if (WaypointNetwork == null) return;

        // Set the first destination for the agent.
        SetNextDestination(false);
    }

    // Set the next destination for the NavMeshAgent.
    void SetNextDestination(bool increment)
    {
        // If there's no waypoint network, return.
        if (!WaypointNetwork) return;

        // Decide which way to step through waypoints based on the 'increment' flag.
        int incStep = increment ? 1 : 0;
        Transform nextWaypointTransform = null;

        // Find the next valid waypoint.
        while (nextWaypointTransform == null)
        {
            // Calculate the index of the next waypoint. Loop back to the beginning if end of list is reached.
            int nextWaypoint = (CurrentIndex + incStep >= WaypointNetwork.Waypoints.Count) ? 0 : CurrentIndex + incStep;

            // Fetch the transform of the next waypoint.
            nextWaypointTransform = WaypointNetwork.Waypoints[nextWaypoint];

            // If the next waypoint is valid, set it as the new destination.
            if (nextWaypointTransform != null)
            {
                CurrentIndex = nextWaypoint;
                _navAgent.destination = nextWaypointTransform.position;
                return;
            }
        }

        // If no valid waypoint was found, increment the current index.
        CurrentIndex++;
    }

    void Update()
    {
        // Update the path state variables.
        HasPath = _navAgent.hasPath;
        PathPending = _navAgent.pathPending;
        PathStale = _navAgent.isPathStale;
        PathStatus = _navAgent.pathStatus;

        // If the agent doesn't have a path and isn't computing one, set the next destination.
        if ((!HasPath && !PathPending) || PathStatus==NavMeshPathStatus.PathInvalid)
        {
            SetNextDestination(true);
        }
        // If the current path is stale, refresh it.
        else if (PathStale)
        {
            SetNextDestination(false);
        }
    }
}
