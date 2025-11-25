using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UILineConnection : MonoBehaviour
    {
        [Header("UI References")]
        public Image lineImage;
    
        private RectTransform _rectTransform;
        private RectTransform _startNode;
        private RectTransform _endNode;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            // Дополнительная инициализация после Awake
            InitializeComponents();
        }
    
        private void InitializeComponents()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }
        
            if (lineImage == null)
            {
                lineImage = GetComponent<Image>();
            }
        
            // Если все еще нет lineImage, создаем его
            if (lineImage != null || _rectTransform == null) return;
            var child = new GameObject("LineImage");
            child.transform.SetParent(transform);
            child.transform.localPosition = Vector3.zero;
            child.transform.localScale = Vector3.one;
            
            var img = child.AddComponent<Image>();
            var childRect = child.GetComponent<RectTransform>();
            childRect.anchorMin = Vector2.zero;
            childRect.anchorMax = Vector2.one;
            childRect.sizeDelta = Vector2.zero;
            childRect.anchoredPosition = Vector2.zero;
            
            lineImage = img;
        }
    
        public void SetConnection(RectTransform start, RectTransform end, Color color)
        {
            // Убедимся что компоненты инициализированы
            InitializeComponents();
        
            if (_rectTransform == null)
            {
                Debug.LogError("RectTransform is null in UILineConnection");
                return;
            }
        
            if (start == null || end == null)
            {
                Debug.LogError("Start or End RectTransform is null in UILineConnection");
                return;
            }
        
            _startNode = start;
            _endNode = end;
        
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

        private void Update()
        {
            // Обновляем позицию если узлы двигаются (например, при изменении размера UI)
            if (_startNode != null && _endNode != null && _rectTransform != null)
            {
                UpdateConnection();
            }
        }
    
        private void UpdateConnection()
        {
            if (_startNode == null || _endNode == null || _rectTransform == null) 
            {
                Debug.LogWarning("Cannot update connection: missing references");
                return;
            }
        
            try
            {
                // Вычисляем позицию и размер линии
                var startPos = GetRectTransformCenter(_startNode);
                var endPos = GetRectTransformCenter(_endNode);
            
                var direction = endPos - startPos;
                var distance = direction.magnitude;
            
                // Защита от нулевой дистанции
                if (distance < 0.01f)
                {
                    _rectTransform.sizeDelta = new Vector2(0, 6f);
                    return;
                }
            
                var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
                // Настраиваем RectTransform
                _rectTransform.anchoredPosition = (startPos + endPos) / 2f;
                _rectTransform.sizeDelta = new Vector2(distance, 6f); // Толщина 6px
                _rectTransform.pivot = new Vector2(0.5f, 0.5f);
                _rectTransform.rotation = Quaternion.Euler(0, 0, angle);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error updating connection: {e.Message}");
            }
        }
    
        private static Vector2 GetRectTransformCenter(RectTransform rt)
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
            if (_rectTransform != null)
            {
                _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, thickness);
            }
        }
    
        // Метод для проверки валидности соединения
        public bool IsValid()
        {
            return _startNode != null && _endNode != null && _rectTransform != null;
        }
    }
}