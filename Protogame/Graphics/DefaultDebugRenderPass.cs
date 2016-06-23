﻿using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Protogame
{
    public class DefaultDebugRenderPass : IDebugRenderPass
    {
        private RasterizerState _oldRasterizerState;

        private RasterizerState _debugRasterizerState;

        private DepthStencilState _oldDepthState;

        private DepthStencilState _debugDepthState;

        private EffectAsset _basicEffect;

        public DefaultDebugRenderPass(IAssetManagerProvider assetManagerProvider)
        {
            _basicEffect = assetManagerProvider.GetAssetManager().Get<EffectAsset>("effect.Color");
            Lines = new List<VertexPositionNormalColor>();
            Triangles = new List<VertexPositionNormalColor>();
            Enabled = true;
        }

        public bool IsPostProcessingPass => false;
        public bool SkipWorldRenderBelow => false;
        public bool SkipWorldRenderAbove => false;
        public bool SkipEntityRender => false;
        public bool SkipEngineHookRender => false;
        public string EffectTechniqueName => RenderPipelineTechniqueName.Forward;

        public bool Enabled { get; set; }

        public List<VertexPositionNormalColor> Lines { get; }

        public List<VertexPositionNormalColor> Triangles { get; }

        public void BeginRenderPass(IGameContext gameContext, IRenderContext renderContext, IRenderPass previousPass, RenderTarget2D postProcessingSource)
        {
            if (!Enabled)
            {
                return;
            }

            _oldRasterizerState = renderContext.GraphicsDevice.RasterizerState;
            _oldDepthState = renderContext.GraphicsDevice.DepthStencilState;

            if (_debugRasterizerState == null)
            {
                _debugRasterizerState = new RasterizerState()
                {
                    CullMode = CullMode.None,
                    FillMode = FillMode.WireFrame,
                    DepthBias = _oldRasterizerState.DepthBias,
                    DepthClipEnable = true,
                    MultiSampleAntiAlias = false,
                    Name = "PhysicsDebugRasterizerState",
                    ScissorTestEnable = false,
                    SlopeScaleDepthBias = _oldRasterizerState.SlopeScaleDepthBias
                };
            }

            if (_debugDepthState == null)
            {
                _debugDepthState = new DepthStencilState()
                {
                    DepthBufferEnable = false,
                    DepthBufferWriteEnable = false
                };
            }

            renderContext.GraphicsDevice.RasterizerState = _debugRasterizerState;
            renderContext.GraphicsDevice.DepthStencilState = _debugDepthState;
            renderContext.PushEffect(_basicEffect.Effect);

            Lines.Clear();
            Triangles.Clear();
        }

        public void EndRenderPass(IGameContext gameContext, IRenderContext renderContext, IRenderPass nextPass)
        {
            if (!Enabled)
            {
                return;
            }

            var world = renderContext.World;
            renderContext.World = Matrix.Identity;

            foreach (var pass in renderContext.Effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                if (Lines.Count > 0)
                {
                    // Batch render all lines.
                    var lines = Lines.ToArray();
                    for (var i = 0; i < lines.Length; i += 1000)
                    {
                        renderContext.GraphicsDevice.DrawUserPrimitives(
                            PrimitiveType.LineList,
                            lines,
                            i,
                            lines.Length - i > 1000 ? 500 : (lines.Length - i) / 2);
                    }
                }

                if (Triangles.Count > 0)
                {
                    // Batch render all triangles.
                    var triangles = Triangles.ToArray();
                    for (var i = 0; i < triangles.Length; i += 1500)
                    {
                        renderContext.GraphicsDevice.DrawUserPrimitives(
                            PrimitiveType.TriangleList,
                            triangles,
                            i,
                            triangles.Length - i > 1500 ? 500 : (triangles.Length - i) / 3);
                    }
                }
            }

            renderContext.World = world;

            renderContext.PopEffect();
            renderContext.GraphicsDevice.RasterizerState = _oldRasterizerState;
            renderContext.GraphicsDevice.DepthStencilState = _oldDepthState;
        }
    }
}