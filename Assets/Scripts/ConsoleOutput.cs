using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ConsoleOutput : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    [SerializeField] private GameObject consoleUI;
    [SerializeField] private TMP_Text logText;
    [SerializeField] private RectTransform consoleRectTransform;
    [SerializeField] private ScrollRect scrollRect;

    private Vector2 pos;
    private bool consoleVisible = false;

    void Start()
    {
        logText.text = "";
        Application.logMessageReceived += HandleLog;
        consoleUI.SetActive(consoleVisible);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) {ToggleConsole();}
        if (consoleVisible) {scrollRect.verticalNormalizedPosition = 0f; }
    }

    void HandleLog(string logText, string stackTrace, LogType type) {this.logText.text += logText + "\n";}

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - pos;
        consoleRectTransform.anchoredPosition += delta;
        pos = eventData.position;
    }

    public void OnPointerDown(PointerEventData eventData) {pos = eventData.position;}

    void OnDestroy() {Application.logMessageReceived -= HandleLog;}

    void ToggleConsole()
    {
        consoleVisible = !consoleVisible;
        consoleUI.SetActive(consoleVisible);
    }
}
