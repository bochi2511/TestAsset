using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayCastCustomNet : MonoBehaviour {

	public Vector3 origin;
	public Vector3 direction;
	private LineRenderer m_line;
	private SteamVR_TrackedController device;
	private float m_LastClickStart = 0f;

	// Use this for initialization
	void Start () {
		m_line = GetComponent<LineRenderer>();
		device = GetComponent<SteamVR_TrackedController>();
	}


	// Update is called once per frame
	void Update () {
		origin = gameObject.transform.position;
		direction = gameObject.transform.rotation * new Vector3(0, 0, 100);
		bool onehit = false;
		foreach (var uddTexture in GameObject.FindObjectsOfType<uDesktopDuplication.Texture>()) {
			var result = uddTexture.RayCast(origin, direction);
			if (result.hit) {
				m_line.enabled = true;
				m_line.SetPosition(0, origin);
				m_line.SetPosition(1, result.position);
				onehit = true;
//				int ix = (int)result.desktopCoord.x;
//				int iy = (int)result.desktopCoord.y;
//				uDesktopDuplication.Utility.SetCursorPos(ix, iy);
			}
		}
		if (!onehit) m_line.enabled = false;
	}
}
