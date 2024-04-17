using System;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Windows;
using File = System.IO.File;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    public static class LitGUI
    {
        public enum WorkflowMode
        {
            Specular = 0,
            Metallic
        }

        public enum SmoothnessMapChannel
        {
            SpecularMetallicAlpha,
            AlbedoAlpha,
        }

        public static class Styles
        {
            public static GUIContent smaMap =
                EditorGUIUtility.TrTextContent("SMA Map", "Smoothness, Metallic, AO combined map texture.");

            public static GUIContent smaRMAFlip =
                EditorGUIUtility.TrTextContent("SMA is RMA", "SMA texture is actually an RMA, with red being roughness instead of smoothness.");

            public static GUIContent workflowModeText = EditorGUIUtility.TrTextContent("Workflow Mode",
                "Select a workflow that fits your textures. Choose between Metallic or Specular.");

            public static GUIContent specularMapText =
                EditorGUIUtility.TrTextContent("Specular Map", "Designates a Specular Map and specular color determining the apperance of reflections on this Material's surface.");

            public static GUIContent metallicMapText =
                EditorGUIUtility.TrTextContent("Metallic Map", "Sets and configures the map for the Metallic workflow.");

            public static GUIContent smoothnessText = EditorGUIUtility.TrTextContent("Smoothness",
                "Controls the spread of highlights and reflections on the surface.");

            public static GUIContent metallicText = EditorGUIUtility.TrTextContent("Metallic",
                "Controls the how metallic the surface is.");

            public static GUIContent smoothnessMapChannelText =
                EditorGUIUtility.TrTextContent("Source",
                    "Specifies where to sample a smoothness map from. By default, uses the alpha channel for your map.");

            public static GUIContent highlightsText = EditorGUIUtility.TrTextContent("Specular Highlights",
                "When enabled, the Material reflects the shine from direct lighting.");

            public static GUIContent reflectionsText =
                EditorGUIUtility.TrTextContent("Environment Reflections",
                    "When enabled, the Material samples reflections from the nearest Reflection Probes or Lighting Probe.");

            public static GUIContent heightMapText = EditorGUIUtility.TrTextContent("Height Map",
                "Defines a Height Map that will drive a parallax effect in the shader making the surface seem displaced.");

            public static GUIContent occlusionText = EditorGUIUtility.TrTextContent("Occlusion Map",
                "Sets an occlusion map to simulate shadowing from ambient lighting.");

            public static GUIContent occlusionStrengthText = EditorGUIUtility.TrTextContent("Occlusion Strength",
                "Occlusion map strength multiplier.");

            public static readonly string[] metallicSmoothnessChannelNames = { "Metallic Alpha", "Albedo Alpha" };
            public static readonly string[] specularSmoothnessChannelNames = { "Specular Alpha", "Albedo Alpha" };

            public static GUIContent clearCoatText = EditorGUIUtility.TrTextContent("Clear Coat",
                "A multi-layer material feature which simulates a thin layer of coating on top of the surface material." +
                "\nPerformance cost is considerable as the specular component is evaluated twice, once per layer.");

            public static GUIContent clearCoatMaskText = EditorGUIUtility.TrTextContent("Mask",
                "Specifies the amount of the coat blending." +
                "\nActs as a multiplier of the clear coat map mask value or as a direct mask value if no map is specified." +
                "\nThe map specifies clear coat mask in the red channel and clear coat smoothness in the green channel.");

            public static GUIContent clearCoatSmoothnessText = EditorGUIUtility.TrTextContent("Smoothness",
                "Specifies the smoothness of the coating." +
                "\nActs as a multiplier of the clear coat map smoothness value or as a direct smoothness value if no map is specified.");
        }

        public struct LitProperties
        {
            // Surface Option Props
            public MaterialProperty workflowMode;

            // Surface Input Props
            public MaterialProperty metallic;
            public MaterialProperty specColor;
            public MaterialProperty metallicGlossMap;
            public MaterialProperty specGlossMap;
            public MaterialProperty smoothness;
            public MaterialProperty smoothnessMapChannel;
            public MaterialProperty bumpMapProp;
            public MaterialProperty bumpScaleProp;
            public MaterialProperty parallaxMapProp;
            public MaterialProperty parallaxScaleProp;
            public MaterialProperty occlusionStrength;
            public MaterialProperty occlusionMap;

            public MaterialProperty smaMap;

            public MaterialProperty smaRMAFlip;

            // Advanced Props
            public MaterialProperty highlights;
            public MaterialProperty reflections;

            public MaterialProperty clearCoat;  // Enable/Disable dummy property
            public MaterialProperty clearCoatMap;
            public MaterialProperty clearCoatMask;
            public MaterialProperty clearCoatSmoothness;

            public LitProperties(MaterialProperty[] properties)
            {
                // Surface Option Props
                workflowMode = BaseShaderGUI.FindProperty("_WorkflowMode", properties, false);
                // Surface Input Props
                metallic = BaseShaderGUI.FindProperty("_Metallic", properties, false);
                specColor = BaseShaderGUI.FindProperty("_SpecColor", properties, false);
                metallicGlossMap = BaseShaderGUI.FindProperty("_MetallicGlossMap", properties, false);
                specGlossMap = BaseShaderGUI.FindProperty("_SpecGlossMap", properties, false);
                smoothness = BaseShaderGUI.FindProperty("_Smoothness", properties, false);
                smoothnessMapChannel = BaseShaderGUI.FindProperty("_SmoothnessTextureChannel", properties, false);
                bumpMapProp = BaseShaderGUI.FindProperty("_BumpMap", properties, false);
                bumpScaleProp = BaseShaderGUI.FindProperty("_BumpScale", properties, false);
                parallaxMapProp = BaseShaderGUI.FindProperty("_ParallaxMap", properties, false);
                parallaxScaleProp = BaseShaderGUI.FindProperty("_Parallax", properties, false);
                occlusionStrength = BaseShaderGUI.FindProperty("_OcclusionStrength", properties, false);
                occlusionMap = BaseShaderGUI.FindProperty("_OcclusionMap", properties, false);
                // Advanced Props
                highlights = BaseShaderGUI.FindProperty("_SpecularHighlights", properties, false);
                reflections = BaseShaderGUI.FindProperty("_EnvironmentReflections", properties, false);

                clearCoat = BaseShaderGUI.FindProperty("_ClearCoat", properties, false);
                clearCoatMap = BaseShaderGUI.FindProperty("_ClearCoatMap", properties, false);
                clearCoatMask = BaseShaderGUI.FindProperty("_ClearCoatMask", properties, false);
                clearCoatSmoothness = BaseShaderGUI.FindProperty("_ClearCoatSmoothness", properties, false);

                smaMap = BaseShaderGUI.FindProperty("_SMAMap", properties, false);
                smaRMAFlip = BaseShaderGUI.FindProperty("_SMA_RMA_Flip", properties, false);
            }
        }

        public static void Inputs(LitProperties properties, MaterialEditor materialEditor, Material material)
        {
            DoMetallicSpecularArea(properties, materialEditor, material);
            DoSMAArea(properties, materialEditor, material);
            BaseShaderGUI.DrawNormalArea(materialEditor, properties.bumpMapProp, properties.bumpScaleProp);

            if (HeightmapAvailable(material))
                DoHeightmapArea(properties, materialEditor);

            if (properties.occlusionMap != null)
            {
                materialEditor.TexturePropertySingleLine(Styles.occlusionText, properties.occlusionMap,
                    properties.occlusionMap.textureValue != null ? properties.occlusionStrength : null);
            }

            // Check that we have all the required properties for clear coat,
            // otherwise we will get null ref exception from MaterialEditor GUI helpers.
            if (ClearCoatAvailable(material))
                DoClearCoat(properties, materialEditor, material);

            if (properties.smaMap != null)
                return;

            if (GUILayout.Button("Convert to SMA"))
            {
                Undo.RecordObject(material, "SMA Conversion");

                // Grab our metallic, smoothness, and occlusion maps
                Texture2D metallicGloss = properties.metallicGlossMap.textureValue as Texture2D;
                Texture2D occlusion = null;

                Texture2D albedo = material.mainTexture as Texture2D;

                if (properties.occlusionMap != null)
                    occlusion = properties.occlusionMap.textureValue as Texture2D;

                // GMA, Gloss, Metallic, AO
                int dimX = 0;
                int dimY = 0;

                if (metallicGloss != null)
                {
                    dimX = Mathf.Max(dimX, metallicGloss.width);
                    dimY = Mathf.Max(dimY, metallicGloss.height);
                }

                if (occlusion != null)
                {
                    dimX = Mathf.Max(dimX, occlusion.width);
                    dimY = Mathf.Max(dimY, occlusion.height);
                }

                if (albedo != null)
                {
                    dimX = Mathf.Max(dimX, albedo.width);
                    dimY = Mathf.Max(dimY, albedo.height);
                }

                if (dimX == 0 || dimY == 0)
                {
                    material.shader = Shader.Find("Strayed/Lit (SMA & RMA)");
                    return;
                }

                Texture2D DuplicateTexture(Texture2D source)
                {
                    if (source == null)
                        return null;

                    RenderTexture temp =
                        RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

                    RenderTexture lastActive = RenderTexture.active;

                    Graphics.Blit(source, temp);

                    RenderTexture.active = temp;

                    Texture2D ret = new Texture2D(source.width, source.height, TextureFormat.RGBAHalf, true, true);

                    ret.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                    ret.Apply();

                    RenderTexture.active = lastActive;

                    return ret;
                }

                Texture2D rmaTemp = new Texture2D(dimX, dimY, TextureFormat.RGBAHalf, true, true);

                Texture2D occlusionDupe = DuplicateTexture(occlusion);
                Texture2D metallicGlossDupe = DuplicateTexture(metallicGloss);

                Texture2D albedoDupe = DuplicateTexture(albedo);

                bool glossIsAlbedo = GetSmoothnessMapChannel(material) == SmoothnessMapChannel.AlbedoAlpha;
                Debug.Log($"[LitGUI::SMAConverter]: Gloss is Albedo? {glossIsAlbedo}");

                float perX = 1.0F / (dimX - 1);
                float perY = 1.0F / (dimY - 1);

                for (int x = 0; x < dimX; x++)
                {
                    float u = x * perX;

                    for (int y = 0; y < dimY; y++)
                    {
                        float v = y * perY;

                        Color color = Color.white;

                        bool setGloss = false;
                        bool setMetal = false;

                        if (metallicGlossDupe)
                        {
                            Color mg = Color.black;

                            if (metallicGlossDupe.width == dimX && metallicGlossDupe.height == dimY)
                                mg = metallicGlossDupe.GetPixel(x, y);
                            else
                                mg = metallicGlossDupe.GetPixelBilinear(u, v);

                            color.g = mg.r;
                            setMetal = true;

                            if (!glossIsAlbedo)
                            {
                                color.r = mg.a;
                                setGloss = true;
                            }
                        }

                        if (albedoDupe && glossIsAlbedo)
                        {
                            Color col = Color.black;

                            if (albedoDupe.width == dimX && albedoDupe.height == dimY)
                                col = albedoDupe.GetPixel(x, y);
                            else
                                col = albedoDupe.GetPixelBilinear(u, v);

                            color.r = col.a;
                            setGloss = true;
                        }

                        //if (properties.smoothness != null && !setGloss)
                        //    color.r = Mathf.LinearToGammaSpace(properties.smoothness.floatValue);

                        if (properties.metallic != null && !setMetal)
                            color.g = (properties.metallic.floatValue);

                        if (occlusionDupe)
                        {
                            Color ao = Color.black;

                            if (occlusionDupe.width == dimX && occlusionDupe.height == dimY)
                                ao = occlusionDupe.GetPixel(x, y);
                            else
                                ao = occlusionDupe.GetPixelBilinear(u, v);

                            color.b = ao.r;
                        }

                        rmaTemp.SetPixel(x, y, color);
                    }
                }

                rmaTemp.Apply();

                string rel = AssetDatabase.GetAssetPath(material);

                string stem = Path.GetDirectoryName(rel);
                string file = Path.GetFileNameWithoutExtension(rel);

                string path = Path.Combine(stem, $"{file}_SMA.png");

                using (var stream = File.Create(path))
                {
                    stream.Write(rmaTemp.EncodeToPNG());
                    stream.Flush();
                }

                AssetDatabase.ImportAsset(path);
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;


                if (importer != null)
                {
                    importer.sRGBTexture = false;
                    importer.streamingMipmaps = true;

                    var settings = importer.GetPlatformTextureSettings("Android");

                    settings.format = TextureImporterFormat.Automatic;
                    settings.maxTextureSize = 512;
                    settings.overridden = true;

                    importer.SetPlatformTextureSettings(settings);

                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                }

                material.shader = Shader.Find("Strayed/Lit (SMA & RMA)");
                material.SetTexture("_SMAMap", tex);

                EditorUtility.SetDirty(material);
            }
        }

        private static bool ClearCoatAvailable(Material material)
        {
            return material.HasProperty("_ClearCoat")
                && material.HasProperty("_ClearCoatMap")
                && material.HasProperty("_ClearCoatMask")
                && material.HasProperty("_ClearCoatSmoothness");
        }

        private static bool HeightmapAvailable(Material material)
        {
            return material.HasProperty("_Parallax")
                && material.HasProperty("_ParallaxMap");
        }

        private static void DoHeightmapArea(LitProperties properties, MaterialEditor materialEditor)
        {
            materialEditor.TexturePropertySingleLine(Styles.heightMapText, properties.parallaxMapProp,
                properties.parallaxMapProp.textureValue != null ? properties.parallaxScaleProp : null);
        }

        private static bool ClearCoatEnabled(Material material)
        {
            return material.HasProperty("_ClearCoat") && material.GetFloat("_ClearCoat") > 0.0;
        }

        public static void DoClearCoat(LitProperties properties, MaterialEditor materialEditor, Material material)
        {
            materialEditor.ShaderProperty(properties.clearCoat, Styles.clearCoatText);
            var coatEnabled = material.GetFloat("_ClearCoat") > 0.0;

            EditorGUI.BeginDisabledGroup(!coatEnabled);
            {
                materialEditor.TexturePropertySingleLine(Styles.clearCoatMaskText, properties.clearCoatMap, properties.clearCoatMask);

                EditorGUI.indentLevel += 2;

                // Texture and HDR color controls
                materialEditor.ShaderProperty(properties.clearCoatSmoothness, Styles.clearCoatSmoothnessText);

                EditorGUI.indentLevel -= 2;
            }
            EditorGUI.EndDisabledGroup();
        }

        public static void DoMetallicSpecularArea(LitProperties properties, MaterialEditor materialEditor, Material material)
        {
            if (properties.smaMap != null)
                return;

            string[] smoothnessChannelNames;
            bool hasGlossMap = false;
            if (properties.workflowMode == null ||
                (WorkflowMode)properties.workflowMode.floatValue == WorkflowMode.Metallic)
            {
                hasGlossMap = properties.metallicGlossMap.textureValue != null;
                smoothnessChannelNames = Styles.metallicSmoothnessChannelNames;
                materialEditor.TexturePropertySingleLine(Styles.metallicMapText, properties.metallicGlossMap,
                    hasGlossMap ? null : properties.metallic);
            }
            else
            {
                hasGlossMap = properties.specGlossMap.textureValue != null;
                smoothnessChannelNames = Styles.specularSmoothnessChannelNames;
                BaseShaderGUI.TextureColorProps(materialEditor, Styles.specularMapText, properties.specGlossMap,
                    hasGlossMap ? null : properties.specColor);
            }
            DoSmoothness(materialEditor, material, properties.smoothness, properties.smoothnessMapChannel, smoothnessChannelNames);
        }

        public static void DoSMAArea(LitProperties properties, MaterialEditor materialEditor, Material material)
        {
            if (properties.smaMap == null)
                return;

            materialEditor.TexturePropertySingleLine(Styles.smaMap, properties.smaMap);

            if (properties.smaMap.textureValue != null && properties.smaMap.textureValue is Texture2D tex2D)
            {
                string path = AssetDatabase.GetAssetPath(tex2D);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer != null && importer.sRGBTexture && GUILayout.Button("Set SMA/RMA to sRGB"))
                {
                    importer.sRGBTexture = false;
                    importer.streamingMipmaps = true;

                    var settings = importer.GetPlatformTextureSettings("Android");

                    settings.format = TextureImporterFormat.Automatic;
                    settings.maxTextureSize = 512;
                    settings.overridden = true;

                    importer.SetPlatformTextureSettings(settings);

                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                }
            }

            EditorGUI.indentLevel += 2;

            materialEditor.ShaderProperty(properties.smaRMAFlip, Styles.smaRMAFlip);

            materialEditor.ShaderProperty(properties.smoothness, Styles.smoothnessText);
            //materialEditor.ShaderProperty(properties.metallic, Styles.metallicText);
            materialEditor.ShaderProperty(properties.occlusionStrength, Styles.occlusionStrengthText);

            EditorGUI.indentLevel -= 2;
        }

        internal static bool IsOpaque(Material material)
        {
            bool opaque = true;
            if (material.HasProperty(Property.SurfaceType))
                opaque = ((BaseShaderGUI.SurfaceType)material.GetFloat(Property.SurfaceType) == BaseShaderGUI.SurfaceType.Opaque);
            return opaque;
        }

        public static void DoSmoothness(MaterialEditor materialEditor, Material material, MaterialProperty smoothness, MaterialProperty smoothnessMapChannel, string[] smoothnessChannelNames)
        {
            EditorGUI.indentLevel += 2;

            materialEditor.ShaderProperty(smoothness, Styles.smoothnessText);

            if (smoothnessMapChannel != null) // smoothness channel
            {
                var opaque = IsOpaque(material);
                EditorGUI.indentLevel++;
                EditorGUI.showMixedValue = smoothnessMapChannel.hasMixedValue;
                if (opaque)
                {
                    EditorGUI.BeginChangeCheck();
                    var smoothnessSource = (int)smoothnessMapChannel.floatValue;
                    smoothnessSource = EditorGUILayout.Popup(Styles.smoothnessMapChannelText, smoothnessSource, smoothnessChannelNames);
                    if (EditorGUI.EndChangeCheck())
                        smoothnessMapChannel.floatValue = smoothnessSource;
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.Popup(Styles.smoothnessMapChannelText, 0, smoothnessChannelNames);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUI.showMixedValue = false;
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel -= 2;
        }

        public static SmoothnessMapChannel GetSmoothnessMapChannel(Material material)
        {
            int ch = (int)material.GetFloat("_SmoothnessTextureChannel");
            if (ch == (int)SmoothnessMapChannel.AlbedoAlpha)
                return SmoothnessMapChannel.AlbedoAlpha;

            return SmoothnessMapChannel.SpecularMetallicAlpha;
        }

        // (shared by all lit shaders, including shadergraph Lit Target and Lit.shader)
        internal static void SetupSpecularWorkflowKeyword(Material material, out bool isSpecularWorkflow)
        {
            isSpecularWorkflow = false;     // default is metallic workflow
            if (material.HasProperty(Property.SpecularWorkflowMode))
                isSpecularWorkflow = ((WorkflowMode)material.GetFloat(Property.SpecularWorkflowMode)) == WorkflowMode.Specular;
            CoreUtils.SetKeyword(material, "_SPECULAR_SETUP", isSpecularWorkflow);
        }

        // setup keywords for Lit.shader
        public static void SetMaterialKeywords(Material material)
        {
            SetupSpecularWorkflowKeyword(material, out bool isSpecularWorkFlow);

            // Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
            // (MaterialProperty value might come from renderer material property block)
            var specularGlossMap = isSpecularWorkFlow ? "_SpecGlossMap" : "_MetallicGlossMap";
            var hasGlossMap = material.GetTexture(specularGlossMap) != null;

            CoreUtils.SetKeyword(material, "_METALLICSPECGLOSSMAP", hasGlossMap);

            if (material.HasProperty("_SMAMap"))
                CoreUtils.SetKeyword(material, "_SMA_COMBINED_MAP", material.GetTexture("_SMAMap"));

            if (material.HasProperty("_SMA_RMA_Flip"))
                CoreUtils.SetKeyword(material, "_SMA_RED_IS_ROUGHNESS",
                material.GetFloat("_SMA_RMA_Flip") == 1.0F);

            if (material.HasProperty("_SpecularHighlights"))
                CoreUtils.SetKeyword(material, "_SPECULARHIGHLIGHTS_OFF",
                    material.GetFloat("_SpecularHighlights") == 0.0f);
            if (material.HasProperty("_EnvironmentReflections"))
                CoreUtils.SetKeyword(material, "_ENVIRONMENTREFLECTIONS_OFF",
                    material.GetFloat("_EnvironmentReflections") == 0.0f);
            if (material.HasProperty("_OcclusionMap"))
                CoreUtils.SetKeyword(material, "_OCCLUSIONMAP", material.GetTexture("_OcclusionMap"));

            if (material.HasProperty("_ParallaxMap"))
            {
                bool enabled = material.GetTexture("_ParallaxMap");

                //if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                //    enabled = false;

                CoreUtils.SetKeyword(material, "_PARALLAXMAP", enabled);
            }

            if (material.HasProperty("_SmoothnessTextureChannel"))
            {
                var opaque = IsOpaque(material);
                CoreUtils.SetKeyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A",
                    GetSmoothnessMapChannel(material) == SmoothnessMapChannel.AlbedoAlpha && opaque);
            }

            // Clear coat keywords are independent to remove possiblity of invalid combinations.
            if (ClearCoatEnabled(material))
            {
                var hasMap = material.HasProperty("_ClearCoatMap") && material.GetTexture("_ClearCoatMap") != null;
                if (hasMap)
                {
                    CoreUtils.SetKeyword(material, "_CLEARCOAT", false);
                    CoreUtils.SetKeyword(material, "_CLEARCOATMAP", true);
                }
                else
                {
                    CoreUtils.SetKeyword(material, "_CLEARCOAT", true);
                    CoreUtils.SetKeyword(material, "_CLEARCOATMAP", false);
                }
            }
            else
            {
                CoreUtils.SetKeyword(material, "_CLEARCOAT", false);
                CoreUtils.SetKeyword(material, "_CLEARCOATMAP", false);
            }
        }
    }
}
