using ImageMagick;
using OpenCvSharp;
using System;
using System.IO;

namespace HeadshotPngProgram.Service
{
    internal class FaceDetect : Abstract.ViewModelBase
    {

        #region Open.CV 

        /// <summary>
        /// Centers the image around the detected face and resizes it to the specified dimensions.
        /// Optionally zooms the image before processing, and returns a MagickImage with a white background and the face-centered image.
        /// </summary>
        /// <param name="magickImage">The input MagickImage to be processed.</param>
        /// <param name="dimensionWidth">The target width for the resized image.</param>
        /// <param name="dimensionHeight">The target height for the resized image.</param>
        /// <param name="targetDPI">The target DPI for the output image.</param>
        /// <param name="isZoom">Indicates whether to apply zoom to the image before processing.</param>
        /// <returns>A MagickImage with the face centered and resized, or null if an error occurs.</returns>
        public MagickImage CenterImage(MagickImage magickImage, int dimensionWidth, int dimensionHeight, int targetDPI, bool isZoom, float zoompercent, bool facedetect)
        {
            try
            {
                // Load the Haar Cascade face detector
                var faceCascade = new CascadeClassifier(@"Asset\haarcascade_frontalface_default.xml");
                var eyesCascade = new CascadeClassifier(@"Asset\haarcascade_eye.xml");
                var smileCascade = new CascadeClassifier(@"Asset\haarcascade_smile.xml");

                // Read the input image (using OpenCvSharp)
                byte[] imageBytes = magickImage.ToByteArray(MagickFormat.Png);
                Mat image = Cv2.ImDecode(imageBytes, ImreadModes.Unchanged);


                //OpenCvSharp.Mat image = OpenCvSharp.Cv2.ImRead(image);
                if (image.Empty())
                {
                    WarningMessage("Image could not be loaded.");
                }


                //Zoom the icon 
                if (isZoom)
                {
                    image = Zoomimage(image, zoompercent);
                }


                // Convert to grayscale for face detection
                Mat gray = new Mat();
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

                // Detect faces
                //var facesDetect = faceCascade.DetectMultiScale(gray, 1.1, 5, OpenCvSharp.HaarDetectionTypes.ScaleImage);
                var facesDetect = faceCascade.DetectMultiScale(gray, 1.05, 3, minSize: new Size(20, 20));
                int faceCenterX = 0;

                if (facedetect)
                {
                    if (facesDetect.Length != 0)
                    {
                        Rect largestFace = facesDetect[0];
                        foreach (var face in facesDetect)
                        {
                            if (face.Width * face.Height > largestFace.Width * largestFace.Height)
                            {
                                largestFace = face;
                            }
                        }

                        // Draw rectangles around detected faces
                        //Cv2.Rectangle(image, largestFace, Scalar.Red, 2);

                        // Calculate the center of the detected face
                        faceCenterX = largestFace.X + largestFace.Width / 2;


                    }
                    else { return null; }
                }
                else 
                {
                    faceCenterX = image.Width / 2;
                }


                #region other detector
                //else
                //{
                //    WarningMessage("No face detected this will proceed to smile detect.");
                //    var smileDetect = smileCascade.DetectMultiScale(gray, 1.05, 3, minSize: new Size(20, 20));

                //    // Draw rectangles around detected eyes

                //    Rect largestEye = smileDetect[0];
                //    foreach (var eye in smileDetect)
                //    {
                //        Cv2.Rectangle(image, eye, Scalar.Blue, 2);
                //    }

                //    if (smileDetect.Length > 0)
                //    {
                //        foreach (var eye in smileDetect)
                //        {
                //            if (eye.Width * eye.Height > largestEye.Width * largestEye.Height)
                //            {
                //                largestEye = eye;
                //            }
                //        }

                //        // Draw the largest eye rectangle in red (or any other color)
                //        Cv2.Rectangle(image, largestEye, Scalar.Red, 2); // Red rectangle

                //        // Calculate the center of the detected face
                //        faceCenterX = largestEye.X + largestEye.Width / 2;

                //    }
                //    else
                //    {
                //        WarningMessage("No smile detected this will proceed to original image.");
                //        // If no eyes are detected, default to the image center
                //        faceCenterX = image.Width / 2;
                //    }
                //}
                #endregion

                //else {  faceCenterX = image.Width / 2;}

                // Get the largest face (if multiple faces are detected)
                //Rect largestFace = facesDetect[0];
                //foreach (var face in facesDetect)
                //{
                //    if (face.Width * face.Height > largestFace.Width * largestFace.Height)
                //    {
                //        largestFace = face;
                //    }
                //}

                // Calculate the center of the detected face
                //int faceCenterX = largestFace.X + largestFace.Width / 2;
                //int faceCenterY = largestFace.Y + largestFace.Height / 2;

                // Calculate the center of the target canvas (dimensionWidth x dimensionHeight)
                int targetCenterX = dimensionWidth / 2;
                int targetCenterY = dimensionHeight / 2;

                // Calculate the translation needed
                // No translation needed for the Y-axis
                int translationX = targetCenterX - faceCenterX;
                int translationY = 0;

                // Create the transformation matrix (translation)
                Mat translationMatrix = Mat.Eye(2, 3, MatType.CV_32F);
                translationMatrix.Set<float>(0, 2, translationX);
                translationMatrix.Set<float>(1, 2, translationY);

                // Apply the affine transformation (translation)
                Mat centeredImage = new Mat();
                Cv2.WarpAffine(image, centeredImage, translationMatrix, new Size(dimensionWidth, dimensionHeight));

                Cv2.ImEncode(".png", centeredImage, out byte[] resultBytes); // Encode Mat to byte array (JPEG)
                using (var ms = new MemoryStream(resultBytes))
                {
                    // Create MagickImage from byte array
                    MagickImage resultMagickImage = new MagickImage(ms);
                    resultMagickImage.Quality = 82;
                    //resultMagickImage.Density = new Density(300);

                    int x = (int)((dimensionWidth - resultMagickImage.Width) / 2);
                    int y = (int)((dimensionHeight - resultMagickImage.Height) / 2);

                    MagickImage whiteBackground = new MagickImage(MagickColors.White, (uint)dimensionWidth, (uint)dimensionHeight);
                    //whiteBackground.UnsharpMask(0, 2, 1, 0);
                    whiteBackground.Composite(resultMagickImage, x, y, CompositeOperator.Over);
                    whiteBackground.Quality = 100;
                    whiteBackground.Density = new Density(targetDPI);
                    whiteBackground.FilterType = FilterType.Lanczos;


                    return whiteBackground;
                }



            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
                return null;
            }
        }

