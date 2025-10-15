using System.Windows.Controls;
using System.Windows;
using System.Windows.Forms;
using System.Threading;
using SharpDX.Direct3D11;




namespace Player_demo
{
    public partial class VideoPlayerControl : System.Windows.Controls.UserControl
    {
        private string fileToPlay = @"G:\new movies\new\Spider-Man-No-Way-Home_1080p.mp4";
        private FFmpeg ffmpeg;
        private DirectX directX;
        private Thread threadPlay;

        public VideoPlayerControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            InitializeVideoPlayer();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            threadPlay?.Abort();
            directX?.Dispose();
        }

        private void InitializeVideoPlayer()
        {
            try
            {
                // Configure the WinForms form
                winFormsForm.Width = (int)ActualWidth;
                winFormsForm.Height = (int)ActualHeight;
                
                // The form is already hosted in WindowsFormsHost, so it's ready
                
                // Initialize DirectX with the form's handle
                directX = new DirectX(winFormsForm.Handle);

                ffmpeg = new FFmpeg();

                if (!ffmpeg.InitHWAccel(directX._device)) 
                { 
                    System.Windows.MessageBox.Show("Failed to Initialize FFmpeg's HW Acceleration"); 
                    return; 
                }
        
                if (!ffmpeg.Open(fileToPlay)) 
                { 
                    System.Windows.MessageBox.Show("FFmpeg failed to open input"); 
                    return; 
                }

                // Start playback thread (same as your WinForms version)
                threadPlay = new Thread(() =>
                {
                    try
                    {
                        while (true)
                        {
                            Texture2D textureHW = ffmpeg.GetFrame();
                            if (textureHW == null) 
                            { 
                                Thread.Sleep(1);
                                continue; 
                            }

                            directX.PresentFrame(textureHW);
                            Thread.Sleep(33);
                        }
                    }
                    catch (ThreadAbortException) { }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Thread error: {ex.Message}");
                    }
                })
                {
                    IsBackground = true
                };

                threadPlay.Start();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Video initialization error: {ex.Message}");
            }
        }

        

        
    }
}