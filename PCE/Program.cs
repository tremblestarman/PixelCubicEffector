using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace PCE
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Input:");
            var command = Console.ReadLine();

            #region ResourcePack
            var resoucePath = Application.StartupPath + "\\Default";
            var resouceScale = 16;
            if (Regex.Match(command, @"(?<=(^|\s)-res:).*?(?=(\s-)|$)").Success) //Set ResourcePack
            {
                resoucePath = Regex.Match(command, @"(?<=(^|\s)-res:).*?(?=(\s-)|$)").Value.Trim('"').Trim(' ');
            }
            if (Regex.Match(command, @"(?<=(^|\s)-resm:).*?(?=(\s-)|$)").Success) //Set ResourcePack Mode
            {
                Int32.TryParse(Regex.Match(command, @"(?<=(^|\s)-resm:).*?(?=(\s-)|$)").Value, out resouceScale);
            }
            #endregion
            #region Input & Output
            string imgPath = "", outPath = "";
            HashSet<string> imgPaths = new HashSet<string>();
            if (Regex.Match(command, @"(?<=(^|\s)-i:).*?(?=(\s-)|$)").Success) //Input
            {
                imgPath = Regex.Match(command, @"(?<=(^|\s)-i:).*?(?=(\s-)|$)").Value.Trim('"').Trim(' ');
                if (File.Exists(imgPath))
                {
                    var info = new FileInfo(imgPath);
                    outPath = info.DirectoryName + "\\" + info.Name.Replace(info.Extension, "_pcp" + info.Extension);
                }
                else if (Directory.Exists(imgPath))
                {
                    foreach (var file in new DirectoryInfo(imgPath).GetFiles().OrderBy(f => f.CreationTime))
                    {
                        imgPaths.Add(file.FullName);
                    }
                }
            }
            if (Regex.Match(command, @"(?<=(^|\s)-o:).*?(?=(\s-)|$)").Success) //Input
            {
                outPath = Regex.Match(command, @"(?<=(^|\s)-o:).*?(?=(\s-)|$)").Value.Trim('"').Trim(' ');
            }
            #endregion
            #region Element
            int eScaleW = 1, eScaleH = 1;
            if (Regex.Match(command, @"(?<=(^|\s)-es:).*?(?=(\s-)|$)").Success) //Elements Scale
            {
                var scaling = Regex.Match(command, @"(?<=(^|\s)-es:).*?(?=(\s-)|$)").Value.Split(',');
                if (scaling.Length == 1) { Int32.TryParse(scaling[0].Trim(' '), out eScaleW); eScaleH = eScaleW; }
                else if (scaling.Length == 1) { Int32.TryParse(scaling[0].Trim(' '), out eScaleW); Int32.TryParse(scaling[1].Trim(' '), out eScaleH); }
                else { Console.WriteLine("Error: Wrong Element Scale."); Console.ReadKey(); }
            }
            #endregion
            #region Operation
            var nopause = true;
            if (Regex.Match(command, @"(?<=(^|\s)-es:).*?(?=(\s-)|$)").Success) //Elements Scale
            {
                nopause = !Regex.Match(command, @"(?<=(^|\s))-c(?=(\s-)|$)").Success;
            }
            #endregion

            //Set CharacteristicColorList
            if (!File.Exists(resoucePath + "\\CharacteristicColors.json"))
            {
                Console.WriteLine("- Reading Colors");
                CharacteristicColorList(resoucePath, resouceScale);
                Console.WriteLine("- Writing \"CharacteristicColors.json\"");
                File.WriteAllText(resoucePath + "\\CharacteristicColors.json", JsonConvert.SerializeObject(_cColorList, Formatting.Indented));
            }
            else
                _cColorList = JsonConvert.DeserializeObject<HashSet<TXColor>>(File.ReadAllText(resoucePath + "\\CharacteristicColors.json"));

            //Generate
            if (imgPaths.Count > 0)
            {
                int index = 0;
                foreach (var i in imgPaths)
                {
                    var img = SelfAdaptElementScale(eScaleW, eScaleH, new Bitmap(i));
                    var newmap = new Bitmap(img.Width * resouceScale / eScaleW, img.Height * resouceScale / eScaleH);
                    var ctop = Console.CursorTop;

                    for (var h = 0; h < img.Height; h += eScaleH)
                    {
                        for (var w = 0; w < img.Width; w += eScaleW)
                        {
                            var b = Clip(img, w, w + eScaleH - 1, h, h + eScaleH - 1);
                            var rep = SimilarTexture(b);
                            newmap = PasteOn(newmap, resoucePath, rep, w * resouceScale / eScaleW, h * resouceScale / eScaleH);
                        }
                        Console.Write("-Progess: [" + (index + 1) + "/" + imgPaths.Count + "] " + ((double)h * 100 / (img.Height - 1)).ToString("0.00") + "%");
                        Console.CursorVisible = false;
                        Console.SetCursorPosition(0, ctop);
                    }

                    var info = new FileInfo(i);
                    if (outPath.Contains("%d")) newmap.Save(outPath.Replace("%d", index.ToString()));
                    else if (outPath.Contains("%n")) newmap.Save(outPath.Replace("%n", info.Name.Replace(info.Extension, "")));
                    else if (outPath != "") newmap.Save(outPath + "_pcp_" + index);
                    else newmap.Save(info.DirectoryName + "\\" + info.Name.Replace(info.Extension, "_pcp_" + index + info.Extension));
                    index++;
                    
                }
                Console.CursorVisible = true;
                Console.CursorTop++;
            }
            else if (File.Exists(imgPath))
            {
                var img = SelfAdaptElementScale(eScaleW, eScaleH, new Bitmap(imgPath));
                var newmap = new Bitmap(img.Width * resouceScale / eScaleW, img.Height * resouceScale / eScaleH);
                var ctop = Console.CursorTop;

                for (var h = 0; h < img.Height; h += eScaleH)
                {
                    for (var w = 0; w < img.Width; w += eScaleW)
                    {
                        var b = Clip(img, w, w + eScaleH - 1, h, h + eScaleH - 1);
                        var rep = SimilarTexture(b);
                        newmap = PasteOn(newmap, resoucePath, rep, w * resouceScale / eScaleW, h * resouceScale / eScaleH);
                    }
                    Console.Write("-Progess: " + ((double)h * 100 / (img.Height - 1)).ToString("0.00") + "%");
                    Console.CursorVisible = false;
                    Console.SetCursorPosition(0, ctop);
                }

                newmap.Save(outPath);
                Console.CursorVisible = true;
                Console.CursorTop++;
            }
            else
            {
                Console.Write("File Not Found.");
            }

            if (nopause) return;
            Console.WriteLine("[ Press any key to continue... ]");
            Console.ReadKey(true);
        }
        #region ReadTextures
        /// <summary>
        /// Characteristic Colors List
        /// </summary>
        static HashSet<TXColor> _cColorList = new HashSet<TXColor>();
        /// <summary>
        /// Get Characteristic Colors List
        /// </summary>
        /// <param name="ResourcePackPath">Resoucepack Path</param>
        /// <param name="ResourcePackPath">Resoucepack Scale</param>
        /// <param name="upper">Parent Folder Name</param>
        static void CharacteristicColorList(string resourcePackPath, int resouceScale, string upper = "")
        {
            FileInfo[] files = new DirectoryInfo(resourcePackPath).GetFiles();
            DirectoryInfo[] directories = new DirectoryInfo(resourcePackPath).GetDirectories();
            foreach (var file in files)
            {
                if (file.Extension == ".png")
                {
                    var img = new Bitmap(file.FullName);
                    var t = GetThumbnail(img);
                    if (t.R != 0 && t.G != 0 && t.B != 0 && t.R != 255 && t.G != 255 && t.B != 255 && Transparent(img) == false && img.Width >= resouceScale && img.Height >= resouceScale)
                    {
                        t.Path = upper + "/" + file.Name;
                        _cColorList.Add(t);
                    }
                }                    
            }
            foreach (var directory in directories)
            {
                CharacteristicColorList(directory.FullName, resouceScale, "/" + directory.Name);
            }
        }
        /// <summary>
        /// Get Color of 1-pixel Thumbnail
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns>Color</returns>
        public static TXColor GetThumbnail(Bitmap bmp)
        {
            var c = new Bitmap(bmp.GetThumbnailImage(1, 1, null, IntPtr.Zero)).GetPixel(0, 0);
            return new TXColor() { R = c.R, G = c.G, B = c.B };
        }
        /// <summary>
        /// If A Picture Contains Transparent Parts
        /// </summary>
        /// <param name="bmp">A Picture</param>
        /// <returns>T/F</returns>
        public static bool Transparent(Bitmap bmp)
        {
            for (var h = 0; h < bmp.Height; h++)
            {
                for (var w = 0; w < bmp.Width; w++)
                {
                    var p = bmp.GetPixel(w, h);
                    if (p.A == 0) return true;
                }
            }
            return false;
        }
        #endregion
        #region Scale
        static Bitmap SelfAdaptElementScale(int element_w, int element_h, Bitmap img)
        {
            return new Bitmap(img, ScaleRounding(img.Width, element_w), ScaleRounding(img.Height, element_h));
        }
        static int ScaleRounding(int sum, int a)
        {
            if (sum % a >= a / 2) return sum + a - sum % a;
            if (sum % a < a / 2) return sum - sum % a;
            else return sum;
        }
        #endregion
        #region GetSimilarTexture
        /// <summary>
        /// Get Path of The Most Similar Texture With A Picture
        /// </summary>
        /// <param name="bmp">A Picture</param>
        /// <returns>Path of The Most Similar Texture</returns>
        static string SimilarTexture(Bitmap bmp)
        {
            var tumb = GetThumbnail(bmp);
            var best = _cColorList.Max(_a => SD(_a, tumb));
            return (from _a in _cColorList where SD(_a, tumb) == best select _a).First().Path;
        }
        /// <summary>
        /// Get Color Similarity Coefficient
        /// </summary>
        /// <param name="c1">Color A</param>
        /// <param name="c2">Color B</param>
        /// <returns>Color Similarity Coefficient</returns>
        static double SD(TXColor c1, TXColor c2)
        {
            return (255 - Math.Abs(c1.R - c2.R) * 0.297 - Math.Abs(c1.G - c2.G) * 0.593 - Math.Abs(c1.B - c2.B) * 0.11) / 255;
        }
        #endregion
        #region GenerateNewPicture
        /// <summary>
        /// Clip A Picture
        /// </summary>
        /// <param name="source">A Picture</param>
        /// <param name="x1">From X</param>
        /// <param name="x2">To X</param>
        /// <param name="y1">From Y</param>
        /// <param name="y2">To Y</param>
        /// <returns>Clipped Part</returns>
        static Bitmap Clip(Bitmap source, int x1, int x2, int y1, int y2)
        {
            if (x1 > x2) Swap(ref x1, ref x2);
            if (y1 > y2) Swap(ref y1, ref y2);
            int _h = y2 - y1 + 1;
            int _w = x2 - x1 + 1;
            var bmp = new Bitmap(_w, _h);
            for (var h = 0; h < _h; h++)
            {
                for (var w = 0; w < _w; w++)
                {
                    var p = source.GetPixel(w + x1, h + y1);
                    var color = Color.FromArgb(p.R, p.G, p.B);
                    bmp.SetPixel(w, h, color);
                }
            }
            return bmp;
        }
        /// <summary>
        /// Paste A Picture On A Canvas
        /// </summary>
        /// <param name="canvas">Canvas</param>
        /// <param name="resoucePath">Resoucepack Path</param>
        /// <param name="texturePath">Texture Path (Relative to Resoucepack Path)</param>
        /// <param name="x1">From X</param>
        /// <param name="y1">From Y</param>
        /// <returns></returns>
        static Bitmap PasteOn(Bitmap canvas, string resoucePath, string texturePath, int x1, int y1, int resouceScale = 16)
        {
            var texture = new Bitmap(resoucePath + texturePath.Replace("/", "\\"));
            for (var h = 0; h < resouceScale; h++)
            {
                for (var w = 0; w < resouceScale; w++)
                {
                    var color = texture.GetPixel(w, h);
                    canvas.SetPixel(x1 + w, y1 + h, color);
                }
            }
            return canvas;
        }
        #endregion
        /// <summary>
        /// Swap A and B
        /// </summary>
        /// <param name="A">Value A</param>
        /// <param name="B">Value B</param>
        static void Swap(ref int A, ref int B)
        {
            var C = A;
            A = B;
            B = C;
        }
    }
    /// <summary>
    /// Texture Relative Path & Characteristic Color
    /// </summary>
    public class TXColor
    {
        public string Path;
        public int R;
        public int G;
        public int B;
    }
}