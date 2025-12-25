using UnityEngine;
using UnityEngine.UIElements;

public class WorldItemManager : MonoBehaviour
{

    private static WorldItemManager _instance;

    public static WorldItemManager Instance { get { return _instance; } }
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string tooltipText = "thing";
    [SerializeField] private long itemsCount = 0;
    [SerializeField] private Vector2 uiSize = new Vector2(100, 100); //todo: в идеале передать сюда размеры модели и ее кординаты

    //todo: думается даже лучше чтоб он сам все спавнил и грузил модели? 

    private VisualElement uiProxy;
    private TransparentTooltipManipulator manipulator;

    void Start()
    {
        CreateUIProxy();
    }

    void CreateUIProxy()
    {
        if (uiDocument == null)
            uiDocument = FindObjectOfType<UIDocument>();

        var root = uiDocument.rootVisualElement;

        uiProxy = new VisualElement
        {
            name = $"Proxy_{gameObject.name}",
            style =
            {
                position = Position.Absolute,
                width = uiSize.x,
                height = uiSize.y,
                backgroundColor = new Color(0, 0, 0, 0)
            }
        };

        root.Add(uiProxy);

        manipulator = new TransparentTooltipManipulator(uiProxy, tooltipText);
        manipulator.target = uiProxy;

        // Обновляем позицию прокси
        UpdateProxyPosition();
    }

    void Update()
    {
        if (uiProxy != null)
        {
            UpdateProxyPosition();
        }
    }

    void UpdateProxyPosition()
    {
        // Конвертируем мировую позицию в экранные координаты
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);

        // UI Toolkit использует Y-ось сверху вниз
        screenPos.y = Screen.height - screenPos.y;

        // Устанавливаем позицию с учетом центра
        uiProxy.style.left = screenPos.x - uiSize.x / 2;
        uiProxy.style.top = screenPos.y - uiSize.y / 2;

        // Проверяем видимость объекта
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position);
        bool isVisible = viewportPos.x >= 0 && viewportPos.x <= 1 &&
                        viewportPos.y >= 0 && viewportPos.y <= 1 &&
                        viewportPos.z > 0;

        uiProxy.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void OnDestroy()
    {
        if (uiProxy != null && uiProxy.panel != null)
        {
            uiProxy.RemoveFromHierarchy();
        }
        manipulator?.Clear();
    }

    public void SetTooltipText(string text)
    {
        tooltipText = text;
        if (manipulator != null)
        {
            manipulator.SetText(text);
        }
    }
    public void SetItemsCount(long count)
    {
        itemsCount = count;
        if (manipulator != null)
        {
            //manipulator.SetText(text);
        }
    }

    public void SetSize(Vector3 vec) //todo: сделать
    {

    }
}