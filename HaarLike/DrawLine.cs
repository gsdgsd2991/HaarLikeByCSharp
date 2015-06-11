using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaarLike
{
    public static class DrawLine
    {
        public static Bitmap DrawLineInpicture(Bitmap bmp,int x1,int y1,int x2,int y2)
        {
           var g =  Graphics.FromImage(bmp);
            g.DrawLine(Pens.Red,x1,y1,x2,y2);
            g.Dispose();
            return bmp;
        }
    }
}
