using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public enum PlayerMoveStatus { NotMoving, Crouching, Walking, Running, NotGrounded, Landing }
public enum CurveControlledBobCallbackType { Horizontal, Vertical }


// Delegates
public delegate void CurveControlBobCallback();

[System.Serializable]
public class CurveControlledBobEvent
{
    public float _time = 0.0f;
    public CurveControlBobCallback _function = null;
    public CurveControlledBobCallbackType _type = CurveControlledBobCallbackType.Vertical;
}

[System.Serializable]
public class CurveControlledBob
{
    [SerializeField] AnimationCurve _bobCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f),
                                                                   new Keyframe(1f, 0f), new Keyframe(1.5f, -1f), 
                                                                   new Keyframe(2f, 0f));

    // Inspector Assigned Bob Control Variables
    [SerializeField] float _horizontalMultiplier = 0.01f;
    [SerializeField] float _verticalMultiplier   = 0.02f;
    [SerializeField] float _verticaltoHorizontalSpeedRatio = 2.0f;
    [SerializeField] float _baseInterval = 1.0f; 

    // Internals
    private float _prevXPlayHead;
    private float _prevYPlayHead;
    private float _xPlayHead;
    private float _yPlayHead;
    private float _curveEndTime;
    private List<CurveControlledBobEvent> _events = new List<CurveControlledBobEvent>();

    public void Initialize()
    {
        // Record time length of bob curve
        _curveEndTime = _bobCurve[_bobCurve.length - 1].time;
        _xPlayHead = 0.0f;
        _yPlayHead = 0.0f;
        _prevXPlayHead = 0.0f;
        _prevYPlayHead = 0.0f; 
    }

    public void RegisterEventCallback(float time, CurveControlBobCallback function, CurveControlledBobCallbackType type)
    {
        CurveControlledBobEvent ccbeEvent = new CurveControlledBobEvent();
        ccbeEvent._time = time;
        ccbeEvent._function = function;
        ccbeEvent._type = type;
        _events.Add(ccbeEvent);
        _events.Sort(
                        delegate (CurveControlledBobEvent t1, CurveControlledBobEvent t2)
                        {
                            return (t1._time.CompareTo(t2._time));
                        }
                     );
    }

    public Vector3 GetVectorOffset(float speed)
    {
        _xPlayHead += (speed * Time.deltaTime) / _baseInterval;
        _yPlayHead += ((speed * Time.deltaTime) / _baseInterval) * _verticaltoHorizontalSpeedRatio;

        if (_xPlayHead > _curveEndTime)
            _xPlayHead -= _curveEndTime;

        if (_yPlayHead > _curveEndTime)
            _yPlayHead -= _curveEndTime;

        float xPos = _bobCurve.Evaluate(_xPlayHead) * _horizontalMultiplier;
        float yPos = _bobCurve.Evaluate(_yPlayHead) * _verticalMultiplier;

        // Return the bobbed Vector3
        return new Vector3(xPos, yPos, 0f);
    }
}


