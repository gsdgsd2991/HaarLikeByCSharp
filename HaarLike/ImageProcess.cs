using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;


namespace HaarLike
{
    public static class ImageProcess
    {
        //灰度与二值化
        public static Bitmap GreyPic(Bitmap original)
        {
            var outBitmap = new Bitmap(original.Width,original.Height,PixelFormat.Format8bppIndexed);
           // return outBitmap;
            var rec = new Rectangle(0,0,original.Width,original.Height);
            var originalData = original.LockBits(rec, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            var outputData = outBitmap.LockBits(rec, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            Grey(originalData,outputData);
            original.UnlockBits(originalData);
            outBitmap.UnlockBits(outputData);
            return outBitmap;
        }

        private static unsafe void Grey(BitmapData originalData,BitmapData outputData)
        {
            var width = originalData.Width;
            var height = originalData.Height;
            var scanOffset = originalData.Stride - width*3;
            var outOffset = outputData.Stride - width;
            var src = (byte*) originalData.Scan0.ToPointer();
            var dst = (byte*) outputData.Scan0.ToPointer();
            int red = 0, green = 0, blue = 0;
           // var allMatrix = new int[height, width*3];
            var redMatrix = new int[height, width];
            var greenMatrix = new int[height, width];
            var blueMatrix = new int[height, width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++,src += 3,dst++)
                {
                    //allMatrix[y, x] = src[0];
                   // allMatrix[y, x + 1] = src[1];
                   // allMatrix[y, x + 2] = src[2];
                     redMatrix[y,x] = src[0];
                     greenMatrix[y,x] = src[1];
                      blueMatrix[y,x] = src[2];
                    //*dst = (byte) (src[0]*0.299+0.587*src[1]+0.114*src[2]); //(src[Color.Red.R]>160? 1:0 );
                }
                src += scanOffset;
                dst += outOffset;
            }
            red = otsuThreshold(redMatrix, height, width);//(height*width);
            green = otsuThreshold(greenMatrix, height, width);//(height*width);
            blue = otsuThreshold(blueMatrix, height, width);//(height*width);
            src = (byte*)originalData.Scan0.ToPointer();
            dst = (byte*)outputData.Scan0.ToPointer();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++, src += 3, dst++)
                {
                    *dst = (byte) ((src[0]<red&&src[1]<green&&src[2]<blue)?0:1); //(src[Color.Red.R]>160? 1:0 );
                }
                src += scanOffset;
                dst += outOffset;
            }
        }

        private static int otsuThreshold(int[,] pic,int height,int width)
        {
            int t = 0;
            double graySum0 = 0;
            double graySum1 = 0;
           double n0 = 0;//前景像素
            double n1 = 0;//背景像素
            double avgGray0 = 0; //前景像素平均灰度
            double avgGray1 = 0;//背景像素平均灰度
            double w0 = 0;//前景像素占总像素比例
            double w1 = 0;//背景像素占总像素比例
            double u = 0;//总平均灰度
            double g = -1;//类间方差
            var histogram = new double[256];//灰度直方图
            double temppg = -1;
            double N = width*height;//总像素数
            for (int i = 0;i < height;i++)
            {
                for (int j = 0; j < width; j++)
                {
                    histogram[pic[i, j]]++;
                }
            }

            for (int i = 0; i < 256; i++)
            {
                graySum0 = 0;
                graySum1 = 0;
                n0 += histogram[i];
                n1 = N - n0;
                if (n1 == 0)
                    break;
                w0 = n0/N;
                w1 = 1 - w0;
                for (int j = 0; j <= i; j++)
                {
                    graySum0 += j*histogram[j];
                }
                avgGray0 = graySum0/n0;
                for (int k = i + 1; k < 256; k++)
                {
                    graySum1 += k*histogram[k];
                }
                avgGray1 = graySum1/n1;
                g = w0*w1*(avgGray0 - avgGray1)*(avgGray0 - avgGray1);
                if (temppg < g)
                {
                    temppg = g;
                    t = i;
                }
            }
            return t;
        }
    }
}
