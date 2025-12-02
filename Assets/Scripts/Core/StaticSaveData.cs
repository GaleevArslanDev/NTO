using System.Collections.Generic;

namespace Core
{
    public static class StaticSaveData
    {
        public static string SaveToLoad { get; set; }
        public static bool ShouldLoadSave => !string.IsNullOrEmpty(SaveToLoad);
        public static string LanguageOverride { get; set; }
        
        // Для передачи настроек между сценами
        public static Dictionary<string, object> CrossSceneData { get; } = new Dictionary<string, object>();
        
        public static void Clear()
        {
            SaveToLoad = null;
            CrossSceneData.Clear();
        }
    }
}