using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayCastCustom : MonoBehaviour {

	public Vector3 origin;
	public Vector3 direction;
	private LineRenderer m_line;
	private SteamVR_TrackedController device;
	private float m_LastClickStart = 0f;

	// Use this for initialization
	void Start () {
		m_line = GetComponent<LineRenderer>();
		device = GetComponent<SteamVR_TrackedController>();
		device.TriggerClicked += Trigger;
		device.TriggerUnclicked += TriggerUnc;
		device.PadClicked += PadClicked;
		device.PadUnclicked += PadUnclicked;
	}

	void Trigger(object sender, ClickedEventArgs e) 
	{
		uDesktopDuplication.Utility.SimulateMouseLeftDown();
		m_LastClickStart = Time.time;
	}

	void TriggerUnc(object sender, ClickedEventArgs e) {
		uDesktopDuplication.Utility.SimulateMouseLeftUp();
	}

	void PadClicked(object sender, ClickedEventArgs e) 
	{
		uDesktopDuplication.Utility.SimulateMouseRightDown();
		m_LastClickStart = Time.time;
	}

	void PadUnclicked(object sender, ClickedEventArgs e) {
		uDesktopDuplication.Utility.SimulateMouseRightUp();
	}

	// Update is called once per frame
	void Update () {
		if (Time.time - m_LastClickStart < 0.5f) return;
		origin = gameObject.transform.position;
		direction = gameObject.transform.rotation * new Vector3(0, 0, 100);
		bool onehit = false;
		foreach (var uddTexture in GameObject.FindObjectsOfType<uDesktopDuplication.Texture>()) {
			var result = uddTexture.RayCast(origin, direction);
			if (result.hit) {
				m_line.enabled = true;
				m_line.SetPosition(0, origin);
				m_line.SetPosition(1, result.position);
				int ix = (int)result.desktopCoord.x;
				int iy = (int)result.desktopCoord.y;
				uDesktopDuplication.Utility.SetCursorPos(ix, iy);
				//				Debug.DrawLine(result.position, result.position + result.normal, Color.yellow);
				//				Debug.Log("COORD: " + result.coords + ", DESKTOP: " + result.desktopCoord);
				onehit = true;
			}
		}
		if (!onehit) m_line.enabled = false;
	}
}
