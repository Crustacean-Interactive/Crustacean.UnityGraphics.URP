using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    public class ComboLitShader : LitShader
    {
        protected override uint materialFilter => (uint)(((Expandable)base.materialFilter & ~(Expandable.SurfaceInputs)) | Expandable.BakedSurfaceInputs | Expandable.PBRSurfaceInputs);

        protected SimpleLitGUI.SimpleLitProperties simpleLitProperties;

        protected MaterialProperty smaBaseMapProp { get; set; }
        protected MaterialProperty bakedBaseMapProp { get; set; }

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);

            simpleLitProperties = new SimpleLitGUI.SimpleLitProperties(properties);

            smaBaseMapProp = FindProperty("_SMABaseMap", properties, true);
            bakedBaseMapProp = FindProperty("_BakedBaseMap", properties, true);
        }

        public override void ValidateMaterial(Material material)
        {
            base.ValidateMaterial(material);
            SetMaterialKeywords(material, SimpleLitGUI.SetMaterialKeywords);
        }

        public override void DrawSurfaceOptions(Material material)
        {
            base.DrawSurfaceOptions(material);

        }

        public override void DrawPBRSurfaceInputs(Material material)
        {
            base.DrawPBRSurfaceInputs(material);

            if (smaBaseMapProp != null && baseColorProp != null) // Draw the baseMap, most shader will have at least a baseMap
            {
                if (baseMapProp != null && bakedBaseMapProp.textureValue == null)
                    smaBaseMapProp.textureValue = baseMapProp.textureValue;

                materialEditor.TexturePropertySingleLine(Styles.smaBaseMap, smaBaseMapProp, baseColorProp);
            }

            LitGUI.Inputs(litProperties, materialEditor, material);
            DrawEmissionProperties(material, true);
            DrawTileOffset(materialEditor, baseMapProp);
        }

        public override void DrawBakedSurfaceInputs(Material material)
        {
            base.DrawBakedSurfaceInputs(material);

            if (bakedBaseMapProp != null && baseColorProp != null) // Draw the baseMap, most shader will have at least a baseMap
            {
                materialEditor.TexturePropertySingleLine(Styles.bakedBaseMap, bakedBaseMapProp, baseColorProp);
            }

            SimpleLitGUI.Inputs(simpleLitProperties, materialEditor, material);
        }

        public override void DrawAdvancedOptions(Material material)
        {
            SimpleLitGUI.Advanced(simpleLitProperties);

            bool autoQueueControl = GetAutomaticQueueControlSetting(material);
            if (autoQueueControl)
                DrawQueueOffsetField();
            materialEditor.EnableInstancingField();
        }
    }
}
