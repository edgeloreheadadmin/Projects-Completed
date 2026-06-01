using System;
using System.Collections.Generic;
using Auras.Core;
using UnityEngine;

namespace Auras.Core
{
    /// <summary>
    /// Generic base class for aura framework controllers.
    /// Provides all binding, state management, and rendering logic.
    /// Frameworks (Biofield, Integrative) inherit to customize layer types and definitions.
    /// Eliminates ~90% code duplication between frameworks.
    /// </summary>
    [ExecuteAlways]
    public abstract class AuraFrameworkBase<TLayerEnum, TLayerState, TLayerDefinition> : MonoBehaviour
        where TLayerEnum : struct, IConvertible
        where TLayerState : struct
        where TLayerDefinition : class
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Header("Profile")]
        [SerializeField]
        protected ScriptableObject profile;

        [SerializeField]
        protected bool animateInEditMode = true;

        [SerializeField]
        protected bool autoPlayBoundParticles = true;

        [Header("Precision & Easing")]
        [SerializeField]
        [Min(0.01f)]
        protected float transitionDuration = 0.5f;

        [SerializeField]
        protected EasingType defaultEasingType = EasingType.EaseInOutCubic;

        [SerializeField]
        [Range(0.1f, 10f)]
        protected float springStiffness = 3f;

        [SerializeField]
        [Range(0f, 1f)]
        protected float springDamping = 0.6f;

        [Header("Bindings")]
        [SerializeField]
        protected List<AuraLayerBinding> bindings = new List<AuraLayerBinding>();

        protected LayerContextCollection layerContexts;
        protected MaterialPropertyBlock propertyBlock;
        protected double lastUpdateTime = 0.0;

        /// <summary>
        /// Get the current profile (framework-specific type).
        /// </summary>
        public abstract ScriptableObject GetProfile();

        /// <summary>
        /// Set the current profile.
        /// </summary>
        public abstract void SetProfile(ScriptableObject value);

        /// <summary>
        /// Get layer definition for a specific layer enum value.
        /// </summary>
        protected abstract TLayerDefinition GetLayerDefinition(int layerId);

        /// <summary>
        /// Convert TLayerEnum to int for indexing.
        /// </summary>
        protected abstract int LayerEnumToInt(TLayerEnum layer);

        /// <summary>
        /// Get the total number of layers for this framework.
        /// </summary>
        protected abstract int GetLayerCount();

        /// <summary>
        /// Get aliases for binding discovery.
        /// </summary>
        protected abstract string[] GetLayerAliases(int layerId);

        public void SetState(TLayerEnum layer, double intensity, double integrity)
        {
            var context = layerContexts?.Get(LayerEnumToInt(layer));
            if (context != null)
            {
                context.TargetIntensity = intensity;
                context.TargetIntegrity = integrity;
            }
        }

        public AuraLayerContext GetContext(TLayerEnum layer)
        {
            return layerContexts?.Get(LayerEnumToInt(layer));
        }

        public void SetAllStates(double intensity, double integrity)
        {
            if (layerContexts != null)
            {
                layerContexts.ForEach(ctx =>
                {
                    ctx.TargetIntensity = intensity;
                    ctx.TargetIntegrity = integrity;
                });
            }
        }

        public void NudgeLayer(TLayerEnum layer, double intensityDelta, double integrityDelta)
        {
            var context = layerContexts?.Get(LayerEnumToInt(layer));
            if (context != null)
            {
                context.TargetIntensity = context.TargetIntensity + intensityDelta;
                context.TargetIntegrity = context.TargetIntegrity + integrityDelta;
            }
        }

        public void ApplyNow()
        {
            if (layerContexts != null)
            {
                layerContexts.ForEach(ctx =>
                {
                    ctx.CurrentIntensity = ctx.TargetIntensity;
                    ctx.CurrentIntegrity = ctx.TargetIntegrity;
                    ctx.InvalidateEnergy();
                });
            }

            ApplyBindings();
        }

        [ContextMenu("Reset To Neutral")]
        public void ResetToNeutral()
        {
            SeedNeutralTargets();
            ApplyNow();
        }

        [ContextMenu("Auto Bind By Name")]
        public void AutoBindByName()
        {
            EnsureBindingSlots();

            Transform[] transforms = GetComponentsInChildren<Transform>(true);
            ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            Light[] lights = GetComponentsInChildren<Light>(true);
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < bindings.Count; i++)
            {
                AuraLayerBinding binding = bindings[i];
                if (binding == null)
                {
                    continue;
                }

                string[] aliases = GetLayerAliases(binding.LayerId);

                if (binding.anchor == null)
                {
                    binding.anchor = FindTransform(transforms, aliases);
                }

                if (binding.particleSystem == null)
                {
                    if (binding.anchor != null)
                    {
                        binding.particleSystem = binding.anchor.GetComponent<ParticleSystem>();
                    }

                    if (binding.particleSystem == null)
                    {
                        binding.particleSystem = FindComponent(particleSystems, aliases, particle => particle.name);
                    }
                }

                if (binding.light == null)
                {
                    if (binding.anchor != null)
                    {
                        binding.light = binding.anchor.GetComponent<Light>();
                    }

                    if (binding.light == null)
                    {
                        binding.light = FindComponent(lights, aliases, light => light.name);
                    }
                }

                if (binding.renderer == null)
                {
                    if (binding.anchor != null)
                    {
                        binding.renderer = binding.anchor.GetComponent<Renderer>();
                    }

                    if (binding.renderer == null)
                    {
                        binding.renderer = FindComponent(renderers, aliases, renderer => renderer.name);
                    }
                }
            }

            ApplyBindings();
        }

        [ContextMenu("Snap Anchors To Profile Scales")]
        public void SnapAnchorsToProfileScales()
        {
            EnsureBindingSlots();

            for (int i = 0; i < bindings.Count; i++)
            {
                AuraLayerBinding binding = bindings[i];
                if (binding?.anchor == null)
                {
                    continue;
                }

                TLayerDefinition definition = GetLayerDefinition(binding.LayerId);
                if (definition == null)
                {
                    continue;
                }

                binding.anchor.localPosition = Vector3.zero;
                binding.anchor.localRotation = Quaternion.identity;
                binding.anchor.localScale = Vector3.one * GetDefinitionBaseScale(definition);
            }
        }

        /// <summary>
        /// Get the base scale from a layer definition.
        /// Implemented by derived classes (Biofield, Integrative).
        /// </summary>
        protected abstract float GetDefinitionBaseScale(TLayerDefinition definition);

        /// <summary>
        /// Get the peak scale from a layer definition.
        /// </summary>
        protected abstract float GetDefinitionPeakScale(TLayerDefinition definition);

        /// <summary>
        /// Get shell thickness from a layer definition.
        /// </summary>
        protected abstract float GetDefinitionShellThickness(TLayerDefinition definition);

        /// <summary>
        /// Get base/peak emission from a layer definition.
        /// </summary>
        protected abstract float GetDefinitionBaseEmission(TLayerDefinition definition);
        protected abstract float GetDefinitionPeakEmission(TLayerDefinition definition);

        /// <summary>
        /// Get light intensity from a layer definition.
        /// </summary>
        protected abstract float GetDefinitionBaseLightIntensity(TLayerDefinition definition);
        protected abstract float GetDefinitionPeakLightIntensity(TLayerDefinition definition);

        /// <summary>
        /// Get pulse speed from a layer definition.
        /// </summary>
        protected abstract float GetDefinitionPulseSpeed(TLayerDefinition definition);

        /// <summary>
        /// Get balanced color from a layer definition.
        /// </summary>
        protected abstract Color GetDefinitionBalancedColor(TLayerDefinition definition);

        protected virtual void Reset()
        {
            EnsureBindingSlots();
            InitializeContexts();
            SeedNeutralTargets();
            ApplyNow();
        }

        protected virtual void OnEnable()
        {
            EnsureBindingSlots();
            InitializeContexts();

            if (TargetsAreEmpty())
            {
                SeedNeutralTargets();
            }

            if (autoPlayBoundParticles)
            {
                PlayBoundParticles();
            }

            ApplyBindings();
        }

        protected virtual void OnValidate()
        {
            EnsureBindingSlots();

            if (!Application.isPlaying && animateInEditMode)
            {
                ApplyBindings();
            }
        }

        protected virtual void LateUpdate()
        {
            if (!Application.isPlaying && !animateInEditMode)
            {
                return;
            }

            if (layerContexts == null)
            {
                InitializeContexts();
            }

            double currentTime = Time.realtimeSinceStartup;
            double deltaTime = currentTime - lastUpdateTime;
            lastUpdateTime = currentTime;

            if (deltaTime < 0.0)
            {
                deltaTime = GetSimulationDelta();
            }

            double blendFactor = Mathf.Clamp01((float)(deltaTime / transitionDuration));

            layerContexts.ForEachOrdered(ctx =>
            {
                if (ctx.CurrentIntensity != ctx.TargetIntensity)
                {
                    double newIntensity = ctx.intensityEasing.InterpolateHighPrecision(
                        ctx.CurrentIntensity, ctx.TargetIntensity, blendFactor);
                    ctx.CurrentIntensity = PrecisionCalculator.Clamp01(newIntensity);
                }

                if (ctx.CurrentIntegrity != ctx.TargetIntegrity)
                {
                    double newIntegrity = ctx.integrityEasing.InterpolateHighPrecision(
                        ctx.CurrentIntegrity, ctx.TargetIntegrity, blendFactor);
                    ctx.CurrentIntegrity = PrecisionCalculator.Clamp01(newIntegrity);
                }

                ctx.InvalidateEnergy();
            });

            ApplyBindings();
        }

        protected void InitializeContexts()
        {
            if (layerContexts == null)
            {
                layerContexts = new LayerContextCollection(GetLayerCount());
                ConfigureEasingCurves();
                lastUpdateTime = Time.realtimeSinceStartup;
            }
        }

        protected void ConfigureEasingCurves()
        {
            EasingCurve intensityEasing = CreateEasingCurve();
            EasingCurve integrityEasing = CreateEasingCurve();

            layerContexts.ForEach(ctx =>
            {
                ctx.intensityEasing = intensityEasing;
                ctx.integrityEasing = integrityEasing;
            });
        }

        protected EasingCurve CreateEasingCurve()
        {
            var curve = new EasingCurve
            {
                type = defaultEasingType,
                springStiffness = springStiffness,
                springDamping = springDamping,
            };
            return curve;
        }

        protected void SeedNeutralTargets()
        {
            if (layerContexts != null)
            {
                layerContexts.ForEach(ctx =>
                {
                    ctx.TargetIntensity = 0.62;
                    ctx.TargetIntegrity = 0.74;
                    ctx.CurrentIntensity = 0.62;
                    ctx.CurrentIntegrity = 0.74;
                    ctx.InvalidateEnergy();
                });
            }
        }

        protected bool TargetsAreEmpty()
        {
            if (layerContexts == null)
            {
                return true;
            }

            foreach (var ctx in layerContexts.GetAll())
            {
                if (ctx.TargetIntensity > 0 || ctx.TargetIntegrity > 0)
                {
                    return false;
                }
            }

            return true;
        }

        protected void EnsureBindingSlots()
        {
            if (bindings == null)
            {
                bindings = new List<AuraLayerBinding>();
            }

            Dictionary<int, AuraLayerBinding> lookup = new Dictionary<int, AuraLayerBinding>();
            for (int i = 0; i < bindings.Count; i++)
            {
                AuraLayerBinding binding = bindings[i];
                if (binding == null || lookup.ContainsKey(binding.LayerId))
                {
                    continue;
                }

                lookup.Add(binding.LayerId, binding);
            }

            List<AuraLayerBinding> ordered = new List<AuraLayerBinding>(GetLayerCount());
            for (int i = 0; i < GetLayerCount(); i++)
            {
                if (!lookup.TryGetValue(i, out AuraLayerBinding binding))
                {
                    binding = new AuraLayerBinding { LayerId = i };
                }
                else
                {
                    binding.LayerId = i;
                }

                ordered.Add(binding);
            }

            bindings = ordered;
        }

        protected void ApplyBindings()
        {
            EnsureBindingSlots();

            for (int i = 0; i < bindings.Count; i++)
            {
                AuraLayerBinding binding = bindings[i];
                if (binding == null)
                {
                    continue;
                }

                var context = layerContexts?.Get(binding.LayerId);
                if (context == null)
                {
                    continue;
                }

                TLayerDefinition definition = GetLayerDefinition(binding.LayerId);
                if (definition == null)
                {
                    continue;
                }

                ApplyBinding(binding, definition, context);
            }
        }

        protected virtual void ApplyBinding(AuraLayerBinding binding, TLayerDefinition definition, AuraLayerContext context)
        {
            double energyHiPrecision = context.GetEnergy();
            double integrityTintHiPrecision = PrecisionCalculator.ComputeIntegrityTint(context.CurrentIntegrity);

            float energy = (float)energyHiPrecision;
            float integrityTint = (float)integrityTintHiPrecision;

            Color balancedColor = GetDefinitionBalancedColor(definition);
            Color lowEnergyColor = Color.Lerp(Color.black, balancedColor, 0.24f);
            Color drivenColor = Color.Lerp(lowEnergyColor, balancedColor, integrityTint);

            float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
            float pulseSpeed = GetDefinitionPulseSpeed(definition);
            float pulse = 1f + (0.08f * (float)context.CurrentIntensity * Mathf.Sin((time * pulseSpeed) + (binding.LayerId * 0.85f)));

            float baseScale = GetDefinitionBaseScale(definition);
            float peakScale = GetDefinitionPeakScale(definition);
            float scale = Mathf.Lerp(baseScale, peakScale, energy) * binding.scaleMultiplier * pulse;

            if (binding.anchor != null && binding.driveAnchorScale)
            {
                binding.anchor.localPosition = Vector3.zero;
                binding.anchor.localScale = Vector3.one * scale;
            }

            if (binding.particleSystem != null)
            {
                ParticleSystem.MainModule main = binding.particleSystem.main;
                if (binding.driveParticleColor)
                {
                    main.startColor = drivenColor;
                }

                if (binding.driveParticleSize)
                {
                    float thickness = GetDefinitionShellThickness(definition);
                    main.startSize = Mathf.Max(0.01f, scale * thickness);
                }

                main.simulationSpeed = Mathf.Lerp(0.82f, 1.6f, energy);

                if (binding.driveParticleEmission)
                {
                    float baseEmission = GetDefinitionBaseEmission(definition);
                    float peakEmission = GetDefinitionPeakEmission(definition);
                    ParticleSystem.EmissionModule emission = binding.particleSystem.emission;
                    emission.rateOverTime = Mathf.Lerp(baseEmission, peakEmission, energy) * binding.emissionMultiplier;
                }
            }

            if (binding.light != null && binding.driveLight)
            {
                float baseLightIntensity = GetDefinitionBaseLightIntensity(definition);
                float peakLightIntensity = GetDefinitionPeakLightIntensity(definition);
                binding.light.color = drivenColor;
                binding.light.intensity = Mathf.Lerp(baseLightIntensity, peakLightIntensity, energy) * binding.lightMultiplier;
                binding.light.range = Mathf.Lerp(baseScale * 1.35f, peakScale * 2.1f, energy);
            }

            if (binding.renderer != null && binding.driveRendererColor)
            {
                ApplyRendererColor(binding.renderer, drivenColor, energy);
            }
        }

        protected void EnsurePropertyBlock()
        {
            propertyBlock ??= new MaterialPropertyBlock();
        }

        protected void ApplyRendererColor(Renderer renderer, Color color, float energy)
        {
            Material sharedMaterial = renderer.sharedMaterial;
            if (sharedMaterial == null)
            {
                return;
            }

            EnsurePropertyBlock();
            propertyBlock.Clear();
            renderer.GetPropertyBlock(propertyBlock);

            if (sharedMaterial.HasProperty(BaseColorId))
            {
                propertyBlock.SetColor(BaseColorId, color);
            }

            if (sharedMaterial.HasProperty(ColorId))
            {
                propertyBlock.SetColor(ColorId, color);
            }

            if (sharedMaterial.HasProperty(EmissionColorId))
            {
                propertyBlock.SetColor(EmissionColorId, color * Mathf.Lerp(0.12f, 1.16f, energy));
            }

            renderer.SetPropertyBlock(propertyBlock);
        }

        protected void PlayBoundParticles()
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i]?.particleSystem != null)
                {
                    bindings[i].particleSystem.Play(true);
                }
            }
        }

        protected static float GetSimulationDelta()
        {
            return Application.isPlaying ? Time.deltaTime : (1f / 60f);
        }

        protected static Transform FindTransform(Transform[] transforms, string[] aliases)
        {
            return FindComponent(transforms, aliases, candidate => candidate.name);
        }

        protected static T FindComponent<T>(T[] candidates, string[] aliases, Func<T, string> getName)
            where T : UnityEngine.Object
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                T candidate = candidates[i];
                if (candidate == null)
                {
                    continue;
                }

                string normalizedName = NormalizeName(getName(candidate));
                for (int aliasIndex = 0; aliasIndex < aliases.Length; aliasIndex++)
                {
                    string alias = aliases[aliasIndex];
                    if (normalizedName.Contains(alias))
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        protected static string NormalizeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            char[] buffer = value.ToLowerInvariant().ToCharArray();
            int writeIndex = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (char.IsLetterOrDigit(buffer[i]))
                {
                    buffer[writeIndex] = buffer[i];
                    writeIndex++;
                }
            }

            return new string(buffer, 0, writeIndex);
        }
    }

    /// <summary>
    /// Generic layer binding structure used by all frameworks.
    /// </summary>
    [System.Serializable]
    public sealed class AuraLayerBinding
    {
        public int LayerId;
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
