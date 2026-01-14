namespace Shooter
{
    internal class Player
    {
        public float PLAYERA { get; private set; }
        public float PLAYERX { get; private set; }
        public float PLAYERY { get; private set; }

        private Player()
        {
            PLAYERA = default;
            PLAYERX = default;
            PLAYERY = default;
        }

        public Player(MiniMap map)
        {
            CreateFromMap(map);
        }

        private void CreateFromMap(MiniMap miniMap)
        {
            for (int i = 0; i < miniMap.Map.Length; i++)
            {
                for (int j = 0; j < miniMap.Map[i].Length; j++)
                {
                    if (miniMap.Map[i][j] is '^' or '<' or '>' or 'v')
                    {
                        PLAYERY = i + .5f;
                        PLAYERX = j + .5f;
                        PLAYERA = miniMap.Map[i][j] switch
                        {
                            '^' => 4.71f,
                            '>' => 0.00f,
                            '<' => 3.14f,
                            'v' => 1.57f,
                            _ => throw new NotImplementedException(),
                        };
                    }
                }
            }
        }
    }
}
