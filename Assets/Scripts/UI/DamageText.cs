using System.Collections;
using TMPro;
using UnityEngine;

namespace UI
{
    public class DamageText : MonoBehaviour
    {
        [Header("Text Settings")]
        public TMP_Text damageText;
        public float floatSpeed = 2f;
        public float scaleDuration = 0.3f;
        public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
        private float _duration;
        private Vector3 _initialPosition;
        private Color _initialColor;
        private Vector3 _initialScale;
        private Camera _camera;

        private void Start()
        {
            _camera = Camera.main;
        }

        private void Awake()
        {
            if (damageText == null)
            {
                damageText = GetComponentInChildren<TextMeshProUGUI>();
            }
        
            if (damageText != null)
            {
                _initialColor = damageText.color;
            }
        
            _initialPosition = transform.position;
            _initialScale = transform.localScale;
        
            transform.localScale = Vector3.zero;
        }
    
        public void Initialize(float damage, float textDuration)
        {
            _duration = textDuration;
        
            if (damageText)
            {
                damageText.text = $"-{damage:F0}";

                damageText.color = damage switch
                {
                    > 20 => Color.red,
                    > 10 => Color.yellow,
                    _ => Color.white
                };

                _initialColor = damageText.color;
            }
        
            StartCoroutine(AnimateText());
        }
    
        private IEnumerator AnimateText()
        {
            var elapsedTime = 0f;
        
            while (elapsedTime < scaleDuration)
            {
                elapsedTime += Time.deltaTime;
                var scaleProgress = elapsedTime / scaleDuration;
                var curveValue = scaleCurve.Evaluate(scaleProgress);
            
                transform.localScale = _initialScale * curveValue;
            
                yield return null;
            }
        
            transform.localScale = _initialScale;
            elapsedTime = 0f;
        
            while (elapsedTime < _duration)
            {
                elapsedTime += Time.deltaTime;
                var progress = elapsedTime / _duration;
            
                var floatHeight = floatSpeed * progress;
                transform.position = _initialPosition + Vector3.up * floatHeight;
            
                if (damageText)
                {
                    var alpha = alphaCurve.Evaluate(progress);
                    damageText.color = new Color(_initialColor.r, _initialColor.g, _initialColor.b, alpha);
                }
            
                var sway = Mathf.Sin(elapsedTime * 5f) * 0.1f;
                transform.position += Vector3.right * sway;
            
                if (progress > 0.7f)
                {
                    var shrinkProgress = (progress - 0.7f) / 0.3f;
                    transform.localScale = _initialScale * (1f - shrinkProgress * 0.5f);
                }

                if (_camera) transform.LookAt(2 * transform.position - _camera.transform.position);

                yield return null;
            }
        
            Destroy(gameObject);
        }
    }
}