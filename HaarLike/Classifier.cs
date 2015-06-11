using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaarLike
{
    public struct rigid
    {
        public int leftX;
        public int leftY;
        public int rightX;
        public int rightY;
    }

    public class Classifier
    {
        //save train data
        private List<List<float>> _trainData = new List<List<float>>();//记录训练的数据
        private List<List<float>> _UnTrainData = new List<List<float>>(); //非人脸训练数据
        public float[] _ClassfiData;//真正用于检测的数据 
        public float[] _UnClassfiData;//非人脸数据
        public float[] _ClassfiDataBig;
        public float[] _ClassfiDataSmall;
        private long[,] _integralImage;//积分图
        public List<rigid> recs;
        //train classifier
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pic">原图像</param>
        /// <param name="threshold">检测阈值</param>
        /// <param name="gap">隔几个像素生成一次长方形检测</param>
        public void Classify(Bitmap pic, float threshold = (float)0.029845, int gap = 5)
        {
            GetIntegralImage(pic);
            // var AllPixelNum = pic.Width * pic.Height;
            recs = new List<rigid>();//存放匹配的人脸区域
            for (var leftboard = 0; leftboard < pic.Width; leftboard += gap)//生成矩形
            {
                for (var topBoard = 0; topBoard < pic.Height; topBoard += gap)
                {
                    for (int rightBoard = leftboard + gap, bottomBoard = topBoard + gap; rightBoard < pic.Width && bottomBoard < pic.Height; rightBoard += gap, bottomBoard += gap)
                    {
                        //获取每个矩形的特征值
                        var features = GetFeature(leftboard, topBoard, rightBoard, bottomBoard);
                        var AllPixelNum = _integralImage[bottomBoard, rightBoard] + _integralImage[topBoard, leftboard] - _integralImage[topBoard, rightBoard] - _integralImage[bottomBoard, leftboard];//(rightBoard - leftboard)*(bottomBoard - topBoard);
                        var pixels = (bottomBoard - topBoard) * (rightBoard - leftboard);
                        int i = 0;
                        for (i = 0; i < features.Count; i++)
                        {
                            long tempFeatureNum = 0;
                            for (int j = 0; j < features[i].Count; j++)
                            {
                                tempFeatureNum += (_integralImage[features[i][j].rightY + 1, features[i][j].rightX + 1] +
                                _integralImage[features[i][j].leftY + 1, features[i][j].leftX + 1] - _integralImage[features[i][j].leftY + 1, features[i][j].rightX + 1] - _integralImage[features[i][j].rightY, features[i][j].leftX]);//交换xy
                            }
                            var x = AllPixelNum - tempFeatureNum*2;
                            if (AllPixelNum == 0 || Math.Abs(x - _ClassfiData[i] * pixels) > threshold * pixels)
                                break;
                            if (  x< _ClassfiDataSmall[i]*pixels||
                                x > _ClassfiDataBig[i]*pixels)
                                break;
                            //进入反例范围内
                            if (Math.Abs(x - _UnClassfiData[i]*pixels) < threshold*pixels*0.4)
                                break;
                        }
                        if (i == features.Count)
                        {
                            recs.Add(new rigid() { leftX = leftboard, leftY = topBoard, rightX = rightBoard, rightY = bottomBoard });
                        }
                        // }
                    }
                }
            }
        }

        //获取积分图
        private void GetIntegralImage(Bitmap pic)
        {
            _integralImage = new long[pic.Height + 1, pic.Width + 1];
            var s = new int[pic.Height + 1, pic.Width + 1];//行方向累加和
            var picData = pic.LockBits(new Rectangle(0, 0, pic.Width, pic.Height), ImageLockMode.ReadOnly,
                PixelFormat.Format8bppIndexed);
            unsafe
            {
                var width = picData.Width;
                var height = picData.Height;
                var offset = picData.Stride - width;
                var dst = (byte*)picData.Scan0.ToPointer();
                for (int i = 1; i <= height; i++)
                {
                    for (int j = 1; j <= width; j++, dst++)
                    {
                        s[i, j] = s[i, j - 1] + *dst;//(*dst < 160? 1:0);
                        _integralImage[i, j] = _integralImage[i - 1, j] + s[i, j];
                    }
                    dst += offset;
                }
            }
            pic.UnlockBits(picData);

        }
        /*
        private void Train(Bitmap pic, List<List<rigid>> blackParts)
        {
            var picData = pic.LockBits(new Rectangle(0, 0, pic.Width, pic.Height), ImageLockMode.ReadOnly,
                PixelFormat.Format8bppIndexed);
            unsafe
            {
                var width = picData.Width;
                var height = picData.Height;
                var outoffset = picData.Stride - width;
                var dst = (byte*)picData.Scan0.ToPointer();
                _trainData.Add(new List<float>());
                //遍历各个特征
                for (int i = 0; i < blackParts.Count; i++)
                {
                    var darkSum = 0;
                    //一种特征的多个区域
                    for (int j = 0; j < blackParts[i].Count; j++)
                    {
                        var tempdst = dst + blackParts[i][j].leftX + blackParts[i][j].leftY * outoffset;
                        for (int y = blackParts[i][j].leftY; y < blackParts[i][j].rightY; y++)
                        {
                            for (int x = blackParts[i][j].leftX; x < blackParts[i][j].rightX; x++)
                            {
                                darkSum += *dst;
                            }
                            dst += outoffset;
                        }

                    }
                    //一种特征对应的特征值
                    _trainData.Last().Add(darkSum / (pic.Width * pic.Height));
                }
                //合并所有测试图像的特征值
                var num = _trainData.Count;
                _ClassfiData = new float[_trainData[0].Count];
                for (int i = 0; i < num; i++)
                {
                    for (int j = 0; j < _trainData[i].Count; j++)
                    {
                        _ClassfiData[j] += _trainData[i][j] / num;
                    }
                }
            }
            pic.UnlockBits(picData);
        }*/
        /*
      Haar_1,//左右边界特征
      Haar_2,//左右细线特征
      Haar_3,//上下边界特征
      Haar_4,//对角线特征
      Haar_5//上下细线特征
       */
        //细线特征取宽度三分之一
        //边界对角线特征取一半
        /*public void TrainClassifier(Bitmap pic)
        {
            var x = GetFeature(pic);
            Train(pic, x);
        }*/
        //pgm文件非人脸数据训练
        private void UnTrain(string fileName, List<List<rigid>> blackParts)
        {
            var stream = new FileStream(fileName, FileMode.Open);
            for (int i = 0; i != 3; )
            {
                var word = stream.ReadByte();
                if (word == 10)
                {
                    i++;
                }
            }
            int[,] fileData = new int[19, 19]; //文件灰度
            _UnTrainData.Add(new List<float>());
            var AllPixel = 0;
            for (int i = 0; i < 19; i++)
            {
                for (int j = 0; j < 19; j++)
                {
                    fileData[i, j] = stream.ReadByte();
                    AllPixel += /*fileData[i, j];*/ (fileData[i, j] < 127 ? 0 : 1);
                }
            }

            for (int i = 0; i < blackParts.Count; i++)
            {
                var darkSum = 0;
                //一种特征的多个区域
                for (int j = 0; j < blackParts[i].Count; j++)
                {
                    for (int y = blackParts[i][j].leftY; y < blackParts[i][j].rightY; y++)
                    {
                        for (int x = blackParts[i][j].leftX; x < blackParts[i][j].rightX; x++)
                        {
                            darkSum += /*fileData[y, x];*/ (fileData[y, x] < 127 ? 0 : 1);
                        }
                        // dst += outoffset;
                    }
                }
                _UnTrainData.Last().Add((AllPixel - darkSum * 2) / ((float)19 * 19));
            }
            var num = _UnTrainData.Count;
            _UnClassfiData = new float[_UnTrainData[0].Count];
            

            for (int i = 0; i < num; i++)
            {
                for (int j = 0; j < _UnTrainData[i].Count; j++)
                {
                    _UnClassfiData[j] += _UnTrainData[i][j] / num;
                    
                }
            }
            //reader.Close();
            stream.Close();
        }

        //pgm文件训练
        private void Train(string fileName, List<List<rigid>> blackParts)
        {
            var stream = new FileStream(fileName, FileMode.Open);
            //var reader = new StreamReader(stream);
            //var fileType = reader.ReadLine();//文件格式P5
            //var fileSize = reader.ReadLine();//图像大小19*19
            //var biggestGrey = reader.ReadLine();//最大灰度255
            for (int i = 0; i != 3; )
            {
                var word = stream.ReadByte();
                if (word == 10)
                {
                    i++;
                }
            }
            int[,] fileData = new int[19, 19]; //文件灰度
            _trainData.Add(new List<float>());
            var AllPixel = 0;
            for (int i = 0; i < 19; i++)
            {
                for (int j = 0; j < 19; j++)
                {
                    fileData[i, j] = stream.ReadByte();
                    AllPixel += /*fileData[i, j];*/ (fileData[i, j] < 100 ? 0 : 1);
                }
            }

            for (int i = 0; i < blackParts.Count; i++)
            {
                var darkSum = 0;
                //一种特征的多个区域
                for (int j = 0; j < blackParts[i].Count; j++)
                {
                    for (int y = blackParts[i][j].leftY; y < blackParts[i][j].rightY; y++)
                    {
                        for (int x = blackParts[i][j].leftX; x < blackParts[i][j].rightX; x++)
                        {
                            darkSum += /*fileData[y, x];*/ (fileData[y, x] < 100 ? 0 : 1);
                        }
                        // dst += outoffset;
                    }
                }
                _trainData.Last().Add((AllPixel - darkSum * 2) / ((float)19 * 19));
            }
            var num = _trainData.Count;
            _ClassfiData = new float[_trainData[0].Count];
            if (_ClassfiDataBig == null || _ClassfiDataSmall == null || _ClassfiData == null)
            {
                

                _ClassfiDataBig = new float[_trainData[0].Count];
                for (int i = 0;i < _ClassfiDataBig.Count();i++)
                {
                    _ClassfiDataBig[i] = int.MinValue;
                }
                _ClassfiDataSmall = new float[_trainData[0].Count];
                for (int i = 0; i < _ClassfiDataSmall.Count(); i++)
                {
                    _ClassfiDataSmall[i] = int.MaxValue;
                }
            }

            for (int i = 0; i < num; i++)
            {
                for (int j = 0; j < _trainData[i].Count; j++)
                {
                    _ClassfiData[j] += _trainData[i][j] / num;
                    _ClassfiDataBig[j] = _ClassfiDataBig[j] < _trainData[i][j] ? _trainData[i][j] : _ClassfiDataBig[j];
                    _ClassfiDataSmall[j] = _ClassfiDataSmall[j] > _trainData[i][j]
                        ? _trainData[i][j]
                        : _ClassfiDataSmall[j];
                }
            }
            //reader.Close();
            stream.Close();
        }
        //读取pgm文件
        public void TrainClassifier(string fileName)
        {
            var x = GetFeature(0, 0, 19, 19);
            Train(fileName, x);
        }

        public void UnTrainClassifier(string fileName)
        {
            var x = GetFeature(0, 0, 19, 19);
            UnTrain(fileName,x);
        }

        public List<List<rigid>> GetFeature(int leftBoard, int topBoard, int rightBoard, int bottomBoard)
        {
            var blackPart = new List<List<rigid>>();
            //左右边界
            rigid rigLeftRight;
            rigLeftRight.leftX = leftBoard;
            rigLeftRight.leftY = topBoard;
            rigLeftRight.rightY = bottomBoard;
            rigLeftRight.rightX = (rightBoard + leftBoard) / 2;
            blackPart.Add(new List<rigid>(new rigid[] { rigLeftRight }));
            //左右细线
            rigid rigLeftRightRope;
            rigLeftRightRope.leftX = (rightBoard + 2 * leftBoard) / 3;
            rigLeftRightRope.leftY = topBoard;
            rigLeftRightRope.rightY = bottomBoard;
            rigLeftRightRope.rightX = leftBoard + (rightBoard - leftBoard) * 2 / 3;
            blackPart.Add(new List<rigid>(new rigid[] { rigLeftRightRope }));
            //上下边界
            rigid rigUpDown;
            rigUpDown.leftX = leftBoard;
            rigUpDown.leftY = topBoard;
            rigUpDown.rightY = (bottomBoard + topBoard) / 2;
            rigUpDown.rightX = rightBoard;
            blackPart.Add(new List<rigid>(new rigid[] { rigUpDown }));
            //对角线
            rigid rigleft, rigRight;
            rigleft.leftX = leftBoard;
            rigleft.leftY = (bottomBoard + topBoard) / 2;
            rigleft.rightY = bottomBoard;
            rigleft.rightX = (leftBoard + rightBoard) / 2;
            rigRight.leftX = (leftBoard + rightBoard) / 2;
            rigRight.leftY = topBoard;
            rigRight.rightY = (bottomBoard + topBoard) / 2;
            rigRight.rightX = rightBoard;
            blackPart.Add(new List<rigid>(new rigid[] { rigleft, rigRight }));
            //上下细线
            rigid rigUpDownRope;
            rigUpDownRope.leftX = leftBoard;
            rigUpDownRope.leftY = topBoard + (bottomBoard - topBoard) / 3;
            rigUpDownRope.rightY = topBoard + (bottomBoard - topBoard) * 2 / 3;
            rigUpDownRope.rightX = rightBoard;
            blackPart.Add(new List<rigid>(new rigid[] { rigUpDownRope }));
            return blackPart;
        }
        public List<List<rigid>> GetFeature(Bitmap pic)
        {
            var blackPart = new List<List<rigid>>();
            //左右边界
            rigid rigLeftRight;
            rigLeftRight.leftX = 0;
            rigLeftRight.leftY = 0;
            rigLeftRight.rightY = pic.Height;
            rigLeftRight.rightX = pic.Width / 2;
            blackPart.Add(new List<rigid>(new rigid[] { rigLeftRight }));
            //左右细线
            rigid rigLeftRightRope;
            rigLeftRightRope.leftX = pic.Width / 3;
            rigLeftRightRope.leftY = 0;
            rigLeftRightRope.rightY = pic.Height;
            rigLeftRightRope.rightX = pic.Width * 2 / 3;
            blackPart.Add(new List<rigid>(new rigid[] { rigLeftRightRope }));
            //上下边界
            rigid rigUpDown;
            rigUpDown.leftX = 0;
            rigUpDown.leftY = 0;
            rigUpDown.rightY = pic.Height / 2;
            rigUpDown.rightX = pic.Width;
            blackPart.Add(new List<rigid>(new rigid[] { rigUpDown }));
            //对角线
            rigid rigleft, rigRight;
            rigleft.leftX = 0;
            rigleft.leftY = pic.Height / 2;
            rigleft.rightY = pic.Height;
            rigleft.rightX = pic.Width / 2;
            rigRight.leftX = pic.Width / 2;
            rigRight.leftY = 0;
            rigRight.rightY = pic.Height / 2;
            rigRight.rightX = pic.Width;
            blackPart.Add(new List<rigid>(new rigid[] { rigleft, rigRight }));
            //上下细线
            rigid rigUpDownRope;
            rigUpDownRope.leftX = 0;
            rigUpDownRope.leftY = pic.Height / 3;
            rigUpDownRope.rightY = pic.Height * 2 / 3;
            rigUpDownRope.rightX = pic.Width;
            blackPart.Add(new List<rigid>(new rigid[] { rigUpDownRope }));
            return blackPart;
        }

    }
}
