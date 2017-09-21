using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomUnityVideoApp : CallApp {


    private CustomVideoCapturerFactory mVideoFactory;

    protected override void Start()
    {
        base.Start();
#if UNITY_WEBGL
        //doesn't work with WebGL
        Debug.LogError("Custom video isn't supported in the browser.");
#else
        //this is a global change that will affect all apps but can be
        //called multiple times without change.
        CustomUnityVideo.Instance.Register();
        mUi.UpdateVideoDropdown();
#endif
    }
}
