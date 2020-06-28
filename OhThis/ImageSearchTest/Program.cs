using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using System.IO;
using OpenCvSharp.Flann;

namespace ImageSearchTest
{
    class Program
    {
        static readonly int MAXFOLDERSTACK = 20;

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage : SourceImagePath DirectoryPath");
                return;
            }

            var imagePath = args[0];
            var directoryPath = args[1];

            if (Directory.Exists(imagePath))
            {
                Console.WriteLine($"invalid image path : {directoryPath}");
                return;
            }

            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"invalid directory path : {directoryPath}");
                return;
            }

            Queue<string> fileNames = new Queue<string>();
            GetFileNames(directoryPath, true, ref fileNames);

            try
            {
                var src1 = Cv2.ImRead(imagePath, ImreadModes.ReducedColor4);// 그냥 빨리 처리하려고 4배 줄임
                int count = 0;
                int errorCount = 0;

                while(fileNames.Count != 0)
                {
                    var filename = fileNames.Dequeue();

                    try
                    {
                        using (var dst = Cv2.ImRead(filename, ImreadModes.ReducedColor4))
                        {
                            var match = BFMatch(src1, dst);

                            var matchCount = match.Count(m => m.Distance < 10.0f);// 이 값은 더 많은 테스트 필요

                            if (matchCount > 5)// 이거도 테스트 필요
                            {
                                Console.WriteLine($"유사한 이미지 발견 !!! : {filename}");
                                System.Diagnostics.Process.Start(filename);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        //Console.WriteLine($"오류발생 : {e.Message} : {filename}");
                        errorCount++;
                    }
                    finally
                    {
                        Console.WriteLine($"{count} 번째 이미지 처리 완료 : {filename}");
                        count++;
                    }
                }

                Console.WriteLine($"{count} 회의 이미지 검색 완료\n{errorCount} 회의 오류 발생");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
                Console.ReadLine();
        }

        static IEnumerable<DMatch> BFMatch(Mat image1, Mat image2)
        {
            Mat dst1 = new Mat();
            Mat dst2 = new Mat();
            var orb = ORB.Create();
            orb.DetectAndCompute(image1, null, out var kp1, dst1);
            orb.DetectAndCompute(image2, null, out var kp2, dst2);

            BFMatcher matcher = new BFMatcher();

            return matcher.Match(dst1, dst2);
        }

        //static IEnumerable<DMatch> FLANNMatch(Mat image1, Mat image2)
        //{
        //    Mat dst1 = new Mat();
        //    Mat dst2 = new Mat();
        //    var orb = ORB.Create();
        //    orb.DetectAndCompute(image1, null, out var kp1, dst1);
        //    orb.DetectAndCompute(image2, null, out var kp2, dst2);

        //    IndexParams indexParams = new IndexParams();
        //    SearchParams searchParams = new SearchParams();

        //    FlannBasedMatcher matcher = new FlannBasedMatcher(indexParams, searchParams);
        //    return matcher.Match(dst1, dst2);
        //}

        static bool GetFileNames(string dirPath, bool searchSubFolder, ref Queue<string> fileQueue)
        {
            try
            {
                GetFileNameProcess(new DirectoryInfo(dirPath), searchSubFolder, ref fileQueue, 0, MAXFOLDERSTACK);
            }
            catch
            {
                return false;
            }

            return true;
        }

        static void GetFileNameProcess(DirectoryInfo directoryInfo, bool searchSubFolder, ref Queue<string> fileQueue, int nowStack, int maxStack)
        {
            if (nowStack >= maxStack)
                return;

            foreach (var fileinfo in directoryInfo.GetFiles())
            {
                var fileName = fileinfo.Name;

                if (String.IsNullOrEmpty(fileName))
                    continue;

                var extension = Path.GetExtension(fileName);

                if (IsImageExtension(extension))
                {
                    fileQueue.Enqueue(fileinfo.FullName);
                }
            }

            if (searchSubFolder)
            {
                foreach (var dirInfo in directoryInfo.GetDirectories())
                {
                    GetFileNameProcess(dirInfo, searchSubFolder, ref fileQueue, nowStack + 1, maxStack);
                }
            }
        }

        static private bool IsImageExtension(string extension)//opencv에서 지원하는 이미지 파일 확장자 검사
        {
            return
                extension.Equals(".png") ||
                extension.Equals(".PNG") ||
                extension.Equals(".jpg") ||
                extension.Equals(".jpeg") ||
                extension.Equals(".jpe") ||
                extension.Equals(".jp2") ||
                extension.Equals(".webp") ||
                extension.Equals(".bmp") ||
                extension.Equals(".dib");
        }
    }
}