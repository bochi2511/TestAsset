using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class DoubleTab : MonoBehaviour, IPointerClickHandler
{
    public UnityEvent onDoubleTab;
    private float mLastClick;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if((eventData.clickTime - mLastClick) < 0.5f)
        {
            if(onDoubleTab != null)
            {
                onDoubleTab.Invoke();
            }
        }
        mLastClick = eventData.clickTime;
    }
}
