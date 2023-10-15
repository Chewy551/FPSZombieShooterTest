// ----------------------------------------------------------------------------
// Copyright (c) 2023 [Chewy551]. All rights reserved.
// ----------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AIBoneControlType { Animated, Ragdoll, RagdollToAnim }

// ------------------------------------------------------------------
// Name : AIZombieStateMachine
// Desc : Class representing the state machine for a zombie AI.
//        Inherits from the AIStateMachine base class and provides
//        specific implementations and properties for zombie behavior.
// ------------------------------------------------------------------
public class AIZombieStateMachine : AIStateMachine
{
    // Inspector Assigned
    [Header("Zombie Parameters")]
    [SerializeField][Range(10.0f, 360.0f)] float _fov           = 50.0f; // Field of view for the zombie (how far it can see)
    [SerializeField][Range(0.0f, 1.0f)]    float _sight         = 0.5f;  // How well the zombie can see (0 = blind, 1 = perfect vision)
    [SerializeField][Range(0.0f, 1.0f)]    float _hearing       = 1.0f;  // How well the zombie can hear (0 = deaf, 1 = perfect hearing)
    [SerializeField][Range(0.0f, 1.0f)]    float _aggression    = 0.5f;  // How aggressive the zombie is (0 = totally docile, 1 = super aggressive)
    [SerializeField][Range(0, 100)]        int   _health        = 100;   // How much health the zombie has
    [SerializeField][Range(0, 100)]        int   _lowerBodyDamage = 0;     // How much damage the zombie has taken to the lower body
    [SerializeField][Range(0, 100)]        int   _upperBodyDamage = 0;   // How much damage the zombie has taken to the upper body
    [SerializeField][Range(0, 100)]        int   _upperBodyThreshold = 30;   // Damage threshold to change animation
    [SerializeField][Range(0, 100)]        int   _limpThreshold = 30;   // Damage threshold to change animation
    [SerializeField][Range(0, 100)]        int   _crawlThreshold = 90;   // Damage threshold to change animation
    [SerializeField][Range(0.0f, 1.0f)]    float _intelligence  = 0.5f;  // How smart the zombie is (0 = totally dumb, 1 = super smart)
    [SerializeField][Range(0.0f, 1.0f)]    float _satisfaction  = 1.0f;  // How satisfied the zombie is (0 = totally unsatisfied, 1 = super satisfied)
    [SerializeField]                       float _replenishRate = 0.5f; // How quickly the zombie regains satisfaction
    [SerializeField]                       float _depletionRate = 0.1f; // How quickly the zombie loses satisfaction when not feeding

    // Private from Animator Parameters
    // These parameters are used to communicate and control the animations of the zombie.
    private int  _seeking    = 0;     // 0 = not seeking, 1/-1 = seeking
    private bool _feeding    = false; // Whether or not the zombie is feeding
    private bool _crawling   = false; // Whether or not the zombie is crawling
    private int  _attackType = 0;     // 0 = none, 1 = head, 2 = body
    private float _speed     = 0.0f;  // The speed of the zombie

    // Ragdoll Stuff
    private AIBoneControlType _boneControlType = AIBoneControlType.Animated; // How the zombie is currently being controlled

    // Hashes
    // Cached hashes for the various parameters used in the Animator, 
    // to avoid repetitive string lookups which are costly.
    private int _speedHash   = Animator.StringToHash("Speed");    
    private int _seekingHash = Animator.StringToHash("Seeking");  
    private int _feedingHash = Animator.StringToHash("Feeding");  
    private int _attackHash  = Animator.StringToHash("Attack");
    private int _crawlingHash = Animator.StringToHash("Crawling");  
    private int _hitTriggerHash = Animator.StringToHash("Hit");
    private int _hitTypeHash = Animator.StringToHash("HitType");

    // Public Properties
    // Public properties provide controlled access to the zombie's characteristics and states.
    public float    replenishRate     { get { return _replenishRate; }}
    public float    fov               { get { return _fov; }}
    public float    hearing           { get { return _hearing; }}
    public float    sight             { get { return _sight; }}
    public bool     crawling          { get { return _crawling; }}
    public float    intelligence      { get { return _intelligence; }}
    public float    satisfaction      { get { return _satisfaction; }         set { _satisfaction = value; }}
    public float    aggression        { get { return _aggression; }           set { _aggression = value; }}
    public int      health            { get { return _health; }               set { _health = value; }} 
    public int      attackType        { get { return _attackType; }           set { _attackType = value; }}
    public bool     feeding           { get { return _feeding; }              set { _feeding = value; }}
    public int      seeking           { get { return _seeking; }              set { _seeking = value; }}
    public float    speed             { get { return _speed; }                set { _speed = value; }}
    public bool isCrawling 
    { 
        get 
        { 
            return (_lowerBodyDamage >= _crawlThreshold); 
        }             
    }

    protected override void Start()
    {
        base.Start();
        UpdateAnimatorDamage();
    }

