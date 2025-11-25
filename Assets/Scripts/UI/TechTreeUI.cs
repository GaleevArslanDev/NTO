using System.Collections.Generic;
using System.Linq;
using Data.Tech;
using Gameplay.Characters.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
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
        public Color defaultConnectionColor = new (0.29f, 0.56f, 0.89f);
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
        public bool allowAllTrees;
        public bool allowUnlock;
        private TechTree _forcedTree;
    
        private TechTree _currentTree;
        private Dictionary<string, GameObject> _nodeObjects = new ();
        private Dictionary<string, UILineConnection> _connectionObjects = new ();
        public bool isUIOpen;
        private bool _isUpgradeMode;
        private TechNode _selectedNode;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        
            if (unlockButton != null)
                unlockButton.onClick.AddListener(OnUnlockButtonClicked);
            
            techTreePanel.SetActive(false);
        }

        private void Start()
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
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey) && !_isUpgradeMode)
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
            if (_forcedTree != null && tree != _forcedTree)
            {
                Debug.LogWarning($"Попытка открыть недоступное дерево для этого NPC");
                return;
            }

            _currentTree = tree;
            _isUpgradeMode = upgradeMode && allowUnlock;

            techTreePanel.SetActive(true);

            if (upgradeModePanel != null)
                upgradeModePanel.SetActive(_isUpgradeMode);

            if (_isUpgradeMode && upgradeModeText != null)
            {
                string npcName = GetNpcNameForTree(tree);
                upgradeModeText.text = $"Режим прокачки - {npcName}";
            }

            if (UIManager.Instance != null)
                if (!isUIOpen)
                    UIManager.Instance.RegisterUIOpen();
            isUIOpen = true;

            GenerateTreeUI();
        }
    
        private string GetNpcNameForTree(TechTree tree)
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
            _forcedTree = null;

            // Показываем все деревья в режиме просмотра
            ShowTechTree(_currentTree);

            // Скрываем панель NPC режима
            if (upgradeModePanel != null)
                upgradeModePanel.SetActive(false);
    
            // Показываем табы переключения
            if (forgeTab != null) forgeTab.gameObject.SetActive(true);
            if (farmTab != null) farmTab.gameObject.SetActive(true);
            if (generalTab != null) generalTab.gameObject.SetActive(true);
        }
    
        public void ShowTechTreeForNpc(TechTree tree, string npcName)
        {
            _forcedTree = tree;
            allowAllTrees = false;
            allowUnlock = true;

            // Показываем только указанное дерево в режиме прокачки
            ShowTechTree(tree, true);

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
            if (_currentTree == null || _currentTree.nodes == null) return;
        
            // Создаем узлы
            foreach (var node in _currentTree.nodes)
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
            foreach (var node in _currentTree.nodes)
            {
                CreateConnections(node);
            }
        
            UpdateConnectionColors();
        }
    
        private void CreateNodeUI(TechNode node)
        {
            if (node == null || nodePrefab == null) return;
        
            var nodeObj = Instantiate(nodePrefab, nodesContainer);
            if (nodeObj == null) return;
        
            _nodeObjects[node.nodeId] = nodeObj;
        
            var rect = nodeObj.GetComponent<RectTransform>();
            if (rect == null) return;
        
            rect.anchoredPosition = node.graphPosition;
        
            var nodeUI = nodeObj.GetComponent<TechNodeUI>();
            if (nodeUI == null) return;
            nodeUI.Initialize(node, _currentTree);
            nodeUI.OnNodeUnlocked += OnNodeUnlocked;
            nodeUI.OnNodeSelected += OnNodeSelected;
            
            if (!_isUpgradeMode)
            {
                nodeUI.SetViewMode();
            }
        }
    
        private void OnNodeSelected(TechNode node)
        {
            _selectedNode = node;
        
            if (_isUpgradeMode)
            {
                UpdateUnlockButton();
            }
        }
    
        private void UpdateUnlockButton()
        {
            if (_selectedNode == null) return;
            var canUnlock = PlayerProgression.Instance.CanUnlockTech(_selectedNode.nodeId, _currentTree);
            unlockButton.interactable = canUnlock && !_selectedNode.isUnlocked;
            
            if (canUnlock && !_selectedNode.isUnlocked)
            {
                unlockButton.GetComponentInChildren<TMP_Text>().text = "Изучить";
            }
            else if (_selectedNode.isUnlocked)
            {
                unlockButton.GetComponentInChildren<TMP_Text>().text = "Изучено";
            }
            else
            {
                unlockButton.GetComponentInChildren<TMP_Text>().text = "Недоступно";
            }
        }
    
        private void CreateConnections(TechNode node)
        {
            if (node == null || connectionPrefab == null) return;

            foreach (var prereqId in node.prerequisiteNodes.Where(prereqId => !string.IsNullOrEmpty(prereqId)))
            {
                if (_nodeObjects.ContainsKey(prereqId) && _nodeObjects.TryGetValue(node.nodeId, out var endNode))
                {
                    var startNode = _nodeObjects[prereqId];

                    if (startNode == null || endNode == null)
                    {
                        Debug.LogWarning($"Start or end node is null for connection {prereqId} -> {node.nodeId}");
                        continue;
                    }
                
                    var startRect = startNode.GetComponent<RectTransform>();
                    var endRect = endNode.GetComponent<RectTransform>();
                
                    if (startRect == null || endRect == null)
                    {
                        Debug.LogWarning($"Start or end RectTransform is null for connection {prereqId} -> {node.nodeId}");
                        continue;
                    }
                
                    // Создаем соединение
                    var connectionObj = Instantiate(connectionPrefab, connectionsContainer);
                    if (connectionObj == null)
                    {
                        Debug.LogError("Failed to instantiate connection prefab");
                        continue;
                    }
                
                    var connection = connectionObj.GetComponent<UILineConnection>();
                    if (connection == null)
                    {
                        Debug.LogError("Connection prefab doesn't have UILineConnection component");
                        Destroy(connectionObj);
                        continue;
                    }
                
                    connection.SetConnection(startRect, endRect, defaultConnectionColor);
                    connection.SetThickness(connectionThickness);
                
                    var connectionId = $"{prereqId}_{node.nodeId}";
                    _connectionObjects[connectionId] = connection;
                }
                else
                {
                    Debug.LogWarning($"Cannot find nodes for connection: {prereqId} -> {node.nodeId}");
                }
            }
        }
    
        private void UpdateConnectionColors()
        {
            foreach (var (connectionId, connection) in _connectionObjects)
            {
                if (connection == null || !connection.IsValid()) continue;
            
                var nodeIds = connectionId.Split('_');
                if (nodeIds.Length < 2) continue;
                var startNodeId = nodeIds[0];
                var endNodeId = nodeIds[1];
                
                var startUnlocked = IsNodeUnlocked(startNodeId);
                var endUnlocked = IsNodeUnlocked(endNodeId);

                var connectionColor = startUnlocked switch
                {
                    true when endUnlocked => unlockedConnectionColor,
                    _ => defaultConnectionColor,
                };

                connection.SetColor(connectionColor);
            }
        }
    
        private bool IsNodeUnlocked(string nodeId)
        {
            if (!_nodeObjects.ContainsKey(nodeId)) return false;
            var nodeUI = _nodeObjects[nodeId].GetComponent<TechNodeUI>();
            return nodeUI != null && nodeUI.IsUnlocked();
        }
    
        private void OnNodeUnlocked()
        {
            UpdateConnectionColors();
        }
    
        private void ClearTree()
        {
            // Отписываемся от событий
            foreach (var nodeObj in _nodeObjects.Values)
            {
                if (nodeObj == null) continue;
                var nodeUI = nodeObj.GetComponent<TechNodeUI>();
                if (nodeUI != null)
                {
                    nodeUI.OnNodeUnlocked -= OnNodeUnlocked;
                }
                Destroy(nodeObj);
            }
            _nodeObjects.Clear();
        
            foreach (var connection in _connectionObjects.Values.Where(connection => connection != null && connection.gameObject != null))
            {
                Destroy(connection.gameObject);
            }
            _connectionObjects.Clear();
        }
    
        public void OnUnlockButtonClicked()
        {
            if (_selectedNode == null || !_isUpgradeMode) return;
            PlayerProgression.Instance.UnlockTech(_selectedNode.nodeId, _currentTree);
            UpdateUnlockButton();
            RefreshTreeUI();
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
            ShowTechTree(_currentTree);
        }
    
        public void CloseTechTree()
        {
            ClearTree();
            isUIOpen = false;
            techTreePanel.SetActive(false);

            // Используем UIManager вместо прямого управления
            if (UIManager.Instance != null)
                UIManager.Instance.RegisterUIClose();
            _forcedTree = null;

            if (_isUpgradeMode)
            {
                _isUpgradeMode = false;
            }
        }
    }
}