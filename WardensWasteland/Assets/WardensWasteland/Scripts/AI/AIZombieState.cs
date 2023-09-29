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
    protected int _visualLayerMask = -1;

    // Reference to the zombie state machine to access specific properties and methods related to the zombie.
    protected AIZombieStateMachine _zombieStateMachine = null;

    // ------------------------------------------------------------------
    // Name : Awake
    // Desc : Initializes layer masks for detecting the player and AI body parts.
    // ------------------------------------------------------------------
    void Awake()
    {
        // Set the layer mask for the player and AI body part.
        _playerLayerMask = LayerMask.GetMask("Player", "AI Body Part") + 1;

        // Set the layer mask for the visual aggravator.
        _visualLayerMask = LayerMask.GetMask("Player", "AI Body Part", "Visual Aggravator") + 1;

        // Set the layer for the AI body part.
        _bodyPartLayer = LayerMask.NameToLayer("AI Body Part");
    }

    public override void SetStateMachine(AIStateMachine stateMachine)
    {
        if (stateMachine.GetType() == typeof(AIZombieStateMachine))
        {
            base.SetStateMachine(stateMachine);
            _zombieStateMachine = (AIZombieStateMachine)stateMachine;
        }
    }

    // ------------------------------------------------------------------
    // Name : OnTriggerEvent
    // Desc : Handles events when the AI encounters a trigger. Determines
    //        if the detected object (player) is a new threat or if it's 
    //        closer than a previously detected threat.
    // ------------------------------------------------------------------
    public override void OnTriggerEvent(AITriggerEventType eventType, Collider other)
    {
        if (_zombieStateMachine == null) return;

        if (eventType != AITriggerEventType.Exit)
        {
            AITargetType curType = _zombieStateMachine.VisualThreat.type;

            // ----------------------------------------------------------------------------
            // Name : Check for Player Threat
            // Desc : Determines if the AI has encountered a player and if so, checks if 
            //        the player is a new threat or closer than a previously detected one.
            //        If the conditions are met, the player is set as the current visual threat.
            // ----------------------------------------------------------------------------
            if (other.CompareTag("Player"))
            {
                // Calculate the distance between the AI and the player.
                float distance = Vector3.Distance(_zombieStateMachine.sensorPosition, other.transform.position);

                // Check if the player is a new threat or if it's closer than a previously detected threat.
                if (curType != AITargetType.Visual_Player || curType == AITargetType.Visual_Player && distance < _zombieStateMachine.VisualThreat.distance)
                {
                    RaycastHit hitInfo;
                    // Check if the player is visible to the AI.
                    if (ColliderIsVisible(other, out hitInfo, _playerLayerMask))
                    {
                        // Update the current visual threat to the player if it's deemed more dangerous.
                        _zombieStateMachine.VisualThreat.Set(AITargetType.Visual_Player, other, other.transform.position, distance);
                    }
                }
            }

            // ----------------------------------------------------------------------------
            // Name : Check for Flashlight Threat
            // Desc : Determines if the AI has encountered a flashlight and if so, calculates
            //        an aggravation factor based on the distance and size of the flashlight's 
            //        light. If the conditions are met, the flashlight is set as the current visual threat.
            // ----------------------------------------------------------------------------
            else if (other.CompareTag("Flash Light") && curType != AITargetType.Visual_Player)
            {
                // Get the flashlight's box collider.
                BoxCollider flashLightTrigger = (BoxCollider)other;
                // Calculate the distance between the AI and the flashlight.
                float distanceToThreat = Vector3.Distance(_zombieStateMachine.sensorPosition, flashLightTrigger.transform.position);
                // Calculate the z-size of the flashlight's box collider.
                float zSize = flashLightTrigger.size.z * flashLightTrigger.transform.lossyScale.z;
                // Calculate the aggravation factor which determines how threatening the flashlight is based on its distance and size.
                float aggrFactor = distanceToThreat / zSize;
                // If the aggravation factor is within the zombie's sight and intelligence thresholds...
                if (aggrFactor <= _zombieStateMachine.sight && aggrFactor <= _zombieStateMachine.intelligence)
                {
                    // ... set the flashlight as the current visual threat.
                    _zombieStateMachine.VisualThreat.Set(AITargetType.Visual_Light, other, other.transform.position, distanceToThreat);
                }
            }

            // ----------------------------------------------------------------------------
            // Name : Check for Sound Emitter Threat
            // Desc : Determines if the AI has encountered a loud sound emitter. If the AI 
            //        is within hearing distance of the sound, and if the sound is closer 
            //        than any previously detected sound, it is set as the current audio threat.
            // ----------------------------------------------------------------------------
            else if (other.CompareTag("AI Sound Emitter"))
            {
                SphereCollider soundTrigger = (SphereCollider)other;
                if (soundTrigger == null) return;

                // Get the position of the Agent Sensor
                Vector3 agentSensorPosition = _zombieStateMachine.sensorPosition;

                Vector3 soundPos;
                float soundRadius;
                AIState.ConvertSphereColliderToWorldSpace(soundTrigger, out soundPos, out soundRadius);

                // How far inside the sound's radius are we
                float distanceToThreat = (soundPos - agentSensorPosition).magnitude;

                // Calculate a distance factor such that it is 1.0 when at sound radius 0 when at center
                float distanceFactor = (distanceToThreat / soundRadius);

                // Bias the factor based on hearing ability of Agent
                distanceFactor += distanceFactor * (1.0f - _zombieStateMachine.hearing);

                // Too far away
                if (distanceFactor > 1.0f) return;

                // If we can hear it and is it closer than what we previously have stored
                if (distanceToThreat < _zombieStateMachine.AudioThreat.distance)
                {
                    // Most dangerous Audio Threat so far
                    _zombieStateMachine.AudioThreat.Set(AITargetType.Audio, other, soundPos, distanceToThreat);
                }
            }

            // ----------------------------------------------------------------------------
            // Name : Check for Food Threat
            // Desc : Determines if the AI has encountered a potential food source and if so, 
            //        checks various conditions to decide if the AI should pursue this food. 
            //        These conditions include ensuring that the AI hasn't already identified 
            //        a player or flashlight as a threat, that the AI is not fully satisfied, 
            //        and that there is no immediate audio threat. If all conditions are met, 
            //        the food is set as the current visual threat.
            // ----------------------------------------------------------------------------
            else if (other.CompareTag("AI Food") && curType != AITargetType.Visual_Player && curType != AITargetType.Visual_Light &&
                _zombieStateMachine.satisfaction <= 0.9f && _zombieStateMachine.AudioThreat.type == AITargetType.None)
            {
                // How far away os the threat from us
                float distanceToThreat = Vector3.Distance(other.transform.position, _zombieStateMachine.sensorPosition);

                // Is this disance smaller than anything we have previous stored
                if (distanceToThreat < _zombieStateMachine.VisualThreat.distance)
                {
                    // If so then check that it is in our FOV and it is within the range of this AI Sight
                    RaycastHit hitInfo;
                    if (ColliderIsVisible(other, out hitInfo, _visualLayerMask))
                    {
                        // Yep this is our most appealing target so far
                        _zombieStateMachine.VisualThreat.Set(AITargetType.Visual_Food, other, other.transform.position, distanceToThreat);
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
        if (_zombieStateMachine == null) return false;

        // Calculate the direction and angle of the detected object.
        Vector3 head = _zombieStateMachine.sensorPosition;
        Vector3 direction = other.transform.position - head;
        float angle = Vector3.Angle(direction, transform.forward);

        // If the detected object is outside the field of view, it's not visible.
        if (angle > _zombieStateMachine.fov * 0.5f) return false;

        // Cast a ray to detect all objects between the AI and the detected object.
        RaycastHit[] hits = Physics.RaycastAll(head, direction.normalized, _zombieStateMachine.sensorRadius * _zombieStateMachine.sight, layerMask);

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
