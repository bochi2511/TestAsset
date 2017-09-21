/* 
 * Copyright (C) 2015 Christoph Kutza
 * 
 * Please refer to the LICENSE file for license information
 */
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Byn.Media;

/// <summary>
/// This class + prefab is a complete app allowing to call another app using a shared text or password
/// to meet online.
/// 
/// It supports Audio, Video and Text chat. Audio / Video can optionally turned on/off via toggles.
/// 
/// After the join button is pressed the (first) app will initialize a native webrtc plugin 
/// and contact a server to wait for incoming connections under the given string.
/// 
/// Another instance of the app can connect using the same string. (It will first try to
/// wait for incoming connections which will fail as another app is already waiting and after
/// that it will connect to the other side)
/// 
/// The important methods are "Setup" to initialize the call class (after join button is pressed) and
/// "Call_CallEvent" which reacts to events triggered by the class.
/// 
/// Also make sure to use your own servers for production (uSignalingUrl and uStunServer).
/// 
/// NOTE: Currently, only 1 to 1 connections are supported. This will change in the future.
/// </summary>
public class CallAppUi : MonoBehaviour
{

    /// <summary>
    /// Texture of the local video
    /// </summary>
    protected Texture2D mLocalVideoTexture = null;

    /// <summary>
    /// Texture of the remote video
    /// </summary>
    protected Texture2D mRemoteVideoTexture = null;
    

    [Header("Setup panel")]
    /// <summary>
    /// Panel with the join button. Will be hidden after setup
    /// </summary>
    public RectTransform uSetupPanel;

    /// <summary>
    /// Input field used to enter the room name.
    /// </summary>
    public InputField uRoomNameInputField;
    /// <summary>
    /// Join button to connect to a server.
    /// </summary>
    public Button uJoinButton;

    public Toggle uAudioToggle;
    public Toggle uVideoToggle;
    public Dropdown uVideoDropdown;

    [Header("Video and Chat panel")]
    public RectTransform uInCallBase;
    public RectTransform uVideoPanel;
    public RectTransform uChatPanel;

    [Header("Default positions/transformations")]
    public RectTransform uVideoBase;
    public RectTransform uChatBase;


    [Header("Fullscreen positions/transformations")]
    public RectTransform uFullscreenPanel;
    public RectTransform uVideoBaseFullscreen;
    public RectTransform uChatBaseFullscreen;




    [Header("Chat panel elements")]
    /// <summary>
    /// Input field to enter a new message.
    /// </summary>
    public InputField uMessageInputField;

    /// <summary>
    /// Output message list to show incoming and sent messages + output messages of the
    /// system itself.
    /// </summary>
    public MessageList uMessageOutput;


    /// <summary>
    /// Send button.
    /// </summary>
    public Button uSendMessageButton;

    /// <summary>
    /// Shutdown button. Disconnects all connections + shuts down the server if started.
    /// </summary>
    public Button uShutdownButton;

    /// <summary>
    /// Slider to just the remote users volume.
    /// </summary>
    public Slider uVolumeSlider;


    [Header("Video panel elements")]
    /// <summary>
    /// Image of the local camera
    /// </summary>
    public RawImage uLocalVideoImage;

    /// <summary>
    /// Image of the remote camera
    /// </summary>
    public RawImage uRemoteVideoImage;
    

    [Header("Resources")]
    public Texture2D uNoCameraTexture;

    protected bool mFullscreen = false;


    protected CallApp mApp;


    protected virtual void Awake()
    {
        mApp = GetComponent<CallApp>();
    }

    


