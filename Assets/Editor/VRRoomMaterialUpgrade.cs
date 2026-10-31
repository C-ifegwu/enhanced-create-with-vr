// Converts the Built-in "Standard" materials of the imported VR Room pack to URP Lit so they render in this URP project.
// Run from: Tools > Create With VR > Upgrade VR Room Materials.
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class VRRoomMaterialUpgrade
{
    [MenuItem("Tools/Create With VR/Upgrade VR Room Materials")]
    public static void Run()
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var particlesUnlit = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        var log = new List<string>();
        var shaderNames = new HashSet<string>();
        int converted = 0;

        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/VR Room" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null || m.shader == null) continue;
            string sn = m.shader.name;
            shaderNames.Add(sn);

            if (sn == "Standard" || sn == "Standard (Specular setup)")
            {
                Convert(m, lit);
                converted++;
            }
            else if (sn == "Origami" || sn == "Nature/Tree Soft Occlusion Leaves")
            {
                Convert(m, lit);
                if (sn.StartsWith("Nature"))
                {
                    m.SetFloat("_Cull", 0f);
                    m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.4f);
                    m.EnableKeyword("_ALPHATEST_ON"); m.renderQueue = 2450;
                }
                else m.SetFloat("_Cull", 0f);
                converted++;
            }
            else if ((sn.StartsWith("Particles/") || sn.StartsWith("Legacy Shaders/Particles")) && particlesUnlit != null)
            {
                var tex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                var col = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                m.shader = particlesUnlit;
                m.SetTexture("_BaseMap", tex);
                m.SetColor("_BaseColor", col);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", sn.Contains("Additive") ? 2f : 0f);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", sn.Contains("Additive") ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = 3000;
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                if (sn.Contains("Additive")) m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                converted++;
            }
            else if (sn == "Hidden/InternalErrorShader" || m.shader.name.Contains("Error"))
            {
                Convert(m, lit);
                converted++;
            }
            EditorUtility.SetDirty(m);
        }
        AssetDatabase.SaveAssets();
        System.IO.File.WriteAllText("upgrade_shaders.txt", "Converted " + converted + "\nShaders seen:\n" + string.Join("\n", shaderNames));
        Debug.Log("VR Room materials upgraded: " + converted);
    }

    static void Convert(Material m, Shader lit)
    {
        // Read the old values before the shader changes.
        Color color = Get(m, "_Color", Color.white);
        Texture main = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
        Vector2 scale = m.HasProperty("_MainTex") ? m.GetTextureScale("_MainTex") : Vector2.one;
        Vector2 offset = m.HasProperty("_MainTex") ? m.GetTextureOffset("_MainTex") : Vector2.zero;
        float metallic = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0f;
        float gloss = m.HasProperty("_Glossiness") ? m.GetFloat("_Glossiness") : 0.5f;
        Texture bump = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;
        float bumpScale = m.HasProperty("_BumpScale") ? m.GetFloat("_BumpScale") : 1f;
        Texture metalMap = m.HasProperty("_MetallicGlossMap") ? m.GetTexture("_MetallicGlossMap") : null;
        Texture occlusion = m.HasProperty("_OcclusionMap") ? m.GetTexture("_OcclusionMap") : null;
        Texture emissionMap = m.HasProperty("_EmissionMap") ? m.GetTexture("_EmissionMap") : null;
        Color emission = Get(m, "_EmissionColor", Color.black);
        bool emissive = m.IsKeywordEnabled("_EMISSION") || emission.maxColorComponent > 0.001f;
        float mode = m.HasProperty("_Mode") ? m.GetFloat("_Mode") : 0f;
        float cutoff = m.HasProperty("_Cutoff") ? m.GetFloat("_Cutoff") : 0.5f;

        m.shader = lit;
        m.SetColor("_BaseColor", color);
        m.SetTexture("_BaseMap", main);
        m.SetTextureScale("_BaseMap", scale);
        m.SetTextureOffset("_BaseMap", offset);
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", gloss);
        if (bump != null) { m.SetTexture("_BumpMap", bump); m.SetFloat("_BumpScale", bumpScale); m.EnableKeyword("_NORMALMAP"); }
        if (metalMap != null) { m.SetTexture("_MetallicGlossMap", metalMap); m.EnableKeyword("_METALLICSPECGLOSSMAP"); }
        if (occlusion != null) { m.SetTexture("_OcclusionMap", occlusion); m.EnableKeyword("_OCCLUSIONMAP"); }
        if (emissive)
        {
            m.SetColor("_EmissionColor", emission);
            if (emissionMap != null) m.SetTexture("_EmissionMap", emissionMap);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }

        if (mode == 1f)
        {
            m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", cutoff);
            m.EnableKeyword("_ALPHATEST_ON");
            m.renderQueue = 2450;
            m.SetOverrideTag("RenderType", "TransparentCutout");
        }
        else if (mode >= 2f)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
        }
    }

    static Color Get(Material m, string prop, Color def) { return m.HasProperty(prop) ? m.GetColor(prop) : def; }
}
