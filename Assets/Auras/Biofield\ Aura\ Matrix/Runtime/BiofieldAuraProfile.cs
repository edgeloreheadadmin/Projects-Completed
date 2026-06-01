using System.Collections.Generic;
using UnityEngine;

namespace Auras.BiofieldAuraMatrix
{
    [CreateAssetMenu(fileName = "BiofieldAuraProfile", menuName = "Auras/Biofield Aura Matrix Profile")]
    public sealed class BiofieldAuraProfile : ScriptableObject
    {
        [SerializeField]
        private List<BiofieldLayerDefinition> layers = new List<BiofieldLayerDefinition>();

        [SerializeField]
        private InteractionProfile interactionProfile;

        private static readonly BiofieldLayerDefinition[] DefaultLayerDefinitions = BuildDefaultLayerDefinitions();

        public IReadOnlyList<BiofieldLayerDefinition> Layers => layers;
        public InteractionProfile InteractionProfile => interactionProfile;

        public BiofieldLayerDefinition GetLayer(BiofieldLayerType layer)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                BiofieldLayerDefinition definition = layers[i];
                if (definition != null && definition.layer == layer)
                {
                    return definition;
                }
            }

            return GetDefaultLayerDefinition(layer);
        }

        public static BiofieldLayerDefinition GetDefaultLayerDefinition(BiofieldLayerType layer)
        {
            return DefaultLayerDefinitions[(int)layer];
        }

        [ContextMenu("Reset To Research Defaults")]
        public void ResetToResearchDefaults()
        {
            layers = new List<BiofieldLayerDefinition>(DefaultLayerDefinitions.Length);
            for (int i = 0; i < DefaultLayerDefinitions.Length; i++)
            {
                layers.Add(Clone(DefaultLayerDefinitions[i]));
            }
        }

        private void Reset()
        {
            ResetToResearchDefaults();
        }

        private void OnValidate()
        {
            EnsureStructure();
        }

        private void EnsureStructure()
        {
            if (layers == null)
            {
                layers = new List<BiofieldLayerDefinition>();
            }

            Dictionary<BiofieldLayerType, BiofieldLayerDefinition> lookup = new Dictionary<BiofieldLayerType, BiofieldLayerDefinition>();
            for (int i = 0; i < layers.Count; i++)
            {
                BiofieldLayerDefinition definition = layers[i];
                if (definition == null || lookup.ContainsKey(definition.layer))
                {
                    continue;
                }

                lookup.Add(definition.layer, definition);
            }

            List<BiofieldLayerDefinition> ordered = new List<BiofieldLayerDefinition>(DefaultLayerDefinitions.Length);
            for (int i = 0; i < DefaultLayerDefinitions.Length; i++)
            {
                BiofieldLayerDefinition fallback = DefaultLayerDefinitions[i];
                if (lookup.TryGetValue(fallback.layer, out BiofieldLayerDefinition existing))
                {
                    existing.layer = fallback.layer;
                    ordered.Add(existing);
                    continue;
                }

                ordered.Add(Clone(fallback));
            }

            layers = ordered;
        }

        private static BiofieldLayerDefinition Clone(BiofieldLayerDefinition source)
        {
            return new BiofieldLayerDefinition
            {
                layer = source.layer,
                displayName = source.displayName,
                primaryFunction = source.primaryFunction,
                biofieldBridge = source.biofieldBridge,
                environmentInteraction = source.environmentInteraction,
                wellBeingRole = source.wellBeingRole,
                chakraCorrespondence = source.chakraCorrespondence,
                supportPractice = source.supportPractice,
                balancedColor = source.balancedColor,
                baseScale = source.baseScale,
                peakScale = source.peakScale,
                shellThickness = source.shellThickness,
                baseEmission = source.baseEmission,
                peakEmission = source.peakEmission,
                baseLightIntensity = source.baseLightIntensity,
                peakLightIntensity = source.peakLightIntensity,
                pulseSpeed = source.pulseSpeed,
            };
        }

        private static BiofieldLayerDefinition[] BuildDefaultLayerDefinitions()
        {
            return new[]
            {
                new BiofieldLayerDefinition
                {
                    layer = BiofieldLayerType.Etheric,
                    displayName = "Etheric",
                    primaryFunction = "Near-body vitality interface and energetic blueprint for the physical form.",
                    biofieldBridge = "Mirrors the body through meridian-like circuitry, fascia conduction, and bioelectric repair signaling.",
                    environmentInteraction = "Responds strongly to grounding, physical surroundings, and geomagnetic or atmospheric stress.",
                    wellBeingRole = "Supports stamina, recovery, embodiment, and a felt sense of physical presence.",
                    chakraCorrespondence = "Root chakra bridge stored as metadata only.",
                    supportPractice = "Grounding, rest, nutrition, movement, bodywork, and time close to natural environments.",
                    balancedColor = ColorFromBytes(214, 92, 65),
                    baseScale = 1.02f,
                    peakScale = 1.18f,
                    shellThickness = 0.07f,
                    baseEmission = 4f,
                    peakEmission = 15f,
                    baseLightIntensity = 0.12f,
                    peakLightIntensity = 0.68f,
                    pulseSpeed = 0.72f,
                },
                new BiofieldLayerDefinition
                {
                    layer = BiofieldLayerType.Emotional,
                    displayName = "Emotional",
                    primaryFunction = "Fluid feeling-field carrying moods, desires, and the immediate emotional weather.",
                    biofieldBridge = "Acts like energy-in-motion around the body, reflecting stress chemistry and heart-state shifts.",
                    environmentInteraction = "Absorbs and broadcasts the tone of rooms, relationships, and social atmospheres.",
                    wellBeingRole = "Supports emotional adaptability, healthy expression, and resilient affective regulation.",
                    chakraCorrespondence = "Sacral and solar-plexus bridge stored as metadata only.",
                    supportPractice = "Creative release, honest feeling work, energetic boundaries, and emotionally safe connection.",
                    balancedColor = ColorFromBytes(236, 156, 71),
                    baseScale = 1.18f,
                    peakScale = 1.36f,
                    shellThickness = 0.08f,
                    baseEmission = 5f,
                    peakEmission = 19f,
                    baseLightIntensity = 0.15f,
                    peakLightIntensity = 0.86f,
                    pulseSpeed = 0.9f,
                },
                new BiofieldLayerDefinition
                {
                    layer = BiofieldLayerType.Mental,
                    displayName = "Mental",
                    primaryFunction = "Thought-form layer for beliefs, focus, cognitive framing, and interpretive order.",
                    biofieldBridge = "Tracks coherent thinking, brain-driven signal patterns, and the structuring force of attention.",
                    environmentInteraction = "Sensitive to information overload, ambient noise, and the mental climate of shared spaces.",
                    wellBeingRole = "Supports clarity, perspective, self-direction, and reduced rumination or cognitive clutter.",
                    chakraCorrespondence = "Solar plexus and throat bridge stored as metadata only.",
                    supportPractice = "Meditation, focused learning, belief repair, quiet environments, and intentional information limits.",
                    balancedColor = ColorFromBytes(242, 209, 85),
                    baseScale = 1.34f,
                    peakScale = 1.54f,
                    shellThickness = 0.08f,
                    baseEmission = 6f,
                    peakEmission = 22f,
                    baseLightIntensity = 0.17f,
                    peakLightIntensity = 0.96f,
                    pulseSpeed = 1.02f,
                },
                new BiofieldLayerDefinition
                {
                    layer = BiofieldLayerType.Astral,
                    displayName = "Astral",
                    primaryFunction = "Relational layer connecting personal feeling to love, empathy, and the shared human field.",
                    biofieldBridge = "Carries the resonance of closeness, compassion, longing, and connection beyond the personal self.",
                    environmentInteraction = "Expands with trust and affection, contracts under betrayal, conflict, or relational overwhelm.",
                    wellBeingRole = "Supports intimacy, compassion, healthy bonding, and attunement without emotional flooding.",
                    chakraCorrespondence = "Heart bridge stored as metadata only.",
                    supportPractice = "Heart coherence, forgiveness work, safe intimacy, gratitude, and reciprocal connection.",
                    balancedColor = ColorFromBytes(117, 199, 148),
                    baseScale = 1.52f,
                    peakScale = 1.74f,
                    shellThickness = 0.09f,
                    baseEmission = 7f,
                    peakEmission = 24f,
                    baseLightIntensity = 0.2f,
                    peakLightIntensity = 1.06f,
                    pulseSpeed = 0.82f,
                },
                new BiofieldLayerDefinition
                {
                    layer = BiofieldLayerType.EthericTemplate,
                    displayName = "Etheric Template",
                    primaryFunction = "Architectural field that organizes pattern, sound, form, and the higher template of expression.",
                    biofieldBridge = "Holds a structured blueprint the lower layers can follow, especially when the system is re-ordering.",
                    environmentInteraction = "Responds to sound, order, ritual structure, and the degree of coherence in daily routine.",
                    wellBeingRole = "Supports repair of chronic disorder, expressive congruence, and stable patterning across the field.",
                    chakraCorrespondence = "Throat bridge stored as metadata only.",
                    supportPractice = "Breath pacing, chanting, speech alignment, disciplined routine, and simplifying visual or mental noise.",
                    balancedColor = ColorFromBytes(110, 156, 226),
                    baseScale = 1.72f,
                    peakScale = 1.95f,
                    shellThickness = 0.09f,
                    baseEmission = 7f,
                    peakEmission = 25f,
                    baseLightIntensity = 0.21f,
                    peakLightIntensity = 1.14f,
                    pulseSpeed = 0.78f,
                },
                new BiofieldLayerDefinition
                {
                    layer = BiofieldLayerType.Celestial,
                    displayName = "Celestial",
                    primaryFunction = "Layer of intuition, spiritual love, inspiration, and luminous compassionate awareness.",
                    biofieldBridge = "Correlates with contemplative openness, peak insight, and high-coherence states of perception.",
                    environmentInteraction = "Strengthened by sacred spaces, beauty, devotion, silence, and compassionate company.",
                    wellBeingRole = "Supports meaning, hope, intuition, and a resilient sense of loving connection to life.",
                    chakraCorrespondence = "Third-eye and heart bridge stored as metadata only.",
                    supportPractice = "Meditation, prayer, awe, music, contemplation, and deliberate cultivation of compassion.",
                    balancedColor = ColorFromBytes(122, 111, 222),
                    baseScale = 1.92f,
                    peakScale = 2.16f,
                    shellThickness = 0.1f,
                    baseEmission = 8f,
                    peakEmission = 29f,
                    baseLightIntensity = 0.24f,
                    peakLightIntensity = 1.28f,
                    pulseSpeed = 0.94f,
                },
                new BiofieldLayerDefinition
                {
                    layer = BiofieldLayerType.Causal,
                    displayName = "Causal",
                    primaryFunction = "Outermost soul field holding long-range purpose, karmic patterning, and divine connection.",
                    biofieldBridge = "Links personal identity with transpersonal awareness, life meaning, and deep pattern integration.",
                    environmentInteraction = "Responsive to vastness, sacred ceremony, cosmic perspective, and collective spiritual intent.",
                    wellBeingRole = "Supports purpose, existential steadiness, perspective, and alignment with one's deepest values.",
                    chakraCorrespondence = "Crown bridge stored as metadata only.",
                    supportPractice = "Meditation, prayer, service, surrender, time under open sky, and alignment with soul-level commitments.",
                    balancedColor = ColorFromBytes(231, 226, 255),
                    baseScale = 2.14f,
                    peakScale = 2.42f,
                    shellThickness = 0.11f,
                    baseEmission = 9f,
                    peakEmission = 33f,
                    baseLightIntensity = 0.28f,
                    peakLightIntensity = 1.45f,
                    pulseSpeed = 1.08f,
                },
            };
        }

        private static Color ColorFromBytes(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 255);
        }
    }
}
