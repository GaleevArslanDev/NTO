using UnityEngine;

namespace Gameplay.Characters.Enemies
{
    public class EnemySaveComponent : MonoBehaviour
    {
        [SerializeField] private string enemyId;
        
        public string GetEnemyId()
        {
            return enemyId;
        }
        
        public void SetEnemyId(string id)
        {
            enemyId = id;
        }
    }
}