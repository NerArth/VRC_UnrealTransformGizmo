using UnityEditor;
using UnityEngine;
using UnityEditor.Overlays;
using UnityEngine.UIElements;
using UnityEditor.Toolbars;

// TODO: Double-check naming consistency
// NOTE: This is not a VRChat-specific tool, but it is intended to be used with the VRChat SDK.

namespace UEStyle.UEGizmos
{
    [InitializeOnLoad]
    public static class UEStyleGizmo
    {
        private const string PrefPrefix = "UEStyle_UEGizmo_";
        private static bool s_Enabled = true;
        private static float s_Sensitivity = 1.0f;
        private static bool s_UseSnapping = true;
        private static Vector3 s_TotalAccumulatedDelta = Vector3.zero;
        private static bool s_LMB = false;
        private static bool s_RMB = false;
        private static bool s_IsDragging = false;
        private static Vector2 s_LastMousePosition;

        static UEStyleGizmo()
        {
            LoadSettings();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void LoadSettings()
        {
            s_Enabled = EditorPrefs.GetBool(PrefPrefix + "Enabled", true);
            s_Sensitivity = EditorPrefs.GetFloat(PrefPrefix + "Sensitivity", 1.0f);
            s_UseSnapping = EditorPrefs.GetBool(PrefPrefix + "UseSnapping", true);
        }

        public static void SaveSettings()
        {
            EditorPrefs.SetBool(PrefPrefix + "Enabled", s_Enabled);
            EditorPrefs.SetFloat(PrefPrefix + "Sensitivity", s_Sensitivity);
            EditorPrefs.SetBool(PrefPrefix + "UseSnapping", s_UseSnapping);
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!s_Enabled) return;

            Event e = Event.current;
            bool modifierPressed = e.control;

            if (e.type == EventType.MouseDown && modifierPressed)
            {
                if (e.button == 0) s_LMB = true;
                if (e.button == 1) s_RMB = true;

                if (s_LMB || s_RMB)
                {
                    s_IsDragging = true;
                    s_LastMousePosition = e.mousePosition;
                    s_TotalAccumulatedDelta = Vector3.zero;
                    e.Use();
                }
            }
            else if (e.type == EventType.MouseDrag && s_IsDragging)
            {
                Vector2 delta = e.mousePosition - s_LastMousePosition;
                s_LastMousePosition = e.mousePosition;

                ApplyTransformation(delta);
                e.Use();
            }
            else if (e.type == EventType.MouseUp)
            {
                if (e.button == 0) s_LMB = false;
                if (e.button == 1) s_RMB = false;

                if (!s_LMB && !s_RMB)
                {
                    s_IsDragging = false;
                    s_TotalAccumulatedDelta = Vector3.zero;
                }
            }
        }

        private static void ApplyTransformation(Vector2 mouseDelta)
        {
            if (Selection.transforms.Length == 0) return;

            Undo.RecordObjects(Selection.transforms, "UE-Style Transform");

            float moveX = mouseDelta.x * 0.01f * s_Sensitivity;
            float moveY = -mouseDelta.y * 0.01f * s_Sensitivity;

            Vector3 axis = Vector3.zero;
            float amount = 0;

            if (s_LMB && s_RMB) // Both -> Y (Up)
            {
                axis = Vector3.up;
                amount = moveY;
            }
            else if (s_LMB) // LMB -> Z (Forward)
            {
                axis = Vector3.forward;
                amount = moveY;
            }
            else if (s_RMB) // RMB -> X (Right)
            {
                axis = Vector3.right;
                amount = moveX;
            }

            if (amount == 0) return;

            foreach (var t in Selection.transforms)
            {
                ApplyByTool(t, axis, amount);
            }
        }

        private static void ApplyByTool(Transform t, Vector3 axis, float amount)
        {
            bool isLocal = Tools.pivotRotation == PivotRotation.Local;
            Vector3 worldAxis = isLocal ? t.TransformDirection(axis) : axis;

            switch (Tools.current)
            {
                case Tool.Move:
                    float snapVal = EditorSnapSettings.move.x;
                    float snapped = ProcessSnap(ref s_TotalAccumulatedDelta.x, amount, snapVal);
                    if (Mathf.Abs(snapped) > 0) t.position += worldAxis * snapped;
                    break;
                case Tool.Rotate:
                    float rotateSnap = EditorSnapSettings.rotate;
                    float rotAmount = amount * 100f; // Scale up for rotation feel
                    float snappedRot = ProcessSnap(ref s_TotalAccumulatedDelta.y, rotAmount, rotateSnap);
                    if (Mathf.Abs(snappedRot) > 0) t.Rotate(worldAxis, snappedRot, Space.World);
                    break;
                case Tool.Scale:
                    float scaleSnap = EditorSnapSettings.scale / 100f;
                    float snappedScale = ProcessSnap(ref s_TotalAccumulatedDelta.z, amount, scaleSnap);
                    if (Mathf.Abs(snappedScale) > 0)
                    {
                        float f = 1.0f + snappedScale;
                        t.localScale = Vector3.Scale(t.localScale, new Vector3(
                            axis.x != 0 ? f : 1,
                            axis.y != 0 ? f : 1,
                            axis.z != 0 ? f : 1
                        ));
                    }
                    break;
            }
        }

