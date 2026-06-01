using System;
using UnityEngine;

namespace Auras.BiofieldAuraMatrix
{
    public enum BiofieldLayerType
    {
        Etheric = 0,
        Emotional = 1,
        Mental = 2,
        Astral = 3,
        EthericTemplate = 4,
        Celestial = 5,
        Causal = 6,
    }

    public enum CircadianAlignmentMode
    {
        Disrupted = 0,
        Recovering = 1,
        Aligned = 2,
        Coherent = 3,
    }

    public enum SocialFieldMode
    {
        Protected = 0,
        Supportive = 1,
        Charged = 2,
        Overwhelming = 3,
    }

    public enum InformationClimateMode
    {
        Quiet = 0,
        Focused = 1,
        Saturated = 2,
        Noisy = 3,
    }

    public enum BiofieldPracticeMode
    {
        Grounding = 0,
        EmotionalRelease = 1,
        HeartCoherence = 2,
        Meditation = 3,
        Service = 4,
    }

    [Serializable]
    public struct BiofieldLayerState
    {
        [Range(0f, 1f)]
        public float intensity;

        [Range(0f, 1f)]
        public float integrity;

        public BiofieldLayerState(float intensity, float integrity)
        {
            this.intensity = Mathf.Clamp01(intensity);
            this.integrity = Mathf.Clamp01(integrity);
        }
    }

    [Serializable]
    public sealed class BiofieldLayerDefinition
    {
        public BiofieldLayerType layer;
        public string displayName;

        [TextArea(2, 4)]
        public string primaryFunction;

        [TextArea(2, 4)]
        public string biofieldBridge;

        [TextArea(2, 4)]
        public string environmentInteraction;

        [TextArea(2, 4)]
        public string wellBeingRole;

        [TextArea(1, 2)]
        public string chakraCorrespondence;

        [TextArea(1, 3)]
        public string supportPractice;

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

    [Serializable]
    public sealed class BiofieldLayerBinding
    {
        public BiofieldLayerType layer;
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
