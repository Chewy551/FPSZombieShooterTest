// ----------------------------------------------------------------------------
// Copyright (c) 2023 [Chewy551]. All rights reserved.
// ----------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// ------------------------------------------------------------------
// Name : AIStateType
// Desc : Enum to define the various states the AI can be in.
// ------------------------------------------------------------------
public enum AIStateType { None, Idle, Alerted, Patrol, Attack, Feeding, Pursuit, Dead }

// ------------------------------------------------------------------
// Name : AITargetType
// Desc : Enum to define the various types of targets the AI can have.
// ------------------------------------------------------------------
public enum AITargetType { None, Waypoint, Visual_Player, Visual_Light, Visual_Food, Audio }

// ------------------------------------------------------------------
// Name : AITriggerEventType
// Desc : Enum to define the various trigger events the AI can respond to.
// ------------------------------------------------------------------
public enum AITriggerEventType { Enter, Stay, Exit }

// ------------------------------------------------------------------
// Name : AITarget
// Desc : Struct to define the properties of a target in the game world.
// ------------------------------------------------------------------
public struct AITarget
{
    private AITargetType _type; // The type of the target (e.g., player, waypoint).
    private Collider _collider; // The collider associated with the target.
    private Vector3 _position; // The current position of the target in the world.
    private float _distance; // The distance from the AI to the target.
    private float _time; // The last time the target was set or updated.

    // Properties for external access to the private variables.
    public AITargetType type { get { return _type; } }
    public Collider collider { get { return _collider; } }
    public Vector3 position { get { return _position; } }
    public float distance { get { return _distance; } set { _distance = value; }}
    public float time { get { return _time; } }

    // ------------------------------------------------------------------
    // Name : Set
    // Desc : Sets the properties of the target.
    // ------------------------------------------------------------------
    public void Set(AITargetType t, Collider c, Vector3 p, float d)
    {
        _type = t;
        _collider = c;
        _position = p;
        _distance = d;
        _time = Time.time;
    }

    // ------------------------------------------------------------------
    // Name : Clear
    // Desc : Clears the properties of the target.
    // ------------------------------------------------------------------
    public void Clear()
    {
        _type = AITargetType.None;
        _collider = null;
        _position = Vector3.zero;
        _time = 0.0f;
        _distance = Mathf.Infinity;
    }

}

// ------------------------------------------------------------------
// Name : AIStateMachine
// Desc : Abstract base class that represents the general structure 
//        and functionality of an AI's state machine.
// ------------------------------------------------------------------
public abstract class AIStateMachine : MonoBehaviour
{
    // Public variables
    public AITarget VisualThreat = new AITarget(); // Represents visual threats detected by the AI (e.g., player).
    public AITarget AudioThreat = new AITarget(); // Represents audio threats detected by the AI (e.g., gunshot noise).

    // Protected class variables
    protected AIState _currentState = null; // Reference to the current state the AI is in.
    protected Dictionary<AIStateType, AIState> _states = new Dictionary<AIStateType, AIState>(); // Dictionary to store and access AI states.
    protected AITarget _target = new AITarget(); // The current target of the AI.
    protected int _rootPositionRefCount = 0; // Reference count for root position updates.
    protected int _rootRotationRefCount = 0; // Reference count for root rotation updates.
    protected bool _isTargetReached = false; // Whether or not the AI has reached its target.

    // Serialized fields allowing for adjustments within the Unity editor.
    [SerializeField] protected AIStateType _currentStateType = AIStateType.Idle;
    [SerializeField] protected SphereCollider _targetTrigger = null; // Collider to detect when target is within range.
    [SerializeField] protected SphereCollider _sensorTrigger = null; // Collider to sense environment.
    [SerializeField] protected AIWaypointNetwork _waypointNetwork = null; // Waypoint network for the AI to follow.
    [SerializeField] protected bool _randomPatrol = false; // Whether or not the AI should patrol randomly.
    [SerializeField] protected int _currentWaypoint = -1; // The current waypoint the AI is moving towards.
    [SerializeField][Range(0, 15)] protected float _stoppingDistance = 1.0f; // The distance at which AI stops moving towards its target.

