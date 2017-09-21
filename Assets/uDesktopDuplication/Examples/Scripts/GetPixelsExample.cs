using System.Runtime.InteropServices;
using UnityEngine;

public class GetPixelsExample : MonoBehaviour
{
    [SerializeField] uDesktopDuplication.Texture uddTexture;

    [SerializeField] int x = 0;
    [SerializeField] int y = 0;
    const int width = 1919;
    const int height = 1079;
    
    public Texture2D texture;
    Color32[] colores = new Color32[width * height];
	[StructLayout(LayoutKind.Explicit)]
	public struct Color32Bytes {
		[FieldOffset(0)]
		public byte[] byteArray;

		[FieldOffset(0)]
		public Color32[] colors;
	}

    void Start()
    {
        texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        GetComponent<Renderer>().material.mainTexture = texture;
    }

    void Update()
    {
        // must be called (performance will be slightly down).
        uDesktopDuplication.Manager.primary.useGetPixels = true;

        var monitor = uddTexture.monitor;
        if (!monitor.hasBeenUpdated) return;

		Color32Bytes data = new Color32Bytes();
/*
		data.colors = new Color32[width * height];
        if (monitor.GetPixels(data.colors, x, y, width, height)) {
			byte[] losbaites = data.byteArray;
			Color32Bytes newdata = new Color32Bytes();
			newdata.byteArray = losbaites;
			texture.SetPixels32(newdata.colors);
			texture.Apply();
			//			texture.SetPixels32(colores);
			//            texture.Apply();
		}

*/


		//Debug.Log(monitor.GetPixel(monitor.cursorX, monitor.cursorY));
    }
}
