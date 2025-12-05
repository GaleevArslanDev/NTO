using Gameplay.Systems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(Button))]
    public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [Header("Sound Settings")]
        [SerializeField] private bool playHoverSound = true;
        [SerializeField] private bool playClickSound = true;
        
        [Header("Custom Settings")]
        [SerializeField] private bool useCustomButtonSounds = false;
        [SerializeField] private AudioClip customHoverSound;
        [SerializeField] private AudioClip customClickSound;
    
        private Button _button;
    
        private void Awake()
        {
            _button = GetComponent<Button>();
        }
    
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (playHoverSound && _button.interactable && SoundManager.Instance != null)
            {
                if (useCustomButtonSounds) 
                    SoundManager.Instance.PlayOneShot(customHoverSound);
                else
                    SoundManager.Instance.PlayUISound(UISoundType.Hover);
            }
        }
    
        public void OnPointerClick(PointerEventData eventData)
        {
            if (playClickSound && _button.interactable && SoundManager.Instance != null)
            {
                if (useCustomButtonSounds) 
                    SoundManager.Instance.PlayOneShot(customClickSound);
                else
                    SoundManager.Instance.PlayUISound(UISoundType.Click);
            }
        }
    
        // Альтернативный метод через события Button
        private void OnEnable()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClick);
            }
        }
    
        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClick);
            }
        }
    
        private void OnButtonClick()
        {
            if (playClickSound && SoundManager.Instance != null)
            {
                if (useCustomButtonSounds) 
                    SoundManager.Instance.PlayOneShot(customClickSound);
                else
                    SoundManager.Instance.PlayUISound(UISoundType.Click);
            }
        }
    }
}