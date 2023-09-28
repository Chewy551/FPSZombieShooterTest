// ----------------------------------------------------------------------------
// Copyright (c) 2023 [Chewy551]. All rights reserved.
// ----------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ------------------------------------------------------------------
// Name : AISensor
// Desc : This class is responsible for sensing the environment 
//        using Unity's trigger events and then passing that 
//        information to the parent state machine.
// ------------------------------------------------------------------

public class AISensor : MonoBehaviour
{
    //Private

    private AIStateMachine _parentStateMachine = null; // Reference to the AIStateMachine that owns this sensor.

    //Public
    public AIStateMachine parentStateMachine { set { _parentStateMachine = value; } } // Property to set the parent state machine for this sensor.

    // ------------------------------------------------------------------
    // Name : OnTriggerEnter
    // Desc : Triggered when an object enters the sensor's trigger collider.
    //        Passes the event and the collider to the parent state machine.
    // ------------------------------------------------------------------
    void OnTriggerEnter(Collider col)
    {
        if (_parentStateMachine != null)
        {
            _parentStateMachine.OnTriggerEvent(AITriggerEventType.Enter, col);
        }
    }

    // ------------------------------------------------------------------
    // Name : OnTriggerStay
    // Desc : Triggered while an object stays inside the sensor's trigger collider.
    //        Continuously passes the event and the collider to the parent state machine.
    // ------------------------------------------------------------------
    void OnTriggerStay(Collider col)
    {
        if (_parentStateMachine != null)
        {
            _parentStateMachine.OnTriggerEvent(AITriggerEventType.Stay, col);
        }
    }

    // ------------------------------------------------------------------
    // Name : OnTriggerExit
    // Desc : Triggered when an object exits the sensor's trigger collider.
    //        Passes the event and the collider to the parent state machine.
    // ------------------------------------------------------------------
    void OnTriggerExit(Collider col)
    {
        if (_parentStateMachine != null)
        {
            _parentStateMachine.OnTriggerEvent(AITriggerEventType.Exit, col);
        }
    }
}
