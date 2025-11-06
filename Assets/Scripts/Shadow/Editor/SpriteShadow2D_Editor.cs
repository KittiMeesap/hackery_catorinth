#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpriteShadow2D))]
public class SpriteShadow2D_Editor : Editor
{
    private SpriteShadow2D shadow;
    private bool editMode = false;

    private void OnEnable()
    {
        shadow = (SpriteShadow2D)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (shadow.mode == SpriteShadow2D.ShadowMode.Freeform)
        {
            GUILayout.Space(10);

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("?? Freeform Editing", EditorStyles.boldLabel);

            if (GUILayout.Button(editMode ? "? Exit Edit Mode" : "?? Enter Edit Mode"))
            {
                editMode = !editMode;
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("?? Reload Outline from Sprite"))
            {
                Undo.RecordObject(shadow, "Reload Outline");
                shadow.LoadSpriteOutline();
                shadow.UpdateShadow();
            }

            GUILayout.Label($"Points: {shadow.freeformPoints.Count}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }
    }

    private void OnSceneGUI()
    {
        if (shadow.mode != SpriteShadow2D.ShadowMode.Freeform || !editMode)
            return;

        Handles.color = Color.cyan;

        // Draw connecting lines
        for (int i = 0; i < shadow.freeformPoints.Count; i++)
        {
            Vector3 a = shadow.transform.TransformPoint(shadow.freeformPoints[i]);
            Vector3 b = shadow.transform.TransformPoint(shadow.freeformPoints[(i + 1) % shadow.freeformPoints.Count]);
            Handles.DrawLine(a, b);
        }

        // Draw points
        for (int i = 0; i < shadow.freeformPoints.Count; i++)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 worldPos = shadow.transform.TransformPoint(shadow.freeformPoints[i]);
            var fmh_67_17_638973613655633204 = Quaternion.identity; Vector3 newWorldPos = Handles.FreeMoveHandle(
                worldPos,
                0.05f,
                Vector3.zero,
                Handles.DotHandleCap
            );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(shadow, "Move Shadow Point");
                shadow.freeformPoints[i] = shadow.transform.InverseTransformPoint(newWorldPos);
                shadow.UpdateShadow();
            }
        }

        // Add / Remove point with mouse click
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3 hit = ray.origin;

            if (e.control && shadow.freeformPoints.Count > 3)
            {
                // Ctrl+Click -> remove nearest point
                int closest = -1;
                float minDist = float.MaxValue;
                for (int i = 0; i < shadow.freeformPoints.Count; i++)
                {
                    float dist = Vector2.Distance(shadow.transform.TransformPoint(shadow.freeformPoints[i]), hit);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = i;
                    }
                }

                if (closest != -1)
                {
                    Undo.RecordObject(shadow, "Remove Point");
                    shadow.freeformPoints.RemoveAt(closest);
                    shadow.UpdateShadow();
                }
            }
            else
            {
                // Click -> add point
                Undo.RecordObject(shadow, "Add Point");
                shadow.freeformPoints.Add(shadow.transform.InverseTransformPoint(hit));
                shadow.UpdateShadow();
            }

            e.Use();
        }
    }
}
#endif
