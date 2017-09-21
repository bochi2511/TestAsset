using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetMonitorID : MonoBehaviour {

	public int fixedID = 0;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		var texture = GetComponent<uDesktopDuplication.Texture>();
		var id = texture.monitorId;
		var n = uDesktopDuplication.Manager.monitorCount;
		if (id != fixedID) {
			if (0 <= fixedID && fixedID <= n) {
				texture.monitorId = fixedID;
			}
		}
	}
}