        private static float ProcessSnap(ref float accumulated, float delta, float snap)
        {
            if (!s_UseSnapping || snap <= 0) return delta;
            
            accumulated += delta;
            if (Mathf.Abs(accumulated) >= snap)
            {
                float snapped = Mathf.Round(accumulated / snap) * snap;
                accumulated -= snapped;
                return snapped;
            }
            return 0;
        }

        // --- Settings Provider ---
        public static bool Enabled { get => s_Enabled; set { s_Enabled = value; SaveSettings(); } }
        public static float Sensitivity { get => s_Sensitivity; set { s_Sensitivity = value; SaveSettings(); } }
        public static bool UseSnapping { get => s_UseSnapping; set { s_UseSnapping = value; SaveSettings(); } }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Project/VRC UE-Style Gizmos", SettingsScope.Project)
            {
                label = "VRC UE-Style Gizmos",
                guiHandler = (searchContext) =>
                {
                    EditorGUI.BeginChangeCheck();
                    s_Enabled = EditorGUILayout.Toggle("Enabled", s_Enabled);
                    s_Sensitivity = EditorGUILayout.Slider("Sensitivity", s_Sensitivity, 0.1f, 5.0f);
                    s_UseSnapping = EditorGUILayout.Toggle("Use Snapping", s_UseSnapping);

                    EditorGUILayout.Space();
                    if (GUILayout.Button("Remove Package Content (Cleanup)"))
                    {
                        CleanupPackage();
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        SaveSettings();
                    }
                }
            };
            return provider;
        }

        private static void CleanupPackage()
        {
            if (EditorUtility.DisplayDialog("Remove Package", "Are you sure you want to delete the VRC UE-Style Gizmos package folder?", "Yes", "Cancel"))
            {
                string path = "Packages/dev.nerarth.ue-gizmos";
                if (!AssetDatabase.IsValidFolder(path))
                {
                    path = "Assets/VRC_UnrealTransformGizmo";
                }

                if (AssetDatabase.IsValidFolder(path))
                {
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.Refresh();
                }
                else
                {
                    Debug.LogWarning("Could not find package folder to remove.");
                }
            }
        }
    }

    [Overlay(typeof(SceneView), "UE-Style Gizmos", true)]
    public class GizmoOverlay : ToolbarOverlay
    {
        GizmoOverlay() : base(GizmoDropdown.ID) { }

        [EditorToolbarElement(ID, typeof(SceneView))]
        class GizmoDropdown : EditorToolbarDropdown
        {
            public const string ID = "UEStyleGizmoDropdown";
            private static Texture2D s_Icon;

            public GizmoDropdown()
            {
                tooltip = "UE-Style Gizmo Settings";
                
                // Load Icon
                if (s_Icon == null)
                {
                    // Try to load from package
                    string path = "Packages/dev.nerarth.ue-gizmos/Editor/Icons/UEGizmoIcon_Transparent256px.png";
                    s_Icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (s_Icon == null)
                    {
                        // Try to load from local
                        path = "Assets/VRC_UnrealTransformGizmo/Editor/Icons/UEGizmoIcon_Transparent256px.png";
                        s_Icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    }
                }

                if (s_Icon != null)
                {
                    icon = s_Icon;
                    text = "";
                }
                else
                {
                    text = "UE-Style Gizmo";
                }

                clicked += ShowDropdown;
            }

            void ShowDropdown()
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Enabled"), UEStyleGizmo.Enabled, () => UEStyleGizmo.Enabled = !UEStyleGizmo.Enabled);
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Use Snapping"), UEStyleGizmo.UseSnapping, () => UEStyleGizmo.UseSnapping = !UEStyleGizmo.UseSnapping);
                menu.AddSeparator("Sensitivity/");
                menu.AddItem(new GUIContent("Sensitivity/0.5x"), UEStyleGizmo.Sensitivity == 0.5f, () => UEStyleGizmo.Sensitivity = 0.5f);
                menu.AddItem(new GUIContent("Sensitivity/1.0x"), UEStyleGizmo.Sensitivity == 1.0f, () => UEStyleGizmo.Sensitivity = 1.0f);
                menu.AddItem(new GUIContent("Sensitivity/2.0x"), UEStyleGizmo.Sensitivity == 2.0f, () => UEStyleGizmo.Sensitivity = 2.0f);
                menu.AddItem(new GUIContent("Sensitivity/5.0x"), UEStyleGizmo.Sensitivity == 5.0f, () => UEStyleGizmo.Sensitivity = 5.0f);

                Rect rect = worldBound;
                rect.position = GUIUtility.GUIToScreenPoint(rect.position);
                menu.DropDown(rect);
            }
        }
    }
}
