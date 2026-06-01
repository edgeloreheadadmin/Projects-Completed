// Minimal Unity API stubs for compiling Auras C# scripts outside of the Unity Editor.
// All types are in namespace UnityEngine to match the existing `using UnityEngine;` directives.

using System;

namespace UnityEngine
{
    // ── ATTRIBUTES ─────────────────────────────────────────────────────────────

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeFieldAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
    public sealed class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string label) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ExecuteAlwaysAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MinAttribute : Attribute
    {
        public MinAttribute(float min) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RangeAttribute : Attribute
    {
        public RangeAttribute(float min, float max) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ContextMenuAttribute : Attribute
    {
        public ContextMenuAttribute(string name) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CreateAssetMenuAttribute : Attribute
    {
        public string fileName;
        public string menuName;
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class TooltipAttribute : Attribute
    {
        public TooltipAttribute(string text) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TextAreaAttribute : Attribute
    {
        public TextAreaAttribute(int minLines, int maxLines) { }
    }

    // ── MATHF (real implementations — used by EasingCurve, AuraLayerContext, etc.) ─

    public static class Mathf
    {
        public const float PI = (float)Math.PI;

        public static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
        public static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float Sin(float f) => (float)Math.Sin(f);
        public static float Cos(float f) => (float)Math.Cos(f);
        public static float Max(float a, float b) => a > b ? a : b;
        public static float Min(float a, float b) => a < b ? a : b;
        public static float Pow(float f, float p) => (float)Math.Pow(f, p);
        public static float Exp(float f) => (float)Math.Exp(f);
        public static float Abs(float f) => f < 0f ? -f : f;
        public static float Sqrt(float f) => (float)Math.Sqrt(f);
    }

    // ── COLOR ───────────────────────────────────────────────────────────────────

    public struct Color
    {
        public float r, g, b, a;

        public Color(float r, float g, float b, float a = 1f)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static Color black => new Color(0f, 0f, 0f, 1f);
        public static Color white => new Color(1f, 1f, 1f, 1f);
        public static Color red   => new Color(1f, 0f, 0f, 1f);
        public static Color green => new Color(0f, 1f, 0f, 1f);
        public static Color blue  => new Color(0f, 0f, 1f, 1f);

        public static Color Lerp(Color a, Color b, float t)
        {
            float ct = t < 0f ? 0f : t > 1f ? 1f : t;
            return new Color(
                a.r + (b.r - a.r) * ct,
                a.g + (b.g - a.g) * ct,
                a.b + (b.b - a.b) * ct,
                a.a + (b.a - a.a) * ct);
        }

        public static Color operator *(Color c, float f)
            => new Color(c.r * f, c.g * f, c.b * f, c.a * f);

        public static Color operator *(float f, Color c)
            => new Color(c.r * f, c.g * f, c.b * f, c.a * f);
    }

    public struct Color32
    {
        public byte r, g, b, a;

        public Color32(byte r, byte g, byte b, byte a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static implicit operator Color(Color32 c)
            => new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
    }

    // ── VECTOR3, QUATERNION ─────────────────────────────────────────────────────

    public struct Vector3
    {
        public float x, y, z;

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3 zero => new Vector3(0f, 0f, 0f);
        public static Vector3 one  => new Vector3(1f, 1f, 1f);

        public static Vector3 operator *(Vector3 v, float s)
            => new Vector3(v.x * s, v.y * s, v.z * s);

        public static Vector3 operator *(float s, Vector3 v)
            => new Vector3(v.x * s, v.y * s, v.z * s);
    }

    public struct Quaternion
    {
        public float x, y, z, w;

        public static Quaternion identity => new Quaternion { x = 0f, y = 0f, z = 0f, w = 1f };
    }

    // ── RUNTIME STATICS ─────────────────────────────────────────────────────────

    // Mutable statics: AuraSimulator.Update() writes these each tick so that
    // any code reading Time.time/deltaTime gets accurate values.
    public static class Time
    {
        public static float time = 0f;
        public static float deltaTime = 0.016f;
        public static float realtimeSinceStartup = 0f;
    }

    public static class Application
    {
        // False so editor-branch code paths are never taken in standalone mode.
        public static bool isPlaying = false;
    }

    public static class Shader
    {
        public static int PropertyToID(string name) => name.GetHashCode();
    }

    // ── ANIMATIONCURVE ──────────────────────────────────────────────────────────

    public class AnimationCurve
    {
        private float _t0, _v0, _t1, _v1;

        private AnimationCurve(float t0, float v0, float t1, float v1)
        {
            _t0 = t0; _v0 = v0; _t1 = t1; _v1 = v1;
        }

        public AnimationCurve() : this(0f, 0f, 1f, 1f) { }

        public static AnimationCurve Linear(float timeStart, float valueStart,
                                            float timeEnd, float valueEnd)
            => new AnimationCurve(timeStart, valueStart, timeEnd, valueEnd);

        public float Evaluate(float t)
        {
            if (_t1 <= _t0) return _v0;
            float frac = Mathf.Clamp01((t - _t0) / (_t1 - _t0));
            return _v0 + (_v1 - _v0) * frac;
        }
    }

    // ── OBJECT HIERARCHY ────────────────────────────────────────────────────────

    public class Object
    {
        public string name { get; set; } = string.Empty;
    }

    public class Component : Object { }

    public class Behaviour : Component { }

    public class MonoBehaviour : Behaviour
    {
        public T GetComponent<T>() where T : Component => null;
        public T[] GetComponentsInChildren<T>(bool includeInactive = false) where T : Component
            => Array.Empty<T>();
    }

    public class ScriptableObject : Object { }

    // ── COMPONENT STUBS (compile-only, no real runtime behavior) ────────────────

    public class Transform : Component
    {
        public Vector3    localPosition { get; set; } = Vector3.zero;
        public Quaternion localRotation { get; set; } = Quaternion.identity;
        public Vector3    localScale    { get; set; } = Vector3.one;

        public T GetComponent<T>() where T : Component => null;
        public Transform Find(string childName) => null;
        public void SetParent(Transform parent, bool worldPositionStays = true) { }
    }

    public class ParticleSystem : Component
    {
        public MainModule     main     { get; } = new MainModule();
        public EmissionModule emission { get; } = new EmissionModule();
        public void Play(bool withChildren = true) { }
        public void Stop(bool withChildren = true) { }

        public class MainModule
        {
            public Color startColor    { get; set; }
            public float startSize     { get; set; }
            public float simulationSpeed { get; set; }
        }

        public class EmissionModule
        {
            public float rateOverTime { get; set; }
        }
    }

    public class Light : Component
    {
        public Color color     { get; set; }
        public float intensity { get; set; }
        public float range     { get; set; }
    }

    public class Renderer : Component
    {
        public Material sharedMaterial { get; set; }
        public void GetPropertyBlock(MaterialPropertyBlock dest) { }
        public void SetPropertyBlock(MaterialPropertyBlock src) { }
    }

    public class Material
    {
        public bool HasProperty(int nameId) => false;
        public bool HasProperty(string name) => false;
    }

    public class MaterialPropertyBlock
    {
        public void Clear() { }
        public void SetColor(int nameId, Color value) { }
        public void SetFloat(int nameId, float value) { }
    }

    public class GameObject : Object
    {
        public Transform transform { get; } = new Transform();

        public GameObject() { }
        public GameObject(string name) { this.name = name; }

        public T GetComponent<T>() where T : Component => null;
        public T AddComponent<T>() where T : Component, new() => new T();
        public static GameObject Find(string name) => null;
    }
}
