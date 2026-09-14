using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HT_SECIM.Core
{
    /// <summary>
    /// "anahtar = deger = deger ..." satirlarini okuyan basit config okuyucu.
    /// // ile baslayan satirlari ve bos satirlari atlar.
    /// </summary>
    public static class ConfigReader
    {
        public static List<string[]> ReadLines(string filePath)
        {
            List<string[]> result = new List<string[]>();

            if (!File.Exists(filePath))
                return result;

            // BOM varsa ona gore, yoksa UTF-8 kabul edilir.
            string[] rawLines = File.ReadAllLines(filePath, Encoding.UTF8);

            foreach (string raw in rawLines)
            {
                string line = raw.Trim();

                if (line.Length == 0) continue;
                if (line.StartsWith("//")) continue;
                if (line.StartsWith("#")) continue;
                if (line.IndexOf('=') < 0) continue;

                string[] parts = line.Split('=');
                for (int i = 0; i < parts.Length; i++)
                    parts[i] = parts[i].Trim();

                result.Add(parts);
            }

            return result;
        }
    }
}