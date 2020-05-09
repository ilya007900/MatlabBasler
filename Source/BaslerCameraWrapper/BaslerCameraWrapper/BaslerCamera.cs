using Basler.Pylon;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace BaslerCameraWrapper
{
    public class BaslerCamera
    {
        private const string ExposureAutoModeOn = "Continuous";
        private const string ExposureAutoModeOff = "Off";
        private const string GainAutoModeOn = "Continuous";
        private const string GainAutoModeOff = "Off";
        private const string FriendlyNameKey = "FriendlyName";

        private ICamera camera;
        private readonly VideoWriter videoWriter = new VideoWriter();

        public event EventHandler ImageRecived;

        #region public properties

        public string Name => camera.CameraInfo[FriendlyNameKey];

        public double ExposureTimeMin => camera.Parameters[PLCamera.ExposureTime].GetMinimum();

        public double ExposureTimeMax => camera.Parameters[PLCamera.ExposureTime].GetMaximum();

        public double ExposureTime
        {
            get
            {
                return camera.Parameters[PLCamera.ExposureTime].GetValue();
            }
            set
            {
                if (value <= ExposureTimeMax && value >= ExposureTimeMin)
                {
                    camera.Parameters[PLCamera.ExposureTime].SetValue(value);
                }
            }
        }

        public bool ExposureAuto
        {
            get
            {
                var currentMode = camera.Parameters[PLCamera.ExposureAuto].GetValue();
                return string.CompareOrdinal(currentMode, ExposureAutoModeOn) == 0;
            }
            set
            {
                if (value)
                {
                    camera.Parameters[PLCamera.ExposureAuto].SetValue(ExposureAutoModeOn);
                }
                else
                {
                    camera.Parameters[PLCamera.ExposureAuto].SetValue(ExposureAutoModeOff);
                }
            }
        }

        public double GainMin => camera.Parameters[PLCamera.Gain].GetMinimum();

        public double GainMax => camera.Parameters[PLCamera.Gain].GetMaximum();

        public double Gain
        {
            get => camera.Parameters[PLCamera.Gain].GetValue();
            set
            {
                if (value >= GainMin && value <= GainMax)
                {
                    camera.Parameters[PLCamera.Gain].SetValue(value);
                }
            }
        }

        public bool GainAuto
        {
            get
            {
                var currentMode = camera.Parameters[PLCamera.GainAuto].GetValue();
                return string.CompareOrdinal(currentMode, GainAutoModeOn) == 0;
            }
            set
            {
                if (value)
                {
                    camera.Parameters[PLCamera.GainAuto].SetValue(GainAutoModeOn);
                }
                else
                {
                    camera.Parameters[PLCamera.GainAuto].SetValue(GainAutoModeOff);
                }
            }
        }

        #endregion

        public BaslerCamera()
        {
            camera = new Camera();
        }

        public void Open()
        {
            if (!camera.IsOpen)
            {
                var openedCamera = camera.Open();
                //camera = openedCamera;
            }
        }

        public void Close()
        {
            if (camera.IsOpen)
            {
                camera.Close();
            }
        }

        public void Start()
        {
            camera.StreamGrabber.ImageGrabbed += StreamGrabber_ImageGrabbed;
            camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
        }

        public void Stop()
        {
            camera.StreamGrabber.ImageGrabbed -= StreamGrabber_ImageGrabbed;
            camera.StreamGrabber.Stop();
        }

        public void StartRecord(string fileName, double playbackFramesPerSecond)
        {
            videoWriter.Create(fileName, playbackFramesPerSecond, camera);
            camera.StreamGrabber.ImageGrabbed += OnRecordImageRecived;
            camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
        }

        public void StopRecord()
        {
            camera.StreamGrabber.ImageGrabbed -= OnRecordImageRecived;
            videoWriter.Close();
            camera.StreamGrabber.Stop();
        }

        public void RecordFrames(string fileName, double playbackFramesPerSecond, int framesCount)
        {
            videoWriter.Create(fileName, playbackFramesPerSecond, camera);
            camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByUser);
            try
            {
                for (var i = 0; i < framesCount; i++)
                {
                    var grabResult = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
                    videoWriter.Write(grabResult);
                    StreamGrabber_ImageGrabbed(this, new ImageGrabbedEventArgs(grabResult));
                }
            }
            finally
            {
                camera.StreamGrabber.Stop();
                videoWriter.Close();
            }
        }

        public void OpenPreview()
        {
            camera.StreamGrabber.ImageGrabbed += OnPreviewImageRecived;
            camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
        }

        public void ClosePreview()
        {
            camera.StreamGrabber.ImageGrabbed -= OnPreviewImageRecived;
            camera.StreamGrabber.Stop();
            ImageWindow.Close(0);
        }

        public byte[,] GetFrame()
        {
            var grabResult = camera.StreamGrabber.GrabOne(3000);
            return GetImage(grabResult);
        }

        private void StreamGrabber_ImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            ImageRecived?.Invoke(sender, e);
        }

        private void OnRecordImageRecived(object sender, ImageGrabbedEventArgs e)
        {
            videoWriter.Write(e.GrabResult);
            ImageWindow.DisplayImage(0, e.GrabResult);
        }

        private void OnPreviewImageRecived(object sender, ImageGrabbedEventArgs e)
        {
            ImageWindow.DisplayImage(0, e.GrabResult);
        }

        private static byte[,] GetImage(IGrabResult grabResult)
        {
            var converter = new PixelDataConverter();
            var bitmap = new Bitmap(grabResult.Width, grabResult.Height, PixelFormat.Format32bppRgb);
            // Lock the bits of the bitmap.
            var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bmpData = bitmap.LockBits(rectangle, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            // Place the pointer to the buffer of the bitmap.
            converter.OutputPixelFormat = PixelType.BGRA8packed;
            var ptrBmp = bmpData.Scan0;
            converter.Convert(ptrBmp, bmpData.Stride * bitmap.Height, grabResult);
            bitmap.UnlockBits(bmpData);

            var width = bitmap.Width;
            var height = bitmap.Height;
            var bnew = new byte[width, height];

            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    var pixelColor = bitmap.GetPixel(i, j);
                    bnew.SetValue(pixelColor.R, i, j);
                }
            }

            return bnew;
        }
    }
}
