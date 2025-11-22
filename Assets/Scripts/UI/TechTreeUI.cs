using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TechTreeUI : MonoBehaviour
{
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
    
    [Header("Input Settings")]
    public KeyCode toggleKey = KeyCode.T;
    
    private TechTree currentTree;
    private Dictionary<string, GameObject> nodeObjects = new Dictionary<string, GameObject>();
    private Dictionary<string, UILineConnection> connectionObjects = new Dictionary<string, UILineConnection>();
    public bool isUIOpen = false;
    
    void Start()
    {
        // Проверяем необходимые ссылки
        if (techTreePanel == null) Debug.LogError("TechTreePanel is not assigned!");
        if (nodesContainer == null) Debug.LogError("NodesContainer is not assigned!");
        if (connectionsContainer == null) Debug.LogError("ConnectionsContainer is not assigned!");
        if (nodePrefab == null) Debug.LogError("NodePrefab is not assigned!");
        if (connectionPrefab == null) Debug.LogError("ConnectionPrefab is not assigned!");
        
        forgeTab.onClick.AddListener(() => ShowTechTree(forgeTree));
        farmTab.onClick.AddListener(() => ShowTechTree(farmTree));
        generalTab.onClick.AddListener(() => ShowTechTree(generalTree));
        
        techTreePanel.SetActive(false);
        ShowTechTree(forgeTree);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleTechTree();
        }
        
        if (isUIOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseTechTree();
        }
    }
    
    public void ShowTechTree(TechTree tree)
    {
        if (tree == null)
        {
            Debug.LogError("TechTree is null!");
            return;
        }
        
        currentTree = tree;
        treeNameText.text = tree.treeName;
        treeDescriptionText.text = tree.description;
        
        ClearTree();
        GenerateTreeUI();
        UpdateConnectionColors();
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
        if (nodeObj == null)
        {
            Debug.LogError("Failed to instantiate node prefab");
            return;
        }
        
        nodeObjects[node.nodeId] = nodeObj;
        
        RectTransform rect = nodeObj.GetComponent<RectTransform>();
        if (rect == null)
        {
            Debug.LogError("Node prefab doesn't have RectTransform component");
            return;
        }
        
        rect.anchoredPosition = node.graphPosition;
        
        TechNodeUI nodeUI = nodeObj.GetComponent<TechNodeUI>();
        if (nodeUI != null)
        {
            nodeUI.Initialize(node, currentTree);
            nodeUI.OnNodeUnlocked += OnNodeUnlocked;
        }
        else
        {
            Debug.LogError("Node prefab doesn't have TechNodeUI component");
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
        techTreePanel.SetActive(false);
        isUIOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}