using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;

public class Bond
{
    public int atom1;
    public int atom2;
}

public static class PdbReader
{
    public static string[] AtomSymbols = { "C", "H", "N", "O", "P", "S" };
    public static float[] AtomRadii = { 1.548f, 1.100f, 1.400f, 1.348f, 1.880f, 1.808f };
    public static string[] AminoAcidSymbols = { "ALA", "ASN", "ASP", "ARG", "CYS", "GLN", "GLU", "GLY", "HIS", "ILE", "LEU", "LYS", "MET", "PHE", "PRO", "SER", "THR", "TRP", "TYR", "VAL" };

    // Color scheme taken from http://life.nthu.edu.tw/~fmhsu/rasframe/COLORS.HTM
    public static Color[] AtomColors = 
    { 
        new Color(100,100,100) / 255,     // C        light grey
        new Color(255,255,255) / 255,     // H        white       
        new Color(143,143,255) / 255,     // N        light blue
        new Color(220,10,10) / 255,       // O        red         
        new Color(255,165,0) / 255,       // P        orange      
        new Color(255,200,50) / 255       // S        yellow      
    };

    // Color scheme taken from http://life.nthu.edu.tw/~fmhsu/rasframe/COLORS.HTM
    public static Color[] AminoAcidColors = 
    { 
        new Color(200,200,200) / 255,     // ALA      dark grey
        new Color(0,220,220) / 255,       // ASN      cyan   
        new Color(230,10,10) / 255,       // ASP      bright red
        new Color(20,90,255) / 255,       // ARG      blue       
        new Color(255,200,50) / 255,      // CYS      yellow 
        new Color(0,220,220) / 255,       // GLN      cyan   
        new Color(230,10,10) / 255,       // GLU      bright red
        new Color(235,235,235) / 255,     // GLY      light grey
        new Color(130,130,210) / 255,     // HIS      pale blue
        new Color(15,130,15) / 255,       // ILE      green  
        new Color(15,130,15) / 255,       // LEU      green  
        new Color(20,90,255) / 255,       // LYS      blue       
        new Color(255,200,50) / 255,      // MET      yellow 
        new Color(50,50,170) / 255,       // PHE      mid blue
        new Color(220,150,130) / 255,     // PRO      flesh  
        new Color(250,150,0) / 255,       // SER      orange 
        new Color(250,150,0) / 255,       // THR      orange 
        new Color(180,90,180) / 255,      // TRP      pink   
        new Color(50,50,170) / 255,       // TYR      mid blue
        new Color(15,130,15) / 255        // VAL      green  
    };
    
	public static int[] GetAtomBonds(Vector4[] atomPositions, int[] atomTypes, int numAtoms)
    {
        var bonds = new List<int>();
        for (int i = 0; i < numAtoms; i++)
        {
            var atom1 = (Vector3)atomPositions[i];
            var atomSymbol1 = AtomSymbols[atomTypes[i]];

            for (int j = i + 1; j < Mathf.Min(i + 80, numAtoms); j++)
            {
                var atom2 = (Vector3)atomPositions[j];
                var atomSymbol2 = AtomSymbols[atomTypes[j]];
                
                float cutoff = 1.6f;

                if ((atomSymbol1 == "H") && (atomSymbol2 == "H")) continue;
                if ((atomSymbol1 == "S") || (atomSymbol2 == "S")) cutoff = 1.84f;
                if ((atomSymbol1 == "O" && atomSymbol2 == "P") || (atomSymbol2 == "O" && atomSymbol1 == "P")) cutoff = 1.84f;
                if ((atomSymbol1 == "O" && atomSymbol2 == "H") || (atomSymbol2 == "O" && atomSymbol1 == "H")) cutoff = 1.84f;

                float dist = Vector3.Distance(atom1, atom2);
                if (dist <= cutoff + 0.1)
                {
                    bonds.Add(i);
                    bonds.Add(j);
                }
            }
        }

        return bonds.ToArray();
    }
    
    public static List<Vector4> ReadPdbFile(string path)
    {
        var atoms = new List<Vector4>();

        foreach (var line in File.ReadAllLines(path))
        {
            if (line.StartsWith("ATOM"))
            {
                var split = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var position = split.Where(s => s.Contains(".")).ToList();
                var symbol = Array.IndexOf(AtomSymbols, split[2][0].ToString());
                if (symbol < 0) throw new Exception("Symbol not found");

                var atom = new Vector4(float.Parse(position[0]), float.Parse(position[1]), float.Parse(position[2]), symbol);
                atoms.Add(atom);
            }

            if (line.StartsWith("TER")) break;
        }

        // Find the bounding box of the molecule and align the molecule with the origin 
        Vector3 bbMin = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 bbMax = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        Vector3 bbCenter;

        foreach (Vector4 atom in atoms)
        {
            bbMin = Vector3.Min(bbMin, new Vector3(atom.x, atom.y, atom.z));
            bbMax = Vector3.Max(bbMax, new Vector3(atom.x, atom.y, atom.z));
        }

        bbCenter = bbMin + (bbMax - bbMin) * 0.5f;

        for (int i = 0; i < atoms.Count; i++)
        {
            atoms[i] -= new Vector4(bbCenter.x, bbCenter.y, bbCenter.z, 0);
        }

        Debug.Log("Loaded " + atoms.Count + " atoms.");

        return atoms;
    }
}

