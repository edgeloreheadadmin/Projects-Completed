// Standalone (non-MonoBehaviour) simulation of the Biofield Aura system.
// Callable directly from Python via Python.NET — no Unity lifecycle required.

using System;
using System.Collections.Generic;
using Auras.Core;
using Auras.BiofieldAuraMatrix;
using UnityEngine;

namespace Auras
{
    public class AuraLayerSnapshot
    {
        public int    LayerId;
        public string LayerName;
        public double Intensity;
        public double Integrity;
        public double Energy;
        public float  ColorR;
        public float  ColorG;
        public float  ColorB;
        public float  Scale;
        public float  PulsePhase;
    }

    public class AuraSimulator
    {
        // ── State ──────────────────────────────────────────────────────────────
        private LayerContextCollection _contexts;
        private readonly BiofieldLayerDefinition[] _definitions;
        private float _simulatedTime = 0f;

        // ── Controller settings ────────────────────────────────────────────────
        private float _blendSpeed     = 6f;
        private float _pulseAmplitude = 0.08f;

        // ── Driver environment modes ───────────────────────────────────────────
        private CircadianAlignmentMode _circadian   = CircadianAlignmentMode.Aligned;
        private SocialFieldMode        _social      = SocialFieldMode.Supportive;
        private InformationClimateMode _information = InformationClimateMode.Focused;
        private BiofieldPracticeMode   _practice    = BiofieldPracticeMode.HeartCoherence;

        // ── Driver support inputs ──────────────────────────────────────────────
        private float _geomagneticPressure = 0.3f;
        private float _natureExposure      = 0.7f;
        private float _energeticBoundaries = 0.72f;
        private float _heartCoherence      = 0.76f;
        private float _contemplativeDepth  = 0.72f;

        // ── Per-layer direct inputs (indexed by BiofieldLayerType) ─────────────
        private float _physicalEmbodiment  = 0.74f;
        private float _emotionalFluidity   = 0.72f;
        private float _cognitiveOrder      = 0.74f;
        private float _relationalOpenness  = 0.76f;
        private float _expressiveAlignment = 0.70f;
        private float _intuitiveSensitivity= 0.78f;
        private float _soulAlignment       = 0.80f;

        // ── Constructor ────────────────────────────────────────────────────────

