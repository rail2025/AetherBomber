// AetherBomber/UI/TextureManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AetherBomber.UI;

public class TextureManager : IDisposable
{
    private readonly Dictionary<string, IDalamudTextureWrap> textures = new();
    private readonly List<IDalamudTextureWrap> backgroundTextures = new();

    public TextureManager()
    {
        LoadGameTextures();
        LoadBackgroundTextures();
    }

    private void LoadGameTextures()
    {
        var textureNames = new[] { "bomb", "chest", "mirror", "dps", "healer", "tank", "bird" };
        foreach (var name in textureNames)
        {
            var texture = LoadTextureFromResource($"AetherBomber.Images.{name}.png");
            if (texture != null)
            {
                this.textures[name] = texture;
            }
        }
    }

    private void LoadBackgroundTextures()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourcePathPrefix = "AetherBomber.Images.";
        var backgroundResourceNames = assembly.GetManifestResourceNames()
            .Where(r => r.StartsWith(resourcePathPrefix + "background") && r.EndsWith(".png"))
            .OrderBy(r => r)
            .ToList();

        foreach (var resourcePath in backgroundResourceNames)
        {
            var texture = LoadTextureFromResource(resourcePath);
            if (texture != null)
            {
                this.backgroundTextures.Add(texture);
            }
        }
    }

    private static IDalamudTextureWrap? LoadTextureFromResource(string path)
    {
        var assembly = Assembly.GetExecutingAssembly();
        try
        {
            using var stream = assembly.GetManifestResourceStream(path);
            if (stream == null)
            {
                Plugin.Log.Warning($"Texture resource not found at path: {path}");
                return null;
            }

            using var image = Image.Load<Rgba32>(stream);
            var rgbaBytes = new byte[image.Width * image.Height * 4];
            image.CopyPixelDataTo(rgbaBytes);
            return Plugin.TextureProvider.CreateFromRaw(RawImageSpecification.Rgba32(image.Width, image.Height), rgbaBytes);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, $"Failed to load texture: {path}");
            return null;
        }
    }

    public IDalamudTextureWrap? GetTexture(string name)
    {
        return this.textures.GetValueOrDefault(name);
    }

    public IDalamudTextureWrap? GetBackground(int index)
    {
        if (this.backgroundTextures.Count == 0) return null;
        return this.backgroundTextures[index % this.backgroundTextures.Count];
    }

    public int GetBackgroundCount() => this.backgroundTextures.Count;

    public void Dispose()
    {
        foreach (var texture in this.textures.Values) texture.Dispose();
        this.textures.Clear();
        foreach (var texture in this.backgroundTextures) texture.Dispose();
        this.backgroundTextures.Clear();
    }
}
