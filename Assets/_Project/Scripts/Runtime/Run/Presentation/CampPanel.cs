using System;
using Technopath.Combat.Modules;
using Technopath.Combat.Archetypes;
using UnityEngine;

namespace Technopath.Run.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CampPanel : MonoBehaviour
    {
        private RunState _run;
        private CampRobotState _selected;
        private RobotModuleDefinition _dragModule;
        private ModuleSlotType _dragSlot;
        private int _dragModifierIndex;
        private bool _dragFromSlot;
        private Vector2 _inventoryScroll;
        private Texture2D _background;
        private GUIStyle _panel;
        private GUIStyle _title;
        private GUIStyle _text;
        private Rect _inventoryRect;
        private RobotArchetypeDefinition[] _archetypes = Array.Empty<RobotArchetypeDefinition>();
        private Action _continueAction;
        private bool _dismantleMode;
        private bool _building;
        private bool _buildPaid;
        private int _nextRobotNumber = 4;

        public void Show(RunState run, RobotArchetypeDefinition[] archetypes, Action continueAction)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _archetypes = archetypes ?? Array.Empty<RobotArchetypeDefinition>();
            _continueAction = continueAction;
            _selected = run.Robots.Count > 0 ? run.Robots[0] : null;
        }

        private void OnGUI()
        {
            if (_run == null) return;
            EnsureStyles();
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, _panel);
            GUI.Label(new Rect(28, 20, 300, 34), "TECHNOPATH — REST STOP", _title);
            GUI.Label(new Rect(Screen.width - 210, 20, 180, 34), $"PARTS  {_run.Parts}", _title);
            DrawSquad();
            DrawInventory();
            if (_selected != null) DrawSelectedRobot();
            if (GUI.Button(new Rect(28, Screen.height - 54, 190, 34), _dismantleMode ? "EXIT DISMANTLE MODE" : "DISMANTLE MODE"))
                _dismantleMode = !_dismantleMode;
            if (GUI.Button(new Rect((Screen.width - 210) * 0.5f, Screen.height - 54, 210, 34), "BUILD ROBOT — 50 PARTS"))
                BeginBuild();
            if (GUI.Button(new Rect(Screen.width - 230, Screen.height - 54, 200, 34), "CONTINUE / TO ROUTE →"))
            {
                _run = null;
                _continueAction?.Invoke();
            }
            if (_building) DrawBuildChoice();
            DrawDraggedModule();
        }

        private void DrawSquad()
        {
            var right = 330f;
            var area = new Rect(28, 75, Screen.width - right - 56, Screen.height - 150);
            var cellWidth = area.width / 3f;
            var cellHeight = area.height / 3f;
            for (var index = 0; index < 9; index++)
            {
                var rect = new Rect(area.x + index % 3 * cellWidth + 6, area.y + index / 3 * cellHeight + 6, cellWidth - 12, cellHeight - 12);
                if (index == 1)
                {
                    GUI.Box(rect, "TECHNOPATH\nfield repair +10%", _panel);
                    continue;
                }
                var robotIndex = index < 1 ? index : index - 1;
                if (robotIndex < _run.Robots.Count)
                {
                    var robot = _run.Robots[robotIndex];
                    if (GUI.Button(rect, $"{robot.Id}\n{robot.Loadout.Archetype.DisplayName}\nHP {robot.Health}/{robot.MaxHealth}"))
                        _selected = robot;
                }
                else GUI.Box(rect, "EMPTY PLATFORM", _panel);
            }
        }

        private void DrawInventory()
        {
            _inventoryRect = new Rect(Screen.width - 310, 70, 282, Screen.height - 145);
            GUILayout.BeginArea(_inventoryRect, GUIContent.none, _panel);
            GUILayout.Label($"MODULES INVENTORY  {_run.ModuleInventory.Count}", _title);
            GUILayout.Label("ALL MODULES", _text);
            _inventoryScroll = GUILayout.BeginScrollView(_inventoryScroll);
            foreach (var module in _run.ModuleInventory)
            {
                GUILayout.Box($"{module.SlotType.ToString().ToUpper()}\n{module.DisplayName}  •  {module.Rarity} Lv.{module.Level}", GUILayout.Height(56));
                var rect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    if (_dismantleMode)
                    {
                        _run.TryDismantleModule(module, 2);
                        Event.current.Use();
                        break;
                    }
                    _dragModule = module;
                    _dragFromSlot = false;
                    Event.current.Use();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            if (_dragFromSlot && Event.current.type == EventType.MouseUp && _inventoryRect.Contains(Event.current.mousePosition))
            {
                _run.TryUnequip(_selected, _dragSlot, _dragModifierIndex);
                ClearDrag();
                Event.current.Use();
            }
        }

        private void DrawSelectedRobot()
        {
            var rect = new Rect((Screen.width - 560) * 0.5f, Screen.height - 245, 560, 190);
            GUILayout.BeginArea(rect, GUIContent.none, _panel);
            var stats = _selected.Loadout.CalculateStats();
            GUILayout.Label($"{_selected.Id} — {_selected.Loadout.Archetype.DisplayName}", _title);
            GUILayout.Label($"HP {_selected.Health}/{_selected.MaxHealth}   SHD {stats.Shield}   ATK {stats.Attack}", _text);
            GUILayout.BeginHorizontal();
            DrawSlot("CORE", ModuleSlotType.Core, 0);
            DrawSlot("PROCESSOR", ModuleSlotType.Processor, 0);
            DrawSlot("MOD 1", ModuleSlotType.Modifier, 0);
            DrawSlot("MOD 2", ModuleSlotType.Modifier, 1);
            DrawSlot("MOD 3", ModuleSlotType.Modifier, 2);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Repair 2 HP / 1 part")) _run.TryRepair(_selected, 1, 2);
            if (GUILayout.Button("Robot info")) { }
            if (GUILayout.Button("Dismantle robot +25 parts"))
            {
                var removed = _selected;
                if (_run.TryDismantleRobot(removed, 25))
                    _selected = _run.Robots.Count > 0 ? _run.Robots[0] : null;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawSlot(string label, ModuleSlotType slot, int modifierIndex)
        {
            var installed = _selected.Loadout.Get(slot, modifierIndex);
            GUILayout.Box($"{label}\n{installed?.DisplayName ?? "EMPTY"}", GUILayout.Width(100), GUILayout.Height(72));
            var rect = GUILayoutUtility.GetLastRect();
            var current = Event.current;
            if (current.type == EventType.MouseDown && rect.Contains(current.mousePosition) && installed != null)
            {
                _dragModule = installed;
                _dragSlot = slot;
                _dragModifierIndex = modifierIndex;
                _dragFromSlot = true;
                current.Use();
            }
            else if (current.type == EventType.MouseUp && rect.Contains(current.mousePosition) && _dragModule != null && !_dragFromSlot)
            {
                _run.TryEquip(_selected, _dragModule, slot, modifierIndex);
                ClearDrag();
                current.Use();
            }
        }

        private void DrawDraggedModule()
        {
            if (_dragModule == null) return;
            var pointer = Event.current.mousePosition;
            GUI.Box(new Rect(pointer.x + 14, pointer.y + 14, 220, 54), _dragModule.DisplayName, _panel);
            if (Event.current.type == EventType.MouseUp)
                ClearDrag();
        }

        private void ClearDrag()
        {
            _dragModule = null;
            _dragFromSlot = false;
        }

        private void BeginBuild()
        {
            if (_building || _run.Robots.Count >= 8 || _archetypes.Length < 3 || !_run.TrySpendParts(50)) return;
            _building = true;
            _buildPaid = true;
        }

        private void DrawBuildChoice()
        {
            var rect = new Rect((Screen.width - 620) * 0.5f, (Screen.height - 260) * 0.5f, 620, 260);
            GUILayout.BeginArea(rect, GUIContent.none, _panel);
            GUILayout.Label("SELECT ONE OF THREE ARCHETYPES", _title);
            GUILayout.BeginHorizontal();
            for (var index = 0; index < 3; index++)
            {
                var archetype = _archetypes[index];
                if (GUILayout.Button($"{archetype.DisplayName}\nHP {archetype.MaximumHealth}\nARM {archetype.MaximumShield}\nATK {archetype.AutoAttackDamage}\n\n{archetype.AbilityName}", GUILayout.Height(150)))
                {
                    _run.AddParts(50);
                    _selected = _run.BuildRobot($"robot-{_nextRobotNumber++}", archetype, 50);
                    _building = false;
                    _buildPaid = false;
                }
            }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Cancel — return 25 parts"))
            {
                if (_buildPaid) _run.AddParts(25);
                _building = false;
                _buildPaid = false;
            }
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_panel != null) return;
            _background = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _background.SetPixel(0, 0, new Color(0.018f, 0.022f, 0.025f, 1f));
            _background.Apply();
            _panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(12, 12, 10, 10) };
            _panel.normal.background = _background;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            _text = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
        }

        private void OnDestroy() { if (_background != null) Destroy(_background); }
    }
}