    /// <summary>
    /// Updates the texture based on the given frame update.
    /// 
    /// Returns true if a complete new texture was created
    /// </summary>
    /// <param name="tex"></param>
    /// <param name="frame"></param>
    protected bool UpdateTexture(ref Texture2D tex, RawFrame frame, FramePixelFormat format)
    {
        bool newTextureCreated = false;
        //texture exists but has the wrong height /width? -> destroy it and set the value to null
        if (tex != null && (tex.width != frame.Width || tex.height != frame.Height))
        {
            Texture2D.Destroy(tex);
            tex = null;
        }
        //no texture? create a new one first
        if (tex == null)
        {
            newTextureCreated = true;
            Debug.Log("Creating new texture with resolution " + frame.Width + "x" + frame.Height + " Format:" + format);

            //so far only ABGR is really supported. this will change later
            if (format == FramePixelFormat.ABGR)
            {
                tex = new Texture2D(frame.Width, frame.Height, TextureFormat.RGBA32, false);
            }
            else
            {
                Debug.LogWarning("YUY2 texture is set. This is only for testing");
                tex = new Texture2D(frame.Width, frame.Height, TextureFormat.YUY2, false);
            }
            tex.wrapMode = TextureWrapMode.Clamp;
        }
        //copy image data into the texture and apply
        //Watch out the RawImage has the top pixels in the top row but
        //unity has the top pixels in the bottom row. Result is an image that is
        //flipped. Fixing this here would waste a lot of CPU power thus
        //the UI will simply set scale.Y of the UI element to -1 to reverse this.
        tex.LoadRawTextureData(frame.Buffer);
        tex.Apply();
        return newTextureCreated;
    }