    // ------------------------------------------------------------------------
    // Name : Update
    // Desc : Overrides the base Update method to implement zombie-specific 
    //        logic and update the Animator parameters.
    // ------------------------------------------------------------------------
    protected override void Update()
    {
        base.Update(); // Call the base class's update method

        // Ensure the animator component exists before trying to set its parameters.
        if (_animator!=null)
        {
            _animator.SetFloat   (_speedHash,    _speed);
            _animator.SetBool    (_feedingHash,  _feeding);
            _animator.SetInteger (_seekingHash,  _seeking);        
            _animator.SetInteger (_attackHash,   _attackType);     
        }

        _satisfaction = Mathf.Max(0, _satisfaction - ((_depletionRate * Time.deltaTime) / 100.0f) * Mathf.Pow(_speed , 3.0f));
    }

    protected void UpdateAnimatorDamage()
    {
        if (_animator != null)
        {
            _animator.SetBool(_crawlingHash, isCrawling);
        }
    }

    public override void TakeDamage(Vector3 position, Vector3 force, int damage, Rigidbody bodyPart, CharacterManager characterManager, int hitDirection = 0)
    {
        if (GameSceneManager.instance != null && GameSceneManager.instance.bloodParticles != null)
        {
            ParticleSystem system = GameSceneManager.instance.bloodParticles;
            system.transform.position = position;
            var settings = system.main;
            settings.simulationSpace = ParticleSystemSimulationSpace.World;
            system.Emit(60);
        }

        float hitStrength = force.magnitude;

        if (_boneControlType == AIBoneControlType.Ragdoll)
        {
            if (bodyPart != null)
            {
                if (hitStrength>1.0f)
                {
                    bodyPart.AddForce(force, ForceMode.Impulse);
                }

                if (bodyPart.CompareTag("Head"))
                {
                    _health = Mathf.Max(_health-damage, 0);
                }
                else if (bodyPart.CompareTag("Upper Body"))
                {
                    _upperBodyDamage += damage;
                }
                else if (bodyPart.CompareTag("Lower Body"))
                {
                    _lowerBodyDamage += damage;
                }

                UpdateAnimatorDamage();

                if (_health > 0)
                {
                    // TODO: Reanimate Zombie
                }
            }

            return;
        }

        // Get local space position of attacker
        Vector3 attackerLocPos = transform.InverseTransformPoint(characterManager.transform.position);

        // Get local space position of hit
        Vector3 hitLocPos = transform.InverseTransformPoint(position);

        bool shouldRagdoll = (hitStrength > 1.0f);

        if (bodyPart != null)
        {

            if (bodyPart.CompareTag("Head"))
            {
                _health = Mathf.Max(_health - damage, 0);
                if (health == 0)
                {
                    shouldRagdoll = true;
                }
            }
            else if (bodyPart.CompareTag("Upper Body"))
            {
                _upperBodyDamage += damage;
                UpdateAnimatorDamage();
            }
            else if (bodyPart.CompareTag("Lower Body"))
            {
                _lowerBodyDamage += damage;
                UpdateAnimatorDamage();
                shouldRagdoll = true;
            }
        }

        if (_boneControlType != AIBoneControlType.Animated || isCrawling || cinematicEnabled || attackerLocPos.z < 0)
        {
            shouldRagdoll = true;
        }

        if (!shouldRagdoll)
        {
            float angle = 0.0f;
            if (hitDirection == 0)
            {
                Vector3 vecToHit = (position - transform.position).normalized;
                angle = AIState.FindSignedAngle(vecToHit, transform.forward);
            }
            
            int hitType = 0;
            if (bodyPart.gameObject.CompareTag("Head"))
            {
                if (angle <- 10 || hitDirection == -1)
                {
                    hitType = 1;
                }
                else if (angle > 10 || hitDirection == 1)
                {
                    hitType = 3;
                }
                else
                {
                    hitType = 2;
                }
            }

            if (bodyPart.gameObject.CompareTag("Upper Body"))
            {
                if (angle < -20 || hitDirection == -1)
                {
                    hitType = 4;
                }
                else if (angle > 20 || hitDirection == 1)
                {
                    hitType = 6;
                }
                else
                {
                    hitType = 5;
                }
            }

            if (_animator)
            {
                _animator.SetInteger(_hitTypeHash, hitType);
                _animator.SetTrigger(_hitTriggerHash);
            }

            return;
        }

        else
        {
            if (_currentState)
            {
                _currentState.OnExitState();
                _currentState = null;
                _currentStateType = AIStateType.None;
            }

            if (_navAgent) _navAgent.enabled = false;
            if (_animator) _animator.enabled = false;
            if (_collider) _collider.enabled = false;


            inMeleeRange = false;

            foreach(Rigidbody body in _bodyParts)
            {
                if (body)
                {
                    body.isKinematic = false;
                }
            }   

            if (hitStrength > 1.0f)
            {
                bodyPart.AddForce(force, ForceMode.Impulse);
            }

            _boneControlType = AIBoneControlType.Ragdoll;

            if (_health > 0)
            {
                // TODO: Reanimate Zombie
            }
        }
    }
}
