using UnityEngine;

namespace Auras.BiofieldAuraMatrix
{
    /// <summary>
    /// Drives the aura layer states based on environmental and practice modes.
    /// Will be refactored in Phase 3 to use explicit dependency graph from InteractionProfile.
    /// Currently maintains original weighted calculation logic.
    /// </summary>
    [ExecuteAlways]
    public sealed class BiofieldAuraDriver : MonoBehaviour
    {
        [SerializeField]
        private BiofieldAuraController controller;

        [SerializeField]
        private bool applyInEditMode = true;

        [SerializeField]
        private bool animateInputs;

        [SerializeField]
        [Range(0.05f, 2f)]
        private float animationSpeed = 0.35f;

        [Header("Environment Modes")]
        [SerializeField]
        private CircadianAlignmentMode circadianAlignment = CircadianAlignmentMode.Aligned;

        [SerializeField]
        private SocialFieldMode socialField = SocialFieldMode.Supportive;

        [SerializeField]
        private InformationClimateMode informationClimate = InformationClimateMode.Focused;

        [SerializeField]
        private BiofieldPracticeMode practiceMode = BiofieldPracticeMode.HeartCoherence;

        [Header("Biofield Supports")]
        [SerializeField]
        [Range(0f, 1f)]
        private float geomagneticPressure = 0.3f;

        [SerializeField]
        [Range(0f, 1f)]
        private float natureExposure = 0.7f;

        [SerializeField]
        [Range(0f, 1f)]
        private float energeticBoundaries = 0.72f;

        [SerializeField]
        [Range(0f, 1f)]
        private float heartCoherence = 0.76f;

        [SerializeField]
        [Range(0f, 1f)]
        private float contemplativeDepth = 0.72f;

        [Header("Layer Inputs")]
        [SerializeField]
        [Range(0f, 1f)]
        private float physicalEmbodiment = 0.74f;

        [SerializeField]
        [Range(0f, 1f)]
        private float emotionalFluidity = 0.72f;

        [SerializeField]
        [Range(0f, 1f)]
        private float cognitiveOrder = 0.74f;

        [SerializeField]
        [Range(0f, 1f)]
        private float relationalOpenness = 0.76f;

        [SerializeField]
        [Range(0f, 1f)]
        private float expressiveAlignment = 0.7f;

        [SerializeField]
        [Range(0f, 1f)]
        private float intuitiveSensitivity = 0.78f;

        [SerializeField]
        [Range(0f, 1f)]
        private float soulAlignment = 0.8f;

        public void SetController(BiofieldAuraController value)
        {
            controller = value;
        }

        public void SetEnvironmentModes(
            CircadianAlignmentMode circadian,
            SocialFieldMode social,
            InformationClimateMode information,
            BiofieldPracticeMode practice)
        {
            circadianAlignment = circadian;
            socialField = social;
            informationClimate = information;
            practiceMode = practice;
        }

        public void SetSupportInputs(
            float geomagneticValue,
            float natureValue,
            float boundariesValue,
            float heartValue,
            float contemplativeValue)
        {
            geomagneticPressure = Mathf.Clamp01(geomagneticValue);
            natureExposure = Mathf.Clamp01(natureValue);
            energeticBoundaries = Mathf.Clamp01(boundariesValue);
            heartCoherence = Mathf.Clamp01(heartValue);
            contemplativeDepth = Mathf.Clamp01(contemplativeValue);
        }

        public void SetLayerInput(BiofieldLayerType layer, float value)
        {
            value = Mathf.Clamp01(value);

            switch (layer)
            {
                case BiofieldLayerType.Etheric:
                    physicalEmbodiment = value;
                    break;
                case BiofieldLayerType.Emotional:
                    emotionalFluidity = value;
                    break;
                case BiofieldLayerType.Mental:
                    cognitiveOrder = value;
                    break;
                case BiofieldLayerType.Astral:
                    relationalOpenness = value;
                    break;
                case BiofieldLayerType.EthericTemplate:
                    expressiveAlignment = value;
                    break;
                case BiofieldLayerType.Celestial:
                    intuitiveSensitivity = value;
                    break;
                case BiofieldLayerType.Causal:
                    soulAlignment = value;
                    break;
            }
        }

