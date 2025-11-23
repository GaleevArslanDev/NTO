using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TechTreeUI : MonoBehaviour
{
    public static TechTreeUI Instance;
    
    [Header("UI References")]
    public GameObject techTreePanel;
    public Transform nodesContainer;
    public Transform connectionsContainer;
    public GameObject nodePrefab;
    public GameObject connectionPrefab;
    
    [Header("Tech Trees")]
    public TechTree forgeTree;
    public TechTree farmTree;
    public TechTree generalTree;
    
    [Header("UI Elements")]
    public TMP_Text treeNameText;
    public TMP_Text treeDescriptionText;
    public Button forgeTab;
    public Button farmTab;
    public Button generalTab;
    
    [Header("Connection Settings")]
    public Color defaultConnectionColor = new Color(0.29f, 0.56f, 0.89f);
    public Color unlockedConnectionColor = Color.green;
    public Color lockedConnectionColor = Color.gray;
    public float connectionThickness = 6f;
    
    [Header("Mode Settings")]
    public GameObject upgradeModePanel;
    public TMP_Text upgradeModeText;
    public Button unlockButton;
    
    [Header("Input Settings")]
    public KeyCode toggleKey = KeyCode.T;
    
    [Header("Access Control")]
    public bool allowAllTrees = false;
    public bool allowUnlock = false;
    private TechTree forcedTree;
    
    private TechTree currentTree;
    private Dictionary<string, GameObject> nodeObjects = new Dictionary<string, GameObject>();
    private Dictionary<string, UILineConnection> connectionObjects = new Dictionary<string, UILineConnection>();
    public bool isUIOpen = false;
    private bool isUpgradeMode = false;
    private TechNode selectedNode;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        
        if (unlockButton != null)
            unlockButton.onClick.AddListener(OnUnlockButtonClicked);
            
        techTreePanel.SetActive(false);
        //ShowTechTree(forgeTree);
    }
    
    void Start()
    {
        // Проверяем необходимые ссылки
        if (techTreePanel == null) Debug.LogError("TechTreePanel is not assigned!");
        if (nodesContainer == null) Debug.LogError("NodesContainer is not assigned!");
        if (connectionsContainer == null) Debug.LogError("ConnectionsContainer is not assigned!");
        if (nodePrefab == null) Debug.LogError("NodePrefab is not assigned!");
        if (connectionPrefab == null) Debug.LogError("ConnectionPrefab is not assigned!");
        
        forgeTab.onClick.AddListener(() =>
        {
            CloseTechTree();
            ShowTechTree(forgeTree);
        });
        farmTab.onClick.AddListener(() =>
        {
            CloseTechTree();
            ShowTechTree(farmTree);
        });
        generalTab.onClick.AddListener(() =>
        {
            CloseTechTree();
            ShowTechTree(generalTree);
        });
        
        techTreePanel.SetActive(false);
        CloseTechTree();
        //ShowTechTree(forgeTree);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(toggleKey) && !isUpgradeMode)
        { 
            ShowTechTreeForViewing();
        }
    
        if (isUIOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseTechTree();
        }
    }
    
    public void ShowTechTree(TechTree tree, bool upgradeMode = false)
    {
        ClearTree();
        // Проверяем доступ
        if (forcedTree != null && tree != forcedTree)
        {
            Debug.LogWarning($"Попытка открыть недоступное дерево для этого NPC");
            return;
        }

        currentTree = tree;
        isUpgradeMode = upgradeMode && allowUnlock;

        techTreePanel.SetActive(true);
        isUIOpen = true;

        if (upgradeModePanel != null)
            upgradeModePanel.SetActive(isUpgradeMode);

        if (isUpgradeMode && upgradeModeText != null)
        {
            string npcName = GetNPCNameForTree(tree);
            upgradeModeText.text = $"Режим прокачки - {npcName}";
        }

        if (UIManager.Instance != null)
            UIManager.Instance.RegisterUIOpen();

        GenerateTreeUI();
    }
    
    private string GetNPCNameForTree(TechTree tree)
    {
        if (tree == forgeTree) return "Брук (Кузница)";
        if (tree == farmTree) return "Горк (Ферма)";
        if (tree == generalTree) return "Зол (Ратуша)";
        return "Прокачка";
    }
    
    public void ShowTechTreeForViewing()
    {
        allowAllTrees = true;
        allowUnlock = false;
        forcedTree = null;

        // Показываем все деревья в режиме просмотра
        ShowTechTree(currentTree, false);

        // Скрываем панель NPC режима
        if (upgradeModePanel != null)
            upgradeModePanel.SetActive(false);
    
        // Показываем табы переключения
        if (forgeTab != null) forgeTab.gameObject.SetActive(true);
        if (farmTab != null) farmTab.gameObject.SetActive(true);
        if (generalTab != null) generalTab.gameObject.SetActive(true);
    }
    
    public void ShowTechTreeForNPC(TechTree tree, string npcName)
    {
        forcedTree = tree;
        allowAllTrees = false;
        allowUnlock = true; // ← Разрешаем прокачку только у NPC

        // Показываем только указанное дерево в режиме прокачки
        ShowTechTree(tree, true); // ← true = режим прокачки

        // Обновляем UI для режима NPC
        if (upgradeModePanel != null)
        {
            upgradeModePanel.SetActive(true);
            upgradeModeText.text = $"Услуги {npcName}";
        }

        // Скрываем табы переключения деревьев
        if (forgeTab != null) forgeTab.gameObject.SetActive(false);
        if (farmTab != null) farmTab.gameObject.SetActive(false);
        if (generalTab != null) generalTab.gameObject.SetActive(false);
    }
    
    private void GenerateTreeUI()
    {
        if (currentTree == null || currentTree.nodes == null) return;
        
        // Создаем узлы
        foreach (var node in currentTree.nodes)
        {
            CreateNodeUI(node);
        }
        
        // Даем кадр для инициализации RectTransform узлов
        StartCoroutine(DelayedCreateConnections());
    }
    
    private System.Collections.IEnumerator DelayedCreateConnections()
    {
        // Ждем один кадр чтобы все узлы инициализировали свои RectTransform
        yield return null;
        
        // Создаем соединения
        foreach (var node in currentTree.nodes)
        {
            CreateConnections(node);
        }
        
        UpdateConnectionColors();
    }
    
    private void CreateNodeUI(TechNode node)
    {
        if (node == null || nodePrefab == null) return;
        
        GameObject nodeObj = Instantiate(nodePrefab, nodesContainer);
        if (nodeObj == null) return;
        
        nodeObjects[node.nodeId] = nodeObj;
        
        RectTransform rect = nodeObj.GetComponent<RectTransform>();
        if (rect == null) return;
        
        rect.anchoredPosition = node.graphPosition;
        
        TechNodeUI nodeUI = nodeObj.GetComponent<TechNodeUI>();
        if (nodeUI != null)
        {
            nodeUI.Initialize(node, currentTree);
            nodeUI.OnNodeUnlocked += OnNodeUnlocked;
            nodeUI.OnNodeSelected += OnNodeSelected;
            
            if (!isUpgradeMode)
            {
                nodeUI.SetViewMode();
            }
        }
    }
    
    private void OnNodeSelected(TechNode node)
    {
        selectedNode = node;
        
        if (isUpgradeMode)
        {
            UpdateUnlockButton();
        }
    }
    
    private void UpdateUnlockButton()
    {
        if (selectedNode != null)
        {
            bool canUnlock = PlayerProgression.Instance.CanUnlockTech(selectedNode.nodeId, currentTree);
            unlockButton.interactable = canUnlock && !selectedNode.isUnlocked;
            
            if (canUnlock && !selectedNode.isUnlocked)
            {
                unlockButton.GetComponentInChildren<TMP_Text>().text = "Изучить";
            }
            else if (selectedNode.isUnlocked)
            {
                unlockButton.GetComponentInChildren<TMP_Text>().text = "Изучено";
            }
            else
            {
                unlockButton.GetComponentInChildren<TMP_Text>().text = "Недоступно";
            }
        }
    }
    
    private void CreateConnections(TechNode node)
    {
        if (node == null || connectionPrefab == null) return;
        
        foreach (string prereqId in node.prerequisiteNodes)
        {
            if (string.IsNullOrEmpty(prereqId)) continue;
            
            if (nodeObjects.ContainsKey(prereqId) && nodeObjects.ContainsKey(node.nodeId))
            {
                GameObject startNode = nodeObjects[prereqId];
                GameObject endNode = nodeObjects[node.nodeId];
                
                if (startNode == null || endNode == null)
                {
                    Debug.LogWarning($"Start or end node is null for connection {prereqId} -> {node.nodeId}");
                    continue;
                }
                
                RectTransform startRect = startNode.GetComponent<RectTransform>();
                RectTransform endRect = endNode.GetComponent<RectTransform>();
                
                if (startRect == null || endRect == null)
                {
                    Debug.LogWarning($"Start or end RectTransform is null for connection {prereqId} -> {node.nodeId}");
                    continue;
                }
                
                // Создаем соединение
                GameObject connectionObj = Instantiate(connectionPrefab, connectionsContainer);
                if (connectionObj == null)
                {
                    Debug.LogError("Failed to instantiate connection prefab");
                    continue;
                }
                
                UILineConnection connection = connectionObj.GetComponent<UILineConnection>();
                if (connection == null)
                {
                    Debug.LogError("Connection prefab doesn't have UILineConnection component");
                    Destroy(connectionObj);
                    continue;
                }
                
                connection.SetConnection(startRect, endRect, defaultConnectionColor);
                connection.SetThickness(connectionThickness);
                
                string connectionId = $"{prereqId}_{node.nodeId}";
                connectionObjects[connectionId] = connection;
            }
            else
            {
                Debug.LogWarning($"Cannot find nodes for connection: {prereqId} -> {node.nodeId}");
            }
        }
    }
    
    private void UpdateConnectionColors()
    {
        foreach (var connectionPair in connectionObjects)
        {
            string connectionId = connectionPair.Key;
            UILineConnection connection = connectionPair.Value;
            
            if (connection == null || !connection.IsValid()) continue;
            
            string[] nodeIds = connectionId.Split('_');
            if (nodeIds.Length >= 2)
            {
                string startNodeId = nodeIds[0];
                string endNodeId = nodeIds[1];
                
                bool startUnlocked = IsNodeUnlocked(startNodeId);
                bool endUnlocked = IsNodeUnlocked(endNodeId);
                
                Color connectionColor;
                if (startUnlocked && endUnlocked)
                {
                    connectionColor = unlockedConnectionColor;
                }
                else if (startUnlocked && !endUnlocked)
                {
                    connectionColor = defaultConnectionColor;
                }
                else
                {
                    connectionColor = lockedConnectionColor;
                }
                
                connection.SetColor(connectionColor);
            }
        }
    }
    
    private bool IsNodeUnlocked(string nodeId)
    {
        if (nodeObjects.ContainsKey(nodeId))
        {
            TechNodeUI nodeUI = nodeObjects[nodeId].GetComponent<TechNodeUI>();
            return nodeUI != null && nodeUI.IsUnlocked();
        }
        return false;
    }
    
    private void OnNodeUnlocked()
    {
        UpdateConnectionColors();
    }
    
    private void ClearTree()
    {
        // Отписываемся от событий
        foreach (var nodeObj in nodeObjects.Values)
        {
            if (nodeObj != null)
            {
                TechNodeUI nodeUI = nodeObj.GetComponent<TechNodeUI>();
                if (nodeUI != null)
                {
                    nodeUI.OnNodeUnlocked -= OnNodeUnlocked;
                }
                Destroy(nodeObj);
            }
        }
        nodeObjects.Clear();
        
        foreach (var connection in connectionObjects.Values)
        {
            if (connection != null && connection.gameObject != null)
                Destroy(connection.gameObject);
        }
        connectionObjects.Clear();
    }
    
    public void OnUnlockButtonClicked()
    {
        if (selectedNode != null && isUpgradeMode)
        {
            PlayerProgression.Instance.UnlockTech(selectedNode.nodeId, currentTree);
            UpdateUnlockButton();
            RefreshTreeUI();
        }
    }
    
    private void RefreshTreeUI()
    {
        ClearTree();
        GenerateTreeUI();
    }
    
    public void ToggleTechTree()
    {
        if (isUIOpen)
        {
            CloseTechTree();
        }
        else
        {
            OpenTechTree();
        }
    }
    
    public void OpenTechTree()
    {
        techTreePanel.SetActive(true);
        isUIOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ShowTechTree(currentTree);
    }
    
    public void CloseTechTree()
    {
        ClearTree();
        isUIOpen = false;
        techTreePanel.SetActive(false);

        // Используем UIManager вместо прямого управления
        if (UIManager.Instance != null)
            UIManager.Instance.RegisterUIClose();

        if (isUpgradeMode)
        {
            isUpgradeMode = false;
        }
    }
}