        //zoom base on zoomfactor = 1.2f
        private Mat Zoomimage(Mat image, float zoomFactor)
        {
            try
            {
                //float zoomFactor = 1.2f; // 40% zoom //1.10f; // 10% zoom
                int newWidth = (int)(image.Width * zoomFactor);
                int newHeight = (int)(image.Height * zoomFactor);

                // Update the image to the final resized version
                Mat zoomedImage = new Mat();
                for (int i = 0; i < 3; i++)  // Resize in 3 steps for smoother quality
                {
                    Cv2.Resize(image, zoomedImage, new Size(newWidth, newHeight), 0, 0, InterpolationFlags.Lanczos4);
                }

                image = zoomedImage;

                return image;
            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
                return null;
            }
        }


        #endregion 


        #region Emgu.CV
        /// <summary>
        /// Centers the image around the detected face and resizes it to the specified dimensions.
        /// Optionally zooms the image before processing, and returns a MagickImage with a white background and the face-centered image.
        /// </summary>
        /// <param name="magickImage">The input MagickImage to be processed.</param>
        /// <param name="dimensionWidth">The target width for the resized image.</param>
        /// <param name="dimensionHeight">The target height for the resized image.</param>
        /// <param name="targetDPI">The target DPI for the output image.</param>
        /// <param name="isZoom">Indicates whether to apply zoom to the image before processing.</param>
        /// <returns>A MagickImage with the face centered and resized, or null if an error occurs.</returns>
        //public MagickImage CenterImage(MagickImage magickImage, int dimensionWidth, int dimensionHeight, int targetDPI, bool isZoom, float zoompercent)
        //{
        //    try
        //    {
        //        // Load the Haar Cascade face detector
        //        var faceCascade = new CascadeClassifier(@"Asset\haarcascade_frontalface_default.xml");

        //        // Read the input image (using OpenCvSharp)
        //        byte[] imageBytes = magickImage.ToByteArray(MagickFormat.Png);
        //        Mat image = new Mat();
        //        CvInvoke.Imdecode(imageBytes, ImreadModes.Unchanged, image);


        //        //OpenCvSharp.Mat image = OpenCvSharp.Cv2.ImRead(image);
        //        if (image.IsEmpty)
        //        {
        //            WarningMessage("Image could not be loaded.");
        //        }

