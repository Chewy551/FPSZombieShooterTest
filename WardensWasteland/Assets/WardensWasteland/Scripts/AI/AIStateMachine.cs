using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Enums defining the possible states an AI can be in.
public enum AIStateType { None, Idle, Alerted, Patrol, Attack, Feeding, Pursuit, Dead }
// Enums defining the possible types of targets an AI can have.
public enum AITargetType { None, Waypoint, Visual_Player, Visual_Light, Visual_Food, Audio }
// Enums defining the types of trigger events that can affect the AI's behavior.
public enum AITriggerEventType { Enter, Stay, Exit }

// Struct to represent the AI's target. 
public struct AITarget
{
    private AITargetType _type; // The type/category of target (e.g., visual player, audio, etc.).
    private Collider _collider; // The collider associated with the target.
    private Vector3 _position; // The position of the target in the world.
    private float _distance; // The distance from the AI to the target.
    private float _time; // The timestamp when the target was last recorded.

    // Public properties to safely access the private fields.
    public AITargetType type { get { return _type; } }
    public Collider collider { get { return _collider; } }
    public Vector3 position { get { return _position; } }
    public float distance { get { return _distance; } set { _distance = value; }}
    public float time { get { return _time; } }

    // Method to set target data.
    public void Set(AITargetType t, Collider c, Vector3 p, float d)
    {
        _type = t;
        _collider = c;
        _position = p;
        _distance = d;
        _time = UnityEngine.Time.time;
    }

    // Method to reset/clear the target data.
    public void Clear()
    {
        _type = AITargetType.None;
        _collider = null;
        _position = Vector3.zero;
        _time = 0.0f;
        _distance = Mathf.Infinity;
    }

}

// Abstract base class for the AI state machine.
public abstract class AIStateMachine : MonoBehaviour
{
    // Public fields to represent different types of threats.
    public AITarget VisualThreat = new AITarget(); // Visual threats like seeing the player.
    public AITarget AudioThreat = new AITarget(); // Audio threats like hearing a noise.

    // protected
    protected AIState _currentState = null; // The current state of the AI.
    protected Dictionary<AIStateType, AIState> _states = new Dictionary<AIStateType, AIState>(); // Dictionary mapping state types to state instances.
    protected AITarget _target = new AITarget(); // The current target of the AI.

    // Serialized fields allowing for adjustments within the Unity editor.
    [SerializeField] protected AIStateType _currentStateType = AIStateType.Idle;
    [SerializeField] protected SphereCollider _targetTrigger = null; // Collider to detect when target is within range.
    [SerializeField] protected SphereCollider _sensorTrigger = null; // Collider to sense environment.
    [SerializeField][Range(0, 15)] protected float _stoppingDistance = 1.0f; // The distance at which AI stops moving towards its target.

    // Cached references for frequently accessed components.
    protected Animator _animator = null; // Reference to the AI's animator.
    protected UnityEngine.AI.NavMeshAgent _navAgent = null; // Reference to the AI's navigation agent.
    protected Collider _collider = null; // Reference to the AI's collider.
    protected Transform _transform = null; // Reference to the AI's transform component.

    // Public properties to provide access to some private/protected members.
    public Animator Animator { get { return _animator; } }
    public UnityEngine.AI.NavMeshAgent NavAgent { get { return _navAgent; } }

    // Initialization of cached references.
    protected virtual void Awake()
    {
        _transform = transform;
        _animator = GetComponent<Animator>();
        _navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        _collider = GetComponent<Collider>();
    }

    // Initialization logic for the state machine.
    protected virtual void Start()
    {
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
            _currentState.OnEnterState();
        }
        else
        {
            _currentState = null;
        }
    }

    // Methods to manage the AI's target.
    public void SetTarget(AITargetType t, Collider c, Vector3 p, float d)
    {
        _target.Set(t, c, p, d);

        if (_targetTrigger != null)
        {
            _targetTrigger.radius = _stoppingDistance;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }

    public void SetTarget(AITargetType t, Collider c, Vector3 p, float d, float s)
    {
        _target.Set(t, c, p, d);

        if (_targetTrigger != null)
        {
            _targetTrigger.radius = s;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }

    public void SetTarget(AITarget t)
    {
        _target = t;

        if (_targetTrigger != null)
        {
            _targetTrigger.radius = _stoppingDistance;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }

    public void ClearTarget()
    {
        _target.Clear();
        if (_targetTrigger != null)
        {
            _targetTrigger.enabled = false;
        }
    }

    // Method to update the AI's perception of threats.
    protected virtual void FixedUpdate()
    {
        VisualThreat.Clear();
        AudioThreat.Clear();

        if (_target.type != AITargetType.None)
        {
            // Update the distance to the current target.
            _target.distance = Vector3.Distance(_transform.position, _target.position);
        }
    }

    // The main update loop for the AI, where we handle AI state transitions and updates.
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
}