    // Cached references for frequently accessed components.
    protected Animator _animator = null; // Reference to the AI's animator.
    protected NavMeshAgent _navAgent = null; // Reference to the AI's navigation agent.
    protected Collider _collider = null; // Reference to the AI's collider.
    protected Transform _transform = null; // Reference to the AI's transform component.

    // Public properties to provide access to some private/protected members.
    public bool inMeleeRange { get; set; } // Whether or not the AI is in melee range of its target.
    public Animator animator { get { return _animator; } }
    public NavMeshAgent navAgent { get { return _navAgent; } }
    public Vector3 sensorPosition
    {
        get
        {
            if (_sensorTrigger == null) return Vector3.zero;
            Vector3 point = _sensorTrigger.transform.position;
            point.x += _sensorTrigger.center.x * _sensorTrigger.transform.lossyScale.x;
            point.y += _sensorTrigger.center.y * _sensorTrigger.transform.lossyScale.y;
            point.z += _sensorTrigger.center.z * _sensorTrigger.transform.lossyScale.z;
            return point;
        }
    }
    public float sensorRadius
    {
        get
        {
            if (_sensorTrigger == null) return 0.0f;
            float radius = Mathf.Max(_sensorTrigger.radius * _sensorTrigger.transform.lossyScale.x,
                                     _sensorTrigger.radius * _sensorTrigger.transform.lossyScale.y);

            return Mathf.Max(radius, _sensorTrigger.radius * _sensorTrigger.transform.lossyScale.z);
        }
    }

    public bool useRootPosition { get { return _rootPositionRefCount > 0; } }
    public bool useRootRotation { get { return _rootRotationRefCount > 0; } }

    // Propertie to get the current Target Type and Position. i.e None, Waypoint, Visual_Player, Visual_Light, Visual_Food, Audio
    public bool isTargetReached { get { return _isTargetReached; } }
    public AITargetType targetType { get { return _target.type; } }
    public Vector3 targetPosition { get { return _target.position; } }
    public int targetColliderID
    {
        get
        {
            if (_target.collider)
            {
                return _target.collider.GetInstanceID();
            }
            else
            {
                return -1;
            }
        }
    }

    // Initialization of cached references.
    protected virtual void Awake()
    {
        _transform = transform;
        _animator = GetComponent<Animator>();
        _navAgent = GetComponent<NavMeshAgent>();
        _collider = GetComponent<Collider>();

        if (GameSceneManager.instance != null)
        {
            if (_collider) GameSceneManager.instance.RegisterAIStateMachine(_collider.GetInstanceID(), this);
            if (_sensorTrigger) GameSceneManager.instance.RegisterAIStateMachine(_sensorTrigger.GetInstanceID(), this);
        }
    }

    // Initialization logic for the state machine.
    protected virtual void Start()
    {
        if (_sensorTrigger!=null)
        {
            AISensor script = _sensorTrigger.GetComponent<AISensor>();
            if (script!=null)
            {
                script.parentStateMachine = this;
            }
        }

        // Populate the _states dictionary with all available states on the AI.
        AIState[] states = GetComponents<AIState>();
        foreach (AIState state in states) 
        {
            if (state != null && !_states.ContainsKey(state.GetStateType()))
            {
                _states[state.GetStateType()] = state;
                state.SetStateMachine(this); // Set the state machine reference for each state.
            }
        }

        // Set the initial state.
        if (_states.ContainsKey(_currentStateType))
        {
            _currentState = _states[_currentStateType];
            _currentState.OnEnterState(); // Trigger the entering mechanism for the current state.
        }
        else
        {
            _currentState = null; // If the state isn't found, default to null.
        }

        if (_animator)
        {
            // If the AI has an animator, link this state machine to all state machine links 
            // present in the animator behaviors.
            AIStateMachineLink[] scripts = _animator.GetBehaviours<AIStateMachineLink>();
            foreach (AIStateMachineLink script in scripts)
            {
                script.stateMachine = this; // Assign the state machine to each state machine link.
            }
        }
    }