        //        //Zoom the icon 
        //        if (isZoom)
        //        {
        //            image = Zoomimage(image, zoompercent);
        //        }


        //        // Convert to grayscale for face detection
        //        Mat gray = new Mat();
        //        CvInvoke.CvtColor(image, gray, ColorConversion.Bgr2Rgb);

        //        // Detect faces
        //        var faces = faceCascade.DetectMultiScale(gray, 1.1, 5, new System.Drawing.Size(30,30));
        //        if (faces.Length == 0)
        //        {
        //            throw new Exception("No face detected.");
        //        }

        //        // Get the largest face (if multiple faces are detected)
        //        Rectangle largestFace = faces[0];
        //        foreach (var face in faces)
        //        {
        //            if (face.Width * face.Height > largestFace.Width * largestFace.Height)
        //            {
        //                largestFace = face;
        //            }
        //        }

        //        // Calculate the center of the detected face
        //        int faceCenterX = largestFace.X + largestFace.Width / 2;
        //        int faceCenterY = largestFace.Y + largestFace.Height / 2;

        //        // Calculate the center of the target canvas (dimensionWidth x dimensionHeight)
        //        int targetCenterX = dimensionWidth / 2;
        //        int targetCenterY = dimensionHeight / 2;

        //        // Calculate the translation needed
        //        // No translation needed for the Y-axis
        //        int translationX = targetCenterX - faceCenterX;
        //        int translationY = 0;

        //        //// Create the transformation matrix (translation)
        //        //OpenCvSharp.Mat translationMatrix = OpenCvSharp.Mat.Eye(2, 3, OpenCvSharp.MatType.CV_32F);
        //        //translationMatrix.Set<float>(0, 2, translationX);
        //        //translationMatrix.Set<float>(1, 2, translationY);

        //        //// Apply the affine transformation (translation)
        //        //OpenCvSharp.Mat centeredImage = new OpenCvSharp.Mat();
        //        //OpenCvSharp.Cv2.WarpAffine(image, centeredImage, translationMatrix, new OpenCvSharp.Size(dimensionWidth, dimensionHeight));

        //        // Create a new blank canvas (white background) to place the image onto
        //        Mat centeredImage = new Mat(dimensionHeight, dimensionWidth, image.Depth, image.NumberOfChannels);
        //        centeredImage.SetTo(new MCvScalar(255, 255, 255)); // White background

        //        // Calculate the placement of the original image on the centered canvas
        //        int xOffset = Math.Max(0, translationX); // Ensure we don't place the image outside the canvas
        //        int yOffset = Math.Max(0, translationY);

        //        // Resize the image to fit the target canvas size if necessary
        //        Mat resizedImage = new Mat();
        //        CvInvoke.Resize(image, resizedImage, new Size(dimensionWidth, dimensionHeight), 0, 0, Emgu.CV.CvEnum.Inter.Linear);

        //        // Copy the resized image into the blank centered image at the calculated position
        //        resizedImage.CopyTo(centeredImage);


        //        byte[] resultBytes = null;

        //        // Using `null` as the third argument for ImEncode since we don't need additional flags or parameters.
        //        bool success = CvInvoke.Imencode(".png", centeredImage, resultBytes, param);

        //        if (success)
        //        {
        //            using (var ms = new MemoryStream(resultBytes))
        //            {
        //                // Create MagickImage from byte array
        //                MagickImage resultMagickImage = new MagickImage(ms);
        //                resultMagickImage.Quality = 82;
        //                resultMagickImage.Density = new Density(300);

        //                int x = (int)((dimensionWidth - resultMagickImage.Width) / 2);
        //                int y = (int)((dimensionHeight - resultMagickImage.Height) / 2);

        //                MagickImage whiteBackground = new MagickImage(MagickColors.White, (uint)dimensionWidth, (uint)dimensionHeight);
        //                //whiteBackground.UnsharpMask(0, 2, 1, 0);
        //                whiteBackground.Composite(resultMagickImage, x, y, CompositeOperator.Over);
        //                whiteBackground.Quality = 100;
        //                whiteBackground.Density = new Density(targetDPI);
        //                whiteBackground.FilterType = FilterType.Lanczos;

        //                return whiteBackground;
        //            }

        //        }
        //        else { return null; }

        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorMessage(ex);
        //        return null;
        //    }
        //}

