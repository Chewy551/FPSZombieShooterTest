// ----------------------------------------------------------------------------
// Copyright (c) 2023 [Chewy551]. All rights reserved.
// ----------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

// ------------------------------------------------------------------
// Name : AIZombieState
// Desc : Abstract class representing a specific state for a zombie AI.
//        Inherits from the AIState base class. This class can be
//        further subclassed to define specific behaviors for various
//        zombie states (e.g. AIZombieIdleState, AIZombieChaseState).
// ------------------------------------------------------------------
public abstract class AIZombieState : AIState
{

    // Cached layer masks for detecting the player and AI body parts.
    protected int _playerLayerMask = -1; 
    protected int _bodyPartLayer = -1;

    // ------------------------------------------------------------------
    // Name : Awake
    // Desc : Initializes layer masks for detecting the player and AI body parts.
    // ------------------------------------------------------------------
    void Awake()
    {
        _playerLayerMask = LayerMask.GetMask("Player", "AI Body Part") + 1;
        _bodyPartLayer = LayerMask.GetMask("AI Body Part");
    }

    // ------------------------------------------------------------------
    // Name : OnTriggerEvent
    // Desc : Handles events when the AI encounters a trigger. Determines
    //        if the detected object (player) is a new threat or if it's 
    //        closer than a previously detected threat.
    // ------------------------------------------------------------------
    public override void OnTriggerEvent(AITriggerEventType eventType, Collider other)
    {
        if (_stateMachine == null) return;

        if (eventType != AITriggerEventType.Exit)
        {
            AITargetType curType = _stateMachine.VisualThreat.type;

            // If the detected object is a player...
            if (other.CompareTag("Player"))
            {
                // Calculate the distance between the AI and the player.
                float distance = Vector3.Distance(_stateMachine.sensorPosition, other.transform.position);

                // Check if the player is a new threat or if it's closer than a previously detected threat.
                if (curType != AITargetType.Visual_Player || curType == AITargetType.Visual_Player && distance < _stateMachine.VisualThreat.distance)
                {
                    RaycastHit hitInfo;
                    // Check if the player is visible to the AI.
                    if (ColliderIsVisible(other, out hitInfo, _playerLayerMask))
                    {
                        // Update the current visual threat to the player if it's deemed more dangerous.
                        _stateMachine.VisualThreat.Set(AITargetType.Visual_Player, other, other.transform.position, distance);
                    }
                }
            }
        }
    }

    // ------------------------------------------------------------------
    // Name : ColliderIsVisible
    // Desc : Determines if a given collider is visible to the AI based on
    //        field of view and obstructions in the line of sight.
    // ------------------------------------------------------------------
    protected virtual bool ColliderIsVisible(Collider other, out RaycastHit hitInfo, int layerMask=-1)
    {
        hitInfo = new RaycastHit();

        // Check if the state machine is correctly initialized and is of the type AIZombieStateMachine.
        if (_stateMachine == null || _stateMachine.GetType()!= typeof(AIZombieStateMachine)) return false;
        AIZombieStateMachine zombieMachine = (AIZombieStateMachine)_stateMachine;

        // Calculate the direction and angle of the detected object.
        Vector3 head = _stateMachine.sensorPosition;
        Vector3 direction = other.transform.position - head;
        float angle = Vector3.Angle(direction, transform.forward);

        // If the detected object is outside the field of view, it's not visible.
        if (angle > zombieMachine.fov * 0.5f) return false;

        // Cast a ray to detect all objects between the AI and the detected object.
        RaycastHit[] hits = Physics.RaycastAll(head, direction.normalized, _stateMachine.sensorRadius * zombieMachine.sight, layerMask);

        // Logic to determine the closest object that is NOT a part of the AI's own body.
        float closestColliderDistance = float.MaxValue;
        Collider closestCollider = null;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            if (hit.distance < closestColliderDistance)
            {
                if (hit.transform.gameObject.layer == _bodyPartLayer)
                {
                    if (_stateMachine != GameSceneManager.instance.GetAIStateMachine(hit.rigidbody.GetInstanceID()))
                    {
                        closestColliderDistance = hit.distance;
                        closestCollider = hit.collider;
                        hitInfo = hit;
                    }
                }
                else
                {
                    closestColliderDistance = hit.distance;
                    closestCollider = hit.collider;
                    hitInfo = hit;
                }
            }
        }

        // If the closest detected object is the initial object in question, then it's visible.
        if (closestCollider && closestCollider.gameObject == other.gameObject) return true;

        return false;
    }
}
