using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;

namespace BatchSyncAndResizeImages
{
    class Program
    {
        static int ResizeMaxPixels = 1920;
        static string[] SupportedExtensions = new string[] { ".jpg", ".jpeg", ".bmp", ".png" };
        static long JpegQuality = 80;

        static bool _ProcessedImages = true;
        static bool _NeedToStopProcessing = false;

        static bool NeedToStopProcessing { get { return _NeedToStopProcessing || (_NeedToStopProcessing = (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)); } }

        static void Main(string[] args)
        {
            string SourcePath = args.Length >= 1 ? args[0] : string.Empty;
            string DestPath = args.Length >= 2 ? args[1] : string.Empty;

            if (Directory.Exists(SourcePath) && Directory.Exists(DestPath))
            {
                ImageCodecInfo encoder = GetImageEncoder(ImageFormat.Jpeg);
                EncoderParameters parameters = new EncoderParameters(1);
                parameters.Param[0] = new EncoderParameter(Encoder.Quality, JpegQuality);    // set JPG quality

                ResizeImages(SourcePath, DestPath, encoder, parameters);
            }
            else
                Console.WriteLine("Syntax: BatchSyncAndResizeImages.exe [SourcePath] [DestPath]");

            Console.Write("\r\nPress any key to exit");
            Console.ReadKey();
        }

        static bool IsExtensionSupported(string ext)
        {
            ext = ext.ToLower();
            foreach (string SupportedExt in SupportedExtensions)
                if (ext == SupportedExt)
                    return true;

            return false;
        }

        static void ResizeImages(string SourcePath, string DestPath, ImageCodecInfo encoder, EncoderParameters parameters)
        {
            bool VerifyDirectoryExists = true;

            if (NeedToStopProcessing)
                return;

            if (_ProcessedImages)
            {
                Console.Write("\r\nSearching for new and changed files (press ESC to abort)...");
                _ProcessedImages = false;
            }
            else
                Console.Write('.');

            //First, search through directory for supported files -> resize those when needed
            foreach (string SourceFile in Directory.GetFiles(SourcePath))
            {
                if (IsExtensionSupported(Path.GetExtension(SourceFile)))
                {
                    string DestFile = Path.Combine(DestPath, Path.GetFileNameWithoutExtension(SourceFile) + ".jpg"); // we always convert into JPG file
                    if (File.Exists(DestFile))
                    {
                        DateTime SourseModified = File.GetLastWriteTimeUtc(SourceFile);
                        DateTime DestModified = File.GetLastWriteTimeUtc(DestFile);
                        if (DestModified >= SourseModified)
                            continue; // we move on to the next file, current one is up-to-date
                    }

                    if (VerifyDirectoryExists)
                    {
                        VerifyDirectoryExists = false;
                        if (!Directory.Exists(DestPath))
                            Directory.CreateDirectory(DestPath);
                    }

                    if (!_ProcessedImages)
                        Console.WriteLine();

                    Console.WriteLine(SourceFile); //output filename to display

                    _ProcessedImages = true;
                    ResizeImage(SourceFile, DestFile, encoder, parameters);
                }

                if (NeedToStopProcessing)
                    break;
            }

            // process any sub directories
            foreach (string subdir in Directory.GetDirectories(SourcePath))
            {
                ResizeImages(subdir, Path.Combine(DestPath, Path.GetFileName(subdir)), encoder, parameters);
                if (NeedToStopProcessing)
                    break;
            }
        }

        static ImageCodecInfo GetImageEncoder(ImageFormat format)
        {
            ImageCodecInfo[] encoders = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo encoder in encoders)
                if (encoder.FormatID == format.Guid)
                    return encoder;

            return null;
        }

        static void ResizeImage(string SourceFile, string DestinationFile, ImageCodecInfo encoder, EncoderParameters parameters)
        {
            try
            {
                using (Image Source = Image.FromFile(SourceFile))
                {
                    int NewWidth, NewHeight;
                    if (Source.Width > ResizeMaxPixels && Source.Width >= Source.Height)
                    {
                        NewWidth = ResizeMaxPixels;
                        NewHeight = (Source.Height * ResizeMaxPixels) / Source.Width;
                        using (Image Dest = ResizeImage(Source, NewWidth, NewHeight))
                            Dest.Save(DestinationFile, encoder, parameters);
                    }
                    else if (Source.Height > ResizeMaxPixels && Source.Height >= Source.Width)
                    {
                        NewWidth = (Source.Width * ResizeMaxPixels) / Source.Height;
                        NewHeight = ResizeMaxPixels;
                        using (Image Dest = ResizeImage(Source, NewWidth, NewHeight))
                            Dest.Save(DestinationFile, encoder, parameters);
                    }
                    else
                    {
                        // no need for resize
                        Source.Save(DestinationFile, encoder, parameters);
                    }
                }
            }
            catch (Exception e)
            {
                File.Delete(DestinationFile); // make sure destination file doesn't exist anymore
                Console.WriteLine("Error: " + e.Message);
            }
        }

        static Image ResizeImage(Image Source, int NewWidth, int NewHeight)
        {
            Image DestImg = new Bitmap(NewWidth, NewHeight);
            using (Graphics gr = Graphics.FromImage(DestImg))
            {
                gr.SmoothingMode =  SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.HighQualityBilinear;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(Source, new Rectangle(0, 0, NewWidth, NewHeight));
            }
            return DestImg;
        }
        
        static bool ThumbnailCallback()
        {
            return true;
        }

    }
}
