using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Technopath.Combat.Board;
using Technopath.Combat.Rules;
using Technopath.Combat.Round;
using Technopath.Combat.Events;
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

        private readonly Dictionary<string, UnitTokenView> _unitViews = new();
        private BattlefieldModel _battlefield;
        private PlayerTurnModel _turn;
        private CombatRoundModel _combat;
        private GridCellView _selectedSource;
        private bool _isAnimating;

        public string SelectionDescription { get; private set; } = "None";
        public string BattleLog { get; private set; } = "Select a player unit, then an adjacent cell.";
        public string DetailedCombatLog { get; private set; } = "PhaseStarted";
        public int ActionPoints => _turn?.ActionPoints ?? 0;
        public string PhaseDescription => _combat?.Phase.ToString() ?? "None";
        public int RoundNumber => _combat?.RoundNumber ?? 0;

        private void Awake()
        {
            ValidateReferences();
            InitializeCells(playerCells, BoardSide.Player);
            InitializeCells(enemyCells, BoardSide.Enemy);
            BuildCombat();
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
            BattleLog = log.ToString();
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
            _combat = new CombatRoundModel(_battlefield, profiles, formationSeed);
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
            BattleLog = "Mutants are executing their intents.";
            var actions = _combat.ResolveMutantTurn(formationSeed + _combat.RoundNumber);

            foreach (var action in actions)
            {
                ShowAttackFeedback(action.Attack);
                BattleLog = action.Attack.HasTarget
                    ? DescribeDamage(action.Attack)
                    : $"{action.MutantId} fired into empty row.";
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
                ShowIntents();
                BattleLog += $" Round {_combat.RoundNumber} started.";
            }
            else
                BattleLog = _combat.Phase == CombatPhase.Victory ? "VICTORY: all required mutants destroyed." : "DEFEAT: Technopath destroyed.";
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
                _unitViews.Add(cell.OccupantId, token);
            }
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
            return $"{side} {position}: {unitId}, HP {unit.Health}/{unit.MaxHealth}, ARM {unit.Armor}/{unit.MaxArmor}, ATK {unit.AttackDamage}";
        }

        private static string DescribeDamage(AutoAttackResult attack)
        {
            var damage = attack.DamageResult;
            return $"{attack.AttackerId} → {attack.TargetId}: ARM -{damage.AbsorbedByArmor}, HP -{damage.HealthDamage}. ";
        }

        private void AppendDetailedEvents()
        {
            var events = _turn.Events.Drain();
            if (events.Count == 0)
                return;

            var builder = new StringBuilder();
            foreach (var combatEvent in events)
            {
                if (builder.Length > 0) builder.Append(" → ");
                builder.Append(combatEvent.Kind);
                if (!string.IsNullOrEmpty(combatEvent.SourceId)) builder.Append($"[{combatEvent.SourceId}]");
                if (!string.IsNullOrEmpty(combatEvent.TargetId)) builder.Append($"({combatEvent.TargetId})");
                if (combatEvent.Value != 0) builder.Append($":{combatEvent.Value}");
            }
            DetailedCombatLog = builder.ToString();
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
            if (unitPrefab == null || unitsRoot == null || attackTracePrefab == null)
                throw new InvalidOperationException("BattlefieldPresenter prefab and units root references are required.");
        }
    }
}
