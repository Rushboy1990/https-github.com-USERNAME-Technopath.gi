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
            IReadOnlyDictionary<string, int> initialHealth = null)
        {
            Battlefield = battlefield;
            _profiles = profiles;
            PlayerTurn = new PlayerTurnModel(battlefield, PlayerTurnModel.StartingActionPoints, archetypes, loadouts, initialHealth);
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
        }

        public IReadOnlyList<MutantActionResult> ResolveMutantTurn(int nextIntentSeed)
        {
            if (Phase != CombatPhase.MutantTurn)
                throw new System.InvalidOperationException("Mutant phase is not active.");

            var results = _resolver.Resolve(Battlefield, PlayerTurn, _intents);
            Phase = CombatPhase.CompletingRound;
            if (!PlayerTurn.IsAlive(StartingFormationFactory.TechnopathId))
                Phase = CombatPhase.Defeat;
            else if (!Battlefield.Enemy.Cells.Any(cell => cell.Occupancy == CellOccupancyKind.Unit))
                Phase = CombatPhase.Victory;
            else
            {
                RoundNumber++;
                PrepareIntents(nextIntentSeed);
                PlayerTurn.BeginNewTurn();
                Phase = CombatPhase.PlayerTurn;
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
