using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FrameDataReader
{
    class Program
    {
        //private const string TargetDirectory = "./";

        private const string TargetDirectory = @"D:\Projects\Unity\BrnoProject\trunk\UnityProject\Data\tunnels\";
        private const string CsvFilePath = TargetDirectory + "tunnel_profiles.csv";

        private static BinaryWriter dataWriter;
        private static BinaryWriter indexWriter;

        private static int lastFlushedFrameId = 1;
        private static int dbg = 0;
        private static int numTunnelPerFrame = 0;


        private static void FlushFrameToDisk(List<float> frame, int frameId)
        {
            for (int i = lastFlushedFrameId + 1; i < frameId; i++)
            {
                Console.WriteLine("Flush frame " + i + " to disk: " + 0);
                indexWriter.Write(0);
            }

            var byteArray = new byte[frame.Count * sizeof(float)];
            Buffer.BlockCopy(frame.ToArray(), 0, byteArray, 0, byteArray.Length);
            dataWriter.Write(byteArray);

            Console.WriteLine("Flush frame " + frameId + " to disk: " + frame.Count * sizeof(float));
            indexWriter.Write(frame.Count*sizeof (float));

            lastFlushedFrameId = frameId;
            dbg += byteArray.Length;

            numTunnelPerFrame = 0;
            frame.Clear();
        }

        static void Main(string[] args)
        {
            List<float> floatFrameList = new List<float>();
            float[] floatFrameTunnelArray = null;

            int previousFrame = 0,
                previousCluster = 0,
                previousTunnel = 0,
                previousSphereCount = 0;

            int totalSphereCount = 0;

            foreach (var line in File.ReadAllLines(CsvFilePath))
            {
                if(line.StartsWith("Snapshot")) continue;

                var split = line.Split(new [] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (String.CompareOrdinal(split[12].Trim(), "X") == 0)
                {
                    // Start new tunnel here
                    int currentFrame = int.Parse(Regex.Match(split[0], @"\d+").Value);
                    int currentCluster = int.Parse(split[1]);
                    int currentTunnel = int.Parse(split[2]);

                    if (currentCluster != previousCluster)
                    {
                        // If this is the first cluster we read, no need to flush the previous frame
                        if (previousCluster != 0)
                        {
                            FlushFrameToDisk(floatFrameList, previousFrame);
                            previousTunnel = 0;
                            previousFrame = 0;
                            lastFlushedFrameId = 1;

                            Console.WriteLine("Debug: " + dbg);
                        }

                        if (currentCluster > 1) 
                            break;

                        Console.WriteLine("Reading cluster: " + currentCluster);

                        dataWriter = new BinaryWriter(File.Open(TargetDirectory + "/cluster_data_" + currentCluster + "/.bin" , FileMode.Create));
                        indexWriter = new BinaryWriter(File.Open(TargetDirectory + "/cluster_index_" + currentCluster + "/.bin", FileMode.Create));
                    }

                    if (currentFrame != previousFrame)
                    {
                        // If this is the first frame we read, no need to flush previous frame
                        if (previousFrame != 0)
                        {
                            FlushFrameToDisk(floatFrameList, previousFrame);
                            previousTunnel = 0;
                        }

                        Console.WriteLine("*******");
                        Console.WriteLine("Reading frame: " + currentFrame);
                    }

                    if (currentTunnel != previousTunnel)
                    {
                        Console.WriteLine("Reading tunnel: " + currentTunnel);
                    }

                    previousFrame = currentFrame;
                    previousCluster = int.Parse(split[1]);
                    previousTunnel = int.Parse(split[2]);
                    previousSphereCount = (split.Length - 13);

                    floatFrameTunnelArray = new float[previousSphereCount * 5];
                    for (int i = 13, j = 0; i < split.Length; i++, j ++)
                    {
                        floatFrameTunnelArray[j * 5] = float.Parse(split[i]);
                        floatFrameTunnelArray[j * 5 + 4] = previousTunnel;
                    }
                }
                else
                {
                    int currentFrame = int.Parse(Regex.Match(split[0], @"\d+").Value);
                    int currentCluster = int.Parse(split[1]);
                    int currentTunnel = int.Parse(split[2]);
                    int currentSphereCount = split.Length - 13;

                    if (previousFrame != currentFrame) throw new Exception("Frame bug");
                    if (previousCluster != currentCluster) throw new Exception("Cluster bug");
                    if (previousTunnel != currentTunnel) throw new Exception("Tunnel bug");
                    if (previousSphereCount != currentSphereCount) throw new Exception("Sphere count bug");

                    if (String.CompareOrdinal(split[12].Trim(), "Y") == 0)
                    {
                        for (int i = 13, j = 0; i < split.Length; i++, j++)
                        {
                            floatFrameTunnelArray[j * 5 + 1] = float.Parse(split[i]);
                        }
                    }
                    if (String.CompareOrdinal(split[12].Trim(), "Z") == 0)
                    {
                        for (int i = 13, j = 0; i < split.Length; i++, j++)
                        {
                            floatFrameTunnelArray[j * 5 + 2] = float.Parse(split[i]);
                        }
                    }
                    if (String.CompareOrdinal(split[12].Trim(), "R") == 0)
                    {
                        for (int i = 13, j = 0; i < split.Length; i++, j++)
                        {
                            floatFrameTunnelArray[j * 5 + 3] = float.Parse(split[i]);
                        }

                        Console.WriteLine("Flush tunnel " + currentTunnel + " to current frame: ");
                        floatFrameList.AddRange(floatFrameTunnelArray);

                        totalSphereCount += previousSphereCount;
                    }
                }

                

                //if (line.StartsWith("ATOM"))
                //{
                    
                //    var position = split.Where(s => s.Contains(".")).ToList();

                //    var floatArray = new[]
                //        {
                //            float.Parse(split[1]),
                //            float.Parse(position[0]),
                //            float.Parse(position[1]),
                //            float.Parse(position[2]),
                //            Array.IndexOf(AtomSymbols, split.Last())
                //        };

                //    if (Math.Abs(floatArray[4] - (-1)) < Single.Epsilon) Console.WriteLine("Unknown symbol: " + split.Last());

                //    var byteArray = new byte[floatArray.Length * sizeof(float)];
                //    Buffer.BlockCopy(floatArray, 0, byteArray, 0, byteArray.Length);

                //    dataWriter.Write(byteArray);
                //}

                //if (line.StartsWith("TER")) break;
            }
        }
    }
}
