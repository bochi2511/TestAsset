Introduction:
    WebRTC Video Chat is a unity plugin which allows you to stream audio, video and send text messages between
    two programs. You can use it to create a live video and audio stream similar to features of Skype, Google Hangout
    or Teamspeak and integrate it directly into your own unity project.
    
    The assets comes with a fully functional open source example to demonstrate how to create a video chat. If this
    is all you need you can simply change the UI to your liking and use it without any programming.
    
    
How does it work?
    The plugin automatically handles all audio/video and network functionality for you. All you need is to
    create a Call object and connect it to the other side and using a shared password. An event handler will
    then return events when a user connects, sends a message, a new video frame is received and so on.
    The video frames will be returned as a raw image. You can then copy it into a texture (see example) or
    use it for other libraries e.g for facial recognition or applying filters.
    
    So far Unity Editor in Windows + Standalone Windows x86 and x64 is supported. Browsers and mobile platforms
    will be supported in the near future. Conference calls with multiple users will be added 
    in an update as well. Note that the network functionality requires a server to work. I will provide
    a development server for free but you will need to set up your own server for production.
    
    Checkout http://because-why-not.com/webrtc/webrtc-network/ for more information, documentation, API
    and test a free sample application.

    Note: This asset uses shared dependencies with WebRtcNetwork. Please read the description  of WebRtcNetwork
    as well. 
===============================================================================================================

Setup:
	Make sure you tick "Run in background" in your player settings. + add the example scenes to your build settings.
    Make sure your platform is set to PC / Mac / Linux Standalone.
    

    for Android: 
        Don't forget to configure your Assets\Plugins\Android\AndroidManifest.xml file. You can find an
        example at WebRtcNetwork\Plugins\android\AndroidManifest.xml.
        
        You need at least following permission:
            <uses-permission android:name="android.permission.INTERNET" />
            <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
            <uses-permission android:name="android.permission.CHANGE_NETWORK_STATE" />
        
        For Video:
            <uses-permission android:name="android.permission.CAMERA" />
            <uses-feature android:name="android.hardware.camera" android:required="false" />
            <uses-feature android:name="android.hardware.camera.autofocus" android:required="false" />
            <uses-feature android:name="android.hardware.camera.front" android:required="false" />
            
        For Audio:
            <uses-permission android:name="android.permission.MODIFY_AUDIO_SETTINGS" />
            <uses-permission android:name="android.permission.RECORD_AUDIO" />
        
        Also have a look at AndroidHelper.SetSpeakerOn. WebRTC treats the audio connection as a phone call on Android thus
        the speaker has to be turned on to have a proper volume.
        
Example CallApp:
    The callscene contains two instances of the CallApp allowing you to test the library within a single application or
    run the same program on two different pc's and connect. It supports streaming audio, video and sending text messges
    to the other side. Simply enter a shared text or password on both sides and press the join button. The app will
    connect both apps and start streaming audio and video (if available).

Important folders, files and classes:
    This library shares dependencies with WebRtcNetwork. Please see the WebRtcNetwork/readme.txt for everything about
    WebRtcNetwork.
    
    