        public float GetLayerInput(BiofieldLayerType layer)
        {
            return layer switch
            {
                BiofieldLayerType.Etheric => physicalEmbodiment,
                BiofieldLayerType.Emotional => emotionalFluidity,
                BiofieldLayerType.Mental => cognitiveOrder,
                BiofieldLayerType.Astral => relationalOpenness,
                BiofieldLayerType.EthericTemplate => expressiveAlignment,
                BiofieldLayerType.Celestial => intuitiveSensitivity,
                BiofieldLayerType.Causal => soulAlignment,
                _ => 0f,
            };
        }

        public void BoostLayer(BiofieldLayerType layer, float amount)
        {
            SetLayerInput(layer, GetLayerInput(layer) + amount);
            ApplyInteractionsNow();
        }

        [ContextMenu("Apply Interactions Now")]
        public void ApplyInteractionsNow()
        {
            ApplyInteractions(true);
        }

        private void Reset()
        {
            controller = GetComponent<BiofieldAuraController>();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying && applyInEditMode)
            {
                ApplyInteractions(true);
            }
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying && !applyInEditMode)
            {
                return;
            }

            ApplyInteractions(false);
        }

        private void ApplyInteractions(bool immediate)
        {
            if (controller == null)
            {
                controller = GetComponent<BiofieldAuraController>();
            }

            if (controller == null)
            {
                return;
            }

            float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
            float earthPulse = animateInputs ? Wave(time, animationSpeed * 1.2f, 0f) : 0.5f;
            float socialPulse = animateInputs ? Wave(time, animationSpeed * 1.05f, 1.3f) : 0.5f;
            float insightPulse = animateInputs ? Wave(time, animationSpeed * 0.82f, 2.5f) : 0.5f;

            float circadianSupport = GetCircadianSupport(circadianAlignment);
            float circadianDisruption = GetCircadianDisruption(circadianAlignment);
            float socialSupport = GetSocialSupport(socialField);
            float socialOverload = GetSocialOverload(socialField);
            float informationClarity = GetInformationClarity(informationClimate);
            float informationNoise = GetInformationNoise(informationClimate);
            float groundingBias = GetGroundingBias(practiceMode);
            float releaseBias = GetReleaseBias(practiceMode);
            float heartBias = GetHeartBias(practiceMode);
            float meditationBias = GetMeditationBias(practiceMode);
            float serviceBias = GetServiceBias(practiceMode);

            float stabilityField = Average(physicalEmbodiment, natureExposure, circadianSupport);
            float regulationField = Average(energeticBoundaries, heartCoherence, contemplativeDepth);
            float clarityField = Average(cognitiveOrder, informationClarity, contemplativeDepth);
            float devotionField = Average(intuitiveSensitivity, soulAlignment, contemplativeDepth);

            BiofieldLayerState etheric = new BiofieldLayerState(
                physicalEmbodiment * 0.48f + natureExposure * 0.18f + groundingBias * 0.14f + circadianSupport * 0.12f + stabilityField * 0.08f + earthPulse * 0.05f - geomagneticPressure * 0.14f - circadianDisruption * 0.08f,
                physicalEmbodiment * 0.28f + energeticBoundaries * 0.22f + natureExposure * 0.18f + groundingBias * 0.14f + circadianSupport * 0.12f + regulationField * 0.06f - geomagneticPressure * 0.16f - circadianDisruption * 0.06f);

            BiofieldLayerState emotional = new BiofieldLayerState(
                emotionalFluidity * 0.42f + socialSupport * 0.18f + releaseBias * 0.14f + heartCoherence * 0.12f + socialPulse * 0.08f + relationalOpenness * 0.08f - socialOverload * 0.1f,
                emotionalFluidity * 0.26f + heartCoherence * 0.24f + energeticBoundaries * 0.18f + socialSupport * 0.16f + releaseBias * 0.1f + regulationField * 0.06f - socialOverload * 0.16f);

            BiofieldLayerState mental = new BiofieldLayerState(
                cognitiveOrder * 0.42f + informationClarity * 0.2f + circadianSupport * 0.12f + contemplativeDepth * 0.1f + clarityField * 0.08f + earthPulse * 0.04f - informationNoise * 0.18f - circadianDisruption * 0.08f,
                cognitiveOrder * 0.28f + contemplativeDepth * 0.22f + informationClarity * 0.2f + energeticBoundaries * 0.1f + meditationBias * 0.1f + clarityField * 0.06f - informationNoise * 0.18f);

            BiofieldLayerState astral = new BiofieldLayerState(
                relationalOpenness * 0.38f + heartCoherence * 0.18f + socialSupport * 0.16f + heartBias * 0.14f + intuitiveSensitivity * 0.08f + socialPulse * 0.06f - socialOverload * 0.08f,
                relationalOpenness * 0.26f + heartCoherence * 0.24f + energeticBoundaries * 0.16f + serviceBias * 0.12f + socialSupport * 0.12f + regulationField * 0.06f - socialOverload * 0.1f);

            BiofieldLayerState ethericTemplate = new BiofieldLayerState(
                expressiveAlignment * 0.38f + cognitiveOrder * 0.16f + informationClarity * 0.12f + meditationBias * 0.08f + stabilityField * 0.1f + earthPulse * 0.06f + groundingBias * 0.1f - informationNoise * 0.06f,
                expressiveAlignment * 0.28f + energeticBoundaries * 0.2f + circadianSupport * 0.12f + contemplativeDepth * 0.14f + informationClarity * 0.12f + clarityField * 0.06f - informationNoise * 0.1f);

            BiofieldLayerState celestial = new BiofieldLayerState(
                intuitiveSensitivity * 0.38f + contemplativeDepth * 0.18f + heartCoherence * 0.14f + meditationBias * 0.14f + serviceBias * 0.08f + devotionField * 0.08f + insightPulse * 0.06f - informationNoise * 0.06f,
                intuitiveSensitivity * 0.26f + contemplativeDepth * 0.22f + heartCoherence * 0.18f + energeticBoundaries * 0.1f + meditationBias * 0.12f + serviceBias * 0.08f + devotionField * 0.04f - socialOverload * 0.06f);

            BiofieldLayerState causal = new BiofieldLayerState(
                soulAlignment * 0.4f + contemplativeDepth * 0.14f + meditationBias * 0.12f + serviceBias * 0.12f + devotionField * 0.12f + insightPulse * 0.1f - circadianDisruption * 0.04f,
                soulAlignment * 0.32f + contemplativeDepth * 0.18f + energeticBoundaries * 0.12f + meditationBias * 0.12f + serviceBias * 0.1f + natureExposure * 0.08f + heartCoherence * 0.08f - geomagneticPressure * 0.08f);

            etheric = new BiofieldLayerState(etheric.intensity, etheric.integrity + (ethericTemplate.integrity * 0.08f));
            emotional = new BiofieldLayerState(emotional.intensity, emotional.integrity + (astral.integrity * 0.08f));
            mental = new BiofieldLayerState(mental.intensity, mental.integrity + (ethericTemplate.integrity * 0.06f) + (celestial.integrity * 0.04f));
            astral = new BiofieldLayerState(astral.intensity, astral.integrity + (emotional.integrity * 0.06f) + (celestial.integrity * 0.04f));
            ethericTemplate = new BiofieldLayerState(ethericTemplate.intensity, ethericTemplate.integrity + (mental.integrity * 0.06f) + (causal.integrity * 0.04f));
            celestial = new BiofieldLayerState(celestial.intensity, celestial.integrity + (astral.integrity * 0.04f) + (causal.integrity * 0.08f));
            causal = new BiofieldLayerState(causal.intensity, causal.integrity + (celestial.integrity * 0.08f) + (regulationField * 0.04f));

            controller.SetState(BiofieldLayerType.Etheric, etheric.intensity, etheric.integrity);
            controller.SetState(BiofieldLayerType.Emotional, emotional.intensity, emotional.integrity);
            controller.SetState(BiofieldLayerType.Mental, mental.intensity, mental.integrity);
            controller.SetState(BiofieldLayerType.Astral, astral.intensity, astral.integrity);
            controller.SetState(BiofieldLayerType.EthericTemplate, ethericTemplate.intensity, ethericTemplate.integrity);
            controller.SetState(BiofieldLayerType.Celestial, celestial.intensity, celestial.integrity);
            controller.SetState(BiofieldLayerType.Causal, causal.intensity, causal.integrity);

            if (immediate)
            {
                controller.ApplyNow();
            }
        }

        private static float Wave(float time, float speed, float phase)
        {
            return 0.5f + (0.5f * Mathf.Sin((time * speed) + phase));
        }

        private static float Average(float a, float b, float c)
        {
            return Mathf.Clamp01((a + b + c) / 3f);
        }

        private static float GetCircadianSupport(CircadianAlignmentMode mode)
        {
            return mode switch
            {
                CircadianAlignmentMode.Disrupted => 0.18f,
                CircadianAlignmentMode.Recovering => 0.46f,
                CircadianAlignmentMode.Aligned => 0.74f,
                CircadianAlignmentMode.Coherent => 0.92f,
                _ => 0.5f,
            };
        }

        private static float GetCircadianDisruption(CircadianAlignmentMode mode)
        {
            return mode switch
            {
                CircadianAlignmentMode.Disrupted => 0.86f,
                CircadianAlignmentMode.Recovering => 0.42f,
                CircadianAlignmentMode.Aligned => 0.18f,
                CircadianAlignmentMode.Coherent => 0.08f,
                _ => 0.3f,
            };
        }

        private static float GetSocialSupport(SocialFieldMode mode)
        {
            return mode switch
            {
                SocialFieldMode.Protected => 0.34f,
                SocialFieldMode.Supportive => 0.86f,
                SocialFieldMode.Charged => 0.54f,
                SocialFieldMode.Overwhelming => 0.18f,
                _ => 0.5f,
            };
        }

        private static float GetSocialOverload(SocialFieldMode mode)
        {
            return mode switch
            {
                SocialFieldMode.Protected => 0.12f,
                SocialFieldMode.Supportive => 0.08f,
                SocialFieldMode.Charged => 0.46f,
                SocialFieldMode.Overwhelming => 0.88f,
                _ => 0.3f,
            };
        }

        private static float GetInformationClarity(InformationClimateMode mode)
        {
            return mode switch
            {
                InformationClimateMode.Quiet => 0.76f,
                InformationClimateMode.Focused => 0.9f,
                InformationClimateMode.Saturated => 0.42f,
                InformationClimateMode.Noisy => 0.16f,
                _ => 0.5f,
            };
        }

        private static float GetInformationNoise(InformationClimateMode mode)
        {
            return mode switch
            {
                InformationClimateMode.Quiet => 0.08f,
                InformationClimateMode.Focused => 0.12f,
                InformationClimateMode.Saturated => 0.54f,
                InformationClimateMode.Noisy => 0.88f,
                _ => 0.3f,
            };
        }

        private static float GetGroundingBias(BiofieldPracticeMode mode)
        {
            return mode switch
            {
                BiofieldPracticeMode.Grounding => 0.94f,
                BiofieldPracticeMode.EmotionalRelease => 0.36f,
                BiofieldPracticeMode.HeartCoherence => 0.42f,
                BiofieldPracticeMode.Meditation => 0.48f,
                BiofieldPracticeMode.Service => 0.34f,
                _ => 0.4f,
            };
        }

        private static float GetReleaseBias(BiofieldPracticeMode mode)
        {
            return mode switch
            {
                BiofieldPracticeMode.Grounding => 0.36f,
                BiofieldPracticeMode.EmotionalRelease => 0.94f,
                BiofieldPracticeMode.HeartCoherence => 0.62f,
                BiofieldPracticeMode.Meditation => 0.48f,
                BiofieldPracticeMode.Service => 0.44f,
                _ => 0.4f,
            };
        }

        private static float GetHeartBias(BiofieldPracticeMode mode)
        {
            return mode switch
            {
                BiofieldPracticeMode.Grounding => 0.32f,
                BiofieldPracticeMode.EmotionalRelease => 0.62f,
                BiofieldPracticeMode.HeartCoherence => 0.96f,
                BiofieldPracticeMode.Meditation => 0.66f,
                BiofieldPracticeMode.Service => 0.72f,
                _ => 0.5f,
            };
        }

        private static float GetMeditationBias(BiofieldPracticeMode mode)
        {
            return mode switch
            {
                BiofieldPracticeMode.Grounding => 0.34f,
                BiofieldPracticeMode.EmotionalRelease => 0.4f,
                BiofieldPracticeMode.HeartCoherence => 0.58f,
                BiofieldPracticeMode.Meditation => 0.98f,
                BiofieldPracticeMode.Service => 0.56f,
                _ => 0.4f,
            };
        }

        private static float GetServiceBias(BiofieldPracticeMode mode)
        {
            return mode switch
            {
                BiofieldPracticeMode.Grounding => 0.3f,
                BiofieldPracticeMode.EmotionalRelease => 0.4f,
                BiofieldPracticeMode.HeartCoherence => 0.62f,
                BiofieldPracticeMode.Meditation => 0.56f,
                BiofieldPracticeMode.Service => 0.98f,
                _ => 0.4f,
            };
        }
    }
}
