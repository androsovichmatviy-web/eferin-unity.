using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OmniGenesis.EditorTools
{
    // Menü "Omni Genesis → Projekt einrichten": legt die Ordner und die Szenen Hauptmenue und Welt an
    // und trägt beide in die Build Settings ein (nötig für das asynchrone Laden).
    public static class OmniGenesisSetup
    {
        const string Root = "Assets/OmniGenesis";
        const string MenuScenePath = Root + "/Scenes/Hauptmenue.unity";
        const string WorldScenePath = Root + "/Scenes/Welt.unity";

        [MenuItem("Omni Genesis/Projekt einrichten")]
        public static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets", "OmniGenesis");
            foreach (var folder in new[] { "Scenes", "Scripts", "Prefabs", "Art", "Audio", "UI_Assets" })
                if (!AssetDatabase.IsValidFolder(Root + "/" + folder)) AssetDatabase.CreateFolder(Root, folder);

            CreateScene(MenuScenePath, "Hauptmenue", typeof(MainMenu));
            CreateScene(WorldScenePath, "Welt", typeof(WorldBootstrap));

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(WorldScenePath, true),
            };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(MenuScenePath);

            EditorUtility.DisplayDialog("Omni Genesis",
                "Fertig! Die Szenen „Hauptmenue“ und „Welt“ wurden erstellt und in die Build Settings eingetragen.\n\nDrücke jetzt Play.",
                "OK");
        }

        static void CreateScene(string path, string rootName, System.Type component)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            var go = new GameObject(rootName);
            go.AddComponent(component);
            EditorSceneManager.SaveScene(scene, path);
        }

        [MenuItem("Omni Genesis/Spielstand löschen")]
        public static void DeleteSave()
        {
            if (!EditorUtility.DisplayDialog("Omni Genesis", "Den Spielstand wirklich löschen?", "Löschen", "Abbrechen")) return;
            SaveSystem.Delete();
            Debug.Log("Omni Genesis: Spielstand gelöscht.");
        }
    }
}
