﻿// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;

using Silk.NET.OpenGL;

namespace Prowl.Runtime.GraphicsBackend.OpenGL;

public sealed unsafe class GLFrameBuffer : GraphicsFrameBuffer
{
    public uint Handle { get; private set; }
    public uint NumOfAttachments { get; private set; }
    public override uint Width { get; protected set; }
    public override uint Height { get; protected set; }

    public static readonly GLEnum[] buffers =
    {
        GLEnum.ColorAttachment0,  GLEnum.ColorAttachment1,  GLEnum.ColorAttachment2,
        GLEnum.ColorAttachment3,  GLEnum.ColorAttachment4,  GLEnum.ColorAttachment5,
        GLEnum.ColorAttachment6,  GLEnum.ColorAttachment7,  GLEnum.ColorAttachment8,
        GLEnum.ColorAttachment9,  GLEnum.ColorAttachment10, GLEnum.ColorAttachment11,
        GLEnum.ColorAttachment12, GLEnum.ColorAttachment13, GLEnum.ColorAttachment14,
        GLEnum.ColorAttachment15, GLEnum.ColorAttachment16, GLEnum.ColorAttachment16,
        GLEnum.ColorAttachment17, GLEnum.ColorAttachment18, GLEnum.ColorAttachment19,
        GLEnum.ColorAttachment20, GLEnum.ColorAttachment21, GLEnum.ColorAttachment22,
        GLEnum.ColorAttachment23, GLEnum.ColorAttachment24, GLEnum.ColorAttachment25,
        GLEnum.ColorAttachment26, GLEnum.ColorAttachment27, GLEnum.ColorAttachment28,
        GLEnum.ColorAttachment29, GLEnum.ColorAttachment30, GLEnum.ColorAttachment31
    };

    public GLFrameBuffer(Attachment[] attachments, uint width, uint height)
    {
        int numTextures = attachments.Length;
        if (numTextures < 0 || numTextures > Graphics.MaxFramebufferColorAttachments)
            throw new Exception("[FrameBuffer] Invalid number of textures! [0-" + Graphics.MaxFramebufferColorAttachments + "]");

        // Generate FBO
        Handle = GLDevice.GL.GenFramebuffer();
        if (Handle <= 0)
            throw new Exception($"[FrameBuffer] Failed to generate new FrameBuffer.");

        NumOfAttachments = (uint)numTextures;
        Width = width;
        Height = height;

        GLDevice.GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);

        unsafe
        {
            // Generate textures
            if (numTextures > 0)
            {
                for (int i = 0; i < numTextures; i++)
                {
                    if (!attachments[i].isDepth)
                    {
                        //InternalTextures[i].SetTextureFilters(TextureMinFilter.Linear, TextureMagFilter.Linear);
                        //InternalTextures[i].SetWrapModes(TextureWrapMode.ClampToEdge, TextureWrapMode.ClampToEdge);
                        GLDevice.GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + i, (attachments[i].texture as GLTexture)!.Target, (attachments[i].texture as GLTexture)!.Handle, 0);
                    }
                    else
                    {
                        GLDevice.GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, (attachments[i].texture as GLTexture)!.Handle, 0);
                    }
                }
                GLDevice.GL.DrawBuffers((uint)numTextures, buffers);
            }

            if (GLDevice.GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
                throw new Exception("RenderTexture: [ID {fboId}] RenderTexture object creation failed.");

            // Unbind FBO
            GLDevice.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
    }

    public override bool IsDisposed { get; protected set; }

    public override void Dispose()
    {
        if (IsDisposed)
            return;

        GLDevice.GL.DeleteFramebuffer(Handle);
        IsDisposed = true;
    }
    public override string ToString()
    {
        return Handle.ToString();
    }
}
