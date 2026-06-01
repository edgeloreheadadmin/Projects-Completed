using System;
using UnityEngine;

namespace Auras.IntegrativeAuraFramework
{
    /// <summary>
    /// Enum for Integrative Aura Framework layers.
    /// Represents the spectrum of mind-body-consciousness integration.
    /// </summary>
    public enum IntegrativeLayerType : int
    {
        Physical = 0,
        Vitality = 1,
        Emotional = 2,
        Mental = 3,
        Transcendent = 4,
        Count = 5,
    }

    /// <summary>
    /// State for a single integrative layer.
    /// Uses same structure as BiofieldLayerState for compatibility with framework base.
    /// </summary>
    [Serializable]
    public struct IntegrativeLayerState
    {
        public float intensity;
        public float integrity;

        public IntegrativeLayerState(float intensity, float integrity)
        {
            this.intensity = Mathf.Clamp01(intensity);
            this.integrity = Mathf.Clamp01(integrity);
        }
    }

    /// <summary>
    /// Definition for an Integrative layer.
    /// Describes visual/audio properties and characteristics.
    /// </summary>
    [Serializable]
    public sealed class IntegrativeLayerDefinition
    {
        public IntegrativeLayerType layer;
        public string displayName;

        [TextArea(2, 4)]
        public string aspectDescription;

        [TextArea(2, 4)]
        public string integrationRole;

        [TextArea(1, 2)]
        public string practiceGuidance;

        public Color balancedColor = Color.white;

        [Min(0.1f)]
        public float baseScale = 1f;

        [Min(0.1f)]
        public float peakScale = 1.2f;

        [Min(0.01f)]
        public float shellThickness = 0.08f;

        [Min(0f)]
        public float baseEmission = 4f;

        [Min(0f)]
        public float peakEmission = 18f;

        [Min(0f)]
        public float baseLightIntensity = 0.12f;

        [Min(0f)]
        public float peakLightIntensity = 1.1f;

        [Min(0.01f)]
        public float pulseSpeed = 0.8f;
    }

    /// <summary>
    /// Binding for an Integrative layer to game objects.
    /// </summary>
    [Serializable]
    public sealed class IntegrativeLayerBinding
    {
        public IntegrativeLayerType layer;
        public Transform anchor;
        public ParticleSystem particleSystem;
        public Light light;
        public Renderer renderer;

        [Min(0f)]
        public float scaleMultiplier = 1f;

        [Min(0f)]
        public float emissionMultiplier = 1f;

        [Min(0f)]
        public float lightMultiplier = 1f;

        public bool driveAnchorScale = true;
        public bool driveParticleEmission = true;
        public bool driveParticleColor = true;
        public bool driveParticleSize = true;
        public bool driveLight = true;
        public bool driveRendererColor = true;
    }
}
