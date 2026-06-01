#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Auras.BiofieldAuraMatrix.Editor
{
    public static class BiofieldAuraMatrixEditorMenu
    {
        private const string DefaultProfilePath = "Assets/Auras/Biofield Aura Matrix/BiofieldAuraProfile.asset";

        [MenuItem("Assets/Create/Auras/Seed Biofield Aura Matrix Profile")]
        public static void SeedDefaultProfileAsset()
        {
            BiofieldAuraProfile existing = AssetDatabase.LoadAssetAtPath<BiofieldAuraProfile>(DefaultProfilePath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            BiofieldAuraProfile asset = ScriptableObject.CreateInstance<BiofieldAuraProfile>();
            asset.ResetToResearchDefaults();

            AssetDatabase.CreateAsset(asset, DefaultProfilePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        [MenuItem("GameObject/Auras/Create Biofield Aura Matrix Rig", false, 11)]
        public static void CreateBiofieldAuraMatrixRig(MenuCommand command)
        {
            BiofieldAuraProfile profile = AssetDatabase.LoadAssetAtPath<BiofieldAuraProfile>(DefaultProfilePath);
            if (profile == null)
            {
                SeedDefaultProfileAsset();
                profile = AssetDatabase.LoadAssetAtPath<BiofieldAuraProfile>(DefaultProfilePath);
            }

            GameObject rig = new GameObject("BiofieldAuraMatrixRig");
            GameObjectUtility.SetParentAndAlign(rig, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(rig, "Create Biofield Aura Matrix Rig");

            BiofieldAuraController controller = Undo.AddComponent<BiofieldAuraController>(rig);
            BiofieldAuraDriver driver = Undo.AddComponent<BiofieldAuraDriver>(rig);
            driver.SetController(controller);

            if (profile != null)
            {
                controller.SetProfile(profile);
            }

            Array layerValues = Enum.GetValues(typeof(BiofieldLayerType));
            for (int i = 0; i < layerValues.Length; i++)
            {
                BiofieldLayerType layer = (BiofieldLayerType)layerValues.GetValue(i);
                CreateAnchor(rig.transform, layer, profile);
            }

            controller.AutoBindByName();
            controller.SnapAnchorsToProfileScales();
            driver.ApplyInteractionsNow();

            Selection.activeGameObject = rig;
        }

        private static void CreateAnchor(Transform parent, BiofieldLayerType layer, BiofieldAuraProfile profile)
        {
            string name = $"{layer}Shell";
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return;
            }

            BiofieldLayerDefinition definition = profile != null
                ? profile.GetLayer(layer)
                : BiofieldAuraProfile.GetDefaultLayerDefinition(layer);

            GameObject child = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(child, "Create Biofield Layer Shell");
            child.transform.SetParent(parent, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one * definition.baseScale;
        }
    }
}
#endif
