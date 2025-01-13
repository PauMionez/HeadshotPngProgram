using DevExpress.Mvvm;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.OCR;
using HeadshotPngProgram.Service;
using ImageMagick;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media.Imaging;



namespace HeadshotPngProgram.ViewModel
{
    internal class MainViewModel : Abstract.ViewModelBase
    {
        //Command
        public AsyncCommand DetectBodyCommand { get; }

        public MainViewModel()
        {

            DetectBodyCommand = new AsyncCommand(DetectBodyAndCreateCutout);
            IsFaceDetectChecked = true;
            EnableCheckedBox = true;
        }

        #region Properties
        private int _progress;
        public int Progress
        {
            get { return _progress; }
            set { _progress = value; OnPropertyChanged(); }
        }

        private string _currentImageName;

        public string CurrentImageName
        {
            get { return _currentImageName; }
            set { _currentImageName = value; OnPropertyChanged(); }
        }

        private int _imageprogress;
        public int ImageProgress
        {
            get { return _imageprogress; }
            set { _imageprogress = value; OnPropertyChanged(); }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get { return _statusMessage; }
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private bool _isProcessing;
        public bool IsProcessing
        {
            get { return _isProcessing; }
            set { _isProcessing = value; OnPropertyChanged(); }
        }

        private BitmapImage _SelectedImageSource;

        public BitmapImage SelectedImageSource
        {
            get { return _SelectedImageSource; }
            set { _SelectedImageSource = value; OnPropertyChanged(); }
        }


        private bool _isFaceDetectChecked;

        public bool IsFaceDetectChecked
        {
            get { return _isFaceDetectChecked; }
            set { _isFaceDetectChecked = value; OnPropertyChanged(); }
        }

        private bool _enableCheckedBox;

        public bool EnableCheckedBox
        {
            get { return _enableCheckedBox; }
            set { _enableCheckedBox = value; OnPropertyChanged(); }
        }


        #endregion


        /// <summary>
        /// Detects a person in an image, resizes the image into different dimensions, and creates various image outputs.
        /// </summary>
        /// <returns></returns>
        private async Task DetectBodyAndCreateCutout()
        {
            try
            {
                EnableCheckedBox = false;
                IsProcessing = true;
                StatusMessage = "Processing...";
                Progress = 0;

                string folderpath = GetFolderPath("Select Folder with PNG images");
                if (string.IsNullOrEmpty(folderpath)) return;


                // Get all PNG files in the selected folder
                string[] imagePaths = Directory.GetFiles(folderpath, "*.png");
                if (imagePaths.Length == 0)
                {
                    StatusMessage = "No PNG images found in the selected folder.";
                    IsProcessing = false;
                    return;
                }

                //Initialize Progress reporting
                //Update StatusMessage
                Progress<string> statusProgress = new Progress<string>((status) =>
                {
                    StatusMessage = status;
                });
                Progress<string> currentimage = new Progress<string>((currenimageprocess) =>
                {
                    CurrentImageName = currenimageprocess;
                });

                //process images asynchronously
                int totalImages = imagePaths.Length;
                await ProcessImageLoadingAsync(imagePaths, statusProgress, currentimage);

                StatusMessage = $"Processing complete! ({totalImages} images processed)";
                Progress = 100;
                IsProcessing = false;
                EnableCheckedBox = true;
            }
            catch (Exception ex)
            {
                StatusMessage = "Processing Failed";
                ErrorMessage(ex);
            }

        }

        /// <summary>
        /// Loads the images from the provided paths and processes, reporting progress updates to UI.
        /// </summary>
        /// <param name="imagePaths">Array of image file paths to process.</param>
        /// <param name="statusProgress">Progress reporter for updating the UI with processing status.</param>
        /// <returns></returns>
        private async Task ProcessImageLoadingAsync(string[] imagePaths, IProgress<string> statusProgress, IProgress<string> imagename)
        {
            try
            {
                int totalImages = imagePaths.Length;

                for (int i = 0; i < totalImages; i++)
                {
                    string imagePath = imagePaths[i];
                    string imageName = Path.GetFileName(imagePath);

                    // Report the image name
                    imagename.Report(imageName);

                    //Process image (asynchronous)
                    await ProcessImagesAsync(imagePath);


                    statusProgress.Report($"({i + 1}/{totalImages}) Processing image...");
                    
                }

                statusProgress.Report($"Processing complete! ({totalImages} images processed)");
            }
            catch (Exception ex)
            {
                StatusMessage = "Processing Failed";
                ErrorMessage(ex);
            }
        }

        /// <summary>
        /// Processes an individual image by generating resized versions of the image with specified dimensions and DPI, 
        /// and saves them to the output folder.
        /// </summary>
        /// <param name="path">Path to the image to process.</param>
        /// <returns></returns>
        private async Task ProcessImagesAsync(string path)
        {
            try
            {
                //string path = GetImageFilePath();
                if (string.IsNullOrEmpty(path)) return;

                // Extract the base filename (without extension)
                string imageName = Path.GetFileNameWithoutExtension(path);

                // Define the output folder where images will be saved
                string directoryPath = Path.GetDirectoryName(path);
                string outputFolder = Path.Combine(directoryPath, "Output");

                // Create a subfolder inside Output folder with the image name
                string imageOutputFolder = Path.Combine(outputFolder, imageName);
                if (!Directory.Exists(imageOutputFolder))
                {
                    Directory.CreateDirectory(imageOutputFolder);
                }

                //Cutout
                await ProcessImageAsync(path, 2100, 1800, 300, $"{imageName}_cutout.jpg", imageOutputFolder, true, 1.1f);
                Progress = 25;
                //5x7
                await ProcessImageAsync(path, 1500, 2100, 300, $"{imageName}_5x7.jpg", imageOutputFolder, true, 1.1f);
                Progress = 50;
                //Icon
                await ProcessImageAsync(path, 120, 155, 72, $"{imageName}_icon.jpg", imageOutputFolder, true, 1.2f);
                Progress = 75;
                //Web
                await ProcessImageAsync(path, 300, 420, 72, $"{imageName}_web.jpg", imageOutputFolder, true, 1.1f);
                Progress = 100;

            }
            catch (Exception ex)
            {
                StatusMessage = "Processing Failed";
                ErrorMessage(ex);
            }
        }


        /// <summary>
        /// Processes the image to detect a person and face, cuts out the image accordingly, and saves the processed image.
        /// </summary>
        /// <param name="path">Path to the image to process.</param>
        /// <param name="dimensionWidth">Width for the resized image.</param>
        /// <param name="dimensionHeight">Height for the resized image.</param>
        /// <param name="targetDPI">Target DPI for the resized image.</param>
        /// <param name="outputName">Name of the output file.</param>
        /// <param name="imageOutputFolder">Folder to save the processed image.</param>
        /// <param name="isZoom">Indicates whether to zoom the image during processing.</param>
        private async Task ProcessImageAsync(string path, int dimensionWidth, int dimensionHeight, int targetDPI, string outputName, string imageOutputFolder, bool isZoom, float zoompercent)
        {
            await Task.Run(() =>
            {
                try
                {
                    using (MagickImage image = new MagickImage(path))
                    {
                        //Service
                        Magick cropimage = new Magick();
                        DetectPerson personDetector = new DetectPerson();
                        FaceDetect FaceDetector = new FaceDetect();

                        //resize to large
                        MagickImage resizedImage = cropimage.ResizeImage(image, 4000, 4000);

                        // Detect person or object in image using yolo
                        var detectedObjects = personDetector.DetectFaceAndUpperBody(resizedImage);

                        foreach (var rect in detectedObjects)
                        {
                            // Use the coordinates from the bounding box as inputs for the ProcessImage method
                            //int personX = rect.X;
                            //int personY = rect.Y - 40;
                            //int personWidth = rect.Width - 65; //rect.Width - 10;
                            //int personHeight = rect.Height + 500;

                            int personX = rect.X;
                            int personY = rect.Y - 40;
                            int personWidth = rect.Width;//rect.Width - 65; //rect.Width - 10;
                            int personHeight = rect.Height;

                            // Crop image process
                            MagickImage cutout = cropimage.ProcessImage(resizedImage, dimensionHeight, personX, personY, personWidth, personHeight);

                            // Alight person/object to center by its face position
                            MagickImage centerimagefinal = FaceDetector.CenterImage(cutout, dimensionWidth, dimensionHeight, targetDPI, isZoom, zoompercent, IsFaceDetectChecked);

                            if (centerimagefinal == null) 
                            { 
                                WarningMessage($"No face detected. Please crop this {outputName} image manually. Thank you!."); 
                                return;
                            }

                            // Convert the MagickImage to a byte array
                            byte[] imageBytes = centerimagefinal.ToByteArray(MagickFormat.Png);

                            using (var ms = new MemoryStream(imageBytes))
                            {
                                BitmapImage bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.StreamSource = ms;
                                bitmap.EndInit();
                                bitmap.Freeze(); // Make it cross-thread accessible
                                SelectedImageSource = bitmap;
                            }

                            // Save the processed image
                            if (!Directory.Exists(imageOutputFolder))
                               {Directory.CreateDirectory(imageOutputFolder); }

                            //string cutoutPath = System.IO.Path.Combine(outputFolder, "NewnewCutout.jpg");
                            string cutoutPath = Path.Combine(imageOutputFolder, outputName);
                            centerimagefinal.Write(cutoutPath);
                        }
                    }

                }
                catch (Exception ex)
                {
                    StatusMessage = "Processing Failed";
                    ErrorMessage(ex);
                }
            });
        }

        #region Trash Code

        //public MagickImage CenterImage(MagickImage magickImage, int dimensionWidth, int dimensionHeight, int targetDPI, bool isZoom)
        //{
        //    try
        //    {
        //        // Load the Haar Cascade face detector
        //        var faceCascade = new OpenCvSharp.CascadeClassifier(@"Asset\haarcascade_frontalface_default.xml");

        //        // Read the input image (using OpenCvSharp)
        //        byte[] imageBytes = magickImage.ToByteArray(MagickFormat.Png);
        //        OpenCvSharp.Mat image = OpenCvSharp.Cv2.ImDecode(imageBytes, OpenCvSharp.ImreadModes.Unchanged);


        //        //OpenCvSharp.Mat image = OpenCvSharp.Cv2.ImRead(image);
        //        if (image.Empty())
        //        {
        //            throw new Exception("Image could not be loaded.");
        //        }

        //        //Zoom the icon 
        //        if (isZoom)
        //        {

        //            float zoomFactor = 1.2f; // 40% zoom //1.10f; // 10% zoom
        //            int newWidth = (int)(image.Width * zoomFactor);
        //            int newHeight = (int)(image.Height * zoomFactor);

        //            // Update the image to the final resized version
        //            OpenCvSharp.Mat zoomedImage = new OpenCvSharp.Mat();
        //            for (int i = 0; i < 3; i++)  // Resize in 3 steps for smoother quality
        //            {
        //                OpenCvSharp.Cv2.Resize(image, zoomedImage, new OpenCvSharp.Size(newWidth, newHeight), 0, 0, OpenCvSharp.InterpolationFlags.Lanczos4);
        //            }

        //            image = zoomedImage;


        //        }


        //        // Convert to grayscale for face detection
        //        OpenCvSharp.Mat gray = new OpenCvSharp.Mat();
        //        OpenCvSharp.Cv2.CvtColor(image, gray, OpenCvSharp.ColorConversionCodes.BGR2GRAY);

        //        // Detect faces
        //        var faces = faceCascade.DetectMultiScale(gray, 1.1, 5, OpenCvSharp.HaarDetectionTypes.ScaleImage);
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

        //        // Calculate the center of the target canvas (dimensionWidth x dimensionHeight)
        //        int targetCenterX = dimensionWidth / 2;
        //        int targetCenterY = dimensionHeight / 2;

        //        // Calculate the translation needed
        //        // No translation needed for the Y-axis
        //        int translationX = targetCenterX - faceCenterX;
        //        int translationY = 0;

        //        // Create the transformation matrix (translation)
        //        OpenCvSharp.Mat translationMatrix = OpenCvSharp.Mat.Eye(2, 3, OpenCvSharp.MatType.CV_32F);
        //        translationMatrix.Set<float>(0, 2, translationX);
        //        translationMatrix.Set<float>(1, 2, translationY);

        //        // Apply the affine transformation (translation)
        //        OpenCvSharp.Mat centeredImage = new OpenCvSharp.Mat();
        //        OpenCvSharp.Cv2.WarpAffine(image, centeredImage, translationMatrix, new OpenCvSharp.Size(dimensionWidth, dimensionHeight));

        //        byte[] resultBytes;
        //        OpenCvSharp.Cv2.ImEncode(".png", centeredImage, out resultBytes); // Encode Mat to byte array (JPEG)
        //        using (var ms = new MemoryStream(resultBytes))
        //        {
        //            // Create MagickImage from byte array
        //            MagickImage resultMagickImage = new MagickImage(ms);
        //            resultMagickImage.Quality = 82;
        //            resultMagickImage.Density = new Density(300);

        //            int x = (int)((dimensionWidth - resultMagickImage.Width) / 2);
        //            int y = (int)((dimensionHeight - resultMagickImage.Height) / 2);

        //            MagickImage whiteBackground = new MagickImage(MagickColors.White, (uint)dimensionWidth, (uint)dimensionHeight);
        //            //whiteBackground.UnsharpMask(0, 2, 1, 0);
        //            whiteBackground.Composite(resultMagickImage, x, y, CompositeOperator.Over);
        //            whiteBackground.Quality = 82;
        //            whiteBackground.Density = new Density(targetDPI);
        //            whiteBackground.FilterType = FilterType.Lanczos;

        //            return whiteBackground; 
        //        }



        //    }
        //    catch (Exception ex)
        //    {
        //        StatusMessage = "Processing Failed";
        //        ErrorMessage(ex);
        //        return null;
        //    }
        //}




        //// Create a MagickImage with a white background
        //MagickImage whiteBackground = new MagickImage((IMagickColor<byte>)MagickColors.White, (uint)dimensionWidth, (uint)dimensionHeight);


        //// Create a new MagickDrawable for overlaying the centered image
        //using (var drawable = new MagickImage((IMagickImage<byte>)centeredImage))
        //{
        //    // Composite the centered image over the white background
        //    whiteBackground.Composite(drawable, Gravity.Center, CompositeOperator.Over);
        //}

        //// Set the DPI for the resulting image
        //whiteBackground.Density = new Density(targetDPI);

        //// Return the resulting image
        //return whiteBackground;


        //// Convert OpenCV Mat back to MagickImage
        //using (var ms = new MemoryStream(resultBytes))
        //{
        //    MagickImage resultMagickImage = new MagickImage(ms); // Create MagickImage from byte array
        //    resultMagickImage.Density = new Density(targetDPI); // Set DPI for the result image
        //    return resultMagickImage; // Return the resulting image
        //}


        //// Convert the OpenCV Mat to a Bitmap
        //System.Drawing.Bitmap bitmapImage = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(centeredImage);

        //// Set the DPI (300 DPI)
        //bitmapImage.SetResolution(targetDPI, targetDPI);  // Set both horizontal and vertical DPI

        //// Prepare the output path where the image will be saved
        //string outputPath = Path.Combine(imageOutputFolder, outputName);

        //// Save the image with 300 DPI
        //bitmapImage.Save(outputPath, System.Drawing.Imaging.ImageFormat.Jpeg);  // You can change the format if needed

        //// Optionally, dispose of the bitmap to free resources
        //bitmapImage.Dispose();

        //using (MagickImage image = new MagickImage(path))
        //{
        //    Magick process = new Magick();

        //    MagickImage resizedImage = process.ResizeImage(image, 4000, 4000);

        //    byte[] imageBytes = resizedImage.ToByteArray(MagickFormat.Jpeg);

        //    // Convert byte array to OpenCV Mat (this is where we use OpenCV)
        //    Mat resizedMat = new Mat();
        //    CvInvoke.Imdecode(imageBytes, ImreadModes.Color, resizedMat); // Convert byte array to Mat


        //    var detectedObjects = yoloDetector.DetectFaceAndUpperBody(resizedMat);
        //    foreach (var rect in detectedObjects)
        //    {
        //        // Use the coordinates from the bounding box as inputs for the ProcessImage method
        //        int personX = rect.X;
        //        int personY = rect.Y;
        //        int personWidth = rect.Width;
        //        int personHeight = rect.Height;

        //        // Define target dimensions for different purposes (e.g., print, web, etc.)
        //        int targetWidth = 2100;
        //        int targetHeight = 1800;
        //        int dpi = 300;

        //        //////5x7 inches pixels
        //        //int targetInchesWidth = 1500;
        //        //int targetInchesHeight = 2100;
        //        //int Inchesdpi = 300;

        //        // Process the image for each detected person
        //        //Magick process = new Magick();
        //        //MagickImage resizedImage = process.ResizeImage(image, 4000, 4000); // Resize limit for memory management
        //        MagickImage cutout = process.ProcessImage(resizedImage, targetWidth, targetHeight, personX, personY, personWidth, personHeight, dpi);
        //        //MagickImage InchesPixel = process.ProcessImage(resizedImage, targetInchesWidth, targetInchesHeight, personX, personY, personWidth, personHeight, Inchesdpi);

        //        // Save the processed image
        //        string directoryPath = System.IO.Path.GetDirectoryName(path);  // Get the directory of the selected image
        //        string outputFolder = System.IO.Path.Combine(directoryPath, "Output");
        //        if (!Directory.Exists(outputFolder))
        //        {
        //            Directory.CreateDirectory(outputFolder);
        //        }

        //        string cutoutPath = System.IO.Path.Combine(outputFolder, "NewnewCutout.jpg");
        //        cutout.Write(cutoutPath);

        //        //string InchesPath = System.IO.Path.Combine(outputFolder, "Newnew5x7inches.jpg");
        //        //InchesPixel.Write(InchesPath);
        //    }
        //    InformationMessage("successful", "");
        //}

        ////2100x1800 pixel
        //int targetWidth = 2100;
        //int targetHeight = 1800;
        //int personX = 1000;
        //int personY = 500;
        //int personWidth = 2040;
        //int personHeight = 1850;
        //int dpi = 300;

        //5x7 inches pixels
        //int targetInchesWidth = 1500;
        //int targetInchesHeight = 2100;
        //int personInchesX = 1400;
        //int personInchesY = 590;
        //int personInchesWidth = 1340;
        //int personInchesHeight = 5000;//5500;

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


        //// Process the image
        //Magick process = new Magick();
        //MagickImage resizedImage = process.ResizeImage(image, 4000, 4000);

        //MagickImage Cutout = process.ProcessImage(resizedImage, targetWidth, targetHeight, personX, personY, personWidth, personHeight, dpi);
        //MagickImage InchesPixel = process.ProcessImage(resizedImage, targetInchesWidth, targetInchesHeight, personInchesX, personInchesY, personInchesWidth, personInchesHeight, dpi);
        //MagickImage Icon = process.ProcessImage(resizedImage, targetIconWidth, targetIconHeight, personIconX, personIconY, personIcoWidth, personIconHeight, Icondpi);
        //MagickImage Web = process.ProcessImage(resizedImage, targetWebWidth, targetWebHeight, personWebX, personWebY, personWebWidth, personWebHeight, Webdpi);


        //string directoryPath = System.IO.Path.GetDirectoryName(path);  // Get the directory of the selected image
        //string outputFolder = System.IO.Path.Combine(directoryPath, "Output");
        //if (!Directory.Exists(outputFolder))
        //{
        //    Directory.CreateDirectory(outputFolder);
        //}


        //string CutoutPath = System.IO.Path.Combine(outputFolder, "Cutput.jpg");
        //Cutout.Write(CutoutPath);

        //string InchesPath = System.IO.Path.Combine(outputFolder, "5x7Inches.jpg");
        //InchesPixel.Write(InchesPath);

        //string IconPath = System.IO.Path.Combine(outputFolder, "Icon.jpg");
        //Icon.Write(IconPath);

        //string WebPath = System.IO.Path.Combine(outputFolder, "Web.jpg");
        //Web.Write(WebPath);

        //    InformationMessage("successful", "");
        //}

        // Load the image from file
        //using (var image = new MagickImage(path))
        //{
        //    image.BackgroundColor = MagickColors.White;
        //    // If the image has transparency (alpha channel), we can remove the alpha channel
        //    if (image.HasAlpha)
        //    {
        //        // Fill the transparent areas with the background color (white)
        //        image.Alpha(AlphaOption.Remove); // Remove transparency
        //    }
        //    // Target dimensions
        //    int targetWidth = 2100;
        //    int targetHeight = 1800;

        //    // Aspect ratio of target size
        //    float aspectRatio = (float)targetWidth / targetHeight;

        //    // Calculate the new size
        //    int newWidth = (int)image.Width;
        //    int newHeight = (int)image.Height;

        //    // Check if we need to crop based on the aspect ratio
        //    if ((float)newWidth / newHeight > aspectRatio)
        //    {
        //        // Image is too wide, so crop the width
        //        newWidth = (int)(newHeight * aspectRatio);
        //    }
        //    else
        //    {
        //        // Image is too tall, so crop the height
        //        newHeight = (int)(newWidth / aspectRatio);
        //    }

        //    // Create a new Bitmap to hold the resized image
        //    //var resizedImage = new Bitmap(image, new System.Drawing.Size(newWidth, newHeight));
        //    image.Resize((uint)newWidth, (uint)newHeight);

        //    // Optionally, crop it further if necessary
        //    var cropX = (newWidth - targetWidth) / 2;
        //    var cropY = (newHeight - targetHeight) / 2;

        //    // Ensure the crop rectangle stays within bounds
        //    cropX = Math.Max(0, cropX);
        //    cropY = Math.Max(0, cropY);

        //    // Apply the crop rectangle to get the final 2100x1800 image
        //    var cropGeometry = new MagickGeometry(cropX, cropY, (uint)targetWidth, (uint)targetHeight);
        //    image.Crop(cropGeometry);

        //    //var cropRect = new System.Drawing.Rectangle(cropX, cropY, targetWidth, targetHeight);
        //    //image.Crop(new MagickGeometry(cropX, cropY, (uint)targetWidth, (uint)targetHeight));

        //    string directoryPath = System.IO.Path.GetDirectoryName(path);  // Get the directory of the selected image
        //    string outputFolder = System.IO.Path.Combine(directoryPath, "Output");

        //    if (!Directory.Exists(outputFolder))
        //    {
        //        Directory.CreateDirectory(outputFolder);
        //    }
        //    string canvasOutputPath = System.IO.Path.Combine(outputFolder, "Cutput_canvas.jpg");

        //    image.Write(canvasOutputPath);

        //}



        //string path = GetImageFilePath();
        ////Mat image = CvInvoke.Imread(path, ImreadModes.AnyColor);

        //byte[] imageData;
        //int bufferSize = 4096;

        //using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite,
        //    FileShare.ReadWrite, bufferSize * 3, useAsync: true))
        //{
        //    imageData = new byte[fs.Length];
        //    fs.ReadAsync(imageData, 0, (int)fs.Length);
        //}

        //// Set as bitmap
        //using (MemoryStream ms = new MemoryStream(imageData))
        //{
        //    BitmapImage bitmap = new BitmapImage();
        //    bitmap.BeginInit();
        //    bitmap.CacheOption = BitmapCacheOption.OnLoad; // Load image into memory to improve performance
        //    bitmap.StreamSource = ms;
        //    bitmap.EndInit();
        //    bitmap.Freeze(); // Freeze to make it cross-thread accessible
        //    ShowOriginal = bitmap;
        //}

        //// Convert the image to JPEG format
        //byte[] jpegBytes = CvInvoke.Imencode(".jpg", imageData);


        //Mat jpegImage = new Mat();
        //CvInvoke.Imdecode(jpegBytes, ImreadModes.Color, jpegImage);


        ////Mat image = new Mat();
        ////CvInvoke.Imdecode(imageData, ImreadModes.AnyColor, image);

        ////Mat image = yoloDetector.EnsureWhiteBackground(path);

        ////Bitmap Original = new Bitmap(image.ToBitmap());
        ////ShowOriginal = yoloDetector.ConvertMatToBitmapImage(image);

        //var detectedObjects = yoloDetector.DetectFaceAndUpperBody(jpegImage);

        //// Create output folder inside the selected image folder
        //string directoryPath = Path.GetDirectoryName(path);  // Get the directory of the selected image
        //string outputFolder = Path.Combine(directoryPath, "Output");

        //if (!Directory.Exists(outputFolder))
        //{
        //    Directory.CreateDirectory(outputFolder);
        //}

        //// Assuming that the detectedObjects contains the bounding boxes (rectangles) of the detected persons
        //if (detectedObjects.Count == 0)
        //{
        //    WarningMessage("No objects detected.");
        //    return;
        //}

        //// Call the method to cut detected objects and place them on a canvas
        //// Pass the detected objects and the image path to the method
        //Mat canvas = yoloDetector.CutObjectAndPlaceOnCanvas(jpegImage, detectedObjects);

        //string canvasOutputPath = Path.Combine(outputFolder, "Cutput_canvas.jpg");

        ////Mat canvas = CvInvoke.Imread(canvasOutputPath);

        //// Convert to Bitmap for DPI handling
        //Bitmap bitmapCanvas = new Bitmap(canvas.ToBitmap());

        //// Set DPI to 300x300
        //bitmapCanvas.SetResolution(300, 300);

        //// Save the image with DPI set to 300
        //bitmapCanvas.Save(canvasOutputPath, System.Drawing.Imaging.ImageFormat.Png);

        //// Convert the image with bounding boxes to BitmapImage for WPF Image control
        //BitmapImage processedImage = yoloDetector.ConvertMatToBitmapImage(canvas);

        //// Display the processed image in the Image control
        //CutoutImage = processedImage;


        //private string GetImageFilePath()
        //{
        //    OpenFileDialog openFileDialog = new OpenFileDialog
        //    {
        //        Filter = "Image Files|*.jpg;*.jpeg;*.png;*.tif;*.tiff",
        //        Title = "Select Input Image"
        //    };

        //    return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : string.Empty;
        //}

        //private BitmapImage ConvertToBitmapImage(Bitmap bitmap)
        //{
        //    using (var memoryStream = new MemoryStream())
        //    {
        //        bitmap.Save(memoryStream, ImageFormat.Png);
        //        memoryStream.Seek(0, SeekOrigin.Begin);
        //        var bitmapImage = new BitmapImage();
        //        bitmapImage.BeginInit();
        //        bitmapImage.StreamSource = memoryStream;
        //        bitmapImage.EndInit();
        //        return bitmapImage;
        //    }
        //}




        //private void DetectBodyAndCreateInches()
        //{
        //    string path = GetImageFilePath();

        //    using (MagickImage image = new MagickImage(path))
        //    {
        //        Magick process = new Magick();

        //        MagickImage resizedImage = process.ResizeImage(image, 4000, 4000);

        //        byte[] imageBytes = resizedImage.ToByteArray(MagickFormat.Jpeg);

        //        // Convert byte array to OpenCV Mat (this is where we use OpenCV)
        //        Mat resizedMat = new Mat();
        //        CvInvoke.Imdecode(imageBytes, ImreadModes.Color, resizedMat); // Convert byte array to Mat


        //        var detectedObjects = yoloDetector.DetectFaceAndUpperBody(resizedMat);
        //        foreach (var rect in detectedObjects)
        //        {
        //            // Use the coordinates from the bounding box as inputs for the ProcessImage method
        //            int personX = rect.X;
        //            int personY = rect.Y;
        //            int personWidth = rect.Width;
        //            int personHeight = rect.Height;

        //            //// Define target dimensions for different purposes (e.g., print, web, etc.)
        //            //int targetWidth = 2100;
        //            //int targetHeight = 1800;
        //            //int dpi = 300;

        //            ////5x7 inches pixels
        //            int targetInchesWidth = 1500;
        //            int targetInchesHeight = 2100;
        //            int Inchesdpi = 300;

        //            // Process the image for each detected person
        //            //Magick process = new Magick();
        //            //MagickImage resizedImage = process.ResizeImage(image, 4000, 4000); // Resize limit for memory management
        //            //MagickImage cutout = process.ProcessImage(resizedImage, targetWidth, targetHeight, personX, personY, personWidth, personHeight, dpi);
        //            MagickImage InchesPixel = process.ProcessImage(resizedImage, targetInchesWidth, targetInchesHeight, personX, personY, personWidth, personHeight, Inchesdpi);

        //            // Save the processed image
        //            string directoryPath = System.IO.Path.GetDirectoryName(path);  // Get the directory of the selected image
        //            string outputFolder = System.IO.Path.Combine(directoryPath, "Output");
        //            if (!Directory.Exists(outputFolder))
        //            {
        //                Directory.CreateDirectory(outputFolder);
        //            }

        //            //string cutoutPath = System.IO.Path.Combine(outputFolder, "NewnewCutout.jpg");
        //            //cutout.Write(cutoutPath);

        //            string InchesPath = System.IO.Path.Combine(outputFolder, "Newnew5x7inches.jpg");
        //            InchesPixel.Write(InchesPath);
        //        }
        //        InformationMessage("successful", "");
        //    }
        //}







        //public Mat CutObjectAndPlaceOnCanvas(Mat image, List<Rectangle> detectedObjects)
        //{
        //    // Load the original image
        //    //Mat image = CvInvoke.Imread(imagePath);

        //    //if (image.IsEmpty)
        //    //{
        //    //    Console.WriteLine("Error: Unable to load image.");
        //    //    return;
        //    //}

        //    // Create a 2100x1800 canvas (white background)
        //    Mat canvas = new Mat(1800, 2100, DepthType.Cv8U, 3); // 3 channels for color image
        //    canvas.SetTo(new MCvScalar(255, 255, 255)); // Set the canvas background to white

        //    // Define the target height to fit the canvas
        //    int targetHeight = canvas.Height; // Canvas height
        //    int targetWidth = canvas.Width; // Canvas width


        //    // Iterate over the detected objects
        //    foreach (var rect in detectedObjects)
        //    {
        //        // Crop the detected object (cut the detected region from the original image)
        //        Mat detectedObject = new Mat(image, rect);

        //        // Calculate aspect ratio of the detected object
        //        double aspectRatio = (double)detectedObject.Width / detectedObject.Height;

        //        // Resize the object to fit the height of the canvas while maintaining the aspect ratio
        //        int resizedHeight = targetHeight; // Fit to canvas height
        //        int resizedWidth = (int)(resizedHeight * aspectRatio); // Adjust width based on aspect ratio

        //        // If the resized width exceeds the canvas width, adjust the height accordingly
        //        if (resizedWidth > targetWidth)
        //        {
        //            resizedWidth = targetWidth; // Fit to canvas width
        //            resizedHeight = (int)(resizedWidth / aspectRatio); // Adjust height based on aspect ratio
        //        }

        //        // You can resize the detected object to fit the canvas if needed (optional)
        //        Mat resizedObject = new Mat();
        //        CvInvoke.Resize(detectedObject, resizedObject, new System.Drawing.Size(resizedWidth, resizedHeight));

        //        // Define the position where the cropped object will be placed on the canvas
        //        int x = (canvas.Width - resizedObject.Width) / 2;  // Center it horizontally
        //        int y = (canvas.Height - resizedObject.Height) / 2; // Center it vertically

        //        // Create a region of interest (ROI) on the canvas where the object will be placed
        //        System.Drawing.Rectangle roi = new System.Drawing.Rectangle(x, y, resizedObject.Width, resizedObject.Height);

        //        var canvasROI = new Mat(canvas, roi);
        //        //Mat canvasROI = canvas[roi];

        //        // Place the cropped object on the canvas at the specified position
        //        resizedObject.CopyTo(canvasROI);
        //    }

        //    //// Convert the canvas to Bitmap for setting the DPI
        //    //Bitmap bitmapCanvas = canvas.ToImage<Bgr, byte>().ToBitmap();

        //    //// Set the DPI of the Bitmap to 300 DPI
        //    //bitmapCanvas.SetResolution(300, 300); // Set DPI to 300

        //    // Save the result to a file
        //    string outputPath = Path.Combine(Environment.CurrentDirectory, "Output", "output_canvas.jpg");
        //    CvInvoke.Imwrite(outputPath, canvas);

        //    // Convert the canvas to BitmapImage for display
        //    BitmapImage processedImage = yoloDetector.ConvertMatToBitmapImage(canvas);

        //    // Display the processed image in Image control (if using WPF)
        //    CutoutImage = processedImage;
        //}



        // Command logic to detect body and create a cutout
        //private void DetectBodyAndCreateCutout()
        //{
        //    //if (string.IsNullOrEmpty(ImagePath))
        //    //    return;

        //    string path = GetImageFilePath();

        //    //string cascadeFilePath = @"Asset\haarcascade_upperbody.xml"; // Path to Haar Cascade file
        //    string facecascadePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Asset", "haarcascade_frontalcatface_extended.xml"); 
        //    string UppercascadePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Asset", "haarcascade_upperbody.xml"); 

        //    var cutout = _bodyDetectionModel.DetectBodyAndCreateCutout(path, facecascadePath, UppercascadePath);

        //    if (cutout != null)
        //    {
        //        // Convert the cutout Image<Bgr, byte> to BitmapImage for displaying
        //        CutoutImage = ConvertToBitmapImage(cutout);
        //    }
        //}

        //// Utility method to convert Emgu.CV Image<Bgr, byte> to BitmapImage
        //private BitmapImage ConvertToBitmapImage(Emgu.CV.Image<Bgr, byte> img)
        //{
        //    using (var ms = new MemoryStream())
        //    {
        //        img.ToBitmap().Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        //        ms.Seek(0, SeekOrigin.Begin);
        //        var bitmapImage = new BitmapImage();
        //        bitmapImage.BeginInit();
        //        bitmapImage.StreamSource = ms;
        //        bitmapImage.EndInit();
        //        return bitmapImage;
        //    }
        //}














        //private async Task InputImageAsync()
        //{
        //    string selectedPath = GetImageFilePath();
        //    if (string.IsNullOrWhiteSpace(selectedPath)) { return; }
        //    //CropExtensionCollection = new List<string> { "jpg", "jpeg", "png", "tif", "tiff" };
        //    //SelectedCropExtension = CropExtensionCollection.First();

        //    byte[] imageData;
        //    int bufferSize = 4096;


        //    // Detect the person and get the bounding box
        //    _detectedRectangle = GetTopLeftAndBottomRightPoints(selectedPath);

        //    // Load the image
        //    _originalImage = new Mat(selectedPath);

        //    // Convert the image to BitmapImage for displaying in WPF
        //    DisplayImage = ConvertMatToBitmapImage(_originalImage);

        //    try
        //    {
        //        // Read the image into a byte array asynchronously
        //        await Task.Run(async () =>
        //        {
        //            try
        //            {
        //                using (FileStream fs = new FileStream(selectedPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize * 3, useAsync: true))
        //                {
        //                    imageData = new byte[fs.Length];
        //                    await fs.ReadAsync(imageData, 0, (int)fs.Length);
        //                }

        //                // Create the cutout image (crop it)
        //                var croppedImage = CreateCutoutImage(imageData, 2100, 1800, 300);

        //                // Save the cropped image to the Output folder
        //                string outputFolder = Path.Combine(Environment.CurrentDirectory, "Output");
        //                if (!Directory.Exists(outputFolder))
        //                {
        //                    Directory.CreateDirectory(outputFolder);
        //                }

        //                string outputFilePath = Path.Combine(outputFolder, "Cutout_image.jpg");
        //                croppedImage.Save(outputFilePath, System.Drawing.Imaging.ImageFormat.Jpeg);

        //                // Show success message
        //                InformationMessage($"Cutout image saved to {outputFilePath}", "Success");


        //                // Create the icon image (crop it to 5x7 inches at 300 DPI, i.e., 1500x2100 pixels)
        //                var iconImage = Create5x7Image(imageData, 1500, 2100, 300);

        //                // Save the icon image to the Output folder
        //                string iconFilePath = Path.Combine(outputFolder, "icon_image.jpg");
        //                iconImage.Save(iconFilePath, System.Drawing.Imaging.ImageFormat.Jpeg);

        //                // Show success message for icon image
        //                InformationMessage($"Icon image saved to {iconFilePath}", "Success");
        //            }
        //            catch (IOException ioex)
        //            {
        //                //MessageBox.Show($"An error occurred while reading the file: {ex.Message}", "File Access Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //                Console.WriteLine(ioex.Message);
        //                Console.WriteLine(ioex.StackTrace);
        //            }
        //            catch (Exception ex)
        //            {
        //                //MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //                Console.WriteLine(ex.Message);
        //                Console.WriteLine(ex.StackTrace);
        //            }
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"An error occurred while loading the image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        //private void ResizeObject()
        //{
        //    if (_detectedRectangle != System.Drawing.Rectangle.Empty)
        //    {
        //        // Crop the detected object
        //        Mat detectedObject = new Mat(_originalImage, _detectedRectangle);

        //        // Resize the object to fit into a 2100x1800 canvas
        //        Mat resizedObject = new Mat();
        //        CvInvoke.Resize(detectedObject, resizedObject, new System.Drawing.Size(2100, 1800));

        //        // Update the display image with the resized object
        //        DisplayImage = ConvertMatToBitmapImage(resizedObject);
        //    }
        //    else
        //    {
        //        MessageBox.Show("No object detected to resize.");
        //    }
        //}

        //// Method to detect the person and return the bounding box
        //private System.Drawing.Rectangle GetTopLeftAndBottomRightPoints(string fullPath)
        //{
        //    System.Drawing.Rectangle result = new System.Drawing.Rectangle();

        //    try
        //    {
        //        // Load the image
        //        using (Mat origImg = CvInvoke.Imread(fullPath, ImreadModes.Color))
        //        {
        //            // Convert to grayscale
        //            Mat gray = new Mat();
        //            CvInvoke.CvtColor(origImg, gray, ColorConversion.Bgr2Gray);

        //            // Apply Gaussian blur
        //            Mat blur = new Mat();
        //            CvInvoke.GaussianBlur(gray, blur, new System.Drawing.Size(13, 13), 50);


        //            // Threshold and apply Canny edge detection
        //            Mat threshold = new Mat();
        //            CvInvoke.Threshold(blur, threshold, 128, 255, ThresholdType.Binary);

        //            Mat canny = new Mat();
        //            CvInvoke.Canny(threshold, canny, 50, 250);

        //            // Find contours
        //            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
        //            Mat hierarchy = new Mat();
        //            CvInvoke.FindContours(canny, contours, hierarchy, RetrType.List, ChainApproxMethod.ChainApproxSimple);

        //            // Find the largest contour (assumed to be the person)
        //            double longestLength = 0;
        //            VectorOfPoint edge = new VectorOfPoint();
        //            for (int i = 0; i < contours.Size; i++)
        //            {
        //                VectorOfPoint contour = contours[i];
        //                if (contour.Size > longestLength)
        //                {
        //                    longestLength = contour.Size;
        //                    edge = contour;
        //                }
        //            }

        //            // Get the bounding box around the largest contour (person)
        //            if (edge.Size > 0)
        //            {
        //                System.Drawing.Rectangle boundingBox = CvInvoke.BoundingRectangle(edge);
        //                result = boundingBox;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error detecting object: " + ex.Message);
        //    }

        //    return result;
        //}

        //// Helper method to convert a Mat to BitmapImage for displaying in WPF
        //private BitmapImage ConvertMatToBitmapImage(Mat mat)
        //{
        //    using (var memoryStream = new MemoryStream())
        //    {
        //        System.Drawing.Bitmap bitmap = mat.ToBitmap();
        //        bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
        //        memoryStream.Seek(0, SeekOrigin.Begin);

        //        BitmapImage bitmapImage = new BitmapImage();
        //        bitmapImage.BeginInit();
        //        bitmapImage.StreamSource = memoryStream;
        //        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        //        bitmapImage.EndInit();

        //        return bitmapImage;
        //    }
        //}

        // Function to create the cutout image (crop)
        //public Bitmap Create5x7Image(byte[] imageData, int width, int height, float dpi)
        //{
        //    using (MemoryStream ms = new MemoryStream(imageData))
        //    {
        //        //Create a BitmapImage from the byte array
        //        BitmapImage bitmapImage = new BitmapImage();
        //        bitmapImage.BeginInit();
        //        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        //        bitmapImage.StreamSource = ms;
        //        bitmapImage.EndInit();
        //        bitmapImage.Freeze(); // Freezes the bitmap for cross-thread usage

        //        // Convert the BitmapImage to a Bitmap
        //        Bitmap bitmap = new Bitmap(bitmapImage.StreamSource);

        //        int personX = 600; //495 //500 // X coordinate of the top-left corner of the bounding box
        //        int personY = 280; //280 //300 // Y coordinate of the top-left corner
        //        int personWidth = 550; //1120 // Width of the bounding box
        //        int personHeight = 800; //800 Height of the bounding box

        //        //int personX = (bitmap.Width - personWidth) / 2;
        //        //int personY = (bitmap.Height - personHeight) / 2;

        //        Rectangle personCropArea = new Rectangle(personX, personY, personWidth, personHeight);
        //        Bitmap croppedPersonImage = bitmap.Clone(personCropArea, bitmap.PixelFormat);

        //        float scaleX = (float)width / croppedPersonImage.Width;
        //        float scaleY = (float)height / croppedPersonImage.Height;
        //        float scale = Math.Min(scaleX, scaleY); // Use the smaller of the two scale factors

        //        int newWidth = (int)(croppedPersonImage.Width * scale);
        //        int newHeight = (int)(croppedPersonImage.Height * scale);

        //        // Create a new blank image with a white background
        //        Bitmap finalImage = new Bitmap(width, height);
        //        using (Graphics g = Graphics.FromImage(finalImage))
        //        {
        //            // Fill the background with white color
        //            g.Clear(Color.White);

        //            // Calculate the position to center the resized image
        //            int x = (width - newWidth) / 2;
        //            int y = (height - newHeight) / 2;

        //            // Draw the resized image onto the white background
        //            g.DrawImage(croppedPersonImage, x, y, newWidth, newHeight);
        //        }



        //        // Set the DPI (300 DPI for printing)
        //        finalImage.SetResolution(dpi, dpi);

        //        croppedPersonImage.Dispose();
        //        return finalImage;
        //    }

        //    }



        //public Bitmap CreateCutoutImage(byte[] imageData, int width, int height, float dpi)
        //{
        //    using (MemoryStream ms = new MemoryStream(imageData))
        //    {
        //        //Create a BitmapImage from the byte array
        //        BitmapImage bitmapImage = new BitmapImage();
        //        bitmapImage.BeginInit();
        //        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        //        bitmapImage.StreamSource = ms;
        //        bitmapImage.EndInit();
        //        bitmapImage.Freeze(); // Freezes the bitmap for cross-thread usage

        //        // Convert the BitmapImage to a Bitmap
        //        Bitmap bitmap = new Bitmap(bitmapImage.StreamSource);

        //        // Resize the image if it's too large to fit into memory
        //        Bitmap resizedBitmap = ResizeImage(bitmap, 4000, 4000);

        //        int personX = 500; //495 //500 // X coordinate of the top-left corner of the bounding box
        //        int personY = 250; //280 //300 // Y coordinate of the top-left corner
        //        int personWidth = 1520; //1120 // Width of the bounding box
        //        int personHeight = 830; //800 Height of the bounding box

        //        //int personX = (bitmap.Width - personWidth) / 2;
        //        //int personY = (bitmap.Height - personHeight) / 2;

        //        Rectangle personCropArea = new Rectangle(personX, personY, personWidth, personHeight);
        //        Bitmap croppedPersonImage = resizedBitmap.Clone(personCropArea, bitmap.PixelFormat);

        //        float scaleX = (float)width / croppedPersonImage.Width;
        //        float scaleY = (float)height / croppedPersonImage.Height;
        //        float scale = Math.Min(scaleX, scaleY); // Use the smaller of the two scale factors

        //        int newWidth = (int)(croppedPersonImage.Width * scale);
        //        int newHeight = (int)(croppedPersonImage.Height * scale);

        //        // Create a new blank image with a white background
        //        Bitmap finalImage = new Bitmap(width, height);
        //        using (Graphics g = Graphics.FromImage(finalImage))
        //        {
        //            // Fill the background with white color
        //            g.Clear(Color.White);

        //            // Calculate the position to center the resized image
        //            int x = (width - newWidth) / 2;
        //            int y = (height - newHeight) / 2;

        //            // Draw the resized image onto the white background
        //            g.DrawImage(croppedPersonImage, x, y, newWidth, newHeight);
        //        }



        //        // Set the DPI (300 DPI for printing)
        //        finalImage.SetResolution(dpi, dpi);

        //        return finalImage;
        //    }
        //}


        //public Bitmap ResizeImage(Bitmap originalImage, int maxWidth, int maxHeight)
        //{
        //    int width = originalImage.Width;
        //    int height = originalImage.Height;

        //    // Calculate the scaling factor
        //    float ratioX = (float)maxWidth / width;
        //    float ratioY = (float)maxHeight / height;
        //    float ratio = Math.Min(ratioX, ratioY);

        //    // Calculate new dimensions
        //    int newWidth = (int)(width * ratio);
        //    int newHeight = (int)(height * ratio);

        //    // Create a new image with the scaled dimensions
        //    Bitmap resizedImage = new Bitmap(originalImage, newWidth, newHeight);
        //    return resizedImage;
        //}



        //// Function to create the cutout image (crop)
        //public Bitmap CreateCutoutImage(byte[] imageData, int width, int height, float dpi)
        //{
        //    using (MemoryStream ms = new MemoryStream(imageData))
        //    {
        //        // Create a BitmapImage from the byte array
        //        BitmapImage bitmapImage = new BitmapImage();
        //        bitmapImage.BeginInit();
        //        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        //        bitmapImage.StreamSource = ms;
        //        bitmapImage.EndInit();
        //        bitmapImage.Freeze(); // Freezes the bitmap for cross-thread usage

        //        // Convert the BitmapImage to a Bitmap
        //        Bitmap bitmap = new Bitmap(bitmapImage.StreamSource);

        //        // Check if the image is large enough for the requested cutout
        //        if (bitmap.Width < width || bitmap.Height < height)
        //        {
        //            throw new ArgumentException("Source image is too small for the requested cutout dimensions.");
        //        }

        //        // Define the crop area (starting from the top-left corner)
        //        Rectangle cropArea = new Rectangle(0, 0, width, height);

        //        // Crop the image
        //        Bitmap croppedImage = bitmap.Clone(cropArea, bitmap.PixelFormat);

        //        // Set the DPI (300 DPI for printing)
        //        croppedImage.SetResolution(dpi, dpi);

        //        return croppedImage;
        //    }
        //}

        //// Method to detect body and create a cutout image
        //public Image<Bgr, byte> DetectBodyAndCreateCutout(string imagePath, string cascadeFilePath)
        //{
        //    // Load the image
        //    var img = new Image<Bgr, byte>(imagePath);

        //    // Load Haar Cascade for full-body detection
        //    var bodyCascade = new CascadeClassifier(cascadeFilePath);

        //    // Convert image to grayscale for better performance in detecting bodies
        //    var grayImg = img.Convert<Gray, byte>();

        //    // Detect bodies in the image (returns list of rectangles where bodies are found)
        //    var bodies = bodyCascade.DetectMultiScale(
        //        grayImg,
        //        scaleFactor: 1.1,
        //        minNeighbors: 3,
        //        minSize: new Size(30, 30),
        //        flags: HaarDetectionType.DoCannyPruning
        //    );

        //    // If no bodies are detected, return null
        //    if (bodies.Length == 0)
        //        return null;

        //    // Create a cutout (crop the body area from the original image)
        //    var body = bodies[0];  // Take the first detected body (you could loop through others)
        //    return img.Copy(body);  // Create and return the cutout
        //}

        //private async Task ProcessCutoutAsync()
        //{

        //}


        #endregion


    }

}

