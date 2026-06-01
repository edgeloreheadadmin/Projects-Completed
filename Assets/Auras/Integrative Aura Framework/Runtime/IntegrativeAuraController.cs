using System;
using System.Collections.Generic;
using Auras.Core;
using UnityEngine;

namespace Auras.IntegrativeAuraFramework
{
    /// <summary>
    /// Integrative framework implementation inheriting from generic AuraFrameworkBase.
    /// Manages mind-body-consciousness integration layer states and bindings.
    /// Provides framework-specific implementations of abstract methods.
    /// </summary>
    public class IntegrativeAuraController : AuraFrameworkBase<IntegrativeLayerType, IntegrativeLayerState, IntegrativeLayerDefinition>
    {
        [Header("Integrative Profile")]
        [SerializeField]
        private IntegrativeAuraProfile integrativeProfile;

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
        /// Get the Integrative profile (implements abstract method).
        /// </summary>
        public override ScriptableObject GetProfile()
        {
            return integrativeProfile;
        }

        /// <summary>
        /// Set the Integrative profile (implements abstract method).
        /// </summary>
        public override void SetProfile(ScriptableObject value)
        {
            if (value is IntegrativeAuraProfile profile)
            {
                integrativeProfile = profile;
                EnsureGraphComputer();
            }
        }

        /// <summary>
        /// Get layer definition for a given layer ID (implements abstract method).
        /// </summary>
        protected override IntegrativeLayerDefinition GetLayerDefinition(int layerId)
        {
            if (integrativeProfile == null)
            {
                return null;
            }

            var enumValue = (IntegrativeLayerType)layerId;
            return integrativeProfile.GetLayer(enumValue);
        }

        /// <summary>
        /// Convert Integrative enum to integer (implements abstract method).
        /// </summary>
        protected override int LayerEnumToInt(IntegrativeLayerType layer)
        {
            return (int)layer;
        }

        /// <summary>
        /// Get total Integrative layer count (implements abstract method).
        /// </summary>
        protected override int GetLayerCount()
        {
            return (int)IntegrativeLayerType.Count;
        }

        /// <summary>
        /// Get aliases for an Integrative layer (implements abstract method).
        /// </summary>
        protected override string[] GetLayerAliases(int layerId)
        {
            var def = GetLayerDefinition(layerId);
            var enumName = ((IntegrativeLayerType)layerId).ToString();
            if (def != null && !string.IsNullOrEmpty(def.displayName))
            {
                return new[] { def.displayName, enumName };
            }
            return new[] { enumName };
        }

        /// <summary>
        /// Get base scale for Integrative layer definition (implements abstract method).
        /// </summary>
        protected override float GetDefinitionBaseScale(IntegrativeLayerDefinition definition)
        {
            return definition?.baseScale ?? 1f;
        }

        /// <summary>
        /// Get peak scale for Integrative layer definition (implements abstract method).
        /// </summary>
        protected override float GetDefinitionPeakScale(IntegrativeLayerDefinition definition)
        {
            return definition?.peakScale ?? 1f;
        }

        /// <summary>
        /// Get shell thickness for Integrative layer definition (implements abstract method).
        /// </summary>
        protected override float GetDefinitionShellThickness(IntegrativeLayerDefinition definition)
        {
            return definition?.shellThickness ?? 0.1f;
        }

        /// <summary>
        /// Get base emission for Integrative layer definition (implements abstract method).
        /// </summary>
        protected override float GetDefinitionBaseEmission(IntegrativeLayerDefinition definition)
        {
            return definition?.baseEmission ?? 0f;
        }

        /// <summary>
        /// Get peak emission for Integrative layer definition (implements abstract method).
        /// </summary>
        protected override float GetDefinitionPeakEmission(IntegrativeLayerDefinition definition)
        {
            return definition?.peakEmission ?? 1f;
        }

        /// <summary>
        /// Get base light intensity for Integrative layer definition (implements abstract method).
        /// </summary>
        protected override float GetDefinitionBaseLightIntensity(IntegrativeLayerDefinition definition)
        {
            return definition?.baseLightIntensity ?? 0f;
        }

        /// <summary>
        /// Get peak light intensity for Integrative layer definition (implements abstract method).
        /// </summary>
        protected override float GetDefinitionPeakLightIntensity(IntegrativeLayerDefinition definition)
        {
            return definition?.peakLightIntensity ?? 1f;
        }

        /// <summary>
        /// Get pulse speed for Integrative layer definition (implements abstract method).
        /// </summary>
        protected override float GetDefinitionPulseSpeed(IntegrativeLayerDefinition definition)
        {
            return definition?.pulseSpeed ?? 1f;
        }

        /// <summary>
        /// Get balanced color for Integrative layer definition (implements abstract method).
        /// </summary>
        protected override Color GetDefinitionBalancedColor(IntegrativeLayerDefinition definition)
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
                integrativeProfile,
                interactionProfile.LayerDependencies
            );
        }

        /// <summary>
        /// Initialize layer contexts for Integrative framework.
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
