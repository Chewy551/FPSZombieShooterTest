using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

// Custom editor for the AIWaypointNetwork object to display waypoints in the Unity Editor.
[CustomEditor(typeof(AIWaypointNetwork))]
public class AIWaypointNetworkEditor : Editor
{
    public override void OnInspectorGUI()
    {
        AIWaypointNetwork network = (AIWaypointNetwork)target;

        // Display Mode
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Display Mode", "Select how to visualize the waypoints in the editor."), GUILayout.Width(90));
        network.DisplayMode = (PathDisplayMode)EditorGUILayout.EnumPopup(network.DisplayMode);
        EditorGUILayout.EndHorizontal();

        if (network.DisplayMode == PathDisplayMode.Paths)
        {
            // Waypoint Start
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Waypoint Start", "Start index for the UI's waypoint selection."), GUILayout.Width(100));
            network.UIStart = EditorGUILayout.IntSlider(network.UIStart, 0, network.Waypoints.Count - 1);
            EditorGUILayout.EndHorizontal();

            // Waypoint End
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Waypoint End", "End index for the UI's waypoint selection."), GUILayout.Width(100));
            network.UIEnd = EditorGUILayout.IntSlider(network.UIEnd, 0, network.Waypoints.Count - 1);
            EditorGUILayout.EndHorizontal();
        }

        DrawDefaultInspector(); 
    }


    // This function is called when the AIWaypointNetwork object is selected in the Unity Editor's Scene view.
    void OnSceneGUI()
    {
        // Get the AIWaypointNetwork object that's currently selected in the Unity Editor.
        AIWaypointNetwork network = (AIWaypointNetwork)target;

        // Loop through each waypoint in the network.
        for (int i = 0; i < network.Waypoints.Count; i++)
        {
            // If a waypoint is missing (null), exit the function early.
            if (network.Waypoints[i] == null) { return; }

            // Define a new label style.
            GUIStyle labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.white;  // Set the label text color to white.

            // Draw a label at the position of the waypoint with its index number.
            Handles.Label(network.Waypoints[i].position, "Waypoint " + i.ToString(), labelStyle);
        }

        // if the network's display mode is set to "Connections", execute the function.
        if (network.DisplayMode == PathDisplayMode.Connections)
        {
            // Prepare an array to store the positions of the waypoints for drawing lines between them.
            Vector3[] linePoints = new Vector3[network.Waypoints.Count + 1];

            // Loop through the waypoints to fill the linePoints array.
            for (int i = 0; i <= network.Waypoints.Count; i++)
            {
                // Get the current waypoint index. If it's the last waypoint, loop back to the first one.
                int index = i != network.Waypoints.Count ? i : 0;

                // If a waypoint is not (null), execute the function.
                if (network.Waypoints[index] != null)
                {
                    // Store the waypoint's position in the array.
                    linePoints[i] = network.Waypoints[index].position;
                }
                else
                {
                    // if it is null, make the lines crazy so we can see it in the editor.
                    linePoints[i] = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
                }
            }
            Handles.color = Color.cyan; // Set the line color to cyan.
            Handles.DrawPolyLine(linePoints);  // Draw lines between the waypoints.
        }

        // if the network's display mode is set to "Paths", execute the function.
        else if (network.DisplayMode == PathDisplayMode.Paths)
        {
            NavMeshPath path = new NavMeshPath();  // Create a new NavMeshPath object.

            if (network.Waypoints[network.UIStart] != null && network.Waypoints[network.UIEnd] != null)
            {
                Vector3 from = network.Waypoints[network.UIStart].position;  // Get the starting waypoint's position.
                Vector3 to = network.Waypoints[network.UIEnd].position;  // Get the ending waypoint's position.

                // Calculate the path between the two waypoints.
                NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path);
                Handles.color = Color.yellow; // Set the line color to yellow.
                Handles.DrawPolyLine(path.corners);  // Draw the path between the waypoints.
            }
            
        }


    }
}
