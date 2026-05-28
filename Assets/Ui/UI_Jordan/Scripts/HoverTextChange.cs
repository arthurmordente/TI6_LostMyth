using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class HoverTextChange : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI buttonText;
    
    [Tooltip("Format: #RRGGBB or #RRGGBBAA")]
    public string hoverColorHex = "#7F21DD";
    public string normalColorHex = "#534066";

    private Color hoverColor;
    private Color normalColor;

/*
    void Start()
    {
        // Convert hex strings to Color on start
        if (!ColorUtility.TryParseHtmlString(hoverColorHex, out hoverColor))
        {
            Debug.LogError("Invalid hover color hex code");
            hoverColor = Color.red;
        }

        if (!ColorUtility.TryParseHtmlString(normalColorHex, out normalColor))
        {
            Debug.LogError("Invalid normal color hex code");
            normalColor = Color.white;
        }
    }
*/
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buttonText != null) buttonText.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (buttonText != null) buttonText.color = normalColor;
    }
}   