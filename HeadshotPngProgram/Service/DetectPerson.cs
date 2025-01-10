using ImageMagick;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace HeadshotPngProgram.Service
{
    internal class DetectPerson : Abstract.ViewModelBase
    {

        /// <summary>
        /// Detects faces and upper bodies (persons) in the given image using Haar Cascade classifiers.
        /// It returns a list of bounding boxes (rectangles) around the detected faces and upper bodies.
        /// </summary>
        /// <param name="image">The input image to be processed for detection.</param>
        /// <returns>A list of rectangles representing detected faces and upper bodies.</returns>
        public List<Rectangle> DetectFaceAndUpperBody(MagickImage image)
        {
            try
            {

                byte[] imageBytes = image.ToByteArray(MagickFormat.Png);
                Mat imagetobytes = Cv2.ImDecode(imageBytes, ImreadModes.Unchanged);

                // Check if the image has an alpha channel (transparency)
                if (imagetobytes.Channels() == 4)
                {
                    // Split the image into its channels (BGRA)
                    Mat[] channels = Cv2.Split(imagetobytes);
                    Mat alphaChannel = channels[3]; // The alpha channel (index 3)

                    // Create a binary mask (threshold alpha channel)
                    Mat binaryMask = new Mat();
                    Cv2.Threshold(alphaChannel, binaryMask, 1, 255, ThresholdTypes.Binary);

                    // Find contours in the binary mask
                    Cv2.FindContours(binaryMask, out OpenCvSharp.Point[][] contours, out HierarchyIndex[] hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                    // Find the largest contour (assumed to be the person)
                    if (contours.Length == 0)
                    {
                        WarningMessage("No contours detected.");
                        return new List<Rectangle>();
                    }

                    var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();
                    Rect boundingBox = Cv2.BoundingRect(largestContour);

                    // Create a list to hold the detected objects (only one person in this case)
                    List<Rectangle> detectedObjects = new List<Rectangle>
                    {
                        new Rectangle(boundingBox.X, boundingBox.Y, boundingBox.Width, boundingBox.Height)
                    };

                    return detectedObjects;
                }
                else
                {
                    WarningMessage("The image does not contain an alpha channel (transparency).");
                    return new List<Rectangle>();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
                return new List<Rectangle>();
            }
        }

        #region Person detect using yolo

        /// <summary>
        /// This class provides functionality to detect faces and upper bodies in images using YOLO (You Only Look Once) object detection.
        /// It initializes the YOLO network with pre-trained weights and config, and uses it to perform detection on input images.
        /// </summary>

        //private readonly string _yoloConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Asset", "yolov3.cfg");
        //private readonly string _yoloWeights = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Asset", "yolov3.weights");
        //private readonly string _cocoNames = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Asset", "coco.names");
        //private readonly string _cocoNames = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Asset", "coco.names");

        //private Net _yoloNet;
        //public readonly string _yoloConfig = @"Asset\yolov3.cfg";
        //public readonly string _yoloWeights = @"Asset\yolov3.weights";


        //public YoloObjectDetection()
        //{
        //    //Load YOLO network (weights and config)
        //    _yoloNet = DnnInvoke.ReadNetFromDarknet(_yoloConfig, _yoloWeights);
        //}


        ///// <summary>
        ///// Detects faces and upper bodies (persons) in the given image using YOLO object detection.
        ///// It returns a list of bounding boxes (rectangles) around the detected faces and upper bodies.
        ///// </summary>
        ///// <param name="image">The input image to be processed for detection.</param>
        ///// <returns>A list of rectangles representing detected faces and upper bodies.</returns>
        //public List<Rectangle> DetectFaceAndUpperBody(Mat image)
        //{
        //    try
        //    {
        //        if (image.IsEmpty)
        //        {
        //            WarningMessage("Error: Unable to load image.");
        //            return new List<Rectangle>();
        //        }

        //        // Prepare the image to match YOLO input size (416x416)
        //        Mat blob = DnnInvoke.BlobFromImage(image, 1.0 / 255.0, new Size(416, 416), swapRB: true);

        //        // Set the input to the network
        //        _yoloNet.SetInput(blob);
        //        _yoloNet.SetPreferableBackend(Emgu.CV.Dnn.Backend.OpenCV);
        //        _yoloNet.SetPreferableTarget(Target.Cpu);

        //        // Run forward pass to get the output
        //        VectorOfMat output = new VectorOfMat();
        //        _yoloNet.Forward(output, _yoloNet.UnconnectedOutLayersNames);

        //        // Filter the results for face and upper body (person class 0 and other classes)
        //        List<Rectangle> detectedObjects = new List<Rectangle>();
        //        float confidenceThreshold = 0.5f;  // Confidence threshold for detecting objects
        //        float nmsThreshold = 0.4f; // Non-max suppression threshold

        //        List<Rectangle> boxes = new List<Rectangle>();
        //        List<float> confidences = new List<float>();
        //        List<int> classIds = new List<int>();

        //        // Iterate over all detections from the output
        //        for (int i = 0; i < output.Size; i++)
        //        {
        //            Mat detectionMat = output[i];
        //            float[] detectionData = new float[detectionMat.Rows * detectionMat.Cols];
        //            System.Runtime.InteropServices.Marshal.Copy(detectionMat.DataPointer, detectionData, 0, detectionData.Length);

        //            // Iterate through each row of the detection data
        //            for (int k = 0; k < detectionMat.Rows; k++)
        //            {
        //                var row = detectionData.Skip(k * detectionMat.Cols).Take(detectionMat.Cols).ToArray();

        //                // Extract class probabilities (skip first 5 values which are for center coordinates and width/height)
        //                var classProbabilities = row.Skip(5).ToArray();

        //                // Get the class ID with the highest probability
        //                int classID = Array.IndexOf(classProbabilities, classProbabilities.Max());
        //                float confidence = classProbabilities[classID];

        //                //only detect persons (class ID 0)
        //                if (confidence > confidenceThreshold && classID == 0)
        //                {
        //                    // Get bounding box coordinates (scaled to original image size)
        //                    int centerX = (int)(row[0] * image.Width);
        //                    int centerY = (int)(row[1] * image.Height);
        //                    int width = (int)(row[2] * image.Width);
        //                    int height = (int)(row[3] * image.Height);

        //                    // Convert center coordinates to top-left corner (x, y)
        //                    int x = (int)(centerX - width / 2);
        //                    int y = (int)(centerY - height / 2);

        //                    // Add the bounding box, confidence, and class ID to the lists
        //                    boxes.Add(new Rectangle(x, y, width, height));
        //                    confidences.Add(confidence);
        //                    classIds.Add(classID);
        //                }
        //            }
        //        }

        //        var indices = DnnInvoke.NMSBoxes(boxes.ToArray(), confidences.ToArray(), confidenceThreshold, nmsThreshold);

        //        // Add the remaining boxes after NMS to the list of detected objects
        //        for (int i = 0; i < indices.Length; i++)
        //        {
        //            int idx = indices[i];
        //            if (idx >= 0)
        //            {
        //                detectedObjects.Add(boxes[idx]);
        //            }
        //        }

        //        return detectedObjects;
        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorMessage(ex);
        //        return null;
        //    }
        //}
        #endregion

    }
}