[RequireComponent (typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    // Inspector Assigned Locomotion Settings
    [SerializeField] private float _walkSpeed = 1.0f;
    [SerializeField] private float _runSpeed = 4.5f;
    [SerializeField] private float _jumpSpeed = 7.5f;
    [SerializeField] private float _stickToGroundForce = 5.0f; 
    [SerializeField] private float _gravityMultiplier = 2.5f;
    [SerializeField] private float _runStepLengthen = 0.75f;
    [SerializeField] private CurveControlledBob _headBob = new CurveControlledBob();

    // Use Standard Assets Mouse Look class for mouse input -> Camera Look Control
    [SerializeField] private UnityStandardAssets.Characters.FirstPerson.MouseLook _mouseLook;

    // Private internal variables
    private Camera _camera = null;
    private bool _jumpButtonPressed = false;
    private Vector2 _inputVector = Vector2.zero;
    private Vector3 _moveDirection = Vector3.zero;
    private bool _previouslyGrounded = false;
    private bool _isWalking = true;
    private bool _isJumping = false;
    private Vector3 _localSpaceCameraPos = Vector3.zero;

    // Timers
    private float _fallingTimer = 0.0f;

    private CharacterController _characterController = null;
    private PlayerMoveStatus _movementStatus = PlayerMoveStatus.NotMoving;

    // Public Properties
    public PlayerMoveStatus movementStatus { get { return _movementStatus; } }
    public float walkSpeed { get { return _walkSpeed; } }
    public float runSpeed { get { return _runSpeed; } }

    protected void Start()
    {
        // Cache compnent references
        _characterController = GetComponent<CharacterController>();

        // Get the main camera and cache local position within the FPS rig
        _camera = Camera.main;
        _localSpaceCameraPos = _camera.transform.localPosition;

        // Set initial to not jumping and not moving
        _movementStatus = PlayerMoveStatus.NotMoving;

        // Reset Timers
        _fallingTimer = 0.0f;

        // Setup Mouse Look Script
        _mouseLook.Init(transform, _camera.transform);

        // Initialize Head Bob
        _headBob.Initialize(); 
    }

    protected void Update()
    {
        // If we are falling increment timer
        if (_characterController.isGrounded)
        {
            _fallingTimer = 0.0f;
        }
        else
        {
            _fallingTimer += Time.deltaTime;
        }

        // Allow Mouse Look a chance to process mouse and rotate camera
        if (Time.timeScale > Mathf.Epsilon)
        {
            _mouseLook.LookRotation(transform, _camera.transform);
        }

        // Process the Jump Button
        // the jump state needs to read here to make sure it is not missed
        if (!_jumpButtonPressed)
        {
            _jumpButtonPressed = Input.GetButtonDown("Jump");
        }

        // Calculate Character Status
        if (!_previouslyGrounded && _characterController.isGrounded)
        {
            if (_fallingTimer > 0.5f)
            {
                // TODO: Play Landing Sound
            }

            _moveDirection.y = 0f;
            _isJumping = false;
            _movementStatus = PlayerMoveStatus.Landing;
        }
        else if (!_characterController.isGrounded)
        {
            _movementStatus = PlayerMoveStatus.NotGrounded;
        }
        else if (_characterController.velocity.sqrMagnitude < 0.01f)
        {
            _movementStatus = PlayerMoveStatus.NotMoving;
        }
        else if (_isWalking)
        {
            _movementStatus = PlayerMoveStatus.Walking;
        }
        else
        {
            _movementStatus = PlayerMoveStatus.Running;
        }

        _previouslyGrounded = _characterController.isGrounded;
    }

    protected void FixedUpdate()
    {
        // Read input for axis
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool wasWalking = _isWalking;
        _isWalking = !Input.GetKey(KeyCode.LeftShift);

        // Set the desired speed to be either our walking speed or our running speed
        float speed = _isWalking ? _walkSpeed : _runSpeed;
        _inputVector = new Vector2(horizontal, vertical);

        // normalize input if it exceeds 1 in combined length:
        if (_inputVector.sqrMagnitude > 1)
        {
            _inputVector.Normalize();
        }

        // Always move along the camera forward as it is the direction that it being aimed at
        Vector3 desiredMove = transform.forward * _inputVector.y + transform.right * _inputVector.x;

        // Get a normal for the surface that is being touched to move along it
        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.position, _characterController.radius, Vector3.down, out hitInfo, _characterController.height / 2f, 1))
        {
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;
        }

        // Scale movement by our current speed (walking or running)
        _moveDirection.x = desiredMove.x * speed;
        _moveDirection.z = desiredMove.z * speed;

        // If grounded
        if (_characterController.isGrounded)
        {
            // Apply severe down force to keep control sticking to floor
            _moveDirection.y = -_stickToGroundForce;

            // If the jump button was pressed then apply speed in up direction
            // and set isJumping to true. Also, reset jump button status
            if (_jumpButtonPressed)
            {
                _moveDirection.y = _jumpSpeed;
                _jumpButtonPressed = false;
                _isJumping = true;
                // TODO: Play Jumping Sound
            }
        }
        else
        {
            // Otherwise we are not on the ground so apply standard system gravity multiplied
            // by our gravity modifier
            _moveDirection += Physics.gravity * _gravityMultiplier * Time.fixedDeltaTime;
        }

        // Move the Character Controller
        _characterController.Move(_moveDirection * Time.fixedDeltaTime);

        // Are we moving
        if (_characterController.velocity.magnitude > 0.01f)
        {
            _camera.transform.localPosition = _localSpaceCameraPos + _headBob.GetVectorOffset(_characterController.velocity.magnitude * (_isWalking? 1.0f:_runStepLengthen));
        }
        else
        {
            _camera.transform.localPosition = _localSpaceCameraPos;
        }
    }
}
