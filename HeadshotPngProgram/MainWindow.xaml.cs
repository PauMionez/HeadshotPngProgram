using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HeadshotPngProgram
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {

        //private Mat _originalImage;
        //private System.Drawing.Rectangle _detectedObjectRect;
        public MainWindow()
        {
            InitializeComponent();
        }


        //private void LoadImageButton_Click(object sender, RoutedEventArgs e)
        //{
        //    // Open file dialog to choose image
        //    var openFileDialog = new Microsoft.Win32.OpenFileDialog();
        //    openFileDialog.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp";
        //    if (openFileDialog.ShowDialog() == true)
        //    {
        //        string imagePath = openFileDialog.FileName;

        //        // Load the image using Emgu CV
        //        Mat _originalImage = new Mat(imagePath);

        //        // Convert to grayscale and apply thresholding
        //        Mat grayImage = new Mat();
        //        CvInvoke.CvtColor(_originalImage, grayImage, ColorConversion.Bgr2Gray);
        //        Mat binaryImage = new Mat();
        //        CvInvoke.Threshold(grayImage, binaryImage, 100, 255, ThresholdType.Binary);

        //        // Find contours (objects)
        //        using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
        //        {
        //            Mat hierarchy = new Mat();
        //            CvInvoke.FindContours(binaryImage, contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple);

        //            // If we find contours, assume the first object is the one we want
        //            if (contours.Size > 0)
        //            {
        //                _detectedObjectRect = CvInvoke.BoundingRectangle(contours[0]);
        //                // Draw the bounding box around the detected object
        //                CvInvoke.Rectangle(_originalImage, _detectedObjectRect, new MCvScalar(0, 255, 0), 2);
        //            }
        //        }

        //        // Display the processed image in the WPF Image control
        //        ImageDisplay.Source = ConvertMatToBitmapImage(_originalImage);
        //    }
        //}

        //// Resize the detected object based on the new dimensions
        //private void ResizeObjectButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (_detectedObjectRect != null)
        //    {
        //        // Extract the detected object from the image
        //        Mat detectedObject = new Mat(_originalImage, _detectedObjectRect);

        //        // Resize the object to a specific pixel size (e.g., 2100x1800 pixels)
        //        Mat resizedObject = new Mat();
        //        CvInvoke.Resize(detectedObject, resizedObject, new System.Drawing.Size(2100, 1800));

        //        // Alternatively, resize based on inches (e.g., 5x7 inches at 300 DPI)
        //        // 5 inches * 300 DPI = 1500 pixels
        //        // 7 inches * 300 DPI = 2100 pixels
        //        Mat resizedObjectInches = new Mat();
        //        CvInvoke.Resize(detectedObject, resizedObjectInches, new System.Drawing.Size(1500, 2100));

        //        // Display resized object in the WPF Image control (can display resizedObject or resizedObjectInches)
        //        ImageDisplay.Source = ConvertMatToBitmapImage(resizedObjectInches);
        //    }
        //}

        //// Convert Mat (Emgu.CV) to BitmapImage (WPF)
        //private BitmapImage ConvertMatToBitmapImage(Mat mat)
        //{
        //    Bitmap bitmap = mat.ToBitmap();
        //    using (var memoryStream = new System.IO.MemoryStream())
        //    {
        //        bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
        //        memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
        //        BitmapImage bitmapImage = new BitmapImage();
        //        bitmapImage.BeginInit();
        //        bitmapImage.StreamSource = memoryStream;
        //        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        //        bitmapImage.EndInit();
        //        return bitmapImage;
        //    }
        //}
    }
}
