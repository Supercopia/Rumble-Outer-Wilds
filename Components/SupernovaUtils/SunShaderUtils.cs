using OuterWildsRumble.Components;
using UnityEngine;

namespace OuterWildsRumble.Components.SupernovaUtils;

public static class SunShaderUtils
{
    // =========================
    // DATA STRUCTS (moved here)
    // =========================

    public struct SunCoreSettings
    {
        public float SunBright;
        public float SunSpeed;

        public Color Color1;
        public Color Color2;
        public Color Color3;
        public Color Color4;
    }

    public struct SunHaloSettings
    {
        public float HaloRing1;
        public Color HaloRing1Color;
        public float HaloRing1Size;
        public float HaloRing1Intensity;
        public float HaloRing1Strength;

        public float HaloRing2;
        public float HaloRing2Str;
        public float HaloRing2Thickness;

        public Color HaloRing2Color;
        public float HaloRing2Size;
        public float HaloRing2Intensity;
        public float HaloRing2Width;
    }

    // =========================
    // PROPERTY HELPERS
    // =========================

    public static float GetFloat(Material m, string prop, float fallback = 0f)
        => m.HasProperty(prop) ? m.GetFloat(prop) : fallback;

    public static Color GetColor(Material m, string prop, Color fallback)
        => m.HasProperty(prop) ? m.GetColor(prop) : fallback;

    public static void SetFloat(Material m, string prop, float value)
    {
        if (m.HasProperty(prop))
            m.SetFloat(prop, value);
    }

    public static void SetColor(Material m, string prop, Color value)
    {
        if (m.HasProperty(prop))
            m.SetColor(prop, value);
    }

    // =========================
    // CORE READ
    // =========================

    public static SunCoreSettings ReadCore(Material mat)
    {
        return new SunCoreSettings
        {
            SunBright = GetFloat(mat, "Vector1_89F39960"),
            SunSpeed  = GetFloat(mat, "Vector1_BDF9B7FB"),

            Color1 = GetColor(mat, "Color_85C936A7", Color.white),
            Color2 = GetColor(mat, "Color_A488C782", Color.white),
            Color3 = GetColor(mat, "Color_132355DA", Color.white),
            Color4 = GetColor(mat, "Color_ED9867B5", Color.white),
        };
    }

    // =========================
    // HALO READ
    // =========================

    public static SunHaloSettings ReadHalo(Material mat)
    {
        return new SunHaloSettings
        {
            HaloRing1 = GetFloat(mat, "Vector1_EEBE656E"),
            HaloRing1Color = GetColor(mat, "Color_7026B652", Color.white),
            HaloRing1Size = GetFloat(mat, "Vector1_173DF909"),
            HaloRing1Intensity = GetFloat(mat, "Vector1_96975452"),
            HaloRing1Strength = GetFloat(mat, "Vector1_EF0B4D34"),

            HaloRing2 = GetFloat(mat, "Vector1_DAD19097"),
            HaloRing2Str = GetFloat(mat, "Vector1_68E638F"),
            HaloRing2Thickness = GetFloat(mat, "Vector1_853440A4"),

            HaloRing2Color = GetColor(mat, "Color_916DBA00", Color.white),
            HaloRing2Size = GetFloat(mat, "Vector1_26B4E625"),
            HaloRing2Intensity = GetFloat(mat, "Vector1_E678A47B"),
            HaloRing2Width = GetFloat(mat, "Vector1_5B67CB2F"),
        };
    }

    // =========================
    // CORE APPLY
    // =========================

    public static void ApplyCore(Material mat, SunCoreSettings s)
    {
        SetFloat(mat, "Vector1_89F39960", s.SunBright);
        SetFloat(mat, "Vector1_BDF9B7FB", s.SunSpeed);

        SetColor(mat, "Color_85C936A7", s.Color1);
        SetColor(mat, "Color_A488C782", s.Color2);
        SetColor(mat, "Color_132355DA", s.Color3);
        SetColor(mat, "Color_ED9867B5", s.Color4);
    }

    // =========================
    // HALO APPLY
    // =========================

    public static void ApplyHalo(Material mat, SunHaloSettings s)
    {
        SetFloat(mat, "Vector1_EEBE656E", s.HaloRing1);
        SetColor(mat, "Color_7026B652", s.HaloRing1Color);
        SetFloat(mat, "Vector1_173DF909", s.HaloRing1Size);
        SetFloat(mat, "Vector1_96975452", s.HaloRing1Intensity);
        SetFloat(mat, "Vector1_EF0B4D34", s.HaloRing1Strength);

        SetFloat(mat, "Vector1_DAD19097", s.HaloRing2);
        SetFloat(mat, "Vector1_68E638F", s.HaloRing2Str);
        SetFloat(mat, "Vector1_853440A4", s.HaloRing2Thickness);

        SetColor(mat, "Color_916DBA00", s.HaloRing2Color);
        SetFloat(mat, "Vector1_26B4E625", s.HaloRing2Size);
        SetFloat(mat, "Vector1_E678A47B", s.HaloRing2Intensity);
        SetFloat(mat, "Vector1_5B67CB2F", s.HaloRing2Width);
    }

    // =========================
    // OPTIONAL GAMEOBJECT WRAPPERS
    // =========================

    public static SunCoreSettings ReadCore(GameObject go)
    {
        var r = go.GetComponent<Renderer>();
        if (!r) return default;

        return ReadCore(r.material);
    }

    public static SunHaloSettings ReadHalo(GameObject go)
    {
        var r = go.GetComponent<Renderer>();
        if (!r) return default;

        return ReadHalo(r.material);
    }

    public static void ApplyCore(GameObject go, SunCoreSettings s)
    {
        var r = go.GetComponent<Renderer>();
        if (!r) return;

        ApplyCore(r.material, s);
    }

    public static void ApplyHalo(GameObject go, SunHaloSettings s)
    {
        var r = go.GetComponent<Renderer>();
        if (!r) return;

        ApplyHalo(r.material, s);
    }
}