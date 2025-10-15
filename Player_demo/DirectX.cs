using System;

using SharpDX.DXGI;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;

using Device    = SharpDX.Direct3D11.Device;
using Resource  = SharpDX.Direct3D11.Resource;
using SharpDX;

namespace Player_demo
{
   public class DirectX
    {
        #region Declaration
        internal Device                     _device;
        SwapChain                           _swapChain;

        Texture2D                           _backBuffer;

        VideoDevice1                        videoDevice1;
        VideoProcessor                      videoProcessor;
        VideoContext1                       videoContext1;
        VideoProcessorEnumerator vpe;
        VideoProcessorContentDescription    vpcd;
        VideoProcessorOutputViewDescription vpovd;
        VideoProcessorInputViewDescription  vpivd;
        VideoProcessorInputView             vpiv;
        VideoProcessorOutputView            vpov;
        VideoProcessorStream[]              vpsa;

        public DirectX(IntPtr outputHandle) { Initialize(outputHandle); }
        #endregion

        // Get's a handle to assosiate with the BackBuffer and Prepares Devices
        // private void Initialize(IntPtr outputHandle)
        // {
        //     // SwapChain Description
        //     var desc = new SwapChainDescription()
        //     {
        //         BufferCount         = 1,
        //         ModeDescription     = new ModeDescription(0, 0, new Rational(0, 0), Format.B8G8R8A8_UNorm), // RBGA | BGRA 32-bit
        //         IsWindowed          = true,
        //         OutputHandle        = outputHandle,
        //         SampleDescription   = new SampleDescription(1, 0),
        //         SwapEffect          = SwapEffect.Discard,
        //         Usage               = Usage.RenderTargetOutput
        //     };
        //
        //     // Create Device, SwapChain & BackBuffer
        //     // Device.CreateWithSwapChain(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.Debug | DeviceCreationFlags.BgraSupport, desc, out _device, out _swapChain);
        //     // _backBuffer     = Texture2D.FromSwapChain<Texture2D>(_swapChain, 0);
        //     
        //     Device.CreateWithSwapChain(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport, desc, out _device, out _swapChain);
        //
        //     // Creates Association between outputHandle and BackBuffer
        //     var factory = _swapChain.GetParent<Factory>();
        //     factory.MakeWindowAssociation(outputHandle, WindowAssociationFlags.IgnoreAll);
        //
        //     // Video Device | Video Context
        //     videoDevice1    = _device.QueryInterface<VideoDevice1>();
        //     videoContext1   = _device.ImmediateContext.QueryInterface<VideoContext1>();
        //
        //     // Creates Video Processor Enumerator
        //     vpcd = new VideoProcessorContentDescription()
        //     {
        //         Usage = VideoUsage.PlaybackNormal,
        //         InputFrameFormat = VideoFrameFormat.Progressive,
        //
        //         InputFrameRate = new Rational(1, 1),
        //         OutputFrameRate = new Rational(1, 1),
        //
        //         // We Set those later
        //         InputWidth = 1,
        //         OutputWidth = 1,
        //         InputHeight = 1,
        //         OutputHeight = 1
        //     };
        //     videoDevice1.CreateVideoProcessorEnumerator(ref vpcd, out vpe);
        //     videoDevice1.CreateVideoProcessor(vpe, 0, out videoProcessor);
        //     
        //     // Prepares Video Processor Input View Description for Video Processor Input View that we pass Shared NV12 Texture (nv12SharedResource) each time
        //     vpivd = new VideoProcessorInputViewDescription()
        //     {
        //         FourCC = 0,
        //         Dimension = VpivDimension.Texture2D,
        //         Texture2D = new Texture2DVpiv() { MipSlice = 0, ArraySlice = 0 }
        //     };
        //
        //     // Creates Video Processor Output to our BackBuffer
        //     vpovd = new VideoProcessorOutputViewDescription() { Dimension = VpovDimension.Texture2D };
        //     videoDevice1.CreateVideoProcessorOutputView((Resource) _backBuffer, vpe, vpovd, out vpov);
        //
        //     // Prepares Streams Array
        //     vpsa = new VideoProcessorStream[1];
        // }
        