        public AuraSimulator()
        {
            int count = Enum.GetValues(typeof(BiofieldLayerType)).Length;
            _definitions = new BiofieldLayerDefinition[count];
            foreach (BiofieldLayerType layer in Enum.GetValues(typeof(BiofieldLayerType)))
                _definitions[(int)layer] = BiofieldAuraProfile.GetDefaultLayerDefinition(layer);

            Initialize();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        public void Initialize()
        {
            int count = Enum.GetValues(typeof(BiofieldLayerType)).Length;
            var nameMap = new Dictionary<string, int>();
            foreach (BiofieldLayerType l in Enum.GetValues(typeof(BiofieldLayerType)))
                nameMap[l.ToString()] = (int)l;

            _contexts = new LayerContextCollection(count, nameMap);
            _simulatedTime = 0f;

            _contexts.ForEach(ctx =>
            {
                ctx.TargetIntensity  = 0.62;
                ctx.TargetIntegrity  = 0.74;
                ctx.CurrentIntensity = 0.62;
                ctx.CurrentIntegrity = 0.74;
                ctx.InvalidateEnergy();
            });
        }

        public void Update(double deltaTime)
        {
            _simulatedTime += (float)deltaTime;
            Time.time      = _simulatedTime;
            Time.deltaTime = (float)deltaTime;

            ApplyDriverInteractions();
            BlendContexts((float)deltaTime);
        }

        public void SetLayerInput(int layerIndex, float value)
        {
            value = Mathf.Clamp01(value);
            switch ((BiofieldLayerType)layerIndex)
            {
                case BiofieldLayerType.Etheric:         _physicalEmbodiment   = value; break;
                case BiofieldLayerType.Emotional:       _emotionalFluidity    = value; break;
                case BiofieldLayerType.Mental:          _cognitiveOrder       = value; break;
                case BiofieldLayerType.Astral:          _relationalOpenness   = value; break;
                case BiofieldLayerType.EthericTemplate: _expressiveAlignment  = value; break;
                case BiofieldLayerType.Celestial:       _intuitiveSensitivity = value; break;
                case BiofieldLayerType.Causal:          _soulAlignment        = value; break;
            }
        }

        public float GetLayerInput(int layerIndex)
        {
            return (BiofieldLayerType)layerIndex switch
            {
                BiofieldLayerType.Etheric         => _physicalEmbodiment,
                BiofieldLayerType.Emotional       => _emotionalFluidity,
                BiofieldLayerType.Mental          => _cognitiveOrder,
                BiofieldLayerType.Astral          => _relationalOpenness,
                BiofieldLayerType.EthericTemplate => _expressiveAlignment,
                BiofieldLayerType.Celestial       => _intuitiveSensitivity,
                BiofieldLayerType.Causal          => _soulAlignment,
                _ => 0f,
            };
        }

        public void SetEnvironmentModes(int circadian, int social, int information, int practice)
        {
            _circadian   = (CircadianAlignmentMode)circadian;
            _social      = (SocialFieldMode)social;
            _information = (InformationClimateMode)information;
            _practice    = (BiofieldPracticeMode)practice;
        }

        public void SetSupportInputs(float geomagnetic, float nature, float boundaries,
                                     float heart, float contemplative)
        {
            _geomagneticPressure = Mathf.Clamp01(geomagnetic);
            _natureExposure      = Mathf.Clamp01(nature);
            _energeticBoundaries = Mathf.Clamp01(boundaries);
            _heartCoherence      = Mathf.Clamp01(heart);
            _contemplativeDepth  = Mathf.Clamp01(contemplative);
        }

        public void SetBlendSpeed(float speed)
        {
            _blendSpeed = Math.Max(0.01f, speed);
        }

        public void SetPulseAmplitude(float amplitude)
        {
            _pulseAmplitude = Mathf.Clamp01(amplitude);
        }

        public AuraLayerSnapshot GetLayerSnapshot(int layerIndex)
        {
            var ctx = _contexts.Get(layerIndex);
            if (ctx == null) return null;
            return BuildSnapshot(ctx, layerIndex);
        }

        public AuraLayerSnapshot[] GetAllSnapshots()
        {
            int count = Enum.GetValues(typeof(BiofieldLayerType)).Length;
            var result = new AuraLayerSnapshot[count];
            for (int i = 0; i < count; i++)
                result[i] = BuildSnapshot(_contexts.Get(i), i);
            return result;
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private AuraLayerSnapshot BuildSnapshot(AuraLayerContext ctx, int layerIndex)
        {
            var def = _definitions[layerIndex];
            double energy = ctx.GetEnergy();

            float integrityTint  = Mathf.Clamp01(0.24f + (float)ctx.CurrentIntegrity * 0.76f);
            Color lowEnergyColor = Color.Lerp(Color.black, def.balancedColor, 0.24f);
            Color drivenColor    = Color.Lerp(lowEnergyColor, def.balancedColor, integrityTint);

            float pulse = Mathf.Sin(_simulatedTime * def.pulseSpeed + layerIndex * 0.85f);
            float scale = Mathf.Lerp(def.baseScale, def.peakScale, (float)energy)
                        * (1f + _pulseAmplitude * (float)ctx.CurrentIntensity * pulse);

            return new AuraLayerSnapshot
            {
                LayerId    = layerIndex,
                LayerName  = def.displayName,
                Intensity  = ctx.CurrentIntensity,
                Integrity  = ctx.CurrentIntegrity,
                Energy     = energy,
                ColorR     = drivenColor.r,
                ColorG     = drivenColor.g,
                ColorB     = drivenColor.b,
                Scale      = scale,
                PulsePhase = pulse,
            };
        }

        private void BlendContexts(float deltaTime)
        {
            float step = _blendSpeed * deltaTime;
            _contexts.ForEachOrdered(ctx =>
            {
                ctx.CurrentIntensity = ctx.intensityEasing.InterpolateHighPrecision(
                    ctx.CurrentIntensity, ctx.TargetIntensity, step);
                ctx.CurrentIntegrity = ctx.integrityEasing.InterpolateHighPrecision(
                    ctx.CurrentIntegrity, ctx.TargetIntegrity, step);
                ctx.InvalidateEnergy();
            });
        }

        // Verbatim copy of BiofieldAuraDriver.ApplyInteractions() adapted to standalone context.
        // No Wave() oscillation — earthPulse/socialPulse/insightPulse held at 0.5 for stable defaults.
        private void ApplyDriverInteractions()
        {
            const float earthPulse   = 0.5f;
            const float socialPulse  = 0.5f;
            const float insightPulse = 0.5f;

            float circadianSupport    = GetCircadianSupport(_circadian);
            float circadianDisruption = GetCircadianDisruption(_circadian);
            float socialSupport       = GetSocialSupport(_social);
            float socialOverload      = GetSocialOverload(_social);
            float informationClarity  = GetInformationClarity(_information);
            float informationNoise    = GetInformationNoise(_information);
            float groundingBias       = GetGroundingBias(_practice);
            float releaseBias         = GetReleaseBias(_practice);
            float heartBias           = GetHeartBias(_practice);
            float meditationBias      = GetMeditationBias(_practice);
            float serviceBias         = GetServiceBias(_practice);

            float stabilityField  = Average(_physicalEmbodiment, _natureExposure, circadianSupport);
            float regulationField = Average(_energeticBoundaries, _heartCoherence, _contemplativeDepth);
            float clarityField    = Average(_cognitiveOrder, informationClarity, _contemplativeDepth);
            float devotionField   = Average(_intuitiveSensitivity, _soulAlignment, _contemplativeDepth);

            var etheric = new BiofieldLayerState(
                _physicalEmbodiment * 0.48f + _natureExposure * 0.18f + groundingBias * 0.14f + circadianSupport * 0.12f + stabilityField * 0.08f + earthPulse * 0.05f - _geomagneticPressure * 0.14f - circadianDisruption * 0.08f,
                _physicalEmbodiment * 0.28f + _energeticBoundaries * 0.22f + _natureExposure * 0.18f + groundingBias * 0.14f + circadianSupport * 0.12f + regulationField * 0.06f - _geomagneticPressure * 0.16f - circadianDisruption * 0.06f);

            var emotional = new BiofieldLayerState(
                _emotionalFluidity * 0.42f + socialSupport * 0.18f + releaseBias * 0.14f + _heartCoherence * 0.12f + socialPulse * 0.08f + _relationalOpenness * 0.08f - socialOverload * 0.1f,
                _emotionalFluidity * 0.26f + _heartCoherence * 0.24f + _energeticBoundaries * 0.18f + socialSupport * 0.16f + releaseBias * 0.1f + regulationField * 0.06f - socialOverload * 0.16f);

            var mental = new BiofieldLayerState(
                _cognitiveOrder * 0.42f + informationClarity * 0.2f + circadianSupport * 0.12f + _contemplativeDepth * 0.1f + clarityField * 0.08f + earthPulse * 0.04f - informationNoise * 0.18f - circadianDisruption * 0.08f,
                _cognitiveOrder * 0.28f + _contemplativeDepth * 0.22f + informationClarity * 0.2f + _energeticBoundaries * 0.1f + meditationBias * 0.1f + clarityField * 0.06f - informationNoise * 0.18f);

            var astral = new BiofieldLayerState(
                _relationalOpenness * 0.38f + _heartCoherence * 0.18f + socialSupport * 0.16f + heartBias * 0.14f + _intuitiveSensitivity * 0.08f + socialPulse * 0.06f - socialOverload * 0.08f,
                _relationalOpenness * 0.26f + _heartCoherence * 0.24f + _energeticBoundaries * 0.16f + serviceBias * 0.12f + socialSupport * 0.12f + regulationField * 0.06f - socialOverload * 0.1f);

            var ethericTemplate = new BiofieldLayerState(
                _expressiveAlignment * 0.38f + _cognitiveOrder * 0.16f + informationClarity * 0.12f + meditationBias * 0.08f + stabilityField * 0.1f + earthPulse * 0.06f + groundingBias * 0.1f - informationNoise * 0.06f,
                _expressiveAlignment * 0.28f + _energeticBoundaries * 0.2f + circadianSupport * 0.12f + _contemplativeDepth * 0.14f + informationClarity * 0.12f + clarityField * 0.06f - informationNoise * 0.1f);

            var celestial = new BiofieldLayerState(
                _intuitiveSensitivity * 0.38f + _contemplativeDepth * 0.18f + _heartCoherence * 0.14f + meditationBias * 0.14f + serviceBias * 0.08f + devotionField * 0.08f + insightPulse * 0.06f - informationNoise * 0.06f,
                _intuitiveSensitivity * 0.26f + _contemplativeDepth * 0.22f + _heartCoherence * 0.18f + _energeticBoundaries * 0.1f + meditationBias * 0.12f + serviceBias * 0.08f + devotionField * 0.04f - socialOverload * 0.06f);

            var causal = new BiofieldLayerState(
                _soulAlignment * 0.4f + _contemplativeDepth * 0.14f + meditationBias * 0.12f + serviceBias * 0.12f + devotionField * 0.12f + insightPulse * 0.1f - circadianDisruption * 0.04f,
                _soulAlignment * 0.32f + _contemplativeDepth * 0.18f + _energeticBoundaries * 0.12f + meditationBias * 0.12f + serviceBias * 0.1f + _natureExposure * 0.08f + _heartCoherence * 0.08f - _geomagneticPressure * 0.08f);

            // Cross-layer integrity cascade (verbatim from BiofieldAuraDriver)
            etheric         = new BiofieldLayerState(etheric.intensity, etheric.integrity + ethericTemplate.integrity * 0.08f);
            emotional       = new BiofieldLayerState(emotional.intensity, emotional.integrity + astral.integrity * 0.08f);
            mental          = new BiofieldLayerState(mental.intensity, mental.integrity + ethericTemplate.integrity * 0.06f + celestial.integrity * 0.04f);
            astral          = new BiofieldLayerState(astral.intensity, astral.integrity + emotional.integrity * 0.06f + celestial.integrity * 0.04f);
            ethericTemplate = new BiofieldLayerState(ethericTemplate.intensity, ethericTemplate.integrity + mental.integrity * 0.06f + causal.integrity * 0.04f);
            celestial       = new BiofieldLayerState(celestial.intensity, celestial.integrity + astral.integrity * 0.04f + causal.integrity * 0.08f);
            causal          = new BiofieldLayerState(causal.intensity, causal.integrity + celestial.integrity * 0.08f + regulationField * 0.04f);

            SetTarget(BiofieldLayerType.Etheric,         etheric);
            SetTarget(BiofieldLayerType.Emotional,       emotional);
            SetTarget(BiofieldLayerType.Mental,          mental);
            SetTarget(BiofieldLayerType.Astral,          astral);
            SetTarget(BiofieldLayerType.EthericTemplate, ethericTemplate);
            SetTarget(BiofieldLayerType.Celestial,       celestial);
            SetTarget(BiofieldLayerType.Causal,          causal);
        }

        private void SetTarget(BiofieldLayerType layer, BiofieldLayerState state)
        {
            var ctx = _contexts.Get((int)layer);
            if (ctx != null)
            {
                ctx.TargetIntensity = state.intensity;
                ctx.TargetIntegrity = state.integrity;
            }
        }

        private static float Average(float a, float b, float c)
            => Mathf.Clamp01((a + b + c) / 3f);

        // ── Mode lookup tables (verbatim from BiofieldAuraDriver) ──────────────

        private static float GetCircadianSupport(CircadianAlignmentMode m) => m switch
        {
            CircadianAlignmentMode.Disrupted  => 0.18f,
            CircadianAlignmentMode.Recovering => 0.46f,
            CircadianAlignmentMode.Aligned    => 0.74f,
            CircadianAlignmentMode.Coherent   => 0.92f,
            _ => 0.5f,
        };

        private static float GetCircadianDisruption(CircadianAlignmentMode m) => m switch
        {
            CircadianAlignmentMode.Disrupted  => 0.86f,
            CircadianAlignmentMode.Recovering => 0.42f,
            CircadianAlignmentMode.Aligned    => 0.18f,
            CircadianAlignmentMode.Coherent   => 0.08f,
            _ => 0.3f,
        };

        private static float GetSocialSupport(SocialFieldMode m) => m switch
        {
            SocialFieldMode.Protected    => 0.34f,
            SocialFieldMode.Supportive   => 0.86f,
            SocialFieldMode.Charged      => 0.54f,
            SocialFieldMode.Overwhelming => 0.18f,
            _ => 0.5f,
        };

        private static float GetSocialOverload(SocialFieldMode m) => m switch
        {
            SocialFieldMode.Protected    => 0.12f,
            SocialFieldMode.Supportive   => 0.08f,
            SocialFieldMode.Charged      => 0.46f,
            SocialFieldMode.Overwhelming => 0.88f,
            _ => 0.3f,
        };

        private static float GetInformationClarity(InformationClimateMode m) => m switch
        {
            InformationClimateMode.Quiet     => 0.76f,
            InformationClimateMode.Focused   => 0.9f,
            InformationClimateMode.Saturated => 0.42f,
            InformationClimateMode.Noisy     => 0.16f,
            _ => 0.5f,
        };

        private static float GetInformationNoise(InformationClimateMode m) => m switch
        {
            InformationClimateMode.Quiet     => 0.08f,
            InformationClimateMode.Focused   => 0.12f,
            InformationClimateMode.Saturated => 0.54f,
            InformationClimateMode.Noisy     => 0.88f,
            _ => 0.3f,
        };

        private static float GetGroundingBias(BiofieldPracticeMode m) => m switch
        {
            BiofieldPracticeMode.Grounding        => 0.94f,
            BiofieldPracticeMode.EmotionalRelease => 0.36f,
            BiofieldPracticeMode.HeartCoherence   => 0.42f,
            BiofieldPracticeMode.Meditation       => 0.48f,
            BiofieldPracticeMode.Service          => 0.34f,
            _ => 0.4f,
        };

        private static float GetReleaseBias(BiofieldPracticeMode m) => m switch
        {
            BiofieldPracticeMode.Grounding        => 0.36f,
            BiofieldPracticeMode.EmotionalRelease => 0.94f,
            BiofieldPracticeMode.HeartCoherence   => 0.62f,
            BiofieldPracticeMode.Meditation       => 0.48f,
            BiofieldPracticeMode.Service          => 0.44f,
            _ => 0.4f,
        };

        private static float GetHeartBias(BiofieldPracticeMode m) => m switch
        {
            BiofieldPracticeMode.Grounding        => 0.32f,
            BiofieldPracticeMode.EmotionalRelease => 0.62f,
            BiofieldPracticeMode.HeartCoherence   => 0.96f,
            BiofieldPracticeMode.Meditation       => 0.66f,
            BiofieldPracticeMode.Service          => 0.72f,
            _ => 0.5f,
        };

        private static float GetMeditationBias(BiofieldPracticeMode m) => m switch
        {
            BiofieldPracticeMode.Grounding        => 0.34f,
            BiofieldPracticeMode.EmotionalRelease => 0.4f,
            BiofieldPracticeMode.HeartCoherence   => 0.58f,
            BiofieldPracticeMode.Meditation       => 0.98f,
            BiofieldPracticeMode.Service          => 0.56f,
            _ => 0.4f,
        };

        private static float GetServiceBias(BiofieldPracticeMode m) => m switch
        {
            BiofieldPracticeMode.Grounding        => 0.3f,
            BiofieldPracticeMode.EmotionalRelease => 0.4f,
            BiofieldPracticeMode.HeartCoherence   => 0.62f,
            BiofieldPracticeMode.Meditation       => 0.56f,
            BiofieldPracticeMode.Service          => 0.98f,
            _ => 0.4f,
        };
    }
}
