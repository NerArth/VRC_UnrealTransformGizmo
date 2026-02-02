using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UEStyle.UEGizmos
{
    public class GizmoPopupWindow : EditorWindow
    {
        private Action _onChanged;
        public static void Show(Rect anchor, Action onChanged)
        {
            var wnd = CreateInstance<GizmoPopupWindow>();
            wnd._onChanged = onChanged;
            wnd.ShowAsDropDown(anchor, new Vector2(320, 220));
        }

        private VisualElement root;

        private void OnEnable()
        {
            root = rootVisualElement;

            // Try to load UXML + USS from package (fallback to code-built UI if missing)
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/VRC_UnrealTransformGizmo/Editor/UXML/GizmoPopup.uxml");
            if (tree != null)
            {
                tree.CloneTree(root);
            }
            else
            {
                // Fallback: build minimal UI in code
                var container = new VisualElement();
                container.Add(new Toggle("Enabled") { name = "enabledToggle" });
                container.Add(new Toggle("Use Snapping") { name = "useSnappingToggle" });
                container.Add(new Toggle("Proportionate Scale") { name = "proportionateToggle" });
                container.Add(new Label("Sensitivity"));
                var s = new Slider(0.1f, 5.0f) { name = "sensitivitySlider" };
                container.Add(s);
                var row = new VisualElement() { name = "sensitivityButtonsRow" };
                row.style.flexDirection = FlexDirection.Row;
                container.Add(row);
                var adv = new Foldout() { text = "Advanced", name = "advancedFoldout" };
                adv.Add(new Button(() => {}) { name = "restoreButton", text = "Restore Defaults" });
                adv.Add(new Button(() => {}) { name = "openSettingsButton", text = "Open Settings..." });
                container.Add(adv);
                root.Add(container);
            }

            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/VRC_UnrealTransformGizmo/Editor/Styles/GizmoPopup.uss");
            if (sheet != null) root.styleSheets.Add(sheet);

            BindControls();
        }

        private void BindControls()
        {
            var enabledToggle = root.Q<Toggle>("enabledToggle");
            if (enabledToggle != null)
            {
                enabledToggle.value = UEStyleGizmo.Enabled;
                enabledToggle.RegisterValueChangedCallback(evt => { UEStyleGizmo.Enabled = evt.newValue; UEStyleGizmo.SaveSettings(); _onChanged?.Invoke(); });
            }

            var snappingToggle = root.Q<Toggle>("useSnappingToggle");
            if (snappingToggle != null)
            {
                snappingToggle.value = UEStyleGizmo.UseSnapping;
                snappingToggle.RegisterValueChangedCallback(evt => { UEStyleGizmo.UseSnapping = evt.newValue; UEStyleGizmo.SaveSettings(); });
            }

            var propToggle = root.Q<Toggle>("proportionateToggle");
            if (propToggle != null)
            {
                propToggle.value = UEStyleGizmo.ProportionateScale;
                propToggle.RegisterValueChangedCallback(evt => { UEStyleGizmo.ProportionateScale = evt.newValue; UEStyleGizmo.SaveSettings(); });
            }

            var sensSlider = root.Q<Slider>("sensitivitySlider");
            if (sensSlider != null)
            {
                sensSlider.lowValue = 0.1f;
                sensSlider.highValue = 5.0f;
                sensSlider.value = UEStyleGizmo.Sensitivity;
                sensSlider.RegisterValueChangedCallback(evt => { UEStyleGizmo.Sensitivity = evt.newValue; UEStyleGizmo.SaveSettings(); });
            }

            var row = root.Q("sensitivityButtonsRow");
            if (row != null)
            {
                foreach (var v in new float[] { 0.5f, 1.0f, 2.0f, 5.0f })
                {
                    float value = v;
                    var btn = new Button(() => {
                        UEStyleGizmo.Sensitivity = value;
                        UEStyleGizmo.SaveSettings();
                        if (sensSlider != null) sensSlider.value = value;
                        _onChanged?.Invoke();
                    }) { text = v.ToString("0.##") + "x" };
                    btn.style.marginRight = 4;
                    row.Add(btn);
                }
            }

            var restoreBtn = root.Q<Button>("restoreButton");
            if (restoreBtn != null)
            {
                restoreBtn.clicked += () => {
                    UEStyleGizmo.Enabled = true;
                    UEStyleGizmo.Sensitivity = 1.0f;
                    UEStyleGizmo.UseSnapping = true;
                    UEStyleGizmo.ProportionateScale = true;
                    UEStyleGizmo.SaveSettings();
                    if (sensSlider != null) sensSlider.value = UEStyleGizmo.Sensitivity;
                    _onChanged?.Invoke();
                };
            }

            var openSettingsBtn = root.Q<Button>("openSettingsButton");
            if (openSettingsBtn != null)
            {
                openSettingsBtn.clicked += () => SettingsService.OpenProjectSettings("Project/VRC UE-Style Gizmos");
            }
        }
    }
}