        private void Initialize(IntPtr outputHandle)
{
    try
    {
        // SwapChain Description
        var desc = new SwapChainDescription()
        {
            BufferCount = 1,
            ModeDescription = new ModeDescription(0, 0, new Rational(0, 0), Format.B8G8R8A8_UNorm),
            IsWindowed = true,
            OutputHandle = outputHandle,
            SampleDescription = new SampleDescription(1, 0),
            SwapEffect = SwapEffect.Discard,
            Usage = Usage.RenderTargetOutput
        };

        // Create Device & SwapChain (remove Debug flag)
        Device.CreateWithSwapChain(SharpDX.Direct3D.DriverType.Hardware, 
            DeviceCreationFlags.BgraSupport, desc, out _device, out _swapChain);
        
        _backBuffer = Texture2D.FromSwapChain<Texture2D>(_swapChain, 0);

        // Get the actual back buffer dimensions
        var backBufferDesc = _backBuffer.Description;

        // Creates Association between outputHandle and BackBuffer
        var factory = _swapChain.GetParent<Factory>();
        factory.MakeWindowAssociation(outputHandle, WindowAssociationFlags.IgnoreAll);

        // Video Device | Video Context
        videoDevice1 = _device.QueryInterface<VideoDevice1>();
        videoContext1 = _device.ImmediateContext.QueryInterface<VideoContext1>();

        // Create Video Processor Enumerator with PROPER dimensions
        vpcd = new VideoProcessorContentDescription()
        {
            Usage = VideoUsage.PlaybackNormal,
            InputFrameFormat = VideoFrameFormat.Progressive,
            InputFrameRate = new Rational(1, 1),
            OutputFrameRate = new Rational(1, 1),
            
            // Set actual dimensions from back buffer
            InputWidth = backBufferDesc.Width,
            OutputWidth = backBufferDesc.Width,
            InputHeight = backBufferDesc.Height,
            OutputHeight = backBufferDesc.Height
        };

        videoDevice1.CreateVideoProcessorEnumerator(ref vpcd, out vpe);
        videoDevice1.CreateVideoProcessor(vpe, 0, out videoProcessor);
        
        // Proper Video Processor Input View Description
        vpivd = new VideoProcessorInputViewDescription()
        {
            FourCC = 0,
            Dimension = VpivDimension.Texture2D,
            Texture2D = new Texture2DVpiv() 
            { 
                MipSlice = 0, 
                ArraySlice = 0 
            }
        };

        // FIX: Proper Video Processor Output View Description
        vpovd = new VideoProcessorOutputViewDescription() 
        { 
            Dimension = VpovDimension.Texture2D,
            Texture2D = new Texture2DVpov()
            {
                MipSlice = 0
            }
        };

        // Create output view
        videoDevice1.CreateVideoProcessorOutputView(
            (Resource)_backBuffer, 
            vpe, 
            vpovd, 
            out vpov);

        // Prepares Streams Array
        vpsa = new VideoProcessorStream[1];
        
        Console.WriteLine($"DirectX initialized successfully. Back buffer: {backBufferDesc.Width}x{backBufferDesc.Height}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DirectX initialization failed: {ex.Message}");
        throw;
    }
}

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        /* ID3D11VideoContext::VideoProcessorBlt | https://docs.microsoft.com/en-us/windows/win32/api/d3d11/nf-d3d11-id3d11videocontext-videoprocessorblt
         * 
         * HRESULT VideoProcessorBlt (
         * ID3D11VideoProcessor               *pVideoProcessor,
         * ID3D11VideoProcessorOutputView     *pView,
         * UINT                               OutputFrame,
         * UINT                               StreamCount,
         * const D3D11_VIDEO_PROCESSOR_STREAM *pStreams );
         * 
         * 1. Opens Shared NV12 Texture (nv12SharedResource) on our SharpDX ID3Device from FFmpeg's ID3Device
         * 2. Creates a new Video Processor Input View that we pass in Video Processor Streams
         * 3. Calls Video Processor Blt to convert (in GPU) Shared NV12 Texture to our BackBuffer RBGA/BGRA Texture
         * 4. Finally Presents the Frame to the outputHandle (SampleUI Form)
         */
        public void PresentFrame(Texture2D textureHW)
        {
            videoDevice1.CreateVideoProcessorInputView(textureHW, vpe, vpivd, out vpiv);
            VideoProcessorStream vps = new VideoProcessorStream()
            {
                PInputSurface = vpiv,
                Enable = new RawBool(true)
            };
            vpsa[0] = vps;
            videoContext1.VideoProcessorBlt(videoProcessor, vpov, 0, 1, vpsa);

            _swapChain.Present(0, PresentFlags.None);

            Utilities.Dispose(ref vpiv);
            Utilities.Dispose(ref textureHW);
        }
        
        // Add this method to your DirectX class
        public void ResizeSwapChain(int width, int height)
        {
            if (_swapChain == null || _backBuffer == null) return;

            try
            {
                // Ensure minimum size
                width = Math.Max(width, 1);
                height = Math.Max(height, 1);

                // Dispose old resources
                Utilities.Dispose(ref _backBuffer);
                Utilities.Dispose(ref vpov);

                // Resize swap chain
                _swapChain.ResizeBuffers(1, width, height, Format.B8G8R8A8_UNorm, SwapChainFlags.None);
        
                // Get new back buffer
                _backBuffer = Texture2D.FromSwapChain<Texture2D>(_swapChain, 0);

                // Get new back buffer dimensions
                var backBufferDesc = _backBuffer.Description;

                // Update video processor description with new size
                vpcd.InputWidth = backBufferDesc.Width;
                vpcd.OutputWidth = backBufferDesc.Width;
                vpcd.InputHeight = backBufferDesc.Height;
                vpcd.OutputHeight = backBufferDesc.Height;

                // Recreate video processor enumerator with new size
                Utilities.Dispose(ref vpe);
                Utilities.Dispose(ref videoProcessor);
        
                videoDevice1.CreateVideoProcessorEnumerator(ref vpcd, out vpe);
                videoDevice1.CreateVideoProcessor(vpe, 0, out videoProcessor);

                // Recreate output view
                videoDevice1.CreateVideoProcessorOutputView(_backBuffer, vpe, vpovd, out vpov);

                Console.WriteLine($"DirectX resized to: {width}x{height}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Resize failed: {ex.Message}");
            }
        }

// Also add a Dispose method if you don't have one
        public void Dispose()
        {
            _backBuffer?.Dispose();
            _swapChain?.Dispose();
            vpov?.Dispose();
            videoProcessor?.Dispose();
            vpe?.Dispose();
            videoContext1?.Dispose();
            videoDevice1?.Dispose();
            _device?.Dispose();
        }
    }
}