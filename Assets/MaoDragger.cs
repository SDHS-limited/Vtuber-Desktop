using UnityEngine;
using UnityEngine.EventSystems;

public class MaoDragger : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private Camera _cam;
    private Vector3 _offset;

    void Start()
    {
        _cam = Camera.main;
        Debug.Log("[MaoDragger] 드레거 시작 " + gameObject.name);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
       
        Vector3 worldPos = _cam.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, Mathf.Abs(transform.position.z - _cam.transform.position.z)));
        
        
        _offset = transform.position - worldPos;
        
        //Debug.Log("[MaoDragger] >>> Pointer DOWN - Drag Start <<<");
    }

    public void OnDrag(PointerEventData eventData)
    {
        
        Vector3 worldPos = _cam.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, Mathf.Abs(transform.position.z - _cam.transform.position.z)));
        transform.position = worldPos + _offset;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        //Debug.Log("[MaoDragger] <<< Pointer UP - Drag End >>>");
    }
}
