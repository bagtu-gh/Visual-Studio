using Microsoft.Xna.Framework;

namespace BroadenHorizons
{
    static class Program
    {
        static void Main()
        {
            using (var game = new BH())
            {
                game.Run();
            }
        }
    }
}