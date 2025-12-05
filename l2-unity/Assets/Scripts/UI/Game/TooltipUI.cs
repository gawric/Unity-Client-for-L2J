using UnityEngine;
using UnityEngine.UI;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private Text text;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    private void Update()
    {
        // панель следует за мышкой
        Vector3 mousePos = Input.mousePosition;
        panel.transform.position = mousePos;
    }

    public void Show(string itemName)
    {
        text.text = itemName;
        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}