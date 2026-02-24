using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DepthPeelingRendererFeature : ScriptableRendererFeature
{
    public LayerMask depthPeelingLayerMask=-1;
    public int peelLayerCount=4;
    public Material depthPeelingInitialMat;
    public Material depthPeelingPeelingMat;
    public Material depthPeelingBlendMat;
    public Shader depthPeelingInitialShader;
        
    class DepthPeelingRenderPass : ScriptableRenderPass
    {
        
        LayerMask layerMask;
        Material initialMat;
        Material peelingMat;
        Material blendMat;
        int layers;
        RTHandle sourceColorRT;
        RTHandle sourceDepthRT;
        
        RTHandle[] colorRT=new RTHandle[4];
        RTHandle[] depthRT=new RTHandle[2];
        RTHandle peelDepthAttachment;
        RTHandle[] mrt=new RTHandle[2];
        RenderTargetIdentifier[] mrtid=new RenderTargetIdentifier[2];
        Shader initialShader;
        Shader peelingShader;
        Shader blendShader;
        
        public void SetUp(RTHandle color, RTHandle depth)
        {
            sourceColorRT = color;
            sourceDepthRT = depth;
        }

        public DepthPeelingRenderPass(LayerMask layermask,Material initialMaterial, Material peelingMaterial, Material blendMaterial,  int peelLayerCount, Shader DPPeelingShader, Shader blendShader=null)
        {
            layerMask = layermask;
            initialMat = initialMaterial;
            peelingMat = peelingMaterial;
            peelingShader=DPPeelingShader;
            blendMat = blendMaterial;
            layers = peelLayerCount;
            
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            mrt[0] = colorRT[0];
            mrt[1] = depthRT[0];
           
            
            ConfigureTarget(mrt,peelDepthAttachment);
            ConfigureClear(ClearFlag.All,new Color(0.0f,0.0f,0.0f,0.0f));
        }

        //var filteringSettings = new FilteringSettings(RenderQueueRange.all, layerMask);
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var colDesc= renderingData.cameraData.cameraTargetDescriptor;
            colDesc.graphicsFormat = GraphicsFormat.R16G16B16A16_UNorm;
            colDesc.depthBufferBits = 0;
            colDesc.msaaSamples = 1;
            colDesc.bindMS = false;
            
            var depthLayerDesc = colDesc;
            depthLayerDesc.graphicsFormat = GraphicsFormat.R16_UNorm;
            depthLayerDesc.depthBufferBits = 0;
            
            var peelDepthDesc = renderingData.cameraData.cameraTargetDescriptor;
            peelDepthDesc.graphicsFormat = GraphicsFormat.None;
            peelDepthDesc.depthBufferBits = 24;
            peelDepthDesc.msaaSamples = 1;
            peelDepthDesc.bindMS = false;
            
            //2 depthTex
            for (int i = 0; i < layers; i++)
            {
                RenderingUtils.ReAllocateIfNeeded(ref colorRT[i], colDesc, name:"DepthPeelingColorTex"+i);
            }
            RenderingUtils.ReAllocateIfNeeded(ref depthRT[0], depthLayerDesc, name:"DepthPeelingDepthTexture0");
            RenderingUtils.ReAllocateIfNeeded(ref depthRT[1], depthLayerDesc, name: "DepthPeelingDepthTexture1");
            RenderingUtils.ReAllocateIfNeeded(ref peelDepthAttachment, peelDepthDesc, name: "DepthPeelingDepthAttachment");

            //Configure(cmd, renderingData.cameraData.cameraTargetDescriptor);
            //Debug.Log(renderingData.cameraData.cameraTargetDescriptor.msaaSamples);
            //Debug.Log(renderingData.cameraData.cameraTargetDescriptor.dimension);
            
        }

       
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
            //------- Initial Pass: Render the scene to get the nearest depth layer
            CommandBuffer cmdInitial = CommandBufferPool.Get("DepthPeelingPass_Initial");
            
            var filteringSettings = new FilteringSettings(RenderQueueRange.all, layerMask);
            
            var shaderTagId = new ShaderTagId("DP_Peeling");
            var drawingSettings = CreateDrawingSettings(shaderTagId, ref renderingData, SortingCriteria.CommonTransparent);
            var renderStateBlock = new RenderStateBlock(RenderStateMask.Raster);
            drawingSettings.overrideMaterial= initialMat;

            cmdInitial.BeginSample("DepthPeeling_Initialize");
            context.ExecuteCommandBuffer(cmdInitial);
            cmdInitial.Clear();
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
               
            cmdInitial.EndSample("DepthPeeling_Initialize");            
            //Blitter.BlitCameraTexture(cmd,colorRT[0], cameraTarget);
            
            context.ExecuteCommandBuffer(cmdInitial);
            CommandBufferPool.Release(cmdInitial);
            
            //---------------- Peeling Passes---------------------
            var renderer = renderingData.cameraData.renderer;
            var cameraTarget = renderer.cameraColorTargetHandle;
            var depthTarget = renderer.cameraDepthTargetHandle;
            sourceColorRT = cameraTarget;
            sourceDepthRT = depthTarget;
            
            RenderTargetIdentifier depthID = sourceDepthRT;
            //drawingSettings.overrideShader= peelingShader;
            drawingSettings.overrideMaterial = peelingMat;
            for (int i = 1; i < layers; i++)
            {
                CommandBuffer cmdPeeling = CommandBufferPool.Get("DepthPeelingPass_Peeling"+i);
                
                mrtid[0]= colorRT[i].nameID;
                mrtid[1]= depthRT[i%2].nameID;//i=1,tex1; i=2,tex0; i=3,tex1
                
                //CoreUtils.SetRenderTarget(cmdPeeling, mrtid[0]);
                //CoreUtils.ClearRenderTarget(cmdPeeling, ClearFlag.Color, new Color(0.0f,0.0f,0.0f,0.0f));
                //CoreUtils.SetRenderTarget(cmdPeeling, mrtid[1]);
                //CoreUtils.ClearRenderTarget(cmdPeeling, ClearFlag.Color, new Color(1.0f,1.0f,1.0f,1.0f));
                
                //CoreUtils.SetRenderTarget(cmdPeeling, mrtid[0]);
                //CoreUtils.SetRenderTarget(cmdPeeling,mrtid,depthRT[1-i%2]);
                cmdPeeling.SetRenderTarget(mrtid, peelDepthAttachment.nameID);
                CoreUtils.ClearRenderTarget(cmdPeeling,ClearFlag.All, new Color(0.0f,0.0f,0.0f,0.0f));
                
                //peelingMat.SetTexture("_PrevDepthTex", depthRT[1 - i % 2]);//i=1,tex0; i=2,tex1; i=3,tex0
                cmdPeeling.SetGlobalTexture("_PrevDepthTex",depthRT[1-i%2]);
                
                
                string sampleName = "DepthPeeling_PeelingPass" + i;
                cmdPeeling.BeginSample(sampleName);
                
                context.ExecuteCommandBuffer(cmdPeeling);
                cmdPeeling.Clear();
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
                
                cmdPeeling.EndSample(sampleName);
                
                context.ExecuteCommandBuffer(cmdPeeling);
                CommandBufferPool.Release(cmdPeeling);
                
            }
            
            //-------------- blend Passe----------------------
            CommandBuffer cmdBlend = CommandBufferPool.Get("DepthPeelingPass_Blend");
            cmdBlend.BeginSample("DepthPeeling_BlendPass");
            context.ExecuteCommandBuffer(cmdBlend);
            cmdBlend.Clear(); 
                // //Blitter.BlitCameraTexture(cmdBlend,sourceColorRT, depthRT[0]);
                // cmdBlend.SetGlobalTexture("_LayerColorTex", colorRT[0]); 
                // cmdBlend.SetGlobalTexture("_DPAccumTex", depthRT[0]);
                // CoreUtils.SetRenderTarget(cmdBlend, sourceColorRT);
                // cmdBlend.DrawProcedural(Matrix4x4.identity, blendMat, 0, MeshTopology.Triangles, 3, 1);
                // //Blitter.BlitCameraTexture(cmdBlend, colorRT[3],sourceColorRT);
            CoreUtils.SetRenderTarget(cmdBlend, sourceColorRT);
            for (int i = 3; i >= 0; i--)
            { 
                cmdBlend.SetGlobalTexture("_LayerColorTex", colorRT[i]);
                cmdBlend.DrawProcedural(Matrix4x4.identity, blendMat, 0, MeshTopology.Triangles, 3, 1);
            }
            
            cmdBlend.EndSample("DepthPeeling_BlendPass");
            
            context.ExecuteCommandBuffer(cmdBlend);
            CommandBufferPool.Release(cmdBlend);
        }

      
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
        
        public void Dispose()
        {
            for (int i = 0; i < layers; i++)
            {
                colorRT[i]?.Release();
            }
            depthRT[0]?.Release();
            depthRT[1]?.Release();
            peelDepthAttachment?.Release();
            //mrt[0]?.Release();
            //mrt[1]?.Release();
          
        }
    }

    DepthPeelingRenderPass m_ScriptablePass;
    
    public override void Create()
    {
        m_ScriptablePass = new DepthPeelingRenderPass(depthPeelingLayerMask, depthPeelingInitialMat,
            depthPeelingPeelingMat, depthPeelingBlendMat, peelLayerCount, depthPeelingInitialShader,
            depthPeelingBlendMat.shader);
        m_ScriptablePass.ConfigureInput(ScriptableRenderPassInput.Color);
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        //var cameraTarget = renderer.cameraColorTargetHandle;
        //var depthTarget = renderer.cameraDepthTargetHandle;
        //m_ScriptablePass.SetUp(cameraTarget, depthTarget);
        renderer.EnqueuePass(m_ScriptablePass);
    }
    
    protected override void Dispose(bool disposing)
    {
        m_ScriptablePass.Dispose();
    }
    
}


