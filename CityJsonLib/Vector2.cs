using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityJsonLib
{
    public class Vector2
    {
        internal static Vector2 zero;
        public float x;
        public float y;        

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;            
        }

        public static implicit operator Vector2(System.Numerics.Vector2 v)
        {
            return new Vector2(v.X, v.Y);
        }
    }


}
