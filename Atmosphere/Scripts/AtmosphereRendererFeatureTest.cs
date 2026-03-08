using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using MelonLoader; // Added for debug logging

public class AtmosphereRendererFeatureTest : ScriptableRendererFeature
{
    // Expose the active instance so other systems (Main) can set the shader after load
    public static AtmosphereRendererFeatureTest Instance;
    

    // The class that previously lived in frameData. 
    // Since older URP versions don't have ContextContainer, we manage this instance in the Feature.
    public class BlitData : IDisposable
    {
        RTHandle m_TextureFront;
        RTHandle m_TextureBack;

        // Current active texture (wrapper for RTHandle instead of TextureHandle)
        public RTHandle texture;

        static Vector4 scaleBias = new Vector4(1f, 1f, 0f, 0f);
        bool m_IsFront = true;

        public void Init(RenderTextureDescriptor targetDescriptor, string textureName = null)
        {
            var texName = String.IsNullOrEmpty(textureName) ? "_BlitTextureData" : textureName;

            // Ensure descriptor is compatible
            targetDescriptor.msaaSamples = 1;
            targetDescriptor.depthBufferBits = 0;

            MelonLogger.Msg($"[BlitData] Init called with descriptor: {targetDescriptor.width}x{targetDescriptor.height}");

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
            MelonLogger.Msg($"[BlitData] Textures allocated, front valid: {m_TextureFront?.rt != null}, back valid: {m_TextureBack?.rt != null}");
        }

        public void Reset()
        {
            m_IsFront = true;
            if(m_TextureFront != null) texture = m_TextureFront;
            MelonLogger.Msg($"[BlitData] Reset, texture set to front, isFront=true");
        }

        // Replaces RecordBlitColor - Copies Camera Source to Internal Texture
        public void ExecuteBlitColor(CommandBuffer cmd, RTHandle source)
        {
            if (texture == null || texture.rt == null)
            {
                MelonLogger.Error("[BlitData] ExecuteBlitColor skipped: texture or its RT is null");
                return;
            }

            MelonLogger.Msg("[BlitData] ExecuteBlitColor: copying source to internal texture");
            // Blit source to internal texture
            Blitter.BlitCameraTexture(cmd, source, texture);
        }

        // Replaces RecordBlitBackToColor - Copies Internal Texture back to Camera
        public void ExecuteBlitBackToColor(CommandBuffer cmd, RTHandle destination)
        {
            if (texture == null || texture.rt == null)
            {
                MelonLogger.Error("[BlitData] ExecuteBlitBackToColor skipped: texture or its RT is null");
                return;
            }

            MelonLogger.Msg("[BlitData] ExecuteBlitBackToColor: copying internal texture to destination");
            // Blit internal texture to destination
            Blitter.BlitCameraTexture(cmd, texture, destination);
        }

        // Replaces RecordFullScreenPass - Blits internal Front to Back (or vice versa) with material
        public void ExecuteFullScreenPass(CommandBuffer cmd, Shader shader, AtmosphereEffect effect)
        {
            if (texture == null || texture.rt == null || shader == null)
            {
                MelonLogger.Error($"[BlitData] ExecuteFullScreenPass failed: textureValid={texture?.rt != null}, shaderValid={shader != null}, effect={effect != null}");
                if (effect == null) MelonLogger.Error("[BlitData] effect is null!");
                return;
            }

            MelonLogger.Msg($"[BlitData] ExecuteFullScreenPass for effect on {effect.gameObject.name}");

            m_IsFront = !m_IsFront;

            var source = texture;
            var destination = m_IsFront ? m_TextureFront : m_TextureBack;
            var material = effect.GetMaterial(shader);

            if (material == null)
            {
                MelonLogger.Error($"[BlitData] material is null after GetMaterial for {effect.gameObject.name}");
                Blitter.BlitCameraTexture(cmd, source, destination);
            }
            else
            {
                MelonLogger.Msg($"[BlitData] material created, shader: {material.shader?.name}");
                Blitter.BlitCameraTexture(cmd, source, destination, material, 0);
            }

            texture = destination;
        }

        public void Dispose()
        {
            MelonLogger.Msg("[BlitData] Dispose called, releasing textures");
            m_TextureFront?.Release();
            m_TextureBack?.Release();
        }
    }

    // --- RENDER PASSES ---

    public class BlitStartRenderPass : ScriptableRenderPass
    {
        private BlitData m_BlitData;
        
        public void Setup(BlitData blitData)
        {
            m_BlitData = blitData;
            MelonLogger.Msg("[BlitStartRenderPass] Setup completed");
        }

