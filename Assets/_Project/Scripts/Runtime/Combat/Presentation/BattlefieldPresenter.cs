using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Technopath.Combat.Board;
using Technopath.Combat.Rules;
using Technopath.Combat.Round;
using Technopath.Combat.Events;
using Technopath.Combat.Archetypes;
using Technopath.Combat.Statuses;
using Technopath.Combat.Modules;
using UnityEngine;

namespace Technopath.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BattlefieldPresenter : MonoBehaviour
    {
        [SerializeField] private GridCellView[] playerCells = Array.Empty<GridCellView>();
        [SerializeField] private GridCellView[] enemyCells = Array.Empty<GridCellView>();
        [SerializeField] private UnitTokenView unitPrefab;
        [SerializeField] private Transform unitsRoot;
        [SerializeField] private AttackTraceView attackTracePrefab;
        [SerializeField] private int formationSeed = 1701;
        [SerializeField] private RobotArchetypeDefinition[] testArchetypes = Array.Empty<RobotArchetypeDefinition>();
        [SerializeField] private RobotLoadoutPresetDefinition[] testLoadoutPresets = Array.Empty<RobotLoadoutPresetDefinition>();
        [SerializeField] private RobotInspectionPanel inspectionPanel;

        private readonly Dictionary<string, UnitTokenView> _unitViews = new();
        private readonly List<string> _combatLogEntries = new();
        private BattlefieldModel _battlefield;
        private PlayerTurnModel _turn;
        private CombatRoundModel _combat;
        private GridCellView _selectedSource;
        private bool _isAnimating;
        private readonly ConditionalAbilityEngine _abilityEngine = new();
        private readonly AbilityEffectResolver _abilityEffects = new();
        private readonly StatusCollection _statuses = new();
        private readonly Dictionary<string, RobotArchetypeDefinition> _unitArchetypes = new();
        private readonly Dictionary<string, RobotLoadout> _unitLoadouts = new();

        public string SelectionDescription { get; private set; } = "None";
        public string BattleLog { get; private set; } = "Select a player unit, then an adjacent cell.";
        public string DetailedCombatLog { get; private set; } = "PhaseStarted";
        public IReadOnlyList<string> CombatLogEntries => _combatLogEntries;
        public int ActionPoints => _turn?.ActionPoints ?? 0;
        public string PhaseDescription => _combat?.Phase.ToString() ?? "None";
        public int RoundNumber => _combat?.RoundNumber ?? 0;

        private void Awake()
        {
            ValidateReferences();
            InitializeCells(playerCells, BoardSide.Player);
            InitializeCells(enemyCells, BoardSide.Enemy);
            BuildCombat();
            RecordLog("Combat started. Player phase begins.");
        }

        public void Select(GridCellView cell)
        {
            if (cell == null || _isAnimating || _combat.Phase != CombatPhase.PlayerTurn || _turn.IsFinished)
                return;

            if (_selectedSource != null && cell.Side == BoardSide.Player &&
                _turn.CanMove(_selectedSource.Position, cell.Position))
            {
                StartCoroutine(PerformMove(_selectedSource, cell));
                return;
            }

            ClearHighlights();
            _selectedSource = null;
            var state = _battlefield.GetGrid(cell.Side)[cell.Position];
            if (state.Occupancy == CellOccupancyKind.Unit && TryCreateRobotInspection(state.OccupantId, out var inspection))
                inspectionPanel.Pin(inspection);
            else
                inspectionPanel.ClearPinned();
            SelectionDescription = state.IsEmpty
                ? $"{cell.Side} {cell.Position}: Empty"
                : DescribeUnit(cell.Side, cell.Position, state.OccupantId);

            cell.ShowSelected();
            if (cell.Side != BoardSide.Player || state.Occupancy != CellOccupancyKind.Unit)
                return;

            _selectedSource = cell;
            foreach (var neighbor in cell.Position.GetOrthogonalNeighbors())
            {
                if (_turn.CanMove(cell.Position, neighbor))
                    GetCell(BoardSide.Player, neighbor).ShowValidNeighbor();
            }
        }

        public void Hover(GridCellView cell, Vector2 pointerPosition)
        {
            RobotInspectionData inspection = null;
            if (cell != null)
            {
                var state = _battlefield.GetGrid(cell.Side)[cell.Position];
                if (state.Occupancy == CellOccupancyKind.Unit)
                    TryCreateRobotInspection(state.OccupantId, out inspection);
            }
            inspectionPanel.ShowHover(inspection, pointerPosition);
        }

        public void ClearInspection()
        {
            inspectionPanel.ShowHover(null, default);
            inspectionPanel.ClearPinned();
        }

        public void FinishTurn()
        {
            if (_turn == null || _isAnimating || _combat.Phase != CombatPhase.PlayerTurn)
                return;
            _selectedSource = null;
            ClearHighlights();
            StartCoroutine(PerformMutantTurn());
        }

        private IEnumerator PerformMove(GridCellView source, GridCellView destination)
        {
            _isAnimating = true;
            _abilityEngine.BeginAction();
            var initiatorId = _battlefield.Player[source.Position].OccupantId;
            var displacedId = _battlefield.Player[destination.Position].OccupantId;
            var result = _turn.Move(source.Position, destination.Position);

            StartCoroutine(_unitViews[initiatorId].MoveTo(destination.transform.position, result.WasSwap));
            if (result.WasSwap)
                StartCoroutine(_unitViews[displacedId].MoveTo(source.transform.position, false));
            yield return new WaitForSeconds(0.32f);

            var log = new StringBuilder();
            log.Append(result.WasSwap ? "Swap. " : "Move. ");
            foreach (var attack in result.Attacks)
            {
                ShowAttackFeedback(attack);
                if (attack.HasTarget)
                    log.Append(DescribeDamage(attack));
                else
                    log.Append($"{attack.AttackerId} fired into empty row. ");
            }
            foreach (var attack in result.Attacks)
            {
                if (attack.HasTarget && !_turn.IsAlive(attack.TargetId) &&
                    _unitViews.TryGetValue(attack.TargetId, out var deadView))
                {
                    _unitViews.Remove(attack.TargetId);
                    Destroy(deadView.gameObject);
                }
            }
            RecordLog(log.ToString());
            AppendDetailedEvents();
            SelectionDescription = "None";
            _selectedSource = null;
            ClearHighlights();
            _isAnimating = false;
        }

        private void BuildCombat()
        {
            _battlefield = StartingFormationFactory.Create(formationSeed);
            var profiles = new List<MutantProfile>
            {
                new("mutant-1", 10, 1),
                new("mutant-2", 20, 2),
                new("mutant-3", 30, 3)
            };
            var archetypes = BuildDistinctStartingArchetypes();
            foreach (var entry in archetypes)
            {
                _unitArchetypes.Add(entry.Key, entry.Value);
                _abilityEngine.Register(entry.Key, entry.Value);
            }
            foreach (var entry in archetypes)
            {
                foreach (var preset in testLoadoutPresets)
                {
                    if (preset != null && preset.Archetype == entry.Value)
                    {
                        _unitLoadouts.Add(entry.Key, preset.BuildRuntimeLoadout());
                        break;
                    }
                }
            }
            _abilityEngine.BeginPhase();
            _combat = new CombatRoundModel(_battlefield, profiles, formationSeed, archetypes, _unitLoadouts);
            _turn = _combat.PlayerTurn;
            SpawnGridUnits(_battlefield.Player);
            SpawnGridUnits(_battlefield.Enemy);
            ShowIntents();
        }

        private IEnumerator PerformMutantTurn()
        {
            _isAnimating = true;
            foreach (var unit in _unitViews.Values) unit.HideIntent();
            _combat.FinishPlayerTurn();
            RecordLog("Mutants are executing their intents.");
            var actions = _combat.ResolveMutantTurn(formationSeed + _combat.RoundNumber);

            foreach (var action in actions)
            {
                ShowAttackFeedback(action.Attack);
                RecordLog(action.Attack.HasTarget
                    ? DescribeDamage(action.Attack)
                    : $"{action.MutantId} fired into empty row.");
                yield return new WaitForSeconds(0.38f);

                if (action.Destination.HasValue && _unitViews.TryGetValue(action.MutantId, out var mutant))
                    yield return mutant.MoveTo(GetCell(BoardSide.Enemy, action.Destination.Value).transform.position, false);

                if (action.Attack.HasTarget && !_turn.IsAlive(action.Attack.TargetId) &&
                    _unitViews.TryGetValue(action.Attack.TargetId, out var deadView))
                {
                    _unitViews.Remove(action.Attack.TargetId);
                    Destroy(deadView.gameObject);
                }
            }

            if (_combat.Phase == CombatPhase.PlayerTurn)
            {
                _abilityEngine.BeginPhase();
                ShowIntents();
                RecordLog($"Round {_combat.RoundNumber} started. Player armor restored.");
            }
            else
                RecordLog(_combat.Phase == CombatPhase.Victory ? "VICTORY: all required mutants destroyed." : "DEFEAT: Technopath destroyed.");
            AppendDetailedEvents();
            _isAnimating = false;
        }

        private void ShowIntents()
        {
            foreach (var intent in _combat.Intents)
            {
                if (_unitViews.TryGetValue(intent.MutantId, out var view))
                    view.ShowAttackIntent(intent.AttackDamage);
            }
        }

        private void SpawnGridUnits(BattleGridModel grid)
        {
            foreach (var cell in grid.Cells)
            {
                if (cell.Occupancy != CellOccupancyKind.Unit)
                    continue;
                var token = Instantiate(unitPrefab, GetCell(grid.Side, cell.Position).transform.position,
                    Quaternion.identity, unitsRoot);
                token.Bind(cell.OccupantId, grid.Side, cell.OccupantId == StartingFormationFactory.TechnopathId);
                if (grid.Side == BoardSide.Player && _unitArchetypes.TryGetValue(cell.OccupantId, out var archetype))
                    token.ShowArchetype(archetype.DisplayName);
                _unitViews.Add(cell.OccupantId, token);
            }
        }

        private Dictionary<string, RobotArchetypeDefinition> BuildDistinctStartingArchetypes()
        {
            var unique = new List<RobotArchetypeDefinition>();
            var ids = new HashSet<string>();
            foreach (var definition in testArchetypes)
            {
                if (definition != null && ids.Add(definition.Id))
                    unique.Add(definition);
            }
            if (unique.Count < 3)
                throw new InvalidOperationException("At least three different archetype definitions are required.");

            var random = new System.Random(formationSeed);
            for (var index = unique.Count - 1; index > 0; index--)
            {
                var swapIndex = random.Next(index + 1);
                (unique[index], unique[swapIndex]) = (unique[swapIndex], unique[index]);
            }

            return new Dictionary<string, RobotArchetypeDefinition>
            {
                ["robot-1"] = unique[0],
                ["robot-2"] = unique[1],
                ["robot-3"] = unique[2]
            };
        }

        private void ShowAttackFeedback(AutoAttackResult attack)
        {
            if (attackTracePrefab == null || !_unitViews.TryGetValue(attack.AttackerId, out var attacker))
                return;

            var destination = attack.HasTarget && _unitViews.TryGetValue(attack.TargetId, out var target)
                ? target.transform.position
                : GetMissDestination(attack);
            Instantiate(attackTracePrefab, unitsRoot).Play(attacker.transform.position, destination, attack);
        }

        private Vector3 GetMissDestination(AutoAttackResult attack)
        {
            var attackerSide = _turn.GetUnit(attack.AttackerId).Side;
            var targetSide = attackerSide == BoardSide.Player ? BoardSide.Enemy : BoardSide.Player;
            var edgeColumn = attackerSide == BoardSide.Player ? GridPosition.Size - 1 : 0;
            var direction = attackerSide == BoardSide.Player ? Vector3.right : Vector3.left;
            return GetCell(targetSide, new GridPosition(attack.FiringRow, edgeColumn)).transform.position + direction;
        }

        private string DescribeUnit(BoardSide side, GridPosition position, string unitId)
        {
            var unit = _turn.GetUnit(unitId);
            var baseDescription = $"{side} {position}: {unitId}, HP {unit.Health}/{unit.MaxHealth}, ARM {unit.Armor}/{unit.MaxArmor}, ATK {unit.AttackDamage}";
            if (_unitLoadouts.TryGetValue(unitId, out var loadout))
            {
                var stats = loadout.CalculateStats();
                var primary = loadout.GetPrimaryAbility();
                var utility = loadout.GetProcessorAbility();
                baseDescription += $" • Build: Core={loadout.Core?.DisplayName ?? "Empty"}, CPU={loadout.Processor?.DisplayName ?? "Empty"}, Mods={string.Join(", ", FormatModifiers(loadout))} • Primary[{primary.Source}]: {primary.Name} — {primary.RulesText}";
                if (utility != null) baseDescription += $" • Utility[{utility.Source}]: {utility.Name} — {utility.RulesText}";
                baseDescription += $" • Sources: {string.Join("; ", stats.Sources)}";
            }
            return _unitArchetypes.TryGetValue(unitId, out var archetype)
                ? $"{baseDescription} • {archetype.DisplayName}: {archetype.AbilityName} — {archetype.AbilityRulesText}"
                : baseDescription;
        }

        private static IEnumerable<string> FormatModifiers(RobotLoadout loadout)
        {
            foreach (var modifier in loadout.Modifiers)
                yield return modifier?.DisplayName ?? "Empty";
        }

        private bool TryCreateRobotInspection(string unitId, out RobotInspectionData inspection)
        {
            inspection = null;
            if (!_unitArchetypes.TryGetValue(unitId, out var archetype) || !_turn.TryGetUnit(unitId, out var unit))
                return false;

            _unitLoadouts.TryGetValue(unitId, out var loadout);
            var primary = loadout?.GetPrimaryAbility();
            var utility = loadout?.GetProcessorAbility();
            var modules = new List<ModifierInspectionData>();
            if (loadout != null)
            {
                modules.Add(CreateModuleInspection("Core", loadout.Core));
                modules.Add(CreateModuleInspection("Processor", loadout.Processor));
                var index = 1;
                foreach (var modifier in loadout.Modifiers)
                {
                    var slotName = $"Modifier {index++}";
                    modules.Add(CreateModuleInspection(slotName, modifier));
                }
            }
            var statuses = new List<string>();
            foreach (var status in _statuses.GetActive(unitId))
                statuses.Add($"{status.Id}: {status.Charges} charge(s)");

            var primaryText = primary != null
                ? $"{primary.Name}: {primary.RulesText}"
                : $"{archetype.AbilityName}: {archetype.AbilityRulesText}";
            var utilityText = utility == null ? null : $"{utility.Name}: {utility.RulesText}";
            inspection = new RobotInspectionData(unitId, unitId, archetype.DisplayName,
                unit.Health, unit.MaxHealth, unit.Armor, unit.MaxArmor, unit.AttackDamage,
                $"Deals {unit.AttackDamage} damage to the first target in its row after movement.",
                primaryText, utilityText, modules, statuses);
            return true;
        }

        private static ModifierInspectionData CreateModuleInspection(string slotName, RobotModuleDefinition module) =>
            module == null
                ? new ModifierInspectionData($"{slotName}: Empty", $"{slotName} slot is empty")
                : new ModifierInspectionData($"{slotName}: {module.DisplayName}", FormatModuleTooltip(module));

        private static string FormatModuleTooltip(RobotModuleDefinition module)
        {
            var stats = $"HP {module.HealthModifier:+#;-#;0}   ARM {module.ArmorModifier:+#;-#;0}   ATK {module.AttackModifier:+#;-#;0}";
            return $"{module.DisplayName}\n{module.Rarity}, level {module.Level}\n{module.RulesText}\n{stats}";
        }

        private static string DescribeDamage(AutoAttackResult attack)
        {
            var damage = attack.DamageResult;
            return $"{attack.AttackerId} → {attack.TargetId}: ARM -{damage.AbsorbedByArmor}, HP -{damage.HealthDamage}. ";
        }

        private void AppendDetailedEvents()
        {
            var builder = new StringBuilder();
            for (var cycle = 0; cycle < 8; cycle++)
            {
                var events = _turn.Events.Drain();
                if (events.Count == 0) break;
                foreach (var combatEvent in events)
                {
                    if (builder.Length > 0) builder.Append(" → ");
                    builder.Append(combatEvent.Kind);
                    if (!string.IsNullOrEmpty(combatEvent.SourceId)) builder.Append($"[{combatEvent.SourceId}]");
                    if (!string.IsNullOrEmpty(combatEvent.TargetId)) builder.Append($"({combatEvent.TargetId})");
                    if (combatEvent.Value != 0) builder.Append($":{combatEvent.Value}");

                    if (combatEvent.Kind == CombatEventKind.Attack && !string.IsNullOrEmpty(combatEvent.TargetId) &&
                        _statuses.TryConsume(combatEvent.TargetId, "status.target-lock", out var bonusDamage))
                    {
                        _turn.ApplyDamageDetailed(combatEvent.TargetId, bonusDamage);
                        builder.Append($" → Status[target-lock]:HP/ARM -{bonusDamage}");
                    }

                    foreach (var activation in _abilityEngine.Evaluate(combatEvent))
                    {
                        if (_abilityEffects.Apply(activation, combatEvent, _turn, _statuses))
                            builder.Append($" → Ability[{activation.UnitId}:{activation.Definition.AbilityName}]={activation.EffectValue}");
                    }
                }
            }
            if (builder.Length > 0)
            {
                DetailedCombatLog = builder.ToString();
                RecordLog($"Events: {DetailedCombatLog}");
            }
        }

        private void RecordLog(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            BattleLog = message.Trim();
            _combatLogEntries.Add(BattleLog);
            if (_combatLogEntries.Count > 200) _combatLogEntries.RemoveAt(0);
        }

        private GridCellView GetCell(BoardSide side, GridPosition position) =>
            (side == BoardSide.Player ? playerCells : enemyCells)[position.Index];

        private void ClearHighlights()
        {
            foreach (var cell in playerCells) cell.ShowNormal();
            foreach (var cell in enemyCells) cell.ShowNormal();
        }

        private static void InitializeCells(IReadOnlyList<GridCellView> cells, BoardSide side)
        {
            for (var index = 0; index < cells.Count; index++)
                cells[index].Initialize(side, new GridPosition(index / GridPosition.Size, index % GridPosition.Size));
        }

        private void ValidateReferences()
        {
            if (playerCells.Length != 9 || enemyCells.Length != 9)
                throw new InvalidOperationException("BattlefieldPresenter requires exactly nine cells for each side.");
            if (unitPrefab == null || unitsRoot == null || attackTracePrefab == null || inspectionPanel == null)
                throw new InvalidOperationException("BattlefieldPresenter prefab and units root references are required.");
            if (testArchetypes.Length < 3)
                throw new InvalidOperationException("BattlefieldPresenter requires at least three test archetype definitions.");
        }
    }
}
