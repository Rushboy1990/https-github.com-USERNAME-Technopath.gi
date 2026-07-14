using System;
using Technopath.Run;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Technopath.Bootstrap
{
    /// <summary>
    /// Temporary IMGUI presentation for validating menu and run-start flow before the uGUI screen is authored.
    /// It owns no run rules; selection is passed to RunSession as a RunStartConfiguration.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TemporaryStartMenuController : MonoBehaviour
    {
        private const string CombatSceneName = "CombatSandbox";
        private static readonly CrewDisplay[] Crews =
        {
            new(StartingCrewId.Rustwalker, "RUSTWALKER CREW", "DURABLE  •  RELIABLE", "Bulwark  /  Relay  /  Striker"),
            new(StartingCrewId.Scraphawk, "SCRAPHAWK UNIT", "MOBILITY  •  SCOUTING", "Striker  /  Relay  /  Bulwark"),
            new(StartingCrewId.Deepvault, "DEEPVAULT PROSPECTORS", "HEAVY DUTY  •  RESOURCEFUL", "Bulwark  /  Relay  /  Striker")
        };

        private bool _isSelectingCrew;
        private StartingCrewId? _selectedCrew;
        private int _selectedDifficulty = 1;
        private string _message;

        private void Awake()
        {
            _selectedDifficulty = RunSession.DifficultyProgress.HighestUnlockedLevel;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame && _isSelectingCrew)
                _isSelectingCrew = false;

            if (!_isSelectingCrew) return;
            if (keyboard.digit1Key.wasPressedThisFrame) SelectCrew(0);
            if (keyboard.digit2Key.wasPressedThisFrame) SelectCrew(1);
            if (keyboard.digit3Key.wasPressedThisFrame) SelectCrew(2);
            if (keyboard.leftArrowKey.wasPressedThisFrame) ChangeDifficulty(-1);
            if (keyboard.rightArrowKey.wasPressedThisFrame) ChangeDifficulty(1);
            if (keyboard.enterKey.wasPressedThisFrame && _selectedCrew.HasValue) StartExpedition();
        }

        private void OnGUI()
        {
            var area = new Rect(0, 0, Screen.width, Screen.height);
            var previousColor = GUI.color;
            GUI.color = new Color(0.025f, 0.03f, 0.04f, 1f);
            GUI.DrawTexture(area, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = previousColor;
            if (_isSelectingCrew) DrawCrewSelection(area);
            else DrawMainMenu(area);
        }

        private void DrawMainMenu(Rect area)
        {
            DrawTitle(new Rect(56, 54, 420, 90), "TECHNOPATH", 34);
            DrawLabel(new Rect(58, 132, 460, 24), "TEMPORARY START MENU", 14, new Color(0.3f, 0.85f, 0.95f));

            var buttons = new[] { "CONTINUE", "NEW EXPEDITION", "PROFILE", "SETTINGS", "EXIT" };
            for (var index = 0; index < buttons.Length; index++)
            {
                var rect = new Rect(58, 190 + index * 52, 280, 38);
                if (GUI.Button(rect, buttons[index]))
                {
                    if (index < 2) _isSelectingCrew = true;
                    else _message = $"{buttons[index]} is a temporary placeholder.";
                }
            }

            DrawLabel(new Rect(58, area.height - 92, 800, 28),
                "Continue currently starts a new run. Progress is kept only until the application closes.", 14, Color.gray);
            if (!string.IsNullOrEmpty(_message))
                DrawLabel(new Rect(58, area.height - 58, 800, 28), _message, 15, new Color(0.95f, 0.75f, 0.25f));
        }

        private void DrawCrewSelection(Rect area)
        {
            DrawTitle(new Rect(36, 34, 520, 52), "TECHNOPATH", 30);
            DrawLabel(new Rect(38, 82, 600, 26), "STARTING SET SELECTION", 15, Color.white);
            DrawLabel(new Rect(38, 110, 800, 24), "Choose a crew and an unlocked difficulty level. [1–3], [← →], [Enter], [Esc]", 13, Color.gray);

            var cardWidth = Mathf.Min(360, (area.width - 96) / 3);
            for (var index = 0; index < Crews.Length; index++)
            {
                var rect = new Rect(36 + index * (cardWidth + 12), 164, cardWidth, 230);
                var selected = _selectedCrew == Crews[index].Id;
                var previous = GUI.color;
                GUI.color = selected ? new Color(0.05f, 0.55f, 0.65f) : new Color(0.14f, 0.14f, 0.16f);
                GUI.Box(rect, GUIContent.none);
                GUI.color = previous;
                DrawLabel(new Rect(rect.x + 18, rect.y + 24, rect.width - 36, 30), Crews[index].Name, 17, Color.white);
                DrawLabel(new Rect(rect.x + 18, rect.y + 62, rect.width - 36, 26), Crews[index].Traits, 13, new Color(0.25f, 0.8f, 0.9f));
                DrawLabel(new Rect(rect.x + 18, rect.y + 108, rect.width - 36, 52), Crews[index].Roster, 15, Color.white);
                if (GUI.Button(new Rect(rect.x + 18, rect.y + 178, rect.width - 36, 34), selected ? "SELECTED" : "SELECT"))
                    SelectCrew(index);
            }

            var difficultyRect = new Rect(area.width * 0.5f - 230, 438, 460, 114);
            GUI.Box(difficultyRect, GUIContent.none);
            if (GUI.Button(new Rect(difficultyRect.x + 16, difficultyRect.y + 35, 46, 44), "<")) ChangeDifficulty(-1);
            if (GUI.Button(new Rect(difficultyRect.xMax - 62, difficultyRect.y + 35, 46, 44), ">")) ChangeDifficulty(1);
            DrawLabel(new Rect(difficultyRect.x + 78, difficultyRect.y + 20, 300, 28), $"LEVEL {_selectedDifficulty} / {DifficultyProgress.LevelCount}", 22, Color.white);
            DrawLabel(new Rect(difficultyRect.x + 78, difficultyRect.y + 56, 300, 26),
                _selectedDifficulty == 1 ? "Base rules. No added modifier." : "Modifier details are not implemented yet.",
                13, new Color(0.3f, 0.8f, 0.9f));

            if (GUI.Button(new Rect(36, area.height - 62, 130, 34), "BACK")) _isSelectingCrew = false;
            GUI.enabled = _selectedCrew.HasValue;
            var previousColor = GUI.color;
            GUI.color = _selectedCrew.HasValue ? new Color(0.15f, 0.85f, 0.96f) : Color.gray;
            if (GUI.Button(new Rect(area.width - 220, area.height - 62, 184, 34), "START EXPEDITION")) StartExpedition();
            GUI.color = previousColor;
            GUI.enabled = true;
        }

        private void SelectCrew(int index) => _selectedCrew = Crews[index].Id;

        private void ChangeDifficulty(int delta)
        {
            var target = Mathf.Clamp(_selectedDifficulty + delta, 1, RunSession.DifficultyProgress.HighestUnlockedLevel);
            if (RunSession.DifficultyProgress.IsUnlocked(target)) _selectedDifficulty = target;
        }

        private void StartExpedition()
        {
            if (!_selectedCrew.HasValue) return;
            RunSession.StartNew(Environment.TickCount, new RunStartConfiguration(_selectedCrew.Value, _selectedDifficulty));
            SceneManager.LoadScene(CombatSceneName, LoadSceneMode.Single);
        }

        private static void DrawTitle(Rect rect, string text, int size) => DrawLabel(rect, text, size, Color.white);

        private static void DrawLabel(Rect rect, string text, int size, Color color)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
        }

        private readonly struct CrewDisplay
        {
            public CrewDisplay(StartingCrewId id, string name, string traits, string roster)
            {
                Id = id;
                Name = name;
                Traits = traits;
                Roster = roster;
            }

            public StartingCrewId Id { get; }
            public string Name { get; }
            public string Traits { get; }
            public string Roster { get; }
        }
    }
}
