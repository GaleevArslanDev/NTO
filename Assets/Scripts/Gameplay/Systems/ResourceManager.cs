using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Systems
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance;
        
        private HashSet<string> _collectedResources = new HashSet<string>();
        private bool _isDataApplied = false;
        
        public System.Action OnDataApplied;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // Новый метод для принудительного применения данных
        public void ApplyCollectedResourcesImmediately(HashSet<string> collectedResources)
        {
            _collectedResources = collectedResources ?? new HashSet<string>();
            _isDataApplied = true;
            OnDataApplied?.Invoke();
            
            Debug.Log($"Applied {_collectedResources.Count} collected resources immediately");
        }
        
        public void MarkResourceCollected(string resourceId)
        {
            _collectedResources.Add(resourceId);
        }
        
        public bool IsResourceCollected(string resourceId)
        {
            return _collectedResources.Contains(resourceId);
        }
        
        public HashSet<string> GetCollectedResources()
        {
            return new HashSet<string>(_collectedResources);
        }
        
        public void SetCollectedResources(HashSet<string> collectedResources)
        {
            _collectedResources = collectedResources ?? new HashSet<string>();
            _isDataApplied = true;
            OnDataApplied?.Invoke();
        }

        public bool IsDataApplied => _isDataApplied;
    }
}