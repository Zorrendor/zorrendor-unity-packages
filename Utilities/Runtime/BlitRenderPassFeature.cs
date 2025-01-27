using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using System.Collections.Generic;

public static class GraphicsBlitURP
{
    public class BlitPass
    {
        public RTHandle target;
        public Rect uv;
        public Material material;
        public bool clearColor;
        public bool changeViewPort;
    }
    
    private static readonly Queue<BlitPass> BlitPasses = new Queue<BlitPass>();
    
    public static int PassCount => BlitPasses.Count;
    public static BlitPass DequeuePass() => BlitPasses.Dequeue();

    public static void Blit(RTHandle destination, Material material, Rect uv)
    {
        BlitPass blitPass = new BlitPass()
        {
            target = destination,
            material = material,
            uv = uv,
            changeViewPort = true
        };
        BlitPasses.Enqueue(blitPass);
    }
    
    public static void Blit(RTHandle destination, Material material)
    {
        BlitPass blitPass = new BlitPass()
        {
            target = destination,
            material = material
        };
        BlitPasses.Enqueue(blitPass);
    }

    public static void ClearColor(RTHandle destination, bool clearColor)
    {
        BlitPass blitPass = new BlitPass()
        {
            target = destination,
            clearColor = clearColor
        };
        BlitPasses.Enqueue(blitPass);
    }
}

public class BlitRenderPassFeature : ScriptableRendererFeature
{
    [SerializeField] RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    private BlitRenderPass scriptablePass;
    
    public override void Create()
    {
        scriptablePass = new BlitRenderPass
        {
            renderPassEvent = renderPassEvent
        };
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        for (int i = 0; i < GraphicsBlitURP.PassCount; i++)
        {
            renderer.EnqueuePass(scriptablePass);
        }
    }
    
    private class BlitRenderPass : ScriptableRenderPass
    {
        const string PassName = "BlitRenderPass";
        private TextureHandle destination;
        
        private class PassData { }
        
        static void ExecutePass(GraphicsBlitURP.BlitPass blitPass, RasterGraphContext context)
        {
            if (blitPass.clearColor)
            {
                context.cmd.ClearRenderTarget(false, true, Color.white);
                return;
            }
            if (blitPass.changeViewPort)
            {
                context.cmd.SetViewport(blitPass.uv);
            }
            context.cmd.DrawProcedural(Matrix4x4.identity, blitPass.material, 0, MeshTopology.Triangles, 3, 1);
        }
        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (GraphicsBlitURP.PassCount == 0)
            {
                Debug.LogError("Something wrong with blit passes");
                return;
            }
            
            using var builder = renderGraph.AddRasterRenderPass<PassData>(PassName, out _);

            var blitPass = GraphicsBlitURP.DequeuePass();
            
            destination = renderGraph.ImportTexture(blitPass.target);
            builder.SetRenderAttachment(destination, 0);
            
            builder.SetRenderFunc((PassData _, RasterGraphContext context) => ExecutePass(blitPass, context));
        }
    }
}