        ////zoom base on zoomfactor = 1.2f
        //private Mat Zoomimage(Mat image, float zoomFactor)
        //{
        //    try
        //    {
        //        //float zoomFactor = 1.2f; // 40% zoom //1.10f; // 10% zoom
        //        int newWidth = (int)(image.Width * zoomFactor);
        //        int newHeight = (int)(image.Height * zoomFactor);

        //        // Update the image to the final resized version
        //        Mat zoomedImage = new Mat();
        //        for (int i = 0; i < 3; i++)  // Resize in 3 steps for smoother quality
        //        {
        //            CvInvoke.Resize(image, zoomedImage, new Size(newWidth, newHeight), 0, 0, Inter.Lanczos4);
        //        }

        //        image = zoomedImage;

        //        return image;
        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorMessage(ex);
        //        return null;
        //    }
        //}
        #endregion

        #region trash code
        //    // Function to perform face detection and center the face in the new image
        //    public MagickImage CenterFaceProcess(MagickImage inputImage, string outputFolderPath, int targetWidth, int targetHeight, int targetDPI, string outputName, string imageOutputFolder)
        //    {
        //        // Load the Haar Cascade face detector
        //        var faceCascade = new CascadeClassifier(@"Asset\haarcascade_frontalface_default.xml");

        //        // Read the input image
        //        //Mat image = Cv2.ImRead(inputImagePath);

        //        // Convert MagickImage to OpenCV Mat
        //        byte[] imageBytes = inputImage.ToByteArray(MagickFormat.Jpeg);
        //        Mat image = Cv2.ImDecode(imageBytes, ImreadModes.Color);

        //        if (image.Empty())
        //        {
        //            throw new Exception("Image could not be loaded.");
        //        }

        //        // Convert to grayscale for face detection
        //        Mat gray = new Mat();
        //        Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

        //        // Detect faces
        //        var faces = faceCascade.DetectMultiScale(gray, 1.1, 5, HaarDetectionTypes.ScaleImage);
        //        if (faces.Length == 0)
        //        {
        //            throw new Exception("No face detected.");
        //        }

        //        // Get the largest face (if multiple faces are detected)
        //        OpenCvSharp.Rect largestFace = faces[0];
        //        foreach (var face in faces)
        //        {
        //            if (face.Width * face.Height > largestFace.Width * largestFace.Height)
        //            {
        //                largestFace = face;
        //            }
        //        }

        //        // Calculate the center of the detected face
        //        int faceCenterX = largestFace.X + largestFace.Width / 2;
        //        int faceCenterY = largestFace.Y + largestFace.Height / 2;

        //        // Calculate the center of the target canvas (2100x1800)
        //        int targetCenterX = targetWidth / 2;
        //        int targetCenterY = targetHeight / 2;

        //        // Calculate the translation needed
        //        int translationX = targetCenterX - faceCenterX;
        //        int translationY = targetCenterY - faceCenterY;

        //        // Create the transformation matrix (translation)
        //        Mat translationMatrix = Mat.Eye(2, 3, MatType.CV_32F);
        //        translationMatrix.Set<float>(0, 2, translationX);
        //        translationMatrix.Set<float>(1, 2, translationY);

        //        // Apply the affine transformation (translation)
        //        Mat centeredImage = new Mat();
        //        Cv2.WarpAffine(image, centeredImage, translationMatrix, new OpenCvSharp.Size(targetWidth, targetHeight));

        //        // Convert the OpenCV Mat to a Bitmap
        //        //System.Drawing.Bitmap bitmapImage = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(centeredImage);

        //        // Set the DPI (300 DPI)
        //        //bitmapImage.SetResolution(targetDPI, targetDPI);


        //        //// Save the resulting image
        //        //string outputImagePath = System.IO.Path.Combine(outputFolderPath, outputName);
        //        //Cv2.ImWrite(outputImagePath, centeredImage);

        //        //// Return the path of the saved image
        //        //return outputImagePath;

        //        // Convert the OpenCV Mat back to a byte array
        //        byte[] outputBytes = new byte[centeredImage.Total() * centeredImage.ElemSize()];
        //        //System.Runtime.InteropServices.Marshal.Copy(centeredImage.DataPointer, outputBytes, 0, outputBytes.Length);

        //        // Convert the byte array to a MagickImage
        //        MagickImage outputMagickImage = new MagickImage(outputBytes);

        //        // Optionally set the DPI (since we need to support printing, we can specify the DPI here)
        //        outputMagickImage.Density = new Density(targetDPI, targetDPI);

        //        // Return the resulting MagickImage (no need to save to a file here)
        //        return outputMagickImage;

        //    }

        #endregion

    }


}

