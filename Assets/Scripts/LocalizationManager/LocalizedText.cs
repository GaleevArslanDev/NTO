using UnityEngine;
using UnityEngine.UI;

// public class
// localized text
public class LocalizedText : MonoBehaviour
{

    // localization manager 
    [Header("Components:")]
    [Tooltip("Reference to localization manager")]
    [SerializeField] private LocalizationManager localizationManager;

    // text
    [Tooltip("Reference to text field for output")]
    [SerializeField] private Text text;

    // name in localization file
    [Header("Parameters:")]
    [Tooltip("String name in localization file")]
    [SerializeField] private string name;

    private void Awake(){
        // if no localization manager, find it on scene
        if(this.localizationManager == null){
            this.localizationManager = FindObjectOfType<LocalizationManager>();
            Debug.LogWarning("Preferably set link to localization manager before starting the game.");
        }
    }

    private void Start(){
        // get localizated text
        string str = this.localizationManager.GetValue(this.name);
        // set text
        this.text.text = str;
    }

}