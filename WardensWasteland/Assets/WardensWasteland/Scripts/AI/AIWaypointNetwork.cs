// ----------------------------------------------------------------------------
// Copyright (c) 2023 [Chewy551]. All rights reserved.
// ----------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Determines how the waypoints are displayed in the Unity editor.
/// </summary>
public enum PathDisplayMode
{
    None,        // No display.
    Connections, // Show direct connections.
    Paths        // Display full paths.
}

/// <summary>
/// Represents a network of waypoints for AI navigation.
/// </summary>
public class AIWaypointNetwork : MonoBehaviour
{
    [HideInInspector]
    public PathDisplayMode DisplayMode = PathDisplayMode.Connections;
    [HideInInspector]
    public int UIStart = 0;
    [HideInInspector]
    public int UIEnd = 0;

    [Tooltip("List of transforms representing each waypoint's position.")]
    public List<Transform> Waypoints = new List<Transform>();
}