        // Replaces RecordRenderGraph
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_BlitData == null)
            {
                MelonLogger.Error("[BlitStartRenderPass] Execute skipped: m_BlitData is null");
                return;
            }

            MelonLogger.Msg("[BlitStartRenderPass] Execute started");
            var cmd = CommandBufferPool.Get("BlitBeforeAtmosphere");
            var cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            
            // Initialize data if needed (Logic moved from RecordBlitColor)
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            m_BlitData.Init(descriptor);

            // Execute the copy
            m_BlitData.ExecuteBlitColor(cmd, cameraTarget);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            MelonLogger.Msg("[BlitStartRenderPass] Execute completed");
        }
    }

    public class AtmosphereRenderPassTest : ScriptableRenderPass
    {
        private BlitData m_BlitData;
        private Shader m_AtmosphereShader;

        struct SortedEffect
        {
            public AtmosphereEffect effect;
            public float distanceToEffect;
        }

        public AtmosphereRenderPassTest(Shader atmosphereShader)
        {
            m_AtmosphereShader = atmosphereShader;
            MelonLogger.Msg($"[AtmosphereRenderPassTest] Constructor called, shader is {(atmosphereShader == null ? "null" : "assigned")}");
        }

        // Allow setting the shader after construction (for cases where the shader is loaded later)
        public void SetShader(Shader shader)
        {
            m_AtmosphereShader = shader;
            MelonLogger.Msg($"[AtmosphereRenderPassTest] SetShader called, shader is {(shader == null ? "null" : shader.name)}");
        }

        public void Setup(BlitData blitData)
        {
            m_BlitData = blitData;
            MelonLogger.Msg("[AtmosphereRenderPassTest] Setup completed");
        }

        private static readonly List<AtmosphereEffect> currentActiveEffects = new();
        public static void RegisterEffect(AtmosphereEffect effect)
        {
            if (!currentActiveEffects.Contains(effect))
            {
                currentActiveEffects.Add(effect);
                MelonLogger.Msg($"[AtmosphereRenderPassTest] Registered effect on {effect.gameObject.name}");
            }
        }
        public static void RemoveEffect(AtmosphereEffect effect)
        {
            if (currentActiveEffects.Remove(effect))
                MelonLogger.Msg($"[AtmosphereRenderPassTest] Removed effect on {effect.gameObject.name}");
        }

        private readonly List<SortedEffect> visibleEffects = new();
        
        private void CullAndSortEffects(Camera camera)
        {
            visibleEffects.Clear();
            var cameraPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
            Vector3 viewPos = camera.transform.position;

            for (int i = currentActiveEffects.Count - 1; i >= 0; i--)
            {
                if (currentActiveEffects[i] == null)
                {
                    currentActiveEffects.RemoveAt(i);
                    continue;
                }
                
                List<Plane> cameraPlaneList = new List<Plane>(cameraPlanes);

                if (currentActiveEffects[i].IsVisible(camera))
                {
                    float dstToSurface = currentActiveEffects[i].DistToAtmosphere(viewPos);
                    visibleEffects.Add(new SortedEffect { effect = currentActiveEffects[i], distanceToEffect = dstToSurface });
                }
            }

            // Bubble sort
            for (int i = 0; i < visibleEffects.Count - 1; i++)
            {
                for (int j = i + 1; j > 0; j--)
                {
                    if (visibleEffects[j - 1].distanceToEffect < visibleEffects[j].distanceToEffect)
                    {
                        (visibleEffects[j], visibleEffects[j - 1]) = (visibleEffects[j - 1], visibleEffects[j]);
                    }
                }
            }

            MelonLogger.Msg($"[AtmosphereRenderPassTest] CullAndSortEffects: {visibleEffects.Count} visible effects");
        }

        // Replaces RecordRenderGraph
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_BlitData == null)
            {
                MelonLogger.Error("[AtmosphereRenderPassTest] Execute skipped: m_BlitData is null");
                return;
            }
            if (m_AtmosphereShader == null)
            {
                MelonLogger.Error("[AtmosphereRenderPassTest] Execute skipped: m_AtmosphereShader is null");
                return;
            }
            if (!renderingData.cameraData.postProcessEnabled)
            {
                MelonLogger.Warning("[AtmosphereRenderPassTest] Execute skipped: post-processing disabled on camera");
                return;
            }

            MelonLogger.Msg("[AtmosphereRenderPassTest] Execute started");

            CullAndSortEffects(renderingData.cameraData.camera);

            if (visibleEffects.Count == 0)
            {
                MelonLogger.Msg("[AtmosphereRenderPassTest] No visible effects, skipping pass");
                return;
            }

            var cmd = CommandBufferPool.Get("Blit Atmosphere Pass");

            for (int i = 0; i < visibleEffects.Count; i++)
            {
                var effect = visibleEffects[i].effect;
                MelonLogger.Msg($"[AtmosphereRenderPassTest] Processing effect {i+1}/{visibleEffects.Count} on {effect.gameObject.name}");
                // Execute the blit for this effect
                m_BlitData.ExecuteFullScreenPass(cmd, m_AtmosphereShader, effect);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            MelonLogger.Msg("[AtmosphereRenderPassTest] Execute completed");
        }
    }

    public class BlitEndRenderPass : ScriptableRenderPass
    {
        private BlitData m_BlitData;

        public void Setup(BlitData blitData)
        {
            m_BlitData = blitData;
            MelonLogger.Msg("[BlitEndRenderPass] Setup completed");
        }

        // Replaces RecordRenderGraph
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_BlitData == null)
            {
                MelonLogger.Error("[BlitEndRenderPass] Execute skipped: m_BlitData is null");
                return;
            }

            MelonLogger.Msg("[BlitEndRenderPass] Execute started");
            var cmd = CommandBufferPool.Get("BlitBackToColorPass");
            var cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

            // Execute copy back
            m_BlitData.ExecuteBlitBackToColor(cmd, cameraTarget);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            MelonLogger.Msg("[BlitEndRenderPass] Execute completed");
        }
    }

    // --- MAIN FEATURE CLASS ---

    private Shader atmosphereShader;
    
    // We instantiate BlitData here because ContextContainer does not exist in older URP
    private BlitData m_SharedBlitData;

    BlitStartRenderPass m_StartPass;
    AtmosphereRenderPassTest m_BlitPass;
    BlitEndRenderPass m_EndPass;

    public override void Create()
    {
        MelonLogger.Msg("[AtmosphereRendererFeatureTest] Create() called");

        if (m_SharedBlitData == null) m_SharedBlitData = new BlitData();

        m_StartPass = new BlitStartRenderPass();
        m_BlitPass = new AtmosphereRenderPassTest(atmosphereShader);
        m_EndPass = new BlitEndRenderPass();

        m_StartPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        m_BlitPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        m_EndPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;

        // Expose instance so runtime code can inject shader after load
        Instance = this;
        MelonLogger.Msg("[AtmosphereRendererFeatureTest] Instance set");

        // If Main has already loaded the shader, forward it to the pass immediately
        try
        {
            //var shader = OuterWildsRumble.Main.atmosphereShader;
            //if (shader != null) //REMOVE COMMENTS
            //{
            //    SetAtmosphereShader(shader);
            //}
            //else
            //{
            //    MelonLogger.Msg("[AtmosphereRendererFeatureTest] No atmosphere shader present on Create()");
            //}
        }
        catch (Exception e)
        {
            MelonLogger.Msg($"[AtmosphereRendererFeatureTest] Exception while trying to fetch Main.atmosphereShader: {e.Message}");
        }

        MelonLogger.Msg($"[AtmosphereRendererFeatureTest] Create completed. atmosphereShader is {(atmosphereShader == null ? "null" : "assigned")}");
    }
    
    public void SetAtmosphereShader(Shader shader)
    {
        atmosphereShader = shader;
        MelonLogger.Msg($"[AtmosphereRendererFeatureTest] Atmosphere shader set to: {shader?.name}");

        // Forward to internal pass if created
        if (m_BlitPass != null)
        {
            m_BlitPass.SetShader(shader);
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        MelonLogger.Msg("[AtmosphereRendererFeatureTest] AddRenderPasses called");

        if (m_SharedBlitData == null)
        {
            MelonLogger.Error("[AtmosphereRendererFeatureTest] m_SharedBlitData is null!");
            return;
        }

        m_SharedBlitData.Reset();
        
        m_StartPass.Setup(m_SharedBlitData);
        m_BlitPass.Setup(m_SharedBlitData);
        m_EndPass.Setup(m_SharedBlitData);

        renderer.EnqueuePass(m_StartPass);
        renderer.EnqueuePass(m_BlitPass);
        renderer.EnqueuePass(m_EndPass);

        MelonLogger.Msg("[AtmosphereRendererFeatureTest] Passes enqueued");
    }

    protected void OnDisable()
    {
        MelonLogger.Msg("[AtmosphereRendererFeatureTest] OnDisable called, disposing BlitData");
        m_SharedBlitData?.Dispose();
    }
}

