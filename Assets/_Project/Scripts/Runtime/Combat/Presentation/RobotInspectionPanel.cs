using UnityEngine;

namespace Technopath.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class RobotInspectionPanel : MonoBehaviour
    {
        private RobotInspectionData _hovered;
        private RobotInspectionData _pinned;
        private Vector2 _pointerPosition;
        private Vector2 _detailsScroll;
        private GUIStyle _box;
        private GUIStyle _title;
        private GUIStyle _text;
        private Texture2D _backgroundTexture;

        public void ShowHover(RobotInspectionData data, Vector2 pointerPosition)
        {
            _hovered = data;
            _pointerPosition = pointerPosition;
        }

        public void Pin(RobotInspectionData data) => _pinned = data;

        public void ClearPinned() => _pinned = null;

        private void OnGUI()
        {
            EnsureStyles();
            if (_hovered != null && (_pinned == null || _hovered.UnitId != _pinned.UnitId))
                DrawCompact(_hovered);
            if (_pinned != null)
                DrawDetails(_pinned);
            if (!string.IsNullOrEmpty(GUI.tooltip))
                DrawModifierTooltip(GUI.tooltip);
        }

        private void DrawCompact(RobotInspectionData data)
        {
            const float width = 360f;
            const float height = 190f;
            var x = Mathf.Clamp(_pointerPosition.x + 18f, 8f, Screen.width - width - 8f);
            var guiPointerY = Screen.height - _pointerPosition.y;
            var y = Mathf.Clamp(guiPointerY + 18f, 8f, Screen.height - height - 8f);
            GUILayout.BeginArea(new Rect(x, y, width, height), GUIContent.none, _box);
            DrawShared(data);
            GUILayout.EndArea();
        }

        private void DrawDetails(RobotInspectionData data)
        {
            const float width = 410f;
            var height = Mathf.Min(560f, Screen.height - 32f);
            GUILayout.BeginArea(new Rect(Screen.width - width - 16f, 16f, width, height), GUIContent.none, _box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("ROBOT DETAILS", _title);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", GUILayout.Width(30f))) ClearPinned();
            GUILayout.EndHorizontal();
            _detailsScroll = GUILayout.BeginScrollView(_detailsScroll);
            DrawShared(data);
            GUILayout.Space(8f);
            DrawSection("Processor utility", data.UtilityAbility, "None");
            GUILayout.Space(8f);
            GUILayout.Label("Installed modules", _title);
            if (data.Modules.Count == 0) GUILayout.Label("None", _text);
            foreach (var module in data.Modules)
                GUILayout.Label(new GUIContent("• " + module.Name, module.Tooltip), _text);
            GUILayout.Space(8f);
            GUILayout.Label("Active statuses", _title);
            if (data.Statuses.Count == 0) GUILayout.Label("None", _text);
            foreach (var status in data.Statuses) GUILayout.Label("• " + status, _text);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawModifierTooltip(string content)
        {
            const float width = 330f;
            const float height = 150f;
            var pointer = Event.current.mousePosition;
            var x = Mathf.Clamp(pointer.x - width - 18f, 8f, Screen.width - width - 8f);
            var y = Mathf.Clamp(pointer.y + 18f, 8f, Screen.height - height - 8f);
            GUI.Box(new Rect(x, y, width, height), content, _box);
        }

        private void DrawShared(RobotInspectionData data)
        {
            GUILayout.Label(data.Name + " — " + data.Archetype, _title);
            GUILayout.Label($"HP {data.Health}/{data.MaximumHealth}   ARM {data.Armor}/{data.MaximumArmor}   ATK {data.Attack}", _text);
            DrawSection("Autoattack", data.AutoAttack, "None");
            DrawSection("Primary ability", data.PrimaryAbility, "None");
        }

        private void DrawSection(string label, string value, string fallback)
        {
            GUILayout.Label(label, _title);
            GUILayout.Label(string.IsNullOrWhiteSpace(value) ? fallback : value, _text);
        }

        private void EnsureStyles()
        {
            if (_box != null) return;
            _backgroundTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _backgroundTexture.SetPixel(0, 0, new Color(0.02f, 0.025f, 0.035f, 1f));
            _backgroundTexture.Apply();
            _box = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10),
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            };
            _box.normal.background = _backgroundTexture;
            _box.normal.textColor = Color.white;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, wordWrap = true };
            _text = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
        }

        private void OnDestroy()
        {
            if (_backgroundTexture != null) Destroy(_backgroundTexture);
        }
    }
}
