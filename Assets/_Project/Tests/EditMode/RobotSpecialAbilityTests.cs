using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Technopath.Combat.Archetypes;
using Technopath.Combat.Board;
using Technopath.Combat.Rules;
using UnityEditor;

namespace Technopath.Tests.EditMode
{
    public sealed class RobotSpecialAbilityTests
    {
        [Test]
        public void Projector_MoveDealsCoordinateAndAutoAttackDamage()
        {
            var field = CreateField("projector", new GridPosition(0, 0), "mutant", new GridPosition(0, 1));
            var projector = LoadRobot("Projector");
            var turn = CreateTurn(field, ("projector", projector));

            turn.Move(new GridPosition(0, 0), new GridPosition(0, 1));

            Assert.That(turn.GetUnit("mutant").Health,
                Is.EqualTo(100 - projector.EffectValue - projector.AutoAttackDamage));
        }

        [Test]
        public void Suppressor_FiresEffectValueAtPhaseStart()
        {
            var field = CreateField("suppressor", new GridPosition(0, 0), "mutant", new GridPosition(0, 0));
            var suppressor = LoadRobot("Suppressor");

            var turn = CreateTurn(field, ("suppressor", suppressor));

            Assert.That(turn.GetUnit("mutant").Health, Is.EqualTo(100 - suppressor.EffectValue));
        }

        [Test]
        public void Duplicator_FiresFromDepartedRowBeforeMoving()
        {
            var field = CreateField("duplicator", new GridPosition(0, 0), "mutant", new GridPosition(0, 0));
            var duplicator = LoadRobot("Duplicator");
            var turn = CreateTurn(field, ("duplicator", duplicator));

            turn.Move(new GridPosition(0, 0), new GridPosition(1, 0));

            Assert.That(turn.GetUnit("mutant").Health, Is.EqualTo(100 - duplicator.EffectValue));
        }

        [Test]
        public void Router_CanMoveToNonAdjacentCell()
        {
            var field = new BattlefieldModel();
            field.Player.TryOccupy(new GridPosition(0, 0), CellOccupancyKind.Unit, "router");
            var turn = CreateTurn(field, ("router", LoadRobot("Router")));

            Assert.That(turn.CanMove(new GridPosition(0, 0), new GridPosition(2, 2)), Is.True);
        }

        [Test]
        public void Transistor_MakesDisplacedUnitAttackTwice()
        {
            var field = new BattlefieldModel();
            field.Player.TryOccupy(new GridPosition(0, 0), CellOccupancyKind.Unit, "transistor");
            field.Player.TryOccupy(new GridPosition(1, 0), CellOccupancyKind.Unit, "ally");
            field.Enemy.TryOccupy(new GridPosition(0, 0), CellOccupancyKind.Unit, "mutant-row-0");
            field.Enemy.TryOccupy(new GridPosition(1, 0), CellOccupancyKind.Unit, "mutant-row-1");
            var turn = CreateTurn(field,
                ("transistor", LoadRobot("Transistor")), ("ally", LoadRobot("Projector")));

            var result = turn.Move(new GridPosition(0, 0), new GridPosition(1, 0));

            Assert.That(result.Attacks.Count(attack => attack.AttackerId == "ally"), Is.EqualTo(2));
        }

        [Test]
        public void Switcher_InitiatedSwapDoesNotSpendActionPoint()
        {
            var field = new BattlefieldModel();
            field.Player.TryOccupy(new GridPosition(0, 0), CellOccupancyKind.Unit, "switcher");
            field.Player.TryOccupy(new GridPosition(1, 0), CellOccupancyKind.Unit, "ally");
            var turn = CreateTurn(field,
                ("switcher", LoadRobot("Switcher")), ("ally", LoadRobot("Projector")));

            turn.Move(new GridPosition(0, 0), new GridPosition(1, 0));

            Assert.That(turn.ActionPoints, Is.EqualTo(PlayerTurnModel.StartingActionPoints));
        }

        [Test]
        public void Distributor_InterceptsHalfDamageFromOrthogonalAlly()
        {
            var field = new BattlefieldModel();
            field.Player.TryOccupy(new GridPosition(0, 0), CellOccupancyKind.Unit, "ally");
            field.Player.TryOccupy(new GridPosition(0, 1), CellOccupancyKind.Unit, "distributor");
            var turn = CreateTurn(field,
                ("ally", LoadRobot("Router")), ("distributor", LoadRobot("Distributor")));
            var allyBefore = TotalDurability(turn.GetUnit("ally"));
            var distributorBefore = TotalDurability(turn.GetUnit("distributor"));

            turn.ApplyDamageDetailed("ally", 10);

            Assert.That(TotalDurability(turn.GetUnit("ally")), Is.EqualTo(allyBefore - 5));
            Assert.That(TotalDurability(turn.GetUnit("distributor")), Is.EqualTo(distributorBefore - 5));
        }

        private static BattlefieldModel CreateField(string robotId, GridPosition robotPosition,
            string mutantId, GridPosition mutantPosition)
        {
            var field = new BattlefieldModel();
            field.Player.TryOccupy(robotPosition, CellOccupancyKind.Unit, robotId);
            field.Enemy.TryOccupy(mutantPosition, CellOccupancyKind.Unit, mutantId);
            return field;
        }

        private static PlayerTurnModel CreateTurn(BattlefieldModel field,
            params (string Id, RobotArchetypeDefinition Definition)[] robots)
        {
            var archetypes = robots.ToDictionary(entry => entry.Id, entry => entry.Definition);
            var enemies = field.Enemy.Cells
                .Where(cell => cell.Occupancy == CellOccupancyKind.Unit)
                .ToDictionary(cell => cell.OccupantId, _ => new CombatUnitSetup(100, 1, 0));
            return new PlayerTurnModel(field, PlayerTurnModel.StartingActionPoints,
                archetypes, null, null, enemies, 1234);
        }

        private static RobotArchetypeDefinition LoadRobot(string assetName) =>
            AssetDatabase.LoadAssetAtPath<RobotArchetypeDefinition>(
                $"Assets/_Project/Content/Definitions/Robots/{assetName}.asset");

        private static int TotalDurability(CombatUnitState unit) => unit.Health + unit.Shield;
    }
}
