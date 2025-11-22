using UnityEngine;
using UnityEngine.UI;

public class UILineConnection : MonoBehaviour
{
    [Header("UI References")]
    public Image lineImage;
    
    private RectTransform rectTransform;
    private RectTransform startNode;
    private RectTransform endNode;
    
    void Awake()
    {
        InitializeComponents();
    }
    
    void Start()
    {
        // Дополнительная инициализация после Awake
        InitializeComponents();
    }
    
    private void InitializeComponents()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        
        if (lineImage == null)
        {
            lineImage = GetComponent<Image>();
        }
        
        // Если все еще нет lineImage, создаем его
        if (lineImage == null && rectTransform != null)
        {
            GameObject child = new GameObject("LineImage");
            child.transform.SetParent(transform);
            child.transform.localPosition = Vector3.zero;
            child.transform.localScale = Vector3.one;
            
            Image img = child.AddComponent<Image>();
            RectTransform childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = Vector2.zero;
            childRect.anchorMax = Vector2.one;
            childRect.sizeDelta = Vector2.zero;
            childRect.anchoredPosition = Vector2.zero;
            
            lineImage = img;
        }
    }
    
    public void SetConnection(RectTransform start, RectTransform end, Color color)
    {
        // Убедимся что компоненты инициализированы
        InitializeComponents();
        
        if (rectTransform == null)
        {
            Debug.LogError("RectTransform is null in UILineConnection");
            return;
        }
        
        if (start == null || end == null)
        {
            Debug.LogError("Start or End RectTransform is null in UILineConnection");
            return;
        }
        
        startNode = start;
        endNode = end;
        
        if (lineImage != null)
        {
            lineImage.color = color;
        }
        else
        {
            Debug.LogWarning("LineImage is null in UILineConnection");
        }
        
        UpdateConnection();
    }
    
    void Update()
    {
        // Обновляем позицию если узлы двигаются (например, при изменении размера UI)
        if (startNode != null && endNode != null && rectTransform != null)
        {
            UpdateConnection();
        }
    }
    
    private void UpdateConnection()
    {
        if (startNode == null || endNode == null || rectTransform == null) 
        {
            Debug.LogWarning("Cannot update connection: missing references");
            return;
        }
        
        try
        {
            // Вычисляем позицию и размер линии
            Vector2 startPos = GetRectTransformCenter(startNode);
            Vector2 endPos = GetRectTransformCenter(endNode);
            
            Vector2 direction = endPos - startPos;
            float distance = direction.magnitude;
            
            // Защита от нулевой дистанции
            if (distance < 0.01f)
            {
                rectTransform.sizeDelta = new Vector2(0, 6f);
                return;
            }
            
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            // Настраиваем RectTransform
            rectTransform.anchoredPosition = (startPos + endPos) / 2f;
            rectTransform.sizeDelta = new Vector2(distance, 6f); // Толщина 6px
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.rotation = Quaternion.Euler(0, 0, angle);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating connection: {e.Message}");
        }
    }
    
    private Vector2 GetRectTransformCenter(RectTransform rt)
    {
        if (rt == null) return Vector2.zero;
        return rt.anchoredPosition;
    }
    
    // Метод для обновления цвета
    public void SetColor(Color color)
    {
        if (lineImage != null)
        {
            lineImage.color = color;
        }
    }
    
    // Метод для обновления толщины
    public void SetThickness(float thickness)
    {
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, thickness);
        }
    }
    
    // Метод для проверки валидности соединения
    public bool IsValid()
    {
        return startNode != null && endNode != null && rectTransform != null;
    }
}