using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BroadenHorizons
{
    public static class SpriteBatchExtensions
    {
        public static void DrawRectangle(this SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color)
        {
            spriteBatch.Draw(pixel, rect, color);
        }

        public static void DrawCircle(this SpriteBatch spriteBatch, Texture2D pixel, Vector2 center, float radius, int sides, Color color)
        {
            Vector2[] points = new Vector2[sides];
            for (int i = 0; i < sides; i++)
            {
                float angle = (float)(2 * Math.PI * i / sides);
                points[i] = center + new Vector2(radius * (float)Math.Cos(angle), radius * (float)Math.Sin(angle));
            }
            for (int i = 0; i < sides - 1; i++)
            {
                spriteBatch.DrawLine(pixel, points[i], points[i + 1], color);
            }
            spriteBatch.DrawLine(pixel, points[sides - 1], points[0], color);
        }

        public static void DrawLine(this SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 end, Color color, int thickness = 1)
        {
            Vector2 delta = end - start;
            float length = delta.Length();
            float angle = (float)Math.Atan2(delta.Y, delta.X);
            spriteBatch.Draw(texture, start, null, color, angle, Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0f);
        }
    }
}