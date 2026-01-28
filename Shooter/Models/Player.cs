using Shooter.Game;

namespace Shooter.Models
{
    internal class Player
    {
        public string Nickname { get; private set; }
        public System.Guid PlayerId { get; private set; }
        public float PlayerA { get; private set; }
        public float PlayerX { get; private set; }
        public float PlayerY { get; private set; }

        private const float speed = 5.0f;

        private float rotationSpeed = 0.28f;

        public Player()
        {
            Nickname = "unknown";
            PlayerA = 4.71f;
            PlayerX = 3.5f;
            PlayerY = 3.5f;
        }

        public Player(string nickname)
        {
            this.Nickname = nickname;
            this.PlayerId = System.Guid.NewGuid();
            PlayerA = 4.71f;
            PlayerX = 3.5f;
            PlayerY = 3.5f;
        }

        //private void CreateFromMap(MiniMap miniMap)
        //{
        //    for (int i = 0; i < miniMap.Map.Length; i++)
        //    {
        //        for (int j = 0; j < miniMap.Map[i].Length; j++)
        //        {
        //            if (miniMap.Map[i][j] is '^' or '<' or '>' or 'v')
        //            {
        //                PlayerY = i + .5f;
        //                PlayerX = j + .5f;
        //                PlayerA = miniMap.Map[i][j] switch
        //                {
        //                    '^' => 4.71f,
        //                    '>' => 0.00f,
        //                    '<' => 3.14f,
        //                    'v' => 1.57f,
        //                    _ => throw new NotImplementedException(),
        //                };
        //            }
        //        }
        //    }
        //}

        public void MoveForward(float elapsedSeconds, MiniMap miniMap)
        {
            var (dx, dy) = GetMovementDelta(elapsedSeconds);
            float nx = PlayerX + dx;
            float ny = PlayerY + dy;

            if (CanMoveTo(nx, ny, miniMap))
            {
                PlayerX = nx;
                PlayerY = ny;
            }
        }

        public void MoveBack(float elapsedSeconds, MiniMap miniMap)
        {
            var (dx, dy) = GetMovementDelta(elapsedSeconds);
            float nx = PlayerX - dx;
            float ny = PlayerY - dy;

            if (CanMoveTo(nx, ny, miniMap))
            {
                PlayerX = nx;
                PlayerY = ny;
            }
        }

        private (float dx, float dy) GetMovementDelta(float elapsedSeconds)
        {
            float step = speed * elapsedSeconds;
            float dx = (float)Math.Cos(PlayerA) * step;
            float dy = (float)Math.Sin(PlayerA) * step;
            return (dx, dy);
        }

        private bool CanMoveTo(float nx, float ny, MiniMap miniMap)
        {
            if (miniMap?.Map == null) return false;

            int testX = (int)nx;
            int testY = (int)ny;

            return testY >= 0
                && testY < miniMap.Map.Length
                && testX >= 0
                && testX < miniMap.Map[testY].Length
                && !IsWall(miniMap.Map[testY][testX]);
        }

        private static bool IsWall(char cell) => cell is '#' or '█';

        public void MoveRight(float elapsedSeconds)
        {
            PlayerA += speed * rotationSpeed * elapsedSeconds;
            if (PlayerA > (float)Math.PI * 2)
            {
                PlayerA %= (float)Math.PI * 2;
            }
        }

        public void MoveLeft(float elapsedSeconds)
        {
            PlayerA -= speed * rotationSpeed * elapsedSeconds;
            if (PlayerA < 0)
            {
                PlayerA %= (float)Math.PI * 2;
                PlayerA += (float)Math.PI * 2;
            }
        }

        public void SetState(float x, float y, float a)
        {
            PlayerX = x;
            PlayerY = y;
            PlayerA = a;
        }
    }
}
