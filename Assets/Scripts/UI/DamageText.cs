using UnityEngine;
using TMPro;
using System.Collections;

public class DamageText : MonoBehaviour
{
    [Header("Text Settings")]
    public TMP_Text damageText;
    public float floatSpeed = 2f;
    public float scaleDuration = 0.3f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    private float duration;
    private Vector3 initialPosition;
    private Color initialColor;
    private Vector3 initialScale;
    
    void Awake()
    {
        if (damageText == null)
        {
            damageText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        if (damageText != null)
        {
            initialColor = damageText.color;
        }
        
        initialPosition = transform.position;
        initialScale = transform.localScale;
        
        transform.localScale = Vector3.zero;
    }
    
    public void Initialize(float damage, float textDuration)
    {
        duration = textDuration;
        
        if (damageText != null)
        {
            damageText.text = $"-{damage:F0}";
            
            if (damage > 20)
            {
                damageText.color = Color.red;
            }
            else if (damage > 10)
            {
                damageText.color = Color.yellow;
            }
            else
            {
                damageText.color = Color.white;
            }
            
            initialColor = damageText.color;
        }
        
        StartCoroutine(AnimateText());
    }
    
    private IEnumerator AnimateText()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < scaleDuration)
        {
            elapsedTime += Time.deltaTime;
            float scaleProgress = elapsedTime / scaleDuration;
            float curveValue = scaleCurve.Evaluate(scaleProgress);
            
            transform.localScale = initialScale * curveValue;
            
            yield return null;
        }
        
        transform.localScale = initialScale;
        elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            float floatHeight = floatSpeed * progress;
            transform.position = initialPosition + Vector3.up * floatHeight;
            
            if (damageText != null)
            {
                float alpha = alphaCurve.Evaluate(progress);
                damageText.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
            }
            
            float sway = Mathf.Sin(elapsedTime * 5f) * 0.1f;
            transform.position += Vector3.right * sway;
            
            if (progress > 0.7f)
            {
                float shrinkProgress = (progress - 0.7f) / 0.3f;
                transform.localScale = initialScale * (1f - shrinkProgress * 0.5f);
            }
            
            transform.LookAt(2 * transform.position - Camera.main.transform.position);
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
}