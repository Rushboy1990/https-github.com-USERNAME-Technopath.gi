namespace Technopath.Combat.Board
{
    public sealed class BattlefieldModel
    {
        public BattlefieldModel()
        {
            Player = new BattleGridModel(BoardSide.Player);
            Enemy = new BattleGridModel(BoardSide.Enemy);
        }

        public BattleGridModel Player { get; }
        public BattleGridModel Enemy { get; }

        public BattleGridModel GetGrid(BoardSide side) => side == BoardSide.Player ? Player : Enemy;
    }
}
