using UnityEngine;
using UnityEngine.Rendering;

namespace F89.Core
{
    /// <summary>
    /// Build-safe runtime materials. Never copy CreatePrimitive defaults — their shaders strip in players.
    /// </summary>
    public static class F89RenderMaterials
    {
        private const string UnlitMaterialPath = "F89_UrpUnlit";
        private const string TransparentUnlitMaterialPath = "F89_UrpUnlitTransparent";
        private const string SpriteMaterialPath = "F89_SpriteDefault";

        private static Material unlitTemplate;
        private static Material transparentUnlitTemplate;
        private static Material spriteTemplate;

        public static Material CreateUnlit(Color color)
        {
            var material = CreateFromTemplate(ref unlitTemplate, UnlitMaterialPath, CreateFallbackUnlit);
            if (material == null)
            {
                return null;
            }

            SetColor(material, color);
            return material;
        }

        public static Material CreateSpriteMaterial()
        {
            return CreateFromTemplate(ref spriteTemplate, SpriteMaterialPath, CreateFallbackSprite);
        }

        public static Material CreateUnlitTextured(Texture2D texture, Color color)
        {
            if (texture == null)
            {
                return null;
            }

            var material = CreateFromTemplate(ref unlitTemplate, UnlitMaterialPath, CreateFallbackUnlit);
            if (material == null)
            {
                return null;
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }
            else
            {
                material.mainTexture = texture;
            }

            SetColor(material, color);
            return material;
        }

        public static Material CreateUnlitTransparentTextured(Texture2D texture, Color color)
        {
            if (texture == null)
            {
                return null;
            }

            var material = CreateFromTemplate(
                ref transparentUnlitTemplate,
                TransparentUnlitMaterialPath,
                CreateFallbackTransparentUnlit);
            if (material == null)
            {
                return null;
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }
            else
            {
                material.mainTexture = texture;
            }

            SetColor(material, color);
            return material;
        }

        public static bool HasWorkingShader(Material material)
        {
            return material != null
                && material.shader != null
                && material.shader.name != "Hidden/InternalErrorShader";
        }

        private static Material CreateFromTemplate(
            ref Material template,
            string resourcePath,
            System.Func<Material> fallbackFactory)
        {
            if (template == null)
            {
                template = Resources.Load<Material>(resourcePath);
                if (!HasWorkingShader(template))
                {
                    template = fallbackFactory();
                }
            }

            return template != null ? new Material(template) : null;
        }

        private static Material CreateFallbackUnlit()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Standard");
            return shader != null ? new Material(shader) : null;
        }

        private static Material CreateFallbackSprite()
        {
            var shader = Shader.Find("Sprites/Default")
                ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            return shader != null ? new Material(shader) : null;
        }

        private static Material CreateFallbackTransparentUnlit()
        {
            var material = CreateFallbackUnlit();
            if (material != null)
            {
                ConfigureTransparentSurface(material);
            }

            return material;
        }

        private static void ConfigureTransparentSurface(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
        }

        private static void SetColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else
            {
                material.color = color;
            }
        }
    }

}
