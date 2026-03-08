using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AtmosphereBlitData : System.IDisposable
{
    RTHandle m_TextureFront;
    RTHandle m_TextureBack;

    public RTHandle texture;
    bool m_IsFront = true;

    public void Init(RenderTextureDescriptor targetDescriptor, string textureName = null)
    {
        var texName = string.IsNullOrEmpty(textureName) ? "_BlitTextureData" : textureName;

        targetDescriptor.msaaSamples = 1;
        targetDescriptor.depthBufferBits = 0;

        RenderingUtils.ReAllocateHandleIfNeeded(
            ref m_TextureFront,
            ref targetDescriptor,
            FilterMode.Bilinear,
            TextureWrapMode.Clamp,
            1,
            0f,
            texName + "Front"
        );

        RenderingUtils.ReAllocateHandleIfNeeded(
            ref m_TextureBack,
            ref targetDescriptor,
            FilterMode.Bilinear,
            TextureWrapMode.Clamp,
            1,
            0f,
            texName + "Back"
        );

        texture = m_TextureFront;
    }

    public void Reset()
    {
        m_IsFront = true;
        texture = m_TextureFront;
    }

    public void ExecuteBlitColor(CommandBuffer cmd, RTHandle source)
    {
        Blitter.BlitCameraTexture(cmd, source, texture);
    }

    public void ExecuteBlitBackToColor(CommandBuffer cmd, RTHandle destination)
    {
        Blitter.BlitCameraTexture(cmd, texture, destination);
    }

    public void ExecuteFullScreenPass(CommandBuffer cmd, Shader shader, AtmosphereEffect effect)
    {
        m_IsFront = !m_IsFront;

        var source = texture;
        var destination = m_IsFront ? m_TextureFront : m_TextureBack;

        var material = effect.GetMaterial(shader);

        if (material == null)
            Blitter.BlitCameraTexture(cmd, source, destination);
        else
            Blitter.BlitCameraTexture(cmd, source, destination, material, 0);

        texture = destination;
    }

    public void Dispose()
    {
        m_TextureFront?.Release();
        m_TextureBack?.Release();
    }
}