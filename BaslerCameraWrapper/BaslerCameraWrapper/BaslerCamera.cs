using Basler.Pylon;

namespace BaslerCameraWrapper
{
    public class BaslerCamera
    {
        private readonly ICamera camera;

        //public string Name { get=>}

        public double ExposureTime
        {
            get
            {
                // Determine the current exposure time
                return camera.Parameters[PLCamera.ExposureTimeAbs].GetValue();
            }
            set
            {
                // Set the exposure time mode to Standard
                // Note: Available on selected camera models only
                camera.Parameters[PLCamera.ExposureTimeMode].SetValue(PLCamera.ExposureTimeMode.Standard);
                // Set the exposure time to 3500 microseconds
                camera.Parameters[PLCamera.ExposureTimeAbs].SetValue(value);
            }
        }

        public BaslerCamera()
        {
            camera = new Camera();
        }
    }
}
