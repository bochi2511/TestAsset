using Byn.Common;
using Byn.Media;
using Byn.Net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class CallApp : MonoBehaviour
{
    /// <summary>
    /// This is a test server. Don't use in production! The server code is in a zip file in WebRtcNetwork
    /// </summary>
    public string uSignalingUrl = "ws://because-why-not.com:12776/callapp";

    /// <summary>
    /// Mozilla stun server. Used to get trough the firewall and establish direct connections.
    /// Replace this with your own production server as well. 
    /// </summary>
    public string uIceServer = "stun:because-why-not.com:12779";

    //
    public string uIceServerUser = "";
    public string uIceServerPassword = "";

    /// <summary>
    /// Second ice server. As I can't guarantee my one is always online.
    /// </summary>
    public string uIceServer2 = "stun:stun.l.google.com:19302";

    /// <summary>
    /// Set true to use send the WebRTC log + wrapper log output to the unity log.
    /// </summary>
    public bool uLog = false;

    /// <summary>
    /// Debug console to be able to see the unity log on every platform
    /// </summary>
    public bool uDebugConsole = false;

    /// <summary>
    /// Do not change. This length is enforced on the server side to avoid abuse.
    /// </summary>
    public const int MAX_CODE_LENGTH = 256;

    /// <summary>
    /// Call class handling all the functionality
    /// </summary>
    protected ICall mCall;

    
    private static bool sLogSet = false;

    protected CallAppUi mUi;
    protected MediaConfig mMediaConfig;
    private ConnectionId mRemoteUserId = ConnectionId.INVALID;


    private bool mUseVideo = true;
    private bool mUseAudio = true;
    private string mUseVideoDevice = null;

    private string mUseAddress = null;

    #region Calls from unity
    //
    protected virtual void Awake()
    {
        mUi = GetComponent<CallAppUi>();
        Init();
        mMediaConfig = CreateMediaConfig();
    }

    protected virtual void Start()
    {
        mUi.SetGuiState(true);

        //fill the video dropbox
        mUi.UpdateVideoDropdown();
    }

    private void OnDestroy()
    {
        CleanupCall();
    }
    private void OnGUI()
    {
        DebugHelper.DrawConsole();
    }
    /// <summary>
    /// The call object needs to be updated regularly to sync data received via webrtc with
    /// unity. All events will be triggered during the update method in the unity main thread
    /// to avoid multi threading errors
    /// </summary>
    protected virtual void Update()
    {
		if (Input.GetKeyDown(KeyCode.T)){
			mUi.uVideoDropdown.value = 3;
			mUi.uRoomNameInputField.text = "Prueba666";
			mUi.JoinButtonPressed();
		}
		
		if (mCall != null)
        {
            //update the call object. This will trigger all buffered events to be fired
            //to ensure it is done in the unity thread at a convenient time.
            mCall.Update();
        }
    }
    #endregion

    protected virtual void Init()
    {

        if (uDebugConsole)
            DebugHelper.ActivateConsole();
        if (uLog)
        {
            if (sLogSet == false)
            {
                SLog.SetLogger(OnLog);
                sLogSet = true;
                SLog.L("Log active");
            }
        }

        //This can be used to get the native webrtc log but causes a huge slowdown
        //only use if not webgl
        bool nativeWebrtcLog = false;

        if (nativeWebrtcLog)
        {
#if UNITY_ANDROID
            //due to BUG0008 the callbacks in android doesn't work yet. use logcat instead of unity log
            Byn.Net.Native.NativeWebRtcNetworkFactory.SetNativeDebugLog(WebRtcCSharp.LoggingSeverity.LS_INFO);
#elif (!UNITY_WEBGL || UNITY_EDITOR)
            Byn.Net.Native.NativeWebRtcNetworkFactory.LogNative(WebRtcCSharp.LoggingSeverity.LS_INFO);
#else
            //webgl. logging isn't supported here and has to be done via the browser.
            Debug.LogWarning("Platform doesn't support native webrtc logging.");
#endif
        }

        if (UnityCallFactory.Instance == null)
        {
            Debug.LogError("UnityCallFactory failed to initialize");
        }

        //not yet implemented
        //CustomUnityVideo.Instance.Register();

    }


    private static void OnLog(object msg, string[] tags)
    {
        StringBuilder builder = new StringBuilder();
        bool warning = false;
        bool error = false;
        builder.Append("TAGS:[");
        foreach (var v in tags)
        {
            builder.Append(v);
            builder.Append(",");
            if (v == SLog.TAG_ERROR || v == SLog.TAG_EXCEPTION)
            {
                error = true;
            }
            else if (v == SLog.TAG_WARNING)
            {
                warning = true;
            }
        }
        builder.Append("]");
        builder.Append(msg);
        if (error)
        {
            LogError(builder.ToString());
        }
        else if (warning)
        {
            LogWarning(builder.ToString());
        }
        else
        {
            Log(builder.ToString());
        }
    }

    private static void Log(string s)
    {
        if (s.Length > 2048 && Application.platform != RuntimePlatform.Android)
        {
            foreach (string splitMsg in SplitLongMsgs(s))
            {
                Debug.Log(splitMsg);
            }
        }
        else
        {
            Debug.Log(s);
        }
    }
    private static void LogWarning(string s)
    {
        if (s.Length > 2048 && Application.platform != RuntimePlatform.Android)
        {
            foreach (string splitMsg in SplitLongMsgs(s))
            {
                Debug.LogWarning(splitMsg);
            }
        }
        else
        {
            Debug.LogWarning(s);
        }
    }
    private static void LogError(string s)
    {
        if(s.Length  > 2048 && Application.platform != RuntimePlatform.Android)
        {
            foreach(string splitMsg in SplitLongMsgs(s))
            {
                Debug.LogError(splitMsg);
            }
        }
        else
        {
            Debug.LogError(s);
        }
    }

    private static string[] SplitLongMsgs(string s)
    {
        const int maxLength = 2048;
        int count = s.Length / maxLength + 1;
        string[] messages = new string[count];
        for(int i = 0; i < count; i++)
        {
            int start = i * maxLength;
            int length = s.Length - start;
            if (length > maxLength)
                length = maxLength;
            messages[i] = "[" + (i+1) + "/" + count +"]" + s.Substring(start, length);

        }
        return messages;
    }
    

    protected virtual NetworkConfig CreateNetworkConfig()
    {
        NetworkConfig netConfig = new NetworkConfig();
        netConfig.IceServers.Add(new IceServer(uIceServer, uIceServerUser, uIceServerPassword));
        netConfig.IceServers.Add(new IceServer(uIceServer2));
        netConfig.SignalingUrl = uSignalingUrl;
        return netConfig;
    }

    /// <summary>
    /// Creates the call object and uses the configure method to activate the 
    /// video / audio support if the values are set to true.
    /// </summary>
    /// generating new frames after this call so the user can see himself before
    /// the call is connected.</param>
    public virtual void SetupCall()
    {
        Append("Setting up ...");

        //hacks to turn off certain connection types. If both set to true only
        //turn servers are used. This helps simulating a NAT that doesn't support
        //opening ports.
        //hack to turn off direct connections
        //Byn.Net.Native.AWebRtcPeer.sDebugIgnoreTypHost = true;
        //hack to turn off connections via stun servers
        //Byn.Net.Native.WebRtcDataPeer.sDebugIgnoreTypSrflx = true;

        NetworkConfig netConfig = CreateNetworkConfig();


        Debug.Log("Creating call using NetworkConfig:" + netConfig);
        //setup the server
        mCall = UnityCallFactory.Instance.Create(netConfig);
        if (mCall == null)
        {
            Append("Failed to create the call");
            return;
        }
        string[] devices = UnityCallFactory.Instance.GetVideoDevices();
        if (devices == null || devices.Length == 0)
        {
            Debug.Log("no device found or no device information available");
        }
        else
        {
            foreach (string s in devices)
                Debug.Log("device found: " + s);
        }
        Append("Call created!");
        mCall.CallEvent += Call_CallEvent;

        mMediaConfig = CreateMediaConfig();

        Debug.Log("Configure call using MediaConfig: " + mMediaConfig);
        mCall.Configure(mMediaConfig);
        mUi.SetGuiState(false);
    }



    /// <summary>
    /// Handler of call events.
    /// 
    /// Can be customized in via subclasses.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected virtual void Call_CallEvent(object sender, CallEventArgs e)
    {
        switch (e.Type)
        {
            case CallEventType.CallAccepted:
                //Outgoing call was successful or an incoming call arrived
                Append("Connection established");
                mRemoteUserId = ((CallAcceptedEventArgs)e).ConnectionId;
                Debug.Log("New connection with id: " + mRemoteUserId
                    + " audio:" + mCall.HasAudioTrack(mRemoteUserId)
                    + " video:" + mCall.HasVideoTrack(mRemoteUserId));
                break;
            case CallEventType.CallEnded:
                //Call was ended / one of the users hung up -> reset the app
                Append("Call ended");
                ResetCall();
                break;
            case CallEventType.ListeningFailed:
                //listening for incoming connections failed
                //this usually means a user is using the string / room name already to wait for incoming calls
                //try to connect to this user
                //(note might also mean the server is down or the name is invalid in which case call will fail as well)
                mCall.Call(mUseAddress);
                break;

            case CallEventType.ConnectionFailed:
                {
                    Byn.Media.ErrorEventArgs args = e as Byn.Media.ErrorEventArgs;
                    Append("Connection failed error: " + args.ErrorMessage);
                    ResetCall();
                }
                break;
            case CallEventType.ConfigurationFailed:
                {
                    Byn.Media.ErrorEventArgs args = e as Byn.Media.ErrorEventArgs;
                    Append("Configuration failed error: " + args.ErrorMessage);
                    ResetCall();
                }
                break;

            case CallEventType.FrameUpdate:
                {

                    //new frame received from webrtc (either from local camera or network)
                    if (e is FrameUpdateEventArgs)
                    {
                        UpdateFrame((FrameUpdateEventArgs)e);
                    }
                    break;
                }

            case CallEventType.Message:
                {
                    //text message received
                    MessageEventArgs args = e as MessageEventArgs;
                    Append(args.Content);
                    break;
                }
            case CallEventType.WaitForIncomingCall:
                {
                    //the chat app will wait for another app to connect via the same string
                    WaitForIncomingCallEventArgs args = e as WaitForIncomingCallEventArgs;
                    Append("Waiting for incoming call address: " + args.Address);
                    break;
                }
        }

    }

    /// <summary>
    /// Destroys the call. Used if unity destroys the object or if a call
    /// ended / failed due to an error.
    /// 
    /// </summary>
    protected virtual void CleanupCall()
    {
        if (mCall != null)
        {
            mRemoteUserId = ConnectionId.INVALID;
            Debug.Log("Destroying call!");
            mCall.CallEvent -= Call_CallEvent;
            mCall.Dispose();
            mCall = null;
            //call the garbage collector. This isn't needed but helps to discover memory issues early on
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Debug.Log("Call destroyed");
        }
    }


    /// <summary>
    /// Create the default configuration for this CallApp instance.
    /// This can be overwritten in a subclass allowing the creation custom apps that
    /// use a slightly different configuration.
    /// </summary>
    /// <returns></returns>
    public virtual MediaConfig CreateMediaConfig()
    {
        MediaConfig mediaConfig = new MediaConfig();
        //testing echo cancellation (native only)
        bool useEchoCancellation = false;
        if(useEchoCancellation)
        {
#if !UNITY_WEBGL
            var nativeConfig = new Byn.Media.Native.NativeMediaConfig();
            nativeConfig.AudioOptions.echo_cancellation = true;
            nativeConfig.AudioOptions.extended_filter_aec = true;
            nativeConfig.AudioOptions.delay_agnostic_aec = true;

            mediaConfig = nativeConfig;
#endif 
        }



        //use video and audio by default (the UI is toggled on by default as well it will change on click )
        mediaConfig.Audio = mUseAudio;
        mediaConfig.Video = mUseVideo;
        mediaConfig.VideoDeviceName = mUseVideoDevice;

        //keep the resolution low.
        //This helps avoiding problems with very weak CPU's and very high resolution cameras
        //(apparently a problem with win10 tablets)
        mediaConfig.MinWidth = 160;
        mediaConfig.MinHeight = 120;
        mediaConfig.MaxWidth = 1920;
        mediaConfig.MaxHeight = 1080;
        mediaConfig.IdealWidth = 320;
        mediaConfig.IdealHeight = 240;
        return mediaConfig;
    }

    /// <summary>
    /// Destroys the call object and shows the setup screen again.
    /// Called after a call ends or an error occurred.
    /// </summary>
    public virtual void ResetCall()
    {
        CleanupCall();
        mUi.SetGuiState(true);
    }

    public virtual void SetRemoteVolume(float volume)
    {
        if (mCall == null)
            return;
        if(mRemoteUserId == ConnectionId.INVALID)
        {
            return;
        }
        mCall.SetVolume(volume, mRemoteUserId);
    }


    /// <summary>
    /// Returns a list of video devices for the UI to show.
    /// </summary>
    /// <returns></returns>
    public string[] GetVideoDevices()
    {
        if (CanSelectVideoDevice())
        {
            List<string> devices = new List<string>();
            string[] videoDevices = UnityCallFactory.Instance.GetVideoDevices();
            devices.Add("Any");
            devices.AddRange(videoDevices);
            return devices.ToArray();
        }
        else
        {
            return new string[] { "Default" };
        };
    }

    /// <summary>
    /// Used by the UI
    /// </summary>
    /// <returns></returns>
    public bool CanSelectVideoDevice()
    {
        return UnityCallFactory.Instance.CanSelectVideoDevice();
    }

    /// <summary>
    /// Called by UI when the join buttin is pressed.
    /// </summary>
    /// <param name="address"></param>
    public virtual void Join(string address)
    {
        if (address.Length > MAX_CODE_LENGTH)
            throw new ArgumentException("Address can't be longer than " + MAX_CODE_LENGTH);
        mUseAddress = address;
        Debug.Log("Try listing on address: " + mUseAddress);
        this.mCall.Listen(mUseAddress);
    }

    /// <summary>
    /// Called by ui to send a message.
    /// </summary>
    /// <param name="msg"></param>
    public virtual void Send(string msg)
    {
        this.mCall.Send(msg);
    }


    public void UseAudio(bool value)
    {
        mUseAudio = value;
    }

    public void UseVideo(bool value)
    {
        mUseVideo = value;
    }
    public void UseVideoDevice(string deviceName)
    {
        mUseVideoDevice = deviceName;
    }

    protected virtual void UpdateFrame(FrameUpdateEventArgs frameUpdateEventArgs)
    {
        //the avoid wasting CPU time the library uses the format returned by the browser -> ABGR little endian thus
        //the bytes are in order R G B A
        //Unity seem to use this byte order but also flips the image horizontally (reading the last row first?)
        //this is reversed using UI to avoid wasting CPU time

        //Debug.Log("frame update remote: " + frameUpdateEventArgs.IsRemote);

        if (frameUpdateEventArgs.IsRemote == false)
        {
            mUi.UpdateLocalTexture(frameUpdateEventArgs.Frame, frameUpdateEventArgs.Format);
        }
        else
        {
            mUi.UpdateRemoteTexture(frameUpdateEventArgs.Frame, frameUpdateEventArgs.Format);
        }
    }


    private void Append(string txt)
    {
        mUi.Append(txt);
    }
}
