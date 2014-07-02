using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Resizer
{
    public delegate void resize_done(string file);
    public delegate void resize_finished();
    
    class ResizerContext
    {
        public ResizerContext(string[] _files, string _src, string _dest, int _quality)
        {
            files = _files;
            src = _src;
            dest = _dest;
            quality = _quality;
        }
        public string[] files;
        public string src;
        public string dest;
        public int quality;

    }

    class ResizerCore
    {
        public event resize_done     file_done;
        public event resize_finished finished;

        private void OnFileDone(string data)
        {
            var handler = file_done;
            if (handler != null)
            {
                handler(data);
            }
        }

        private void OnFinished()
        {
            var handler = finished;
            if (handler != null)
            {
                handler();
            }
        }

        public void run(ResizerContext ctx)
        {
            Stream BitmapStream;
            System.Drawing.Image img;
            Bitmap image;

            foreach (string file in ctx.files)
            {
                using (BitmapStream = System.IO.File.Open(file, System.IO.FileMode.Open))
                {
                    using (img = System.Drawing.Image.FromStream(BitmapStream))
                    {
                        using (image = new Bitmap(img))
                        {
                            string filedest = System.IO.Path.Combine(ctx.dest, System.IO.Path.GetFileName(file));

                            if (image.Width != 3648 && image.Width != 2736) continue;

                            if (image.Width == 3648)
                            {
                                resize(image, filedest, 1280, 960, ctx.quality);
                            }
                            else
                            {
                                resize(image, filedest, 960, 1280, ctx.quality);
                            }
                        }
                    }
                }
                OnFileDone(file);
            }
            OnFinished();
        }

        private static ImageCodecInfo GetEncoderInfo(ImageFormat format)
        {
            return ImageCodecInfo.GetImageDecoders().SingleOrDefault(c => c.FormatID == format.Guid);
        }

        private static void resize(Bitmap image, string dest, int maxWidth, int maxHeight, int quality)
        {
            // Get the image's original width and height
            int originalWidth = image.Width;
            int originalHeight = image.Height;

            // To preserve the aspect ratio
            float ratioX = (float)maxWidth / (float)originalWidth;
            float ratioY = (float)maxHeight / (float)originalHeight;
            float ratio = Math.Min(ratioX, ratioY);

            // New width and height based on aspect ratio
            int newWidth = (int)(originalWidth * ratio);
            int newHeight = (int)(originalHeight * ratio);

            // Convert other formats (including CMYK) to RGB.
            using (Bitmap newImage = new Bitmap(newWidth, newHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
            {
                // Draws the image in the specified size with quality mode set to HighQuality
                using (Graphics graphics = Graphics.FromImage(newImage))
                {
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.DrawImage(image, 0, 0, newWidth, newHeight);
                }

                // Get an ImageCodecInfo object that represents the JPEG codec.
                ImageCodecInfo imageCodecInfo = GetEncoderInfo(ImageFormat.Jpeg);

                // Create an Encoder object for the Quality parameter.
                System.Drawing.Imaging.Encoder encoder = System.Drawing.Imaging.Encoder.Quality;

                // Create an EncoderParameters object. 
                EncoderParameters encoderParameters = new EncoderParameters(1);

                // Save the image as a JPEG file with quality level.
                EncoderParameter encoderParameter = new EncoderParameter(encoder, quality);
                encoderParameters.Param[0] = encoderParameter;

                newImage.Save(dest, imageCodecInfo, encoderParameters);
            }
        }
    }
}
