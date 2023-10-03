using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIZombieState_Feeding1 : AIZombieState
{
    // Inspector Assigned
    [SerializeField] float _slerpSpeed = 5.0f;

    // Private Fields
    private int _eatingStateHash = Animator.StringToHash("Feeding State");
    private int _eatingLayerIndex = -1;

    public override AIStateType GetStateType()
    {
        return AIStateType.Feeding;
    }

    public override void OnEnterState()
    {
        Debug.Log("Entering Feeding State");
        base.OnEnterState();
        if (_zombieStateMachine == null) return;

        // Get layer index
        if (_eatingLayerIndex == -1)
        {
            _eatingLayerIndex = _zombieStateMachine.animator.GetLayerIndex("Cinematic");
        }

        // Configure State Machine
        _zombieStateMachine.feeding = true;
        _zombieStateMachine.speed = 0;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.attackType = 0;

        // Agent updates position but not rotation
        _zombieStateMachine.NavAgentControl(true, false);
    }

    public override void OnExitState()
    {
        if (_zombieStateMachine != null)
        {
            _zombieStateMachine.feeding = false;
        }
    }

    public override AIStateType OnUpdate()
    {
        if (_zombieStateMachine.satisfaction > 0.9f)
        {
            _zombieStateMachine.GetWaypointPosition(false);
            return AIStateType.Alerted;
        }

        //If Visual Threat then drop into alert mode
        if (_zombieStateMachine.VisualThreat.type != AITargetType.None && _zombieStateMachine.VisualThreat.type != AITargetType.Visual_Food)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Alerted;
        }

        // If Audio Threat then drop into alert mode
        if (_zombieStateMachine.AudioThreat.type == AITargetType.Audio)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
            return AIStateType.Alerted;
        }

        // Is the feeding animation playing
        if (_zombieStateMachine.animator.GetCurrentAnimatorStateInfo(_eatingLayerIndex).shortNameHash == _eatingStateHash)
        {
            _zombieStateMachine.satisfaction = Mathf.Min(_zombieStateMachine.satisfaction + ((Time.deltaTime * _zombieStateMachine.replenishRate)/ 100.0f), 1.0f);
        }

        if (!_zombieStateMachine.useRootRotation)
        {
            // Keep the zombie facing the feeding pos at all times
            Vector3 targetPos = _zombieStateMachine.targetPosition;
            targetPos.y = _zombieStateMachine.transform.position.y;
            Quaternion newRot = Quaternion.LookRotation(targetPos - _zombieStateMachine.transform.position);
            _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot, Time.deltaTime * _slerpSpeed);
        }

        return AIStateType.Feeding;
    }
}
