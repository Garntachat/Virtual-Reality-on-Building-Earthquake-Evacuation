using UnityEngine;

namespace ChulaEarthquakeVR
{
    public static class StudentAppearance
    {
        private const string Preference = "CEVR.StudentOutfit.v1";
        public static readonly string[] Names = { "Classic white uniform", "Burgundy engineering shirt", "Blue lab shirt" };
        public static int Selected => Mathf.Clamp(PlayerPrefs.GetInt(Preference, 0), 0, Names.Length - 1);
        public static Color ShirtColor(int index) => index == 1
            ? new Color(0.48f, 0.12f, 0.20f) : index == 2
            ? new Color(0.25f, 0.50f, 0.72f) : Color.white;

        public static void Select(int index)
        {
            PlayerPrefs.SetInt(Preference, Mathf.Clamp(index, 0, Names.Length - 1));
            PlayerPrefs.Save();
        }

        // Recolor only the shirt's neutral pixels in this model's existing UV atlas.
        // Face, hair, hands, shoes, trousers and animation bindings are unchanged.
        public static Texture2D CreateOutfitTexture(Texture2D source)
        {
            if (source == null || Selected == 0) return null;
            RenderTexture previous = RenderTexture.active;
            bool previousSrgbWrite = GL.sRGBWrite;
            RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Texture2D copy = null;
            try
            {
                GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, true, false);
                copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                Color[] pixels = copy.GetPixels();
                Color tint = ShirtColor(Selected);
                for (int y = 0; y < source.height; y++)
                for (int x = 0; x < source.width; x++)
                {
                    // Unity texture coordinates start at the lower left.
                    if ((float)x / source.width >= 0.599f || (float)y / source.height <= 0.03f ||
                        (float)y / source.height >= 0.52f) continue;
                    int i = y * source.width + x;
                    Color c = pixels[i];
                    if (Mathf.Min(c.r, Mathf.Min(c.g, c.b)) < 0.65f ||
                        Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b)) > 0.13f) continue;
                    pixels[i] = new Color(c.r * tint.r, c.g * tint.g, c.b * tint.b, c.a);
                }
                copy.SetPixels(pixels);
                copy.Apply(true, true);
                copy.name = Names[Selected];
                return copy;
            }
            catch
            {
                if (copy != null) Object.Destroy(copy);
                throw;
            }
            finally
            {
                RenderTexture.active = previous;
                GL.sRGBWrite = previousSrgbWrite;
                RenderTexture.ReleaseTemporary(temporary);
            }
        }
    }
}
