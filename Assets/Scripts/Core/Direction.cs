// ============================================================
// Direction.cs
// Arrow direction enum + helper utilities.
// ============================================================

using UnityEngine;

namespace ArrowPuzzle.Core
{
    public enum Direction
    {
        Up    = 0,
        Right = 1,
        Down  = 2,
        Left  = 3
    }

    public static class DirectionExtensions
    {
        /// <summary>Returns the unit Vector2Int for grid movement.</summary>
        public static Vector2Int ToVector(this Direction dir)
        {
            return dir switch
            {
                Direction.Up    => new Vector2Int( 0,  1),
                Direction.Right => new Vector2Int( 1,  0),
                Direction.Down  => new Vector2Int( 0, -1),
                Direction.Left  => new Vector2Int(-1,  0),
                _               => Vector2Int.zero
            };
        }

        /// <summary>World-space Vector3 (XY plane, Z=0).</summary>
        public static Vector3 ToVector3(this Direction dir)
        {
            var v = dir.ToVector();
            return new Vector3(v.x, v.y, 0f);
        }

        /// <summary>Rotation angle (degrees) so a right-facing sprite aligns with dir.</summary>
        public static float ToAngle(this Direction dir)
        {
            return dir switch
            {
                Direction.Up    =>  90f,
                Direction.Right =>   0f,
                Direction.Down  => 270f,
                Direction.Left  => 180f,
                _               =>   0f
            };
        }

        /// <summary>Returns the opposite direction.</summary>
        public static Direction Opposite(this Direction dir)
        {
            return dir switch
            {
                Direction.Up    => Direction.Down,
                Direction.Right => Direction.Left,
                Direction.Down  => Direction.Up,
                Direction.Left  => Direction.Right,
                _               => dir
            };
        }
    }
}

