using ImageMagick;
using System;

namespace HeadshotPngProgram.Service
{
    internal class Magick : Abstract.ViewModelBase
    {
        /// <summary>
        /// This method processes the input image by cropping it to a specified bounding box (person area), resizing it to fit a target height, 
        /// and adjusting the width to maintain the aspect ratio. The image is returned with the specified quality and filter settings.
        /// </summary>
        /// <param name="image">The input image to be processed.</param>
        /// <param name="targetWidth">The target width for resizing (not used in current implementation).</param>
        /// <param name="targetHeight">The target height to resize the image to.</param>
        /// <param name="personX">The X-coordinate of the top-left corner of the bounding box for the person.</param>
        /// <param name="personY">The Y-coordinate of the top-left corner of the bounding box for the person.</param>
        /// <param name="personWidth">The width of the bounding box for the person.</param>
        /// <param name="personHeight">The height of the bounding box for the person.</param>
        /// <param name="dpi">The desired DPI for the image (currently not applied in the code).</param>
        /// <returns>A processed MagickImage with the person cropped and resized to the target dimensions while maintaining aspect ratio.</returns>
        public MagickImage ProcessImage(MagickImage image, int targetHeight, int personX, int personY, int personWidth, int personHeight)
        {
            try
            {
                // Crop the bounding box of the person
                MagickGeometry cropGeometry = new MagickGeometry(personX, personY, (uint)personWidth, (uint)personHeight);
                image.Crop(cropGeometry);

                // Calculate the scale factor for the height to fit within the target height
                float scaleY = (float)targetHeight / image.Height;

                // Resize the image based on the target height while keeping the aspect ratio for width
                int newHeight = targetHeight;
                int newWidth = (int)(image.Width * scaleY); // Maintain aspect ratio by scaling width

                //image.Density = new Density(300, 300);
                //image.UnsharpMask(0, 2, 1, 0);
                image.Resize((uint)newWidth, (uint)newHeight);
                image.Quality = 100;
                image.FilterType = FilterType.Lanczos;
                return image;

            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
                return null;
            }
            #region setting own canva
            //// Calculate scale to fit the target dimensions while maintaining the aspect ratio
            //float scaleX = (float)targetWidth / image.Width;
            //float scaleY = (float)targetHeight / image.Height;
            //float scale = Math.Min(scaleX, scaleY); // Use the smaller of the two scale factors

            //// Resize the cropped image to fit within target dimensions
            //int newWidth = (int)(image.Width * scale);
            //int newHeight = (int)(image.Height * scale);

            // Create a blank image with a white background
            //MagickImage finalImage = new MagickImage(MagickColors.Transparent, (uint)targetWidth, (uint)targetHeight);

            // Resize the cropped image to fit within the white background
            //image.Resize((uint)newWidth, (uint)newHeight);

            // Calculate the position to center the image on the white background
            //int x = (targetWidth - newWidth) / 2;
            //int y = (targetHeight - newHeight) / 2;

            // Draw the resized image onto the white background
            //finalImage.Composite(image, x, y, CompositeOperator.Over);

            // Set the resolution for printing (DPI)
            #endregion
        }

        /// <summary>
        /// Resizes the input image to fit within the specified maximum width and height while maintaining the aspect ratio.
        /// The image is resized using Lanczos interpolation for better quality.
        /// </summary>
        /// <param name="image">The input image to be resized.</param>
        /// <param name="maxWidth">The maximum width the image can be resized to.</param>
        /// <param name="maxHeight">The maximum height the image can be resized to.</param>
        /// <returns>A resized MagickImage that fits within the specified dimensions while maintaining the aspect ratio.</returns>
        public MagickImage ResizeImage(MagickImage image, int maxWidth, int maxHeight)
        {
            try
            {
                // Calculate the scaling ratio to fit within maxWidth and maxHeight while maintaining aspect ratio
                float ratioX = (float)maxWidth / image.Width;
                float ratioY = (float)maxHeight / image.Height;
                float ratio = Math.Min(ratioX, ratioY);

                int newWidth = (int)(image.Width * ratio);
                int newHeight = (int)(image.Height * ratio);

                // Set the interpolation method to Lanczos for better quality
                //image.Density = new Density(300, 300);

                // Resize the image while maintaining the aspect ratio
                //image.UnsharpMask(0, 2, 1, 0);
                image.Resize((uint)newWidth, (uint)newHeight);
                image.Quality = 100;
                image.FilterType = FilterType.Lanczos;

                return image;
            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
                return null;
            }
        }



        #region trash code

        //public OpenCvSharp.Mat ProcessImage(OpenCvSharp.Mat image, int targetWidth, int targetHeight, int personX, int personY, int personWidth, int personHeight, int dpi)
        //{
        //    // Crop the image based on the bounding box of the person
        //    OpenCvSharp.Rect roi = new OpenCvSharp.Rect(personX, personY, personWidth, personHeight);
        //    OpenCvSharp.Mat croppedImage = new OpenCvSharp.Mat(image, roi);

        //    // Resize the image while maintaining aspect ratio
        //    return ResizeImage(croppedImage, targetWidth, targetHeight);
        //}

        //public OpenCvSharp.Mat ResizeImage(OpenCvSharp.Mat image, int targetWidth, int targetHeight)
        //{
        //    // Calculate the scaling ratio for resizing while maintaining the aspect ratio
        //    float scaleX = (float)targetWidth / image.Width;
        //    float scaleY = (float)targetHeight / image.Height;
        //    float scale = Math.Min(scaleX, scaleY); // Use the smaller of the two scale factors to fit within bounds

        //    // Calculate new width and height
        //    int newWidth = (int)(image.Width * scale);
        //    int newHeight = (int)(image.Height * scale);

        //    // Resize the image
        //    OpenCvSharp.Mat resizedImage = new OpenCvSharp.Mat();
        //    OpenCvSharp.Cv2.Resize(image, resizedImage, new OpenCvSharp.Size(newWidth, newHeight), interpolation: OpenCvSharp.InterpolationFlags.Linear);

        //    return resizedImage;
        //}

        ////2100x1800 pixel
        //int targetWidth = 2100;
        //int targetHeight = 1800;
        //int personX = 1000;
        //int personY = 500;
        //int personWidth = 2040;
        //int personHeight = 1850;
        //int dpi = 300;

        ////5x7 inches pixels
        //int targetInchesWidth = 1500;
        //int targetInchesHeight = 2100;
        //int personInchesX = 2800;
        //int personInchesY = 1100;
        //int personInchesWidth = 2350;
        //int personInchesHeight = 3900;//5500;

        ////icon inches pixels
        //int targetIconWidth = 120;
        //int targetIconHeight = 155;
        //int personIconX = 2800;
        //int personIconY = 1150;//1150
        //int personIcoWidth = 5350;
        //int personIconHeight = 4150;//4100
        //int Icondpi = 72;

        ////Web inches pixels
        //int targetWebWidth = 300;
        //int targetWebHeight = 420;
        //int personWebX = 2800;
        //int personWebY = 1100;//1150
        //int personWebWidth = 5350;
        //int personWebHeight = 4500;//4100
        //int Webdpi = 72;
        #endregion
    }
}
