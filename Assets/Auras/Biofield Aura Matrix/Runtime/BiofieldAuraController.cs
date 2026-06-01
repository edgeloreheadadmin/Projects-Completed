using System;
using System.Collections.Generic;
using Auras.Core;
using UnityEngine;

namespace Auras.BiofieldAuraMatrix
{
    /// <summary>
    /// Biofield framework implementation inheriting from generic AuraFrameworkBase.
    /// Manages aura layer states and their visual/audio bindings using explicit dependencies.
    /// Provides framework-specific implementations of abstract methods.
    /// </summary>
    public class BiofieldAuraController : AuraFrameworkBase<BiofieldLayerType, BiofieldLayerState, BiofieldLayerDefinition>
    {
        [Header("Biofield Profile")]
        [SerializeField]
        private BiofieldAuraProfile bioieldProfile;

        [SerializeField]
        private InteractionProfile interactionProfile;

        [SerializeField]
        private bool useDependencyGraph = true;

        private DependencyGraphComputer graphComputer;

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureGraphComputer();
        }

        /// <summary>
        /// Get the Biofield profile (implements abstract method).
        /// </summary>
        public override ScriptableObject GetProfile()
        {
            return bioieldProfile;
        }

        /// <summary>
        /// Set the Biofield profile (implements abstract method).
        /// </summary>
        public override void SetProfile(ScriptableObject value)
        {
            if (value is BiofieldAuraProfile profile)
            {
                bioieldProfile = profile;
                EnsureGraphComputer();
            }
        }

        /// <summary>
        /// Get layer definition for a given layer ID (implements abstract method).
        /// </summary>
        protected override BiofieldLayerDefinition GetLayerDefinition(int layerId)
        {
            if (bioieldProfile == null)
            {
                return null;
            }

            var enumValue = (BiofieldLayerType)layerId;
            return bioieldProfile.GetLayer(enumValue);
        }

        /// <summary>
        /// Convert Biofield enum to integer (implements abstract method).
        /// </summary>
        protected override int LayerEnumToInt(BiofieldLayerType layer)
        {
            return (int)layer;
        }

        /// <summary>
        /// Get total Biofield layer count (implements abstract method).
        /// </summary>
        protected override int GetLayerCount()
        {
            return (int)BiofieldLayerType.Count;
        }

        /// <summary>
        /// Get aliases for a Biofield layer (implements abstract method).
        /// </summary>
        protected override string[] GetLayerAliases(int layerId)
        {
            // Use displayName and enum name as fallback aliases
            var def = GetLayerDefinition(layerId);
            var enumName = ((BiofieldLayerType)layerId).ToString();
            if (def != null && !string.IsNullOrEmpty(def.displayName))
            {
                return new[] { def.displayName, enumName };
            }
            return new[] { enumName };
        }

        /// <summary>
        /// Get base scale for Biofield layer definition (implements abstract method).
        /// </summary>
        protected override float GetDefinitionBaseScale(BiofieldLayerDefinition definition)
        {
            return definition?.baseScale ?? 1f;
        }

        /// <summary>
        /// Get peak scale for Biofield layer definition (implements abstract method).
        /// </summary>
        protected override float GetDefinitionPeakScale(BiofieldLayerDefinition definition)
        {
            return definition?.peakScale ?? 1f;
        }

        /// <summary>
        /// Get shell thickness for Biofield layer definition (implements abstract method).
        /// </summary>
        protected override float GetDefinitionShellThickness(BiofieldLayerDefinition definition)
        {
            return definition?.shellThickness ?? 0.1f;
        }

        /// <summary>
        /// Get base emission for Biofield layer definition (implements abstract method).
        /// </summary>
        protected override float GetDefinitionBaseEmission(BiofieldLayerDefinition definition)
        {
            return definition?.baseEmission ?? 0f;
        }

        /// <summary>
        /// Get peak emission for Biofield layer definition (implements abstract method).
        /// </summary>
        protected override float GetDefinitionPeakEmission(BiofieldLayerDefinition definition)
        {
            return definition?.peakEmission ?? 1f;
        }

        /// <summary>
        /// Get base light intensity for Biofield layer definition (implements abstract method).
        /// </summary>
        protected override float GetDefinitionBaseLightIntensity(BiofieldLayerDefinition definition)
        {
            return definition?.baseLightIntensity ?? 0f;
        }

        /// <summary>
        /// Get peak light intensity for Biofield layer definition (implements abstract method).
        /// </summary>
        protected override float GetDefinitionPeakLightIntensity(BiofieldLayerDefinition definition)
        {
            return definition?.peakLightIntensity ?? 1f;
        }

        /// <summary>
        /// Get pulse speed for Biofield layer definition (implements abstract method).
        /// </summary>
        protected override float GetDefinitionPulseSpeed(BiofieldLayerDefinition definition)
        {
            return definition?.pulseSpeed ?? 1f;
        }

        /// <summary>
        /// Get balanced color for Biofield layer definition (implements abstract method).
        /// </summary>
        protected override Color GetDefinitionBalancedColor(BiofieldLayerDefinition definition)
        {
            return definition?.balancedColor ?? Color.white;
        }

        /// <summary>
        /// Ensure dependency graph computer is initialized for this framework.
        /// </summary>
        private void EnsureGraphComputer()
        {
            if (!useDependencyGraph || interactionProfile == null)
            {
                return;
            }

            if (layerContexts == null)
            {
                return;
            }

            graphComputer = new DependencyGraphComputer(
                layerContexts,
                bioieldProfile,
                interactionProfile.LayerDependencies
            );
        }


        /// <summary>
        /// Initialize layer contexts for Biofield framework.
        /// </summary>
        protected override void InitializeContexts()
        {
            if (layerContexts == null)
            {
                return;
            }

            for (int i = 0; i < GetLayerCount(); i++)
            {
                var definition = GetLayerDefinition(i);
                if (definition == null)
                {
                    continue;
                }

                var context = layerContexts.Get(i);
                if (context == null)
                {
                    context = new AuraLayerContext(i);
                    layerContexts.Register(i, context);
                    if (!string.IsNullOrEmpty(definition.displayName))
                    {
                        layerContexts.RegisterLayerName(i, definition.displayName);
                    }
                }

                // Configure default easing curves for this layer
                context.intensityEasing = EasingCurve.EaseInOutCubic();
                context.integrityEasing = EasingCurve.EaseInOutCubic();
            }

            EnsureGraphComputer();
        }
    }
}
