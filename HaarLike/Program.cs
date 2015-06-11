using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaarLike
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("positive img");
           // var fileName = Console.ReadLine();
            //var inputFile = new Bitmap(fileName);
            //var process = new ImageProcess();
            //var result = ImageProcess.GreyPic(inputFile);
            //result.Save("ProcessResult.png");
            var classfy = new Classifier();
            var files = Directory.GetFiles(@"F:\c#\HaarLike\train\face"/*fileName*/);
           // classfy.Classify(result);\
            foreach (var file in files)
            {
                classfy.TrainClassifier(file);
            }
            foreach (var data in classfy._ClassfiData)
            {
                Console.WriteLine(data);
            }
            Console.WriteLine("negative img");
           // fileName = Console.ReadLine();
            files = Directory.GetFiles(@"F:\c#\HaarLike\train\non-face"/*fileName*/);
            foreach (var file in files)
            {
                classfy.UnTrainClassifier(file);
            }
            foreach (var data in classfy._UnClassfiData)
            {
                Console.WriteLine(data);
            }
            Console.WriteLine("input classify img");
            var classifyFile = Console.ReadLine();
            var watch = new Stopwatch();
            watch.Start();
            var inputFile = new Bitmap(classifyFile);
            var processedImage = ImageProcess.GreyPic(inputFile);
            processedImage.Save("ProcessResult.png");
            classfy.Classify(processedImage);
            watch.Stop();
            Console.WriteLine(watch.ElapsedMilliseconds+"ms");
            Console.WriteLine(classfy.recs.Count+" objects");
            foreach (var rec in classfy.recs)
            {
                //Console.Write("("+rec.leftX+","+rec.leftY+");("+rec.rightX+","+rec.rightY+").");
                
                inputFile = DrawLine.DrawLineInpicture(inputFile, rec.leftX,rec.leftY ,rec.rightX ,rec.leftY );
                inputFile = DrawLine.DrawLineInpicture(inputFile, rec.leftX, rec.leftY, rec.leftX, rec.rightY);
                inputFile = DrawLine.DrawLineInpicture(inputFile, rec.leftX, rec.rightY, rec.rightX, rec.rightY);
                inputFile = DrawLine.DrawLineInpicture(inputFile, rec.rightX, rec.leftY, rec.rightX, rec.rightY);
            }
           
            inputFile.Save("classifiedPic.png");
            //Console.WriteLine("Processed");
        }
    }
}
