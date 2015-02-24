using System;
using System.IO;
using System.Linq;

namespace FrameDataReader
{
    class Program
    {
        //private const string TargetDirectory = "./";

        private const string TargetDirectory = @"D:\Projects\UnityProjects\BrnoProjectWithVolume\Data\atoms\";
        private const string DataFilePath = TargetDirectory + "data.bin";

        public static string[] AtomSymbols = { "C", "H", "N", "O", "P", "S" }; 
        public static string[] AminoAcidSymbols = { "ALA", "ASN", "ASP", "ARG", "CYS", "GLN", "GLU", "GLY", "HIS", "ILE", "LEU", "LYS", "MET", "PHE", "PRO", "SER", "THR", "TRP", "TYR", "VAL" };

        static void Main(string[] args)
        {
            var fileEntries = Directory.GetFiles(TargetDirectory, "*.pdb");
            var dataWriter = new BinaryWriter(File.Open(DataFilePath, FileMode.Create));
            
            var fileCount = 0;

            foreach (var fileName in fileEntries)
            {
                foreach (var line in File.ReadAllLines(fileName))
                {
                    if (line.StartsWith("ATOM"))
                    {
                        var split = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var position = split.Where(s => s.Contains(".")).ToList();
                        
                        var atomSymbolId = Array.IndexOf(AtomSymbols, split[2][0].ToString());
                        if (atomSymbolId < 0) throw new Exception("Atom symbol not found");
                        
                        var aminoAcidSymbol = split[3];
                        if (aminoAcidSymbol == "HIP" || aminoAcidSymbol == "HID" || aminoAcidSymbol == "HIE") aminoAcidSymbol = "HIS";
                        var aminoAcidSymbolId = Array.IndexOf(AminoAcidSymbols, aminoAcidSymbol);
                        if (aminoAcidSymbolId < 0) throw new Exception("Amino-acid symbol not found");

                        var aminoAcidId = int.Parse(split[4]);
                        var floatArray = new[]
                        {
                            atomSymbolId,
                            aminoAcidId,
                            aminoAcidSymbolId,
                            float.Parse(position[0]),
                            float.Parse(position[1]),
                            float.Parse(position[2]),
                        };

                        var byteArray = new byte[floatArray.Length * sizeof(float)];
                        Buffer.BlockCopy(floatArray, 0, byteArray, 0, byteArray.Length);

                        dataWriter.Write(byteArray);
                    }

                    if (line.StartsWith("TER")) break;
                }

                fileCount++;

                if (fileCount % 100 == 0)
                    Console.WriteLine("Frame: " + (fileCount / 100) * 100);
            }
        }
    }
}
