using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PPLL_Feature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public ComputeShader clearShader;
        public Shader buildShader;
        public Shader resolveShader;
    }

    public Settings settings = new Settings();
    PPLLPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new PPLLPass(settings);
        // 渲染时机：在透明物体之后
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.clearShader != null && settings.buildShader != null && settings.resolveShader != null)
        {
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        m_ScriptablePass?.Dispose();
    }

    class PPLLPass : ScriptableRenderPass
    {
        Settings settings;
        Material resolveMaterial;
        
        // Buffers
        ComputeBuffer startOffetBuffer; 
        ComputeBuffer fragLinkedBuffer;

        // 临时 RTHandle
        RTHandle blitTempRT;
        
        // IDs
        int startOffsetBufferID = Shader.PropertyToID("startOffetBuffer");
        int fragLinkedBufferID = Shader.PropertyToID("fragLinkedBuffer");
        int screenWidthID = Shader.PropertyToID("screenWidth");
        int blitRT_ID = Shader.PropertyToID("_PPLL_BlitRT"); 

        ShaderTagId shaderTagId = new ShaderTagId("PerPixelLinkedList");

        public PPLLPass(Settings settings)
        {
            this.settings = settings;
            if (settings.resolveShader) resolveMaterial = CoreUtils.CreateEngineMaterial(settings.resolveShader);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            int width = desc.width;
            int height = desc.height;

            // --- 1. Buffer 初始化 ---
            int maxSortedPixels = 16;
            int bufferSize = width * height * maxSortedPixels;
            int bufferStride = 12; 

            if (fragLinkedBuffer == null || fragLinkedBuffer.count != bufferSize)
            {
                if (fragLinkedBuffer != null) fragLinkedBuffer.Release();
                fragLinkedBuffer = new ComputeBuffer(bufferSize, bufferStride, ComputeBufferType.Counter);
            }

            if (startOffetBuffer == null || startOffetBuffer.count != width * height)
            {
                if (startOffetBuffer != null) startOffetBuffer.Release();
                startOffetBuffer = new ComputeBuffer(width * height, 4, ComputeBufferType.Raw);
            }

            // --- 2. Clear Buffer ---
            int kernel = settings.clearShader.FindKernel("ClearStartOffset");
            cmd.SetComputeBufferParam(settings.clearShader, kernel, startOffsetBufferID, startOffetBuffer);
            cmd.SetComputeIntParam(settings.clearShader, screenWidthID, width);
            
            int groupX = Mathf.CeilToInt(width / 32.0f);
            int groupY = Mathf.CeilToInt(height / 32.0f);
            cmd.DispatchCompute(settings.clearShader, kernel, groupX, groupY, 1);

            // --- 3. 绑定全局 Buffer ---
            cmd.SetGlobalBuffer(startOffsetBufferID, startOffetBuffer);
            cmd.SetGlobalBuffer(fragLinkedBufferID, fragLinkedBuffer);
            cmd.SetBufferCounterValue(fragLinkedBuffer, 0);

            // --- 4. 【关键修复】初始化临时 RT ---
            desc.depthBufferBits = 0; // 不需要深度
            desc.msaaSamples = 1;     // 【关键】强制关闭 MSAA，否则无法作为 Shader 纹理输入
            
            RenderingUtils.ReAllocateIfNeeded(ref blitTempRT, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_PPLL_BlitRT");

            ConfigureTarget(renderingData.cameraData.renderer.cameraColorTargetHandle, renderingData.cameraData.renderer.cameraDepthTargetHandle);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (resolveMaterial == null) return;

            // 获取当前屏幕颜色目标
            RTHandle sourceColor = renderingData.cameraData.renderer.cameraColorTargetHandle;

            // 安全检查：防止 Assertion Failed
            if (sourceColor == null || sourceColor.rt == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("PPLL Pass");
            
            // --- Pass 1: Build (绘制透明物体) ---
            using (new ProfilingScope(cmd, new ProfilingSampler("DrawPPLL")))
            {
                var drawSettings = CreateDrawingSettings(shaderTagId, ref renderingData, SortingCriteria.CommonTransparent);
                var filterSettings = new FilteringSettings(RenderQueueRange.transparent);
                
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);
            }

            // --- Pass 2: Resolve (Blit) ---
            using (new ProfilingScope(cmd, new ProfilingSampler("PPLLBlitColor")))
            {
                // 【关键修复 1】使用 cmd.Blit 替代 Blitter.BlitCameraTexture
                // 这能避免 "Assertion failed" 错误，因为它不进行严格的管线状态检查
                // 作用：把当前屏幕 (sourceColor) 拷贝到临时 RT (blitTempRT)
                cmd.Blit(sourceColor, blitTempRT);
                
                // 将含有背景的 RT 传给 Shader
                cmd.SetGlobalTexture(blitRT_ID, blitTempRT);

                // 绑定 Buffer
                resolveMaterial.SetBuffer(fragLinkedBufferID, fragLinkedBuffer);
                resolveMaterial.SetBuffer(startOffsetBufferID, startOffetBuffer);
                
                // 【关键修复 2】绘制全屏 Quad
                // source: null (因为 Shader 里用的不是 _MainTex，而是 _PPLL_BlitRT)
                // dest: sourceColor (写回屏幕)
                // material: resolveMaterial
                cmd.Blit(null, sourceColor, resolveMaterial);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            startOffetBuffer?.Release();
            fragLinkedBuffer?.Release();
            blitTempRT?.Release(); 
            CoreUtils.Destroy(resolveMaterial);
        }
    }
}