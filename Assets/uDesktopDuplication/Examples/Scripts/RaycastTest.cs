using UnityEngine;
using System.Collections;

public class RaycastTest : MonoBehaviour
{
    public Transform from;
    public Transform to;
	private Vector3 direction;
	private LineRenderer m_line;

	private void Start() {
		m_line = GetComponent<LineRenderer>();
	}
	void Update()
    {
		//		if (!from || !to) return;
		//        Debug.DrawLine(from.position, to.position, Color.red);
		direction = from.rotation * new Vector3(0, 0, 100);
		Debug.DrawRay(from.position, direction, Color.red);
        foreach (var uddTexture in GameObject.FindObjectsOfType<uDesktopDuplication.Texture>()) {
            var result = uddTexture.RayCast(from.position, direction);
            if (result.hit) {
				m_line.enabled = true;
				m_line.SetPosition(0, from.position);
				m_line.SetPosition(1, result.position);
				int ix = (int)result.desktopCoord.x;
				int iy = (int)result.desktopCoord.y;
				uDesktopDuplication.Utility.SetCursorPos(ix, iy);
				Debug.DrawLine(result.position, result.position + result.normal, Color.yellow);
                Debug.Log("COORD: " + result.coords + ", DESKTOP: " + result.desktopCoord);
            }
        }
    }
}
