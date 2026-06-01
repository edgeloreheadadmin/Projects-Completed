using System;
using System.Collections.Generic;
using Auras.Core;
using UnityEngine;

namespace Auras.BiofieldAuraMatrix
{
    /// <summary>
    /// Manages aura layer states and their visual/audio bindings.
    /// Now uses type-safe AuraLayerContext collection instead of array indexing.
    /// </summary>
    [ExecuteAlways]
    public sealed class BiofieldAuraController : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Header("Profile")]
        [SerializeField]
        private BiofieldAuraProfile profile;

        [SerializeField]
        private bool animateInEditMode = true;

        [SerializeField]
        private bool autoPlayBoundParticles = true;

        [SerializeField]
        [Min(0.1f)]
        private float blendSpeed = 6f;

        [SerializeField]
        [Range(0f, 0.35f)]
        private float pulseAmplitude = 0.08f;

        [Header("Bindings")]
        [SerializeField]
        private List<BiofieldLayerBinding> bindings = new List<BiofieldLayerBinding>();

        private LayerContextCollection layerContexts;
        private MaterialPropertyBlock propertyBlock;

        public BiofieldAuraProfile Profile => profile;

        public void SetProfile(BiofieldAuraProfile value)
        {
            profile = value;
        }

        public void SetState(BiofieldLayerType layer, double intensity, double integrity)
        {
            var context = layerContexts?.Get((int)layer);
            if (context != null)
            {
                context.TargetIntensity = intensity;
                context.TargetIntegrity = integrity;
            }
        }

        public AuraLayerContext GetContext(BiofieldLayerType layer)
        {
            return layerContexts?.Get((int)layer);
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

        public void NudgeLayer(BiofieldLayerType layer, double intensityDelta, double integrityDelta)
        {
            var context = layerContexts?.Get((int)layer);
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
                BiofieldLayerBinding binding = bindings[i];
                if (binding == null)
                {
                    continue;
                }

                string[] aliases = GetAliases(binding.layer);

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
                BiofieldLayerBinding binding = bindings[i];
                if (binding?.anchor == null)
                {
                    continue;
                }

                BiofieldLayerDefinition definition = profile != null
                    ? profile.GetLayer(binding.layer)
                    : BiofieldAuraProfile.GetDefaultLayerDefinition(binding.layer);

                binding.anchor.localPosition = Vector3.zero;
                binding.anchor.localRotation = Quaternion.identity;
                binding.anchor.localScale = Vector3.one * definition.baseScale;
            }
        }

        private void Reset()
        {
            EnsureBindingSlots();
            InitializeContexts();
            SeedNeutralTargets();
            ApplyNow();
        }

        private void OnEnable()
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

        private void OnValidate()
        {
            EnsureBindingSlots();

            if (!Application.isPlaying && animateInEditMode)
            {
                ApplyBindings();
            }
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying && !animateInEditMode)
            {
                return;
            }

            if (layerContexts == null)
            {
                InitializeContexts();
            }

            float step = blendSpeed * GetSimulationDelta();
            layerContexts.ForEachOrdered(ctx =>
            {
                double newIntensity = ctx.intensityEasing.InterpolateHighPrecision(
                    ctx.CurrentIntensity, ctx.TargetIntensity, step);
                double newIntegrity = ctx.integrityEasing.InterpolateHighPrecision(
                    ctx.CurrentIntegrity, ctx.TargetIntegrity, step);

                ctx.CurrentIntensity = newIntensity;
                ctx.CurrentIntegrity = newIntegrity;
                ctx.InvalidateEnergy();
            });

            ApplyBindings();
        }

        private void InitializeContexts()
        {
            if (layerContexts == null)
            {
                layerContexts = new LayerContextCollection(Enum.GetValues(typeof(BiofieldLayerType)).Length);

                var layerNameMap = new Dictionary<string, int>();
                foreach (BiofieldLayerType layer in Enum.GetValues(typeof(BiofieldLayerType)))
                {
                    layerNameMap[layer.ToString()] = (int)layer;
                }

                foreach (var kvp in layerNameMap)
                {
                    layerContexts.RegisterLayerName(kvp.Key, kvp.Value);
                }
            }
        }

        private void SeedNeutralTargets()
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

        private bool TargetsAreEmpty()
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

        private void EnsureBindingSlots()
        {
            if (bindings == null)
            {
                bindings = new List<BiofieldLayerBinding>();
            }

            Dictionary<BiofieldLayerType, BiofieldLayerBinding> lookup = new Dictionary<BiofieldLayerType, BiofieldLayerBinding>();
            for (int i = 0; i < bindings.Count; i++)
            {
                BiofieldLayerBinding binding = bindings[i];
                if (binding == null || lookup.ContainsKey(binding.layer))
                {
                    continue;
                }

                lookup.Add(binding.layer, binding);
            }

            List<BiofieldLayerBinding> ordered = new List<BiofieldLayerBinding>(Enum.GetValues(typeof(BiofieldLayerType)).Length);
            Array layerValues = Enum.GetValues(typeof(BiofieldLayerType));
            for (int i = 0; i < layerValues.Length; i++)
            {
                BiofieldLayerType layer = (BiofieldLayerType)layerValues.GetValue(i);
                if (!lookup.TryGetValue(layer, out BiofieldLayerBinding binding))
                {
                    binding = new BiofieldLayerBinding { layer = layer };
                }

                binding.layer = layer;
                ordered.Add(binding);
            }

            bindings = ordered;
        }

        private void ApplyBindings()
        {
            EnsureBindingSlots();

            for (int i = 0; i < bindings.Count; i++)
            {
                BiofieldLayerBinding binding = bindings[i];
                if (binding == null)
                {
                    continue;
                }

                var context = layerContexts?.Get((int)binding.layer);
                if (context == null)
                {
                    continue;
                }

                BiofieldLayerDefinition definition = profile != null
                    ? profile.GetLayer(binding.layer)
                    : BiofieldAuraProfile.GetDefaultLayerDefinition(binding.layer);

                ApplyBinding(binding, definition, context);
            }
        }

        private void ApplyBinding(BiofieldLayerBinding binding, BiofieldLayerDefinition definition, AuraLayerContext context)
        {
            double energy = context.GetEnergy();
            double integrityTint = Mathf.Clamp01(0.24f + ((float)context.CurrentIntegrity * 0.76f));
            Color lowEnergyColor = Color.Lerp(Color.black, definition.balancedColor, 0.24f);
            Color drivenColor = Color.Lerp(lowEnergyColor, definition.balancedColor, (float)integrityTint);
            float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
            float pulse = 1f + (pulseAmplitude * (float)context.CurrentIntensity * Mathf.Sin((time * definition.pulseSpeed) + ((int)binding.layer * 0.85f)));
            float scale = Mathf.Lerp(definition.baseScale, definition.peakScale, (float)energy) * binding.scaleMultiplier * pulse;

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
                    main.startSize = Mathf.Max(0.01f, scale * definition.shellThickness);
                }

                main.simulationSpeed = Mathf.Lerp(0.82f, 1.6f, (float)energy);

                if (binding.driveParticleEmission)
                {
                    ParticleSystem.EmissionModule emission = binding.particleSystem.emission;
                    emission.rateOverTime = Mathf.Lerp(definition.baseEmission, definition.peakEmission, (float)energy) * binding.emissionMultiplier;
                }
            }

            if (binding.light != null && binding.driveLight)
            {
                binding.light.color = drivenColor;
                binding.light.intensity = Mathf.Lerp(definition.baseLightIntensity, definition.peakLightIntensity, (float)energy) * binding.lightMultiplier;
                binding.light.range = Mathf.Lerp(definition.baseScale * 1.35f, definition.peakScale * 2.1f, (float)energy);
            }

            if (binding.renderer != null && binding.driveRendererColor)
            {
                ApplyRendererColor(binding.renderer, drivenColor, (float)energy);
            }
        }

        private void EnsurePropertyBlock()
        {
            propertyBlock ??= new MaterialPropertyBlock();
        }

        private void ApplyRendererColor(Renderer renderer, Color color, float energy)
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

        private void PlayBoundParticles()
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i]?.particleSystem != null)
                {
                    bindings[i].particleSystem.Play(true);
                }
            }
        }

        private static float GetSimulationDelta()
        {
            return Application.isPlaying ? Time.deltaTime : (1f / 60f);
        }

        private static Transform FindTransform(Transform[] transforms, string[] aliases)
        {
            return FindComponent(transforms, aliases, candidate => candidate.name);
        }

        private static T FindComponent<T>(T[] candidates, string[] aliases, Func<T, string> getName)
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

        private static string NormalizeName(string value)
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

        private static string[] GetAliases(BiofieldLayerType layer)
        {
            return layer switch
            {
                BiofieldLayerType.Etheric => new[] { "etheric", "ethericshell" },
                BiofieldLayerType.Emotional => new[] { "emotional", "emotionalshell" },
                BiofieldLayerType.Mental => new[] { "mental", "mentalshell" },
                BiofieldLayerType.Astral => new[] { "astral", "astralshell" },
                BiofieldLayerType.EthericTemplate => new[] { "etherictemplate", "templateshell", "template" },
                BiofieldLayerType.Celestial => new[] { "celestial", "celestialshell" },
                BiofieldLayerType.Causal => new[] { "causal", "causalshell", "ketheric" },
                _ => Array.Empty<string>(),
            };
        }
    }
}