    // ------------------------------------------------------------------------
    // Name : GetWaypointPosition
    // Desc : Fetched the world space position of the state machine's currently
    //        set waypoint with optional increment
    // ------------------------------------------------------------------------
    public Vector3 GetWaypointPosition(bool increment)
    {
        if (_waypointNetwork == null)
        {
            Debug.Log("There is no waypoint network");
            return Vector3.zero;
        }

        if (_currentWaypoint == -1)
        {
            if (_randomPatrol)
            {
                _currentWaypoint = Random.Range(0, _waypointNetwork.Waypoints.Count);
            }
            else
            {
                _currentWaypoint = 0;
            }
        }
        else if (increment)
        {
            NextWaypoint();
        }

        // Fetch the new waypoint from the waypoint list
        if (_waypointNetwork.Waypoints[_currentWaypoint] != null)
        {
            Transform newWaypoint = _waypointNetwork.Waypoints[_currentWaypoint];

            // This is our new target position
            SetTarget(AITargetType.Waypoint,
                      null,
                      newWaypoint.transform.position,
                      Vector3.Distance(newWaypoint.transform.position, transform.position));

            return newWaypoint.transform.position;
        }

        return Vector3.zero;
    }

    // ------------------------------------------------------------------------
    // Name : NextWaypoint
    // Desc : Called to select a new waypoint. Either randomly selects a new
    //        waypoint from the waypoint network or increments the current
    //        waypoint index (with wrap-around) to the next waypoint in the
    //        network in sequence. Sets the new waypoint as the new target
    //        and generates a nav agent path for it.
    // ------------------------------------------------------------------------
    private void NextWaypoint()
    {
        // Increase the current waypoint with wrap-around to zerp (or choose a random waypoint)
        if (_randomPatrol && _waypointNetwork.Waypoints.Count > 1)
        {
            // Keep generating random waypoint until we find on that isn't current one
            // NOTE: Very important that waypoint networks do not only have one waypoint
            int oldWaypoint = _currentWaypoint;
            while (_currentWaypoint == oldWaypoint)
            {
                _currentWaypoint = Random.Range(0, _waypointNetwork.Waypoints.Count);
            }
        }
        else
        {
            _currentWaypoint = _currentWaypoint == _waypointNetwork.Waypoints.Count - 1 ? 0 : _currentWaypoint + 1;
            Debug.Log("OnDestinationReached choose next waypoint: " + _currentWaypoint);
        }

    }


