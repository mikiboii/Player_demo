using System;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Device = SharpDX.Direct3D11.Device;
using Resource = SharpDX.Direct3D11.Resource;

namespace Player_demo
{
    public class DirectXWPF : IDisposable
    {
        #region DirectX Fields
        internal Device _device;
        internal DeviceContext _context;
        private SwapChain _swapChain;
        private Texture2D _backBuffer;

        // Video processing
        private VideoDevice1 videoDevice1;
        private VideoContext1 videoContext1;
        private VideoProcessorEnumerator vpe;
        private VideoProcessor videoProcessor;
        private VideoProcessorContentDescription vpcd;
        private VideoProcessorOutputViewDescription vpovd;
        private VideoProcessorInputViewDescription vpivd;
        private VideoProcessorInputView vpiv;
        private VideoProcessorOutputView vpov;
        private VideoProcessorStream[] vpsa;
        #endregion

        public bool IsDisposed { get; private set; } = false;

        private int _width = 1920;
        private int _height = 1080;

        public DirectXWPF(int width = 1920, int height = 1080)
        {
            _width = width;
            _height = height;

            // Initialize with a fake handle (for WPF off-screen rendering)
            Initialize(IntPtr.Zero);
        }

        private void Initialize(IntPtr outputHandle)
        {
            try
            {
                // SwapChain description (offscreen, no actual window needed)
                var desc = new SwapChainDescription()
                {
                    BufferCount = 1,
                    ModeDescription = new ModeDescription(_width, _height, new Rational(60, 1), Format.B8G8R8A8_UNorm),
                    IsWindowed = true,
                    OutputHandle = outputHandle, // zero for offscreen
                    SampleDescription = new SampleDescription(1, 0),
                    SwapEffect = SwapEffect.Discard,
                    Usage = Usage.RenderTargetOutput
                };

                Device.CreateWithSwapChain(
                    SharpDX.Direct3D.DriverType.Hardware,
                    DeviceCreationFlags.BgraSupport,
                    desc,
                    out _device,
                    out _swapChain);

                _context = _device.ImmediateContext;
                _backBuffer = Texture2D.FromSwapChain<Texture2D>(_swapChain, 0);

                // Video device & context
                videoDevice1 = _device.QueryInterface<VideoDevice1>();
                videoContext1 = _context.QueryInterface<VideoContext1>();

                // Video processor content description
                vpcd = new VideoProcessorContentDescription()
                {
                    Usage = VideoUsage.PlaybackNormal,
                    InputFrameFormat = VideoFrameFormat.Progressive,
                    InputFrameRate = new Rational(1, 1),
                    OutputFrameRate = new Rational(1, 1),
                    InputWidth = _width,
                    InputHeight = _height,
                    OutputWidth = _width,
                    OutputHeight = _height
                };

                videoDevice1.CreateVideoProcessorEnumerator(ref vpcd, out vpe);
                videoDevice1.CreateVideoProcessor(vpe, 0, out videoProcessor);

                // Input & output view description
                vpivd = new VideoProcessorInputViewDescription()
                {
                    FourCC = 0,
                    Dimension = VpivDimension.Texture2D,
                    Texture2D = new Texture2DVpiv() { MipSlice = 0, ArraySlice = 0 }
                };

                vpovd = new VideoProcessorOutputViewDescription()
                {
                    Dimension = VpovDimension.Texture2D,
                    Texture2D = new Texture2DVpov() { MipSlice = 0 }
                };

                videoDevice1.CreateVideoProcessorOutputView((Resource)_backBuffer, vpe, vpovd, out vpov);

                // Streams array
                vpsa = new VideoProcessorStream[1];

                Console.WriteLine($"[DirectXWPF] Initialized {_width}x{_height}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DirectXWPF] Initialization failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Present a hardware-decoded frame to the back buffer.
        /// </summary>
        public void PresentFrame(Texture2D textureHW)
        {
            if (IsDisposed) return;

            videoDevice1.CreateVideoProcessorInputView(textureHW, vpe, vpivd, out vpiv);
            vpsa[0] = new VideoProcessorStream() { PInputSurface = vpiv, Enable = new RawBool(true) };

            videoContext1.VideoProcessorBlt(videoProcessor, vpov, 0, 1, vpsa);

            _swapChain.Present(0, PresentFlags.None);

            Utilities.Dispose(ref vpiv);
            Utilities.Dispose(ref textureHW);
        }

        public Texture2D GetBackBuffer() => _backBuffer;

        public void Resize(int width, int height)
        {
            if (IsDisposed) return;

            _width = width;
            _height = height;

            Utilities.Dispose(ref vpov);
            Utilities.Dispose(ref _backBuffer);

            _backBuffer = Texture2D.FromSwapChain<Texture2D>(_swapChain, 0);
            videoDevice1.CreateVideoProcessorOutputView((Resource)_backBuffer, vpe, vpovd, out vpov);
        }

        public void Dispose()
        {
            if (IsDisposed) return;

            Utilities.Dispose(ref vpiv);
            Utilities.Dispose(ref vpov);
            Utilities.Dispose(ref videoProcessor);
            Utilities.Dispose(ref vpe);
            Utilities.Dispose(ref _backBuffer);
            Utilities.Dispose(ref _swapChain);
            Utilities.Dispose(ref videoDevice1);
            Utilities.Dispose(ref videoContext1);
            Utilities.Dispose(ref _context);
            Utilities.Dispose(ref _device);

            IsDisposed = true;
            Console.WriteLine("[DirectXWPF] Disposed");
        }
    }
}
