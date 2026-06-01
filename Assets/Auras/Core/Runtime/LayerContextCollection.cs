using System;
using System.Collections.Generic;
using UnityEngine;

namespace Auras.Core
{
    /// <summary>
    /// Type-safe, dictionary-based replacement for array-based layer state management.
    /// Provides named access to layer contexts without index misalignment risks.
    /// </summary>
    public class LayerContextCollection
    {
        private readonly Dictionary<int, AuraLayerContext> contextsByLayerId = new();
        private readonly Dictionary<string, int> layerNameToId = new();

        /// <summary>
        /// Initialize collection with layer count and optional name mapping.
        /// </summary>
        public LayerContextCollection(int layerCount, Dictionary<string, int> nameToIdMap = null)
        {
            for (int i = 0; i < layerCount; i++)
            {
                contextsByLayerId[i] = AuraLayerContext.Create(i);
            }

            if (nameToIdMap != null)
            {
                foreach (var kvp in nameToIdMap)
                {
                    layerNameToId[kvp.Key] = kvp.Value;
                }
            }
        }

        /// <summary>
        /// Get or create a context for a layer ID.
        /// </summary>
        public AuraLayerContext GetOrCreate(int layerId)
        {
            if (!contextsByLayerId.TryGetValue(layerId, out var context))
            {
                context = AuraLayerContext.Create(layerId);
                contextsByLayerId[layerId] = context;
            }

            return context;
        }

        /// <summary>
        /// Get context by layer ID.
        /// </summary>
        public AuraLayerContext Get(int layerId)
        {
            if (contextsByLayerId.TryGetValue(layerId, out var context))
            {
                return context;
            }

            return null;
        }

        /// <summary>
        /// Get context by layer name (if name mapping was provided).
        /// </summary>
        public AuraLayerContext GetByName(string layerName)
        {
            if (layerNameToId.TryGetValue(layerName, out int layerId))
            {
                return Get(layerId);
            }

            return null;
        }

        /// <summary>
        /// Try to get context by layer ID.
        /// </summary>
        public bool TryGet(int layerId, out AuraLayerContext context)
        {
            return contextsByLayerId.TryGetValue(layerId, out context);
        }

        /// <summary>
        /// Try to get context by layer name.
        /// </summary>
        public bool TryGetByName(string layerName, out AuraLayerContext context)
        {
            context = null;
            if (layerNameToId.TryGetValue(layerName, out int layerId))
            {
                return TryGet(layerId, out context);
            }

            return false;
        }

        /// <summary>
        /// Register a layer name to ID mapping.
        /// </summary>
        public void RegisterLayerName(string name, int layerId)
        {
            layerNameToId[name] = layerId;
        }

        /// <summary>
        /// Get all layer contexts.
        /// </summary>
        public IEnumerable<AuraLayerContext> GetAll()
        {
            return contextsByLayerId.Values;
        }

        /// <summary>
        /// Get count of contexts.
        /// </summary>
        public int Count => contextsByLayerId.Count;

        /// <summary>
        /// Clear all contexts.
        /// </summary>
        public void Clear()
        {
            contextsByLayerId.Clear();
            layerNameToId.Clear();
        }

        /// <summary>
        /// Apply a function to all contexts.
        /// </summary>
        public void ForEach(Action<AuraLayerContext> action)
        {
            foreach (var context in contextsByLayerId.Values)
            {
                action?.Invoke(context);
            }
        }

        /// <summary>
        /// Apply a function to contexts in order of layer ID.
        /// </summary>
        public void ForEachOrdered(Action<AuraLayerContext> action)
        {
            var keys = new List<int>(contextsByLayerId.Keys);
            keys.Sort();

            foreach (int layerId in keys)
            {
                if (contextsByLayerId.TryGetValue(layerId, out var context))
                {
                    action?.Invoke(context);
                }
            }
        }
    }
}