    /// <summary>
    /// Updates the local video. If the frame is null it will hide the video image
    /// </summary>
    /// <param name="frame"></param>
    public virtual void UpdateLocalTexture(RawFrame frame, FramePixelFormat format)
    {
        if (uLocalVideoImage != null)
        {
            if (frame != null)
            {
                UpdateTexture(ref mLocalVideoTexture, frame, format);
                uLocalVideoImage.texture = mLocalVideoTexture;
                if (uLocalVideoImage.gameObject.activeSelf == false)
                {
                    uLocalVideoImage.gameObject.SetActive(true);
                }
                //apply rotation
                //watch out uLocalVideoImage should be scaled -1 X to make the local camera appear mirrored
                //it should also be scaled -1 Y because Unity reads the image from bottom to top
                //uLocalVideoImage.transform.rotation = Quaternion.Euler(0, 0, frame.Rotation);
            }
            else
            {
                uLocalVideoImage.texture = null;
                //uRemoteVideoImage.transform.rotation = Quaternion.Euler(0, 0, 0);
                uLocalVideoImage.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Updates the remote video. If the frame is null it will hide the video image.
    /// </summary>
    /// <param name="frame"></param>
    public virtual void UpdateRemoteTexture(RawFrame frame, FramePixelFormat format)
    {
        if (uRemoteVideoImage != null)
        {
            if (frame != null)
            {
                bool changed = UpdateTexture(ref mRemoteVideoTexture, frame, format);
                uRemoteVideoImage.texture = mRemoteVideoTexture;
                //watch out: due to conversion from WebRTC to Unity format the image is flipped (top to bottom)
                //this also inverts the rotation
                //uRemoteVideoImage.transform.rotation = Quaternion.Euler(0, 0, frame.Rotation * -1);
            }
            else
            {
                uRemoteVideoImage.texture = uNoCameraTexture;
                //uRemoteVideoImage.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
        }
    }

    /// <summary>
    /// toggle audio on / off
    /// </summary>
    /// <param name="state"></param>
    public void AudioToggle(bool state)
    {
        mApp.UseAudio(state);
    }

    /// <summary>
    /// toggle video on / off
    /// </summary>
    /// <param name="state"></param>
    public void VideoToggle(bool state)
    {
        mApp.UseVideo(state);
        UpdateVideoDropdown();
    }


    /// <summary>
    /// Updates the dropdown menu based on the current video devices and toggle status
    /// </summary>
    public void UpdateVideoDropdown()
    {
        uVideoDropdown.ClearOptions();
        uVideoDropdown.AddOptions(new List<string>(mApp.GetVideoDevices()));
        uVideoDropdown.interactable = mApp.CanSelectVideoDevice();
    }
    public void VideoDropdownOnValueChanged(int index)
    {
        if (index <= 0)
        {
            mApp.UseVideoDevice(null);
        }
        else
        {
            string dev = uVideoDropdown.options[index].text;
            mApp.UseVideoDevice(dev);
            Debug.Log("New video device selected: " + dev);
        }
    }


    /// <summary>
    /// Adds a new message to the message view
    /// </summary>
    /// <param name="text"></param>
    public void Append(string text)
    {
        if (uMessageOutput != null)
        {
            uMessageOutput.AddTextEntry(text);
        }
        Debug.Log("Chat output: " + text);
    }

    private void SetFullscreen(bool value)
    {
        mFullscreen = value;
        if (mFullscreen)
        {
            uVideoPanel.SetParent(uVideoBaseFullscreen, false);
            uChatPanel.SetParent(uChatBaseFullscreen, false);
            uInCallBase.gameObject.SetActive(false);
            uFullscreenPanel.gameObject.SetActive(true);
        }
        else
        {
            uVideoPanel.GetComponent<RectTransform>().SetParent(uVideoBase, false);
            uChatPanel.GetComponent<RectTransform>().SetParent(uChatBase, false);
            uInCallBase.gameObject.SetActive(true);
            uFullscreenPanel.gameObject.SetActive(false);
        }
    }
    public void Fullscreen()
    {

        bool newValues = !mFullscreen;

        //just in case: make sure fullscreen button is ignored if in setup mode
        if (newValues == true && uSetupPanel.gameObject.activeSelf)
            return;
        SetFullscreen(newValues);
    }
    /// <summary>
    /// Shows the setup screen or the chat + video
    /// </summary>
    /// <param name="showSetup">true Shows the setup. False hides it.</param>
    public void SetGuiState(bool showSetup)
    {
        uSetupPanel.gameObject.SetActive(showSetup);

        uSendMessageButton.interactable = !showSetup;
        uShutdownButton.interactable = !showSetup;
        uMessageInputField.interactable = !showSetup;

        //this is going to hide the textures until it is updated with a new frame update
        UpdateLocalTexture(null, FramePixelFormat.Invalid);
        UpdateRemoteTexture(null, FramePixelFormat.Invalid);
        SetFullscreen(false);
    }

    /// <summary>
    /// Join button pressed. Tries to join a room.
    /// </summary>
    public void JoinButtonPressed()
    {
        mApp.SetupCall();
        EnsureLength();
        Append("Trying to listen on address " + uRoomNameInputField.text);
        mApp.Join(uRoomNameInputField.text);
    }

    private void EnsureLength()
    {
        if (uRoomNameInputField.text.Length > CallApp.MAX_CODE_LENGTH)
        {
            uRoomNameInputField.text = uRoomNameInputField.text.Substring(0, CallApp.MAX_CODE_LENGTH);
        }
    }

    public string GetRoomname()
    {
        EnsureLength();
        return uRoomNameInputField.text;
    }

    /// <summary>
    /// This is called if the send button
    /// </summary>
    public void SendButtonPressed()
    {
        //get the message written into the text field
        string msg = uMessageInputField.text;
        SendMsg(msg);
    }

    /// <summary>
    /// User either pressed enter or left the text field
    /// -> if return key was pressed send the message
    /// </summary>
    public void InputOnEndEdit()
    {
        if (Input.GetKey(KeyCode.Return))
        {
            string msg = uMessageInputField.text;
            SendMsg(msg);
        }
    }

    /// <summary>
    /// Sends a message to the other end
    /// </summary>
    /// <param name="msg"></param>
    private void SendMsg(string msg)
    {
        if (String.IsNullOrEmpty(msg))
        {
            //never send null or empty messages. webrtc can't deal with that
            return;
        }

        Append(msg);
        mApp.Send(msg);

        //reset UI
        uMessageInputField.text = "";
        uMessageInputField.Select();
    }



    /// <summary>
    /// Shutdown button pressed. Shuts the network down.
    /// </summary>
    public void ShutdownButtonPressed()
    {
        mApp.ResetCall();
    }

    public void OnVolumeChanged(float value)
    {
        mApp.SetRemoteVolume(value);
    }
}
