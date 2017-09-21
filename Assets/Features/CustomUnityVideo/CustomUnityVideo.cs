using UnityEngine;
using System.Collections;
using WebRtcCSharp;
using System.Collections.Generic;
using Byn.Common;
using Byn.Media.Native;
using Byn.Media;
using System.Runtime.InteropServices;

/// <summary>
/// Simulates a specific instance of a video device. It returns images that are sent to webrtc as it would
/// come right from a webcam.
/// </summary>
class CustomVideoCapturer : HLCustomVideoCapturer
{
    //private VideoType mWebRtcInputFormat = VideoType.kYUY2;

    private int mSourceWidth;
    private int mSourceHeight;
    private byte[] mData = null;
	private Camera lacamara;
	private int camWidth;
	private int camHeight;
	private byte[] losbaites;

	public enum CapturerState
    {
        Invalid,
        Created,
        Running,
        Stopped,
        Disposed
    }

    private CapturerState mState = CapturerState.Invalid;
    public CapturerState State
    {
        get
        {
            return mState;
        }
    }

    public CustomVideoCapturer()
    {
        mState = CapturerState.Created;
    }
    
    public override bool Start(VideoFormat capture_format)
    {
        mSourceWidth = capture_format.width;
        mSourceHeight = capture_format.height;
        mData = new byte[capture_format.width * capture_format.height * 4];

        mState = CapturerState.Running;

        //get a first image to make sure the C++ buffer isn't filled with random values from memory
        UpdateNow();
        Debug.Log("Video source started. Video format requested by webrtc: " + capture_format.width + "x" + capture_format.height);

        return mState == CapturerState.Running;
    }

    public override void Stop()
    {
        Debug.Log("Video source stopped.");
        mState = CapturerState.Stopped;
    }

    /// <summary>
    /// Gets the newest frame and sends it out to WebRTC for processing.
    /// </summary>
    public void UnityUpdate()
    {
        //called via unity
        if (mState == CapturerState.Running)
        {
			//GenerateTestImage();
			GenerateCamImage();
			UpdateNow();
        }
    }

	private static byte[] Color32ArrayToByteArray(Color32[] colors) {
		if (colors == null || colors.Length == 0)
			return null;

		int lengthOfColor32 = Marshal.SizeOf(typeof(Color32));
		int length = lengthOfColor32 * colors.Length;
		byte[] bytes = new byte[length];

		GCHandle handle = default(GCHandle);
		try {
			handle = GCHandle.Alloc(colors, GCHandleType.Pinned);
			System.IntPtr ptr = handle.AddrOfPinnedObject();
			Marshal.Copy(ptr, bytes, 0, length);
		}
		finally {
			if (handle != default(GCHandle))
				handle.Free();
		}

		return bytes;
	}

	private void GenerateCamImage() {
		lacamara = GameObject.Find("Camera").GetComponent<Camera>();
		camWidth = lacamara.targetTexture.width;
		camHeight = lacamara.targetTexture.height;
		if (camWidth != mSourceWidth || camHeight != mSourceHeight) {
			Debug.Log("W * H: " + camWidth + " * " + camHeight);
			Debug.Log("mSource :" + mSourceWidth + " * " + mSourceHeight);
			return;
		}
		Debug.Log("W * H: " + camWidth + " * " + camHeight);
		Texture2D textura = new Texture2D(camWidth, camHeight, TextureFormat.ARGB32, false);
		RenderTexture.active = lacamara.targetTexture;

		textura.ReadPixels(new Rect(0, 0, camWidth, camHeight), 0, 0);

		Color32[] data = new Color32[camWidth * camHeight];
		data = textura.GetPixels32();
		mData = Color32ArrayToByteArray(data);
		Debug.Log("mData.length " + mData.Length);
		RenderTexture.active = null;
	}

	private void GenerateTestImage()
    {
        for(int y = 0; y < mSourceHeight; y++)
        {
            for (int x = 0; x < mSourceWidth; x++)
            {
                
                if(x == 0 || y == 0 || x == mSourceWidth - 1 || y == mSourceHeight - 1)
                {
                    //red border around the image 1 pixel wide to find bugs that might
                    //cut off the border
                    mData[(y * mSourceWidth + x) * 4 + 0] = 0;//B
                    mData[(y * mSourceWidth + x) * 4 + 1] = 0;//G
                    mData[(y * mSourceWidth + x) * 4 + 2] = 255;//R
                    mData[(y * mSourceWidth + x) * 4 + 3] = 255;//A
                }
                else
                {
                    int ax = (x + Time.frameCount) % mSourceWidth;
                    int ay = (y + Time.frameCount) % mSourceHeight;

                    byte r = 0;
                    byte g = 0;
                    byte b = 0;

                    if(between(ax, 0, 64, ay, 0, 64))
                    {
                        r = 255;
                        g = 0;
                        b = 0;
                    }else if (between(ax, 64, 128, ay, 0, 64))
                    {
                        r = 0;
                        g = 255;
                        b = 0;
                    }
                    else if (between(ax, 0, 64, ay, 64, 128))
                    {
                        r = 0;
                        g = 0;
                        b = 255;
                    }
                    else if (between(ax, 64, 128, ay, 64, 128))
                    {
                        r = 255;
                        g = 255;
                        b = 255;
                    }

                    mData[(y * mSourceWidth + x) * 4 + 0] = b;//B
                    mData[(y * mSourceWidth + x) * 4 + 1] = g;//G
                    mData[(y * mSourceWidth + x) * 4 + 2] = r;//R
                    mData[(y * mSourceWidth + x) * 4 + 3] = 255;//A
                }
            }
        }
    }

