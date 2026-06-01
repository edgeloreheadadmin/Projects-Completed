using Auras.Core;
using UnityEditor;
using UnityEngine;

namespace Auras.IntegrativeAuraFramework
{
    /// <summary>
    /// Editor menu items for Integrative Aura Framework setup and management.
    /// </summary>
    public static class IntegrativeAuraEditorMenu
    {
        [MenuItem("Assets/Create/Auras/Integrative Aura Matrix/Integration Profile")]
        public static void CreateIntegrativeProfile()
        {
            var profile = ScriptableObject.CreateInstance<IntegrativeAuraProfile>();
            profile.ResetToResearchDefaults();

            string path = EditorUtility.SaveFilePanelInProject(
                "Create Integrative Aura Profile",
                "IntegrativeAuraProfile",
                "asset",
                "Choose a location for the profile"
            );

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(profile, path);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = profile;
            }
            else
            {
                Object.DestroyImmediate(profile);
            }
        }

        [MenuItem("Assets/Create/Auras/Integrative Aura Matrix/Interaction Profile")]
        public static void CreateInteractionProfile()
        {
            var profile = ScriptableObject.CreateInstance<InteractionProfile>();

            string path = EditorUtility.SaveFilePanelInProject(
                "Create Interaction Profile",
                "IntegrativeInteractionProfile",
                "asset",
                "Choose a location for the interaction profile"
            );

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(profile, path);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = profile;
            }
            else
            {
                Object.DestroyImmediate(profile);
            }
        }

        [MenuItem("GameObject/Auras/Create Integrative Aura Rig")]
        public static void CreateIntegrativeAuraRig()
        {
            var rigGo = new GameObject("IntegrativeAuraRig");

            // Add controller
            var controller = rigGo.AddComponent<IntegrativeAuraController>();

            // Create layer anchors
            for (int i = 0; i < (int)IntegrativeLayerType.Count; i++)
            {
                var layerName = ((IntegrativeLayerType)i).ToString();
                var layerGo = new GameObject(layerName);
                layerGo.transform.SetParent(rigGo.transform);
                layerGo.transform.localPosition = Vector3.zero;
                layerGo.transform.localRotation = Quaternion.identity;
                layerGo.transform.localScale = Vector3.one * (1f + i * 0.1f);
            }

            // Create profile
            var profile = ScriptableObject.CreateInstance<IntegrativeAuraProfile>();
            profile.ResetToResearchDefaults();
            controller.SetProfile(profile);

            // Auto-bind
            controller.AutoBindByName();

            Selection.activeGameObject = rigGo;
        }
    }
}
