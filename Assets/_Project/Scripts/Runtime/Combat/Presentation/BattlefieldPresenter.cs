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
using UnityEngine.SceneManagement;
using Technopath.Run;
using Technopath.Run.Rewards;
using Technopath.Run.Presentation;

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
        [Header("Victory reward prototype")]
        [SerializeField] private VictoryRewardPanel victoryRewardPanel;
        [SerializeField] private string restStopScenePath = "Assets/_Project/Scenes/RestStop.unity";
        [SerializeField] private RobotModuleDefinition[] rewardModulePool = Array.Empty<RobotModuleDefinition>();
        [SerializeField, Min(0)] private int rewardModuleCount = 3;
        [SerializeField, Min(0)] private int rewardParts = 4;

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
        private RunFlowController _runFlow;
        private RunState _runState;
        private readonly BattleRewardGenerator _rewardGenerator = new();
        private bool _victoryRewardGranted;
        private bool _isPresentingEnemyTurn;

        public string SelectionDescription { get; private set; } = "None";
        public string BattleLog { get; private set; } = "Select a player unit, then an adjacent cell.";
        public string DetailedCombatLog { get; private set; } = "PhaseStarted";
        public IReadOnlyList<string> CombatLogEntries => _combatLogEntries;
        public int ActionPoints => _turn?.ActionPoints ?? 0;
        public string PhaseDescription => _isPresentingEnemyTurn
            ? CombatPhase.MutantTurn.ToString()
            : _combat?.Phase.ToString() ?? "None";
        public int RoundNumber => _combat?.RoundNumber ?? 0;

        private void Awake()
        {
            ValidateReferences();
            EnsureRunSession();
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

        public void DebugWinBattle()
        {
            if (_combat == null || _isAnimating || _combat.Phase == CombatPhase.Victory || _combat.Phase == CombatPhase.Defeat)
                return;
            _combat.DebugForceVictory();
            foreach (var entry in new List<KeyValuePair<string, UnitTokenView>>(_unitViews))
            {
                if (_turn.TryGetUnit(entry.Key, out var unit) && unit.Side == BoardSide.Enemy)
                {
                    _unitViews.Remove(entry.Key);
                    Destroy(entry.Value.gameObject);
                }
            }
            _selectedSource = null;
            ClearHighlights();
            RecordLog("DEBUG VICTORY: battle completed by Win battle button.");
            ShowVictoryReward();
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
            var encounter = _runState.CurrentEncounter;
            var battleSeed = formationSeed + encounter.Seed;
            var robotIds = new List<string>();
            foreach (var robot in _runState.Robots) robotIds.Add(robot.Id);
            _battlefield = StartingFormationFactory.Create(battleSeed, robotIds);
            var mutantDamageBonus = encounter.Kind == RunEncounterKind.Boss ? 2 : encounter.Kind == RunEncounterKind.Elite ? 1 : 0;
            var profiles = new List<MutantProfile>
            {
                new("mutant-1", 10, 1 + mutantDamageBonus),
                new("mutant-2", 20, 2 + mutantDamageBonus),
                new("mutant-3", 30, 3 + mutantDamageBonus)
            };
            var archetypes = new Dictionary<string, RobotArchetypeDefinition>();
            var initialHealth = new Dictionary<string, int>
            {
                [StartingFormationFactory.TechnopathId] = _runState.TechnopathHealth
            };
            foreach (var robot in _runState.Robots)
            {
                archetypes.Add(robot.Id, robot.Loadout.Archetype);
                _unitLoadouts.Add(robot.Id, robot.Loadout);
                initialHealth.Add(robot.Id, robot.Health);
            }
            foreach (var entry in archetypes)
            {
                _unitArchetypes.Add(entry.Key, entry.Value);
                _abilityEngine.Register(entry.Key, entry.Value);
            }
            _abilityEngine.BeginPhase();
            _combat = new CombatRoundModel(_battlefield, profiles, battleSeed, archetypes, _unitLoadouts, initialHealth);
            _turn = _combat.PlayerTurn;
            SpawnGridUnits(_battlefield.Player);
            SpawnGridUnits(_battlefield.Enemy);
            ShowIntents();
            RecordLog($"Encounter: {encounter.DisplayName}. Difficulty {encounter.Difficulty}.");
        }

        private IEnumerator PerformMutantTurn()
        {
            _isAnimating = true;
            _isPresentingEnemyTurn = true;
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

            _isPresentingEnemyTurn = false;

            if (_combat.Phase == CombatPhase.PlayerTurn)
            {
                _abilityEngine.BeginPhase();
                ShowIntents();
                RecordLog($"Round {_combat.RoundNumber} started. Player armor restored.");
            }
            else
            {
                if (_combat.Phase == CombatPhase.Victory)
                {
                    RecordLog("VICTORY: all required mutants destroyed.");
                    ShowVictoryReward();
                }
                else RecordLog("DEFEAT: Technopath destroyed.");
                if (_combat.Phase == CombatPhase.Defeat)
                {
                    _runFlow.Defeat();
                    victoryRewardPanel.ShowRunResult(false, _runState, RestartRun);
                }
            }
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

        private void ShowVictoryReward()
        {
            if (_victoryRewardGranted) return;
            _victoryRewardGranted = true;
            inspectionPanel.ClearPinned();
            CaptureSurvivingRobotsForCamp();
            _runFlow.CompleteBattle();
            var encounter = _runState.CurrentEncounter;
            var reward = _rewardGenerator.Grant(_runState, rewardModulePool,
                encounter.RewardModuleCount, encounter.RewardParts,
                formationSeed + _combat.RoundNumber * 101);
            Action continueAction = encounter.IsBoss
                ? () => victoryRewardPanel.ShowRunResult(true, _runState, RestartRun)
                : () => StartCoroutine(OpenRestStopScene());
            victoryRewardPanel.Show(reward, continueAction);
        }

        private IEnumerator OpenRestStopScene()
        {
            var combatScene = gameObject.scene;
            var load = SceneManager.LoadSceneAsync(restStopScenePath, LoadSceneMode.Additive);
            if (load == null) throw new InvalidOperationException($"Could not load Rest Stop scene: {restStopScenePath}");
            yield return load;
            var restScene = SceneManager.GetSceneByPath(restStopScenePath);
            RestStopController controller = null;
            foreach (var root in restScene.GetRootGameObjects())
            {
                controller = root.GetComponentInChildren<RestStopController>(true);
                if (controller != null) break;
            }
            if (controller == null) throw new InvalidOperationException("Rest Stop scene requires RestStopController.");
            _runFlow.EnterRestStop();
            controller.Initialize(_runState, testArchetypes);
            SceneManager.SetActiveScene(restScene);
            yield return SceneManager.UnloadSceneAsync(combatScene);
        }

        private void CaptureSurvivingRobotsForCamp()
        {
            var survivors = new List<CampRobotState>();
            foreach (var entry in _unitLoadouts)
            {
                if (_turn.TryGetUnit(entry.Key, out var unit) && unit.IsAlive)
                    survivors.Add(new CampRobotState(entry.Key, entry.Value, unit.Health));
            }
            _runState.ReplaceRobots(survivors);
            if (_turn.TryGetUnit(StartingFormationFactory.TechnopathId, out var technopath))
                _runState.SetTechnopathHealth(technopath.Health, technopath.MaxHealth);
        }

        private void EnsureRunSession()
        {
            _runFlow = RunSession.IsActive ? RunSession.Flow : RunSession.StartNew(formationSeed);
            _runState = _runFlow.State;
            if (_runState.Robots.Count > 0) return;

            var archetypes = BuildDistinctStartingArchetypes();
            foreach (var entry in archetypes)
            {
                RobotLoadout loadout = null;
                foreach (var preset in testLoadoutPresets)
                    if (preset != null && preset.Archetype == entry.Value)
                    {
                        loadout = preset.BuildRuntimeLoadout();
                        break;
                    }
                loadout ??= new RobotLoadout(entry.Value);
                _runState.AddRobot(new CampRobotState(entry.Key, loadout, loadout.CalculateStats().Health));
            }
        }

        private void RestartRun()
        {
            RunSession.Reset();
            SceneManager.LoadScene(gameObject.scene.path, LoadSceneMode.Single);
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
            if (unitPrefab == null || unitsRoot == null || attackTracePrefab == null || inspectionPanel == null || victoryRewardPanel == null)
                throw new InvalidOperationException("BattlefieldPresenter prefab and units root references are required.");
            if (string.IsNullOrWhiteSpace(restStopScenePath))
                throw new InvalidOperationException("BattlefieldPresenter requires Rest Stop scene path.");
            if (rewardModulePool.Length == 0)
                throw new InvalidOperationException("BattlefieldPresenter reward module pool cannot be empty.");
            if (testArchetypes.Length < 3)
                throw new InvalidOperationException("BattlefieldPresenter requires at least three test archetype definitions.");
        }
    }
}