    private bool between(int x, int minx, int maxx, int y, int miny, int maxy)
    {
        if (x > minx && x < maxx && y > miny && y < maxy)
            return true;
        return false;
    }

    private void UpdateNow()
    {
		//delivers our test ARGB images to webrtc (very expensive operation for high res images as
		//the image needs to be converted to an internal format)
		UpdateFrame(VideoType.kARGB, mData, (uint)mData.Length, mSourceWidth, mSourceHeight);
		//Debug.Log("losbaites"  + " "+ losbaites.Length + " " + camWidth + " "+ camHeight);
		//UpdateFrame(VideoType.kARGB, losbaites, (uint)losbaites.Length, camWidth, camHeight);
	}

	public override void Dispose()
    {
        base.Dispose();
    }

}

/// <summary>
/// This class hooks right into webrtc's method to create and querty video devices.
/// 
/// </summary>
class CustomVideoCapturerFactory : HLVideoCapturerFactory
{
    /// <summary>
    /// 
    /// </summary>
    private static string mDeviceName = "CustomVideoDevice";

    /// <summary>
    /// 
    /// </summary>
    public static string DeviceName
    {
        get
        {
            return mDeviceName;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private List<CustomVideoCapturer> mActiveCapturers = new List<CustomVideoCapturer>();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="deviceName"></param>
    /// <returns></returns>
    public override HLCustomVideoCapturer Create(string deviceName)
    {
        if (deviceName == mDeviceName)
        {
            var capturer = new CustomVideoCapturer();
            mActiveCapturers.Add(capturer);
            return capturer;
        }
        return null;
    }

    /// <summary>
    /// Used to allow webrtc to request for any supported video devices.
    /// Called in WebRTC Worker thread!
    /// </summary>
    /// <returns></returns>
    public override StringVector GetVideoDevices()
    {
        StringVector vector = new StringVector();
        vector.Add(mDeviceName);
        return vector;
    }

    /// <summary>
    /// Destroys all video capturers + releases native pointer in the base class HLVideoCapturerFactory
    /// </summary>
    public override void Dispose()
    {
        foreach (var v in mActiveCapturers)
        {
            v.Dispose();
        }
        mActiveCapturers.Clear();
        base.Dispose();
    }

    /// <summary>
    /// </summary>
    public void UpdateDevices()
    {
        foreach (var v in mActiveCapturers.ToArray())
        {
            if (v.State == CustomVideoCapturer.CapturerState.Running)
            {
                v.UnityUpdate();
            }
            else
            {
                //it has stopped. webrtc removed it
                Debug.Log("Stopped. Cleanup");
                mActiveCapturers.Remove(v);
                v.Dispose();
            }
        }
    }
}

/// <summary>
/// Unity side. It handles the initialization and updates the video factory + devices each unity update call.
/// </summary>
class CustomUnityVideo : UnitySingleton<CustomUnityVideo>
{
    

    private CustomVideoCapturerFactory factory;
    private bool mRegistered = false;

    /// <summary>
    /// 
    /// </summary>
    public void Register()
    {
        if (mRegistered == false)
        {
            factory = new CustomVideoCapturerFactory();

            //directly registers the source in the native library. This will be removed in the future
            //and replaced by a simpler + more stable system

            NativeWebRtcCallFactory nativeFactory = UnityCallFactory.Instance.InternalFactory as NativeWebRtcCallFactory;
            if (nativeFactory != null)
            {
                nativeFactory.NetworkFactory.NativeFactory.AddVideoCaptureFactory(factory);
                Debug.Log("Registered");
            }
            else
            {
                Debug.LogError("Couldn't register video factory! Only native webrtc supports custom video sources!");
            }
            mRegistered = true;
        }
    }

    public void Update()
    {
        if (factory != null)
            factory.UpdateDevices();
    }

    protected override void OnDestroy()
    {
        Debug.Log("Shutting down. All video sources will be destroyed");
        if (factory != null)
        {
            factory.Dispose();
            factory = null;
        }
        base.OnDestroy();
    }
}