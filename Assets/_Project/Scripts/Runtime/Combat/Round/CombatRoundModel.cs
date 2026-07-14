using System.Collections.Generic;
using System.Linq;
using Technopath.Combat.Board;
using Technopath.Combat.Rules;
using Technopath.Combat.Archetypes;
using Technopath.Combat.Modules;

namespace Technopath.Combat.Round
{
    public sealed class CombatRoundModel
    {
        private readonly MutantTurnPlanner _planner = new();
        private readonly MutantTurnResolver _resolver = new();
        private readonly IReadOnlyList<MutantProfile> _profiles;
        private IReadOnlyList<MutantIntent> _intents;

        public CombatRoundModel(BattlefieldModel battlefield, IReadOnlyList<MutantProfile> profiles, int seed,
            IReadOnlyDictionary<string, RobotArchetypeDefinition> archetypes = null)
            : this(battlefield, profiles, seed, archetypes, null)
        {
        }

        public CombatRoundModel(BattlefieldModel battlefield, IReadOnlyList<MutantProfile> profiles, int seed,
            IReadOnlyDictionary<string, RobotArchetypeDefinition> archetypes,
            IReadOnlyDictionary<string, RobotLoadout> loadouts,
            IReadOnlyDictionary<string, int> initialHealth = null,
            CombatUnitSetup technopathSetup = null)
        {
            Battlefield = battlefield;
            _profiles = profiles;
            var enemySetups = profiles.ToDictionary(profile => profile.UnitId,
                profile => new CombatUnitSetup(profile.MaximumHealth, profile.AttackDamage, profile.MaximumShield));
            IReadOnlyDictionary<string, CombatUnitSetup> playerSetups = technopathSetup == null
                ? null
                : new Dictionary<string, CombatUnitSetup>
                {
                    [StartingFormationFactory.TechnopathId] = technopathSetup
                };
            PlayerTurn = new PlayerTurnModel(battlefield, PlayerTurnModel.StartingActionPoints, archetypes, loadouts,
                initialHealth, enemySetups, seed, playerSetups);
            Phase = CombatPhase.PreparingIntents;
            PrepareIntents(seed);
            Phase = CombatPhase.PlayerTurn;
        }

        public BattlefieldModel Battlefield { get; }
        public PlayerTurnModel PlayerTurn { get; }
        public CombatPhase Phase { get; private set; }
        public int RoundNumber { get; private set; } = 1;
        public IReadOnlyList<MutantIntent> Intents => _intents;

        public void FinishPlayerTurn()
        {
            if (Phase != CombatPhase.PlayerTurn)
                throw new System.InvalidOperationException("Player phase is not active.");
            PlayerTurn.FinishTurn();
            Phase = CombatPhase.MutantTurn;
            PlayerTurn.ResolveStatusPhaseStart(BoardSide.Enemy);
        }

        public IReadOnlyList<MutantActionResult> ResolveMutantTurn(int nextIntentSeed)
        {
            if (Phase != CombatPhase.MutantTurn)
                throw new System.InvalidOperationException("Mutant phase is not active.");

            var results = _resolver.Resolve(Battlefield, PlayerTurn, _intents);
            Phase = CombatPhase.CompletingRound;
            PlayerTurn.ResolveStatusPhaseEnd(BoardSide.Enemy,
                results.Where(result => result.Destination.HasValue)
                    .Select(result => result.MutantId).ToHashSet());
            PlayerTurn.RestoreShields(BoardSide.Enemy);
            if (!PlayerTurn.IsAlive(StartingFormationFactory.TechnopathId))
                Phase = CombatPhase.Defeat;
            else if (!Battlefield.Enemy.Cells.Any(cell => cell.Occupancy == CellOccupancyKind.Unit))
                Phase = CombatPhase.Victory;
            else
            {
                RoundNumber++;
                PrepareIntents(nextIntentSeed);
                PlayerTurn.BeginNewTurn();
                Phase = PlayerTurn.IsAlive(StartingFormationFactory.TechnopathId)
                    ? CombatPhase.PlayerTurn
                    : CombatPhase.Defeat;
            }
            return results;
        }

        public void DebugForceVictory()
        {
            if (Phase == CombatPhase.Victory || Phase == CombatPhase.Defeat) return;
            foreach (var cell in Battlefield.Enemy.Cells.ToArray())
                if (cell.Occupancy == CellOccupancyKind.Unit)
                    Battlefield.Enemy.RemoveUnit(cell.OccupantId);
            Phase = CombatPhase.Victory;
        }

        private void PrepareIntents(int seed)
        {
            Phase = CombatPhase.PreparingIntents;
            _intents = _planner.Prepare(Battlefield.Enemy, _profiles, seed);
        }
    }
}