    // ------------------------------------------------------------------
    // Name : SetTarget
    // Desc : Sets the AI's current target based on provided parameters. Target Type / Collider / Position / Distance
    // ------------------------------------------------------------------
    public void SetTarget(AITargetType type, Collider collider, Vector3 position, float distance)
    {
        _target.Set(type, collider, position, distance); // Set the properties of the target.

        // If a target trigger collider exists, position it at the target's location and enable it.
        if (_targetTrigger != null)
        {
            _targetTrigger.radius = _stoppingDistance;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }

    // ------------------------------------------------------------------
    // Name : SetTarget
    // Desc : Sets the AI's current target based on provided parameters. Target Type / Collider / Position / Distance / Stopping Distance
    // ------------------------------------------------------------------
    public void SetTarget(AITargetType type, Collider collider, Vector3 position, float distance, float stoppingDistance)
    {
        _target.Set(type, collider, position, distance);

        if (_targetTrigger != null)
        {
            _targetTrigger.radius = stoppingDistance;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }

    // ------------------------------------------------------------------
    // Name : SetTarget
    // Desc : Sets the AI's current target based on provided parameters. Target Type
    // ------------------------------------------------------------------
    public void SetTarget(AITarget type)
    {
        _target = type;

        if (_targetTrigger != null)
        {
            _targetTrigger.radius = _stoppingDistance;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }

    // ------------------------------------------------------------------
    // Name : ClearTarget
    // Desc : Clears the current target of the AI and disables the target trigger.
    // ------------------------------------------------------------------
    public void ClearTarget()
    {
        _target.Clear();
        if (_targetTrigger != null)
        {
            _targetTrigger.enabled = false;
        }
    }

    // ------------------------------------------------------------------
    // Name : FixedUpdate
    // Desc : Update method called at fixed intervals. Used for updating AI's perception of threats.
    // ------------------------------------------------------------------
    protected virtual void FixedUpdate()
    {
        VisualThreat.Clear(); // Clear any existing visual threats.
        AudioThreat.Clear(); // Clear any existing audio threats.

        // If the AI has a target, update the distance to that target.
        if (_target.type != AITargetType.None)
        {
            // Update the distance to the current target.
            _target.distance = Vector3.Distance(_transform.position, _target.position);
        }

        _isTargetReached = false; // Reset the target reached flag.
    }

    // ------------------------------------------------------------------
    // Name : Update
    // Desc : Called by Unity each frame. Gives the current state a
    // chance to update itself and perform transitions.
    // ------------------------------------------------------------------

    protected virtual void Update()
    {
        // If there's no current state, there's nothing to update.
        if (_currentState == null) return;

        // Update the current state and get any suggested new state.
        AIStateType newStateType = _currentState.OnUpdate();

        // If the state suggests a change, and it's different from the current state...
        if (newStateType != _currentStateType)
        {
            AIState newState = null;

            // Try to get the new state from the dictionary of states.
            if (_states.TryGetValue(newStateType, out newState))
            {
                // Transition out of the current state.
                _currentState.OnExitState();
                // Transition into the new state.
                newState.OnEnterState();
                _currentState = newState;
            }

            // If the new suggested state isn't in our dictionary, default to an Idle state.
            else if (_states.TryGetValue(AIStateType.Idle, out newState))
            {
                _currentState.OnExitState();
                newState.OnEnterState();
                _currentState = newState;
            }

            // Update the current state type.
            _currentStateType = newStateType;
        }
    }

    // Called when the AI enters a trigger.
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (_targetTrigger == null || other != _targetTrigger) return;

        _isTargetReached = true; // Notify the AI that it has reached its destination.

        if (_currentState)
        {
            _currentState.OnDestinationReached(true); // Notify the current state that the AI has reached its destination.
        }
    }

    protected virtual void OnTriggerStay(Collider other)
    {
        if (_targetTrigger == null || other != _targetTrigger) return;

        _isTargetReached = true; // Notify the AI that it has reached its destination.
    }

    // Called when the AI exits a trigger.
    protected void OnTriggerExit(Collider other)
    {
        if (_targetTrigger == null || _targetTrigger != other) return;

        _isTargetReached = false; // Notify the AI that it has left its destination.

        if (_currentState!=null)
        {
            _currentState.OnDestinationReached(false); // Notify the current state that the AI has left its destination.
        }
    }

    // Called when a trigger event occurs.
    public virtual void OnTriggerEvent(AITriggerEventType type, Collider other)
    {
        if (_currentState != null)
        {
            _currentState.OnTriggerEvent(type, other);
        }
    }

    // Updates based on the AI's animator.
    protected virtual void OnAnimatorMove()
    {
        if (_currentState != null)
        {
            _currentState.OnAnimatorUpdated();
        }
    }

    // Updates based on the AI's animator's Inverse Kinematics.
    protected virtual void OnAnimatorIK(int layerIndex)
    {
        if (_currentState != null)
        {
            _currentState.OnAnimatorIKUpdated();
        }
    }

    // Controls the AI's navigation agent's position and rotation updates.
    public void NavAgentControl(bool positionUpdate, bool rotationUpdate)
    {
        if (_navAgent)
        {
            _navAgent.updatePosition = positionUpdate;
            _navAgent.updateRotation = rotationUpdate;
        }
    }
    // ------------------------------------------------------------------
    // Name : AddRootMotionRequest
    // Desc : Called by the State Machine Behaviours to
    //        Enable/Disable root motion
    // ------------------------------------------------------------------
    public void AddRootMotionRequest(int rootPosition, int rootRotation)
    {
        _rootPositionRefCount += rootPosition;
        _rootRotationRefCount += rootRotation;
    }

}
