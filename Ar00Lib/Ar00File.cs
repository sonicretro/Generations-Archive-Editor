using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ar00Lib
{
    public class Ar00File
    {
        public class File
        {
            public string Name { get; set; }
            public byte[] Data { get; set; }

            public File()
            {
                Name = string.Empty;
            }

            public File(string fileName)
            {
                Name = Path.GetFileName(fileName);
                Data = System.IO.File.ReadAllBytes(fileName);
            }

            public File(string name, byte[] data)
            {
                Name = name;
                Data = data;
            }
        }

        public List<File> Files { get; set; }
        public int Padding { get; set; }

        public Ar00File()
        {
            Files = new List<File>();
            Padding = 0x40;
        }

        public Ar00File(string filename)
        {
            string fp = filename;
            int i = -1;
            int maxfile = 100;
            if (filename.EndsWith(".ar.00", StringComparison.OrdinalIgnoreCase))
            {
                fp = fp.Remove(filename.Length - 3);
                i = 0;
                if (System.IO.File.Exists(fp + "l"))
                    maxfile = BitConverter.ToInt32(System.IO.File.ReadAllBytes(fp + "l"), 4);
            }
            Files = new List<File>();
            while (System.IO.File.Exists(filename) & i < maxfile)
            {
                byte[] file = System.IO.File.ReadAllBytes(filename);
                if (BitConverter.ToUInt32(file, 0) != 0 || BitConverter.ToUInt32(file, 4) != 0x10 || BitConverter.ToUInt32(file, 8) != 0x14)
                    throw new Exception("Error: Unknown archive type");
                if (i < 1)
                    Padding = BitConverter.ToInt32(file, 0xC);
                int addr = 0x10;
                while (addr < file.Length)
                {
                    string name = GetCString(file, addr + 0x14);
                    byte[] f = new byte[BitConverter.ToInt32(file, addr + 4)];
                    Array.Copy(file, addr + BitConverter.ToInt32(file, addr + 8), f, 0, f.Length);
                    Files.Add(new File(name, f));
                    addr += BitConverter.ToInt32(file, addr);
                }
                if (i != -1)
                {
                    i++;
                    filename = fp + "." + i.ToString("00");
                }
                else
                    break;
            }
        }

        public void Save(string filename) { Save(filename, false); }

        public void Save(string filename, bool split)
        {
            string fn = filename;
            string fp = filename;
            int i = -1;
            if (fn.EndsWith(".ar.00", StringComparison.OrdinalIgnoreCase) & split)
            {
                fp = fp.Remove(filename.Length - 3);
                i = 0;
            }
            List<File> f = new List<File>(Files);
            List<byte> file = new List<byte>();
            file.AddRange(BitConverter.GetBytes(0u));
            file.AddRange(BitConverter.GetBytes(0x10u));
            file.AddRange(BitConverter.GetBytes(0x14u));
            file.AddRange(BitConverter.GetBytes(Padding));
            while (f.Count > 0)
            {
                File item = f[0];
                int hlen = 0x14 + item.Name.Length + 1;
                int off = (hlen + file.Count) % Padding;
                if (off != 0)
                    hlen += Padding - off;
                if (file.Count + hlen + item.Data.Length > 0xA00000 & i != -1)
                {
                    System.IO.File.WriteAllBytes(fn, file.ToArray());
                    file = new List<byte>();
                    file.AddRange(BitConverter.GetBytes(0u));
                    file.AddRange(BitConverter.GetBytes(0x10u));
                    file.AddRange(BitConverter.GetBytes(0x14u));
                    file.AddRange(BitConverter.GetBytes(Padding));
                    hlen = 0x14 + item.Name.Length + 1;
                    off = (hlen + file.Count) % Padding;
                    if (off != 0)
                        hlen += Padding - off;
                    i++;
                    fn = fp + "." + i.ToString("00");
                }
                file.AddRange(BitConverter.GetBytes(hlen + item.Data.Length));
                file.AddRange(BitConverter.GetBytes(item.Data.Length));
                file.AddRange(BitConverter.GetBytes(hlen));
                file.AddRange(BitConverter.GetBytes(0));
                file.AddRange(BitConverter.GetBytes(0));
                file.AddRange(Encoding.ASCII.GetBytes(item.Name));
                file.Add(0);
                off = file.Count % Padding;
                if (off != 0)
                    file.AddRange(new byte[Padding - off]);
                file.AddRange(item.Data);
                f.RemoveAt(0);
            }
            System.IO.File.WriteAllBytes(fn, file.ToArray());
        }

        private static string GetCString(byte[] file, int address)
        {
            int textsize = 0;
            while (file[address + textsize] > 0)
                textsize += 1;
            return Encoding.ASCII.GetString(file, address, textsize);
        }

        public static void GenerateArlFile(string ar00file) { GenerateArlFile(ar00file, false); }

        public static void GenerateArlFile(string ar00file, bool split)
        {
            string fp = ar00file;
            int s = -1;
            if (ar00file.EndsWith(".ar.00", StringComparison.OrdinalIgnoreCase) & split)
            {
                fp = fp.Remove(ar00file.Length - 3);
                s = 0;
            }
            string fn = ar00file;
            string dest = fp;
            if (dest.EndsWith(".ar.00", StringComparison.OrdinalIgnoreCase))
                dest = dest.Remove(ar00file.Length - 3);
            dest = Path.ChangeExtension(dest, "arl");
            List<byte> header = new List<byte>(Encoding.ASCII.GetBytes("ARL2"));
            List<byte> output = new List<byte>();
            while (System.IO.File.Exists(fn))
            {
                byte[] file = System.IO.File.ReadAllBytes(fn);
                if (BitConverter.ToUInt32(file, 0) != 0 || BitConverter.ToUInt32(file, 4) != 0x10 || BitConverter.ToUInt32(file, 8) != 0x14)
                    throw new Exception("Error: Unknown archive type");
                header.AddRange(BitConverter.GetBytes(file.Length));
                int addr = 0x10;
                while (addr < file.Length)
                {
                    string name = GetCString(file, addr + 0x14);
                    output.Add((byte)name.Length);
                    output.AddRange(Encoding.ASCII.GetBytes(name));
                    addr += BitConverter.ToInt32(file, addr);
                }
                if (s != -1)
                {
                    s++;
                    fn = fp + "." + s.ToString("00");
                }
                else
                    break;
            }
            header.InsertRange(4, BitConverter.GetBytes(Math.Max(s, 1)));
            output.InsertRange(0, header);
            System.IO.File.WriteAllBytes(dest, output.ToArray());
        }
    }
}