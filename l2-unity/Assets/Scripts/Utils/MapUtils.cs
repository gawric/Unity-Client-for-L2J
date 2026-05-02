using UnityEngine;
using UnityEngine.UIElements;

public static class MapUtils
{
    // worldPos: позиция в мировых координатах
    // bottomLeft, topRight: две точки углов (могут быть в любом порядке)
    // mapSize: размер UI-карты в пикселях/единицах (width, height)
    // useXZ: если мир в XZ плоскости, поставьте true (использует x и z)
    // invertYForUI: если в UI ноль вверху (top-left origin), поставьте true
    public static Vector2 WorldToMap(Vector3 worldPos, Vector3 cornerA, Vector3 cornerB, Vector2 mapSize, bool useXZ = false, bool invertYForUI = true)
    {
        // определяем min/max по осям
        float minX = Mathf.Min(cornerA.x, cornerB.x);
        float maxX = Mathf.Max(cornerA.x, cornerB.x);

        float minY = useXZ ? Mathf.Min(cornerA.z, cornerB.z) : Mathf.Min(cornerA.y, cornerB.y);
        float maxY = useXZ ? Mathf.Max(cornerA.z, cornerB.z) : Mathf.Max(cornerA.y, cornerB.y);

        float worldWidth = maxX - minX;
        float worldHeight = maxY - minY;

        const float EPS = 1e-6f;
        if (Mathf.Abs(worldWidth) < EPS) worldWidth = EPS;
        if (Mathf.Abs(worldHeight) < EPS) worldHeight = EPS;

        float wx = worldPos.x;
        float wy = useXZ ? worldPos.z : worldPos.y;

        // нормализация в [0,1]
        float nx = (wx - minX) / worldWidth;
        float ny = (wy - minY) / worldHeight;

        nx = Mathf.Clamp01(nx);
        ny = Mathf.Clamp01(ny);

        if (invertYForUI)
            ny = 1f - ny;

        // масштабируем на размеры UI
        float mapX = nx * mapSize.x;
        float mapY = ny * mapSize.y;

        return new Vector2(mapX, mapY);
    }

    public static bool PlaceMarkerOnMap(ref Vector2 offset , VisualElement map, VisualElement marker, Vector2 mapPos, Vector2 mapSizeFromWorldToMap,
                     float markerPivotX = 0.5f, float markerPivotY = 0.5f, bool worldYIsBottom = false)
    {
        float uiMapW = map.layout.width;
        float uiMapH = map.layout.height;

        // защитные проверки
        if (uiMapW <= 0f || uiMapH <= 0f) return false;
        if (mapSizeFromWorldToMap.x <= 0f || mapSizeFromWorldToMap.y <= 0f) return false;

        float sx = uiMapW / mapSizeFromWorldToMap.x;
        float sy = uiMapH / mapSizeFromWorldToMap.y;

        float x = mapPos.x * sx;
        float y = mapPos.y * sy;

        if (worldYIsBottom)
        {
            // инвертируем Y, если world Y отсчитывается снизу
            y = uiMapH - y;
        }

        float mw = marker.layout.width;
        float mh = marker.layout.height;

        float left = x - mw * markerPivotX;
        float top = y - mh * markerPivotY;

        // --- проверка: если текущее положение уже равно целевому, ничего не делаем ---
        // используем небольшую толерантность для сравнения float
        const float eps = 0.5f; // подберите по необходимости (пиксели)
        float currentLeft = marker.layout.x;
        float currentTop = marker.layout.y;

        if (Mathf.Abs(currentLeft - left) <= eps && Mathf.Abs(currentTop - top) <= eps)
        {
            return false;
        }

        marker.style.left = left;
        marker.style.top = top;

        return true;
    }

    // map: VisualElement, содержащая карту (ширина map.layout.width)
    // viewport: VisualElement, видимая область (viewport.layout.width/height)
    // marker: VisualElement маркера внутри map

    public static void PanViewportToMarker(ref Vector2 offset , VisualElement map, VisualElement viewport, VisualElement marker, float padding = 0f)
    {
        float mapW = map.layout.width;
        float mapH = map.layout.height;
        float viewW = viewport.layout.width;
        float viewH = viewport.layout.height;


        float markerCenterX = marker.layout.x + marker.layout.width * 0.5f;
        float markerCenterY = marker.layout.y + marker.layout.height * 0.5f;


        float desiredX = markerCenterX - viewW * 0.5f + Mathf.Sign(markerCenterX - viewW * 0.5f) * padding;
        float desiredY = markerCenterY - viewH * 0.5f + Mathf.Sign(markerCenterY - viewH * 0.5f) * padding;


        float maxOffsetX = Mathf.Max(0f, mapW - viewW);
        float maxOffsetY = Mathf.Max(0f, mapH - viewH);

        desiredX = Mathf.Clamp(desiredX, 0f, maxOffsetX);
        desiredY = Mathf.Clamp(desiredY, 0f, maxOffsetY);

        offset.x = desiredX;
        offset.y = desiredY;

        map.style.left = -desiredX;
        map.style.top = -desiredY;
    }
}