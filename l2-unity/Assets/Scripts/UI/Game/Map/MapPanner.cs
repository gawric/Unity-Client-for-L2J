using UnityEngine;
using UnityEngine.UIElements;

public class MapPanner 
{
    private VisualElement _viewport;
    private VisualElement _map;
    private VisualElement _marker;

    private Vector2 lastPointer;
    private Vector2 offset; 

    private bool dragging;

    private const float VIEW_W = 294f;
    private const float VIEW_H = 300f;
    private const float MAP_W = 1812f;
    private const float MAP_H = 2620f;
    private int offsetMap = 10;
    private Vector3 MIN_POS_LEFT = VectorUtils.ConvertPosToUnity(new Vector3(-127845f, 259384, -4530));
    private Vector3 MAX_POS_TOP = VectorUtils.ConvertPosToUnity(new Vector3(197885f, -258615f , -4672f));
    private Button _buttonLocation;
    private bool _isDisabled = false;
    private bool _isManualUpdate = false;

    private Vector2 _mapPos = Vector2.zero;
    //setting map
    Vector2 mapSize = new Vector2(MAP_H, MAP_W);
    Vector2 mapSizeD = new Vector2(MAP_W, MAP_H);


    public void SetElements(VisualElement viewport, VisualElement map, VisualElement minmapElement , Button buttonLocation)
    {
        _viewport = viewport;
        _viewport.pickingMode = PickingMode.Position;
        _marker = minmapElement;
        _map = map;
        _buttonLocation = buttonLocation;
        _buttonLocation.RegisterCallback<MouseUpEvent>(MoveMarkerToPoint);
        _marker.RegisterCallback<GeometryChangedEvent>(OnMarkerGeometryChanged);
        _viewport.RegisterCallback<GeometryChangedEvent>(MoveMarkerToPoint);

        viewport.RegisterCallback<PointerDownEvent>(evt => {
            dragging = true;
            lastPointer = evt.localPosition; // инициализируем, чтобы избежать прыжка
            viewport.CapturePointer(evt.pointerId);
        });

    }

    public void SetManualUpdate(bool isManualUpdate)
    {
        _isManualUpdate = isManualUpdate;
    }

    public void SetDisabled(bool disabled)
    {
        _isDisabled = disabled;
    }
    public void RegisterCallback()
    {

        _viewport.RegisterCallback<PointerDownEvent>(OnPointerDown);
        _viewport.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        _viewport.RegisterCallback<PointerUpEvent>(OnPointerUp);
        _viewport.RegisterCallback<PointerCancelEvent>(OnPointerCancel);

    }

    void UnRegisterCallback()
    {
        if (_viewport != null)
        {
            _viewport.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            _viewport.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _viewport.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            _viewport.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
        }
    }



    void MoveMarkerToPoint(GeometryChangedEvent up)
    {
        MoveMarker(false);

    }

    //event click button Location
    void MoveMarkerToPoint(MouseUpEvent up)
    {
        if (_isDisabled) return;
        MapUtils.PanViewportToMarker(ref offset, _map, _viewport, _marker);
    }

    public void UpdateMarker(bool isHideWindow)
    {
        MoveMarker(isHideWindow);
    }

    private void MoveMarker(bool isHideWindow)
    {
        if (_map == null || _marker == null || _isDisabled || isHideWindow) return;

        Vector3 worldPos2D = PlayerController.Instance.GetPlayerPosition();
          //Vector3 worldPos2D = StorageNpc.getInstance().GetFirstUser().PlayerInfoInterlude.Identity.GetXZPos();
        //Debug.Log($"MoveMarker position user : {worldPos2D}");

        Vector2 _mapPos = MapUtils.WorldToMap(worldPos2D, MIN_POS_LEFT, MAX_POS_TOP, mapSize, useXZ: true, invertYForUI: false);

        MapUtils.PlaceMarkerOnMap(ref offset, _map, _marker, new Vector2(_mapPos.y - offsetMap, _mapPos.x - offsetMap), mapSizeD);
    }

    private void OnMarkerGeometryChanged(GeometryChangedEvent evt)
    {
        if(_isDisabled) return;

        if(evt.newRect.x == 0) return;

        if (_isManualUpdate == true)
        {
            _isManualUpdate = false;
            MapUtils.PanViewportToMarker(ref offset, _map, _viewport, _marker);
        }

    }

    void OnPointerDown(PointerDownEvent evt)
    {

       
        _viewport.CapturePointer(evt.pointerId);
        dragging = true;
        lastPointer = evt.localPosition;
        evt.StopPropagation();
    }

    void OnPointerMove(PointerMoveEvent evt)
    {
       
        if (!dragging) return;

        Vector2 local = evt.localPosition;
        Vector2 delta = local - lastPointer;


        offset -= delta;


        float maxX = Mathf.Max(0f, MAP_W - VIEW_W);
        float maxY = Mathf.Max(0f, MAP_H - VIEW_H);
        offset.x = Mathf.Clamp(offset.x, 0f, maxX);
        offset.y = Mathf.Clamp(offset.y, 0f, maxY);


        _map.style.left = -offset.x;
        _map.style.top = -offset.y;

        lastPointer = local;
        evt.StopPropagation();
    }



void OnPointerUp(PointerUpEvent evt)
    {
       
        if (dragging)
        {
            _viewport.ReleasePointer(evt.pointerId);
            dragging = false;
            evt.StopPropagation();
        }
    }

    void OnPointerCancel(PointerCancelEvent evt)
    {
        if (dragging)
        {
            
            _viewport.ReleasePointer(evt.pointerId);
            dragging = false;
            evt.StopPropagation();
        }
    }

    public void MoveMarkerToOrigin()
    {
        _marker.style.left = 0;
        _marker.style.top = 0;

        _viewport.style.left = 0;
        _viewport.style.top = 0;
    }

}