using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    [Header("Runtime Data - Auto-filled from NPCBehaviour")]
    public NPCData NPCData;
    
    [Header("Dialogue Settings")]
    public DialogueTree[] dialogueTrees;
    
    [Header("Visual Settings")]
    public Sprite portrait;
    public AudioClip defaultVoice;
    
    private void Start()
    {
        if (NPCData != null)
        {
            foreach (var tree in dialogueTrees)
            {
                if (tree != null)
                    DialogueManager.Instance.LoadDialogueTree(tree);
            }
            
            if (RelationshipManager.Instance != null)
            {
                RelationshipManager.Instance.RegisterNPC(NPCData);
            }
        }
    }
    
    public void StartDialogueWithPlayer()
    {
        if (DialogueManager.Instance.IsInDialogue || NPCData == null) return;
        
        bool dialogueStarted = DialogueManager.Instance.StartDialogue(this);
        
        if (!dialogueStarted)
        {
            Debug.Log($"Не удалось начать диалог с {NPCData.npcName}");
        }
    }
    
    public void StartSpecificDialogue(string treeName)
    {
        if (DialogueManager.Instance.IsInDialogue) return;
        
        DialogueManager.Instance.StartDialogue(this, treeName);
    }
    
    public void OnDialogueEnded()
    {
        Debug.Log($"Диалог с {NPCData.npcName} завершен");
    }
    
    private void OnDestroy()
    {
        if (DialogueManager.Instance != null)
        {
            foreach (var tree in dialogueTrees)
            {
                if (tree != null)
                {
                    DialogueManager.Instance.UnloadDialogueTree(tree.treeName);
                }
            }
        }
    }
}