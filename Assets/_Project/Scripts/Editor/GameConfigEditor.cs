using Technopath.Configuration;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Technopath.Editor
{
    [CustomEditor(typeof(GameConfig))]
    public sealed class GameConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var config = (GameConfig)target;
            if (!config.IsValid(out var error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
                return;
            }

            var sceneIsInBuild = EditorBuildSettings.scenes.Any(scene =>
                scene.enabled &&
                System.IO.Path.GetFileNameWithoutExtension(scene.path) == config.CombatSandboxSceneName);

            if (!sceneIsInBuild)
            {
                EditorGUILayout.HelpBox(
                    $"Scene '{config.CombatSandboxSceneName}' is not enabled in Build Settings.",
                    MessageType.Error);
            }
            else
                EditorGUILayout.HelpBox("Configuration is valid.", MessageType.Info);
        }
    }
}
