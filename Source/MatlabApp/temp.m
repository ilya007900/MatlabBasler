dllPath = strrep(pwd, "MatlabApp", "BaslerCameraWrapper\BaslerCameraWrapper\bin\Debug\BaslerCameraWrapper.dll");
NET.addAssembly(dllPath);

cameraBasler = BaslerCameraWrapper.BaslerCamera();
cameraBasler.Open();

%addlistener(cameraBasler, 'ImageRecived', @myCallBack);

cameraBasler.StartRecord('imfilek', 30);
pause(5);
cameraBasler.StopRecord();

%function myCallBack(sender, e)
%    myBytes = sender
%end