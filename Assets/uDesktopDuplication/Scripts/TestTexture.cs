using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine;

namespace uDesktopDuplication
{ 
	public class TestTexture : MonoBehaviour {

		public Renderer rend;

		// Use this for initialization
		void Start () {
			rend = GetComponent<Renderer>();
		}
	
		// Update is called once per frame
		void LateUpdate ()
		{

		
			Monitor monitor;
			Texture2D tex, newTex;

			if (Input.GetKeyDown(KeyCode.T)) {
		
				monitor = Manager.GetMonitor(0);
				tex = monitor.texture;
				byte[] texArray;
				byte[] textureImage;
				texArray = tex.GetRawTextureData();
				textureImage = tex.EncodeToPNG();
				File.WriteAllBytes(Application.dataPath + "/../ScreenTexture.png", textureImage);
				Debug.Log("texture size: " + texArray.Length);
				newTex = new Texture2D(tex.width, tex.height, tex.format, tex.mipmapCount > 1);
				//newTex.LoadRawTextureData(texArray);
				//newTex.Apply();
				newTex.LoadImage(textureImage);
				rend.material.mainTexture = newTex;


			}

		}
	}

}