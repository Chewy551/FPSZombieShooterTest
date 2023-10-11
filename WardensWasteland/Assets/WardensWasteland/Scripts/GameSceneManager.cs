// ----------------------------------------------------------------------------
// Copyright (c) 2023 [Chewy551]. All rights reserved.
// ----------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInfo
{
    public Collider collider = null;
    public CharacterManager characterManager = null;
    public Camera camera = null;
    public CapsuleCollider meleeTrigger = null;
}

// ------------------------------------------------------------------
// Name : GameSceneManager
// Desc : This class is responsible for managing game scenes. It provides 
//        methods to register and retrieve AI state machines by ID.
//        It follows the singleton pattern to ensure only one instance
//        of the manager exists.
// ------------------------------------------------------------------
public class GameSceneManager : MonoBehaviour
{
    // Inspector Assigned
    [SerializeField] private ParticleSystem _bloodParticles = null; // Reference to the blood particle system


    // Statics
    private static GameSceneManager _instance = null; // Singleton instance

    // Public property to access the singleton instance
    public static GameSceneManager instance
    {
        get 
        {
            if (_instance == null)
            {
                _instance = (GameSceneManager)FindObjectOfType(typeof(GameSceneManager));
            }
            return _instance;
        }
    }

    // Dictionary to store all registered AI state machines by their ID
    private Dictionary<int, AIStateMachine> _stateMachines = new Dictionary<int, AIStateMachine>(); // Dictionary mapping AI instance IDs to state machines.
    private Dictionary<int, PlayerInfo> _playerInfos = new Dictionary<int, PlayerInfo>(); // Dictionary mapping player instance IDs to player info.


    // Properties
    public ParticleSystem bloodParticles { get { return _bloodParticles; } } // Accessor for the blood particle system


    // ------------------------------------------------------------------
    // Name : RegisterAIStateMachine
    // Desc : Registers an AI state machine with a specific key.
    // ------------------------------------------------------------------
    public void RegisterAIStateMachine(int key, AIStateMachine stateMachine)
    {
        if (!_stateMachines.ContainsKey(key))
        {
            _stateMachines[key] = stateMachine;
        }
    }

    // ------------------------------------------------------------------
    // Name : GetAIStateMachine
    // Desc : Retrieves an AI state machine by its key.
    // ------------------------------------------------------------------
    public AIStateMachine GetAIStateMachine(int key)
    {
        AIStateMachine machine = null;
        if (_stateMachines.TryGetValue(key, out machine))
        {
            return machine;
        }
        return null;
    }

    // ------------------------------------------------------------------
    // Name : RegisterPlayerInfo
    // Desc : Registers a Player with a specific key.
    // ------------------------------------------------------------------
    public void RegisterPlayerInfo(int key, PlayerInfo playerInfo)
    {
        if (!_playerInfos.ContainsKey(key))
        {
            _playerInfos[key] = playerInfo;
        }
    }

    // ------------------------------------------------------------------
    // Name : GetPlayerInfo
    // Desc : Retrieves a Player by its key.
    // ------------------------------------------------------------------
    public PlayerInfo GetPlayerInfo(int key)
    {
        PlayerInfo player = null;
        if (_playerInfos.TryGetValue(key, out player))
        {
            return player;
        }
        return null;
    }
}
