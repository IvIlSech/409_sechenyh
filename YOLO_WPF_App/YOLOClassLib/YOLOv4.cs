using System.Net;
using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using YOLOv4MLNet.DataStructures;
using static Microsoft.ML.Transforms.Image.ImageResizingEstimator;
using DatabaseClassLib;

namespace YOLOClassLib
{
    public class YOLOv4
    {
        public string ModelPath = @"C:\Users\Иван\Desktop\YOLOv4MLNet-master\yolov4.onnx";

        const string ImageOutputFolder = @"C:\Users\Иван\Desktop\Output";

        static readonly string[] ClassesNames = new string[] { "person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat", "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "sofa", "pottedplant", "bed", "diningtable", "toilet", "tvmonitor", "laptop", "mouse", "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush" };

        public PredictionEngine<YoloV4BitmapData, YoloV4Prediction> MyPredictionEngine { get; }

        private void DownloadModel()
        {
            using (var client = new WebClient())
            {
                client.DownloadFile(new Uri("https://github.com/onnx/models/raw/main/vision/object_detection_segmentation/yolov4/model/yolov4.onnx"), "yolov4.onnx");
            }
        }

        private int LabelToIndex(string label)
        {
            string[] str_arr = ClassesNames;
            for (int i = 0; i < ClassesNames.Length; i++)
            {
                if (str_arr[i] == label)
                    return i;
            }
            return -1;
        }

        public YOLOv4(bool download_model)
        {
            if (!File.Exists(ModelPath) && download_model)
                DownloadModel();
            Directory.CreateDirectory(ImageOutputFolder);
            var mlContext = new MLContext();

            // Define scoring pipeline
            var model = mlContext.Transforms.ResizeImages(inputColumnName: "bitmap", outputColumnName: "input_1:0", imageWidth: 416, imageHeight: 416, resizing: ResizingKind.IsoPad)
                .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "input_1:0", scaleImage: 1f / 255f, interleavePixelColors: true))
                .Append(mlContext.Transforms.ApplyOnnxModel(
                    shapeDictionary: new Dictionary<string, int[]>()
                    {
                        { "input_1:0", new[] { 1, 416, 416, 3 } },
                        { "Identity:0", new[] { 1, 52, 52, 3, 85 } },
                        { "Identity_1:0", new[] { 1, 26, 26, 3, 85 } },
                        { "Identity_2:0", new[] { 1, 13, 13, 3, 85 } },
                    },
                    inputColumnNames: new[]
                    {
                        "input_1:0"
                    },
                    outputColumnNames: new[]
                    {
                        "Identity:0",
                        "Identity_1:0",
                        "Identity_2:0"
                    },
                    modelFile: ModelPath, recursionLimit: 100)).Fit(mlContext.Data.LoadFromEnumerable(new List<YoloV4BitmapData>()));

            // Create prediction engine
            MyPredictionEngine = mlContext.Model.CreatePredictionEngine<YoloV4BitmapData, YoloV4Prediction>(model);
        }

        public string IndexToLabel(int index)
        { return ClassesNames[index]; }

        public Dataset MakeDataset(string image_path)
        {
        
            var label_list = new List<int>();
            var x1_list = new List<float>();
            var x2_list = new List<float>();
            var y1_list = new List<float>();
            var y2_list = new List<float>();
            using (var bitmap = new Bitmap(System.Drawing.Image.FromFile(image_path)))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    // predict
                    foreach (var res in MyPredictionEngine.Predict(new YoloV4BitmapData() { Image = bitmap }).GetResults(ClassesNames, 0.3f, 0.7f))
                    {
                        // draw predictions
                        var x1 = res.BBox[0];
                        var y1 = res.BBox[1];
                        var x2 = res.BBox[2];
                        var y2 = res.BBox[3];
                        g.DrawRectangle(Pens.Red, x1, y1, x2 - x1, y2 - y1);
                        using (var brushes = new SolidBrush(Color.FromArgb(50, Color.Red)))
                        {
                            g.FillRectangle(brushes, x1, y1, x2 - x1, y2 - y1);
                        }

                        g.DrawString(res.Label + " " + res.Confidence.ToString("0.00"),
                                     new Font("Arial", 12), Brushes.Blue, new PointF(x1, y1));
                        label_list.Add(LabelToIndex(res.Label));
                        x1_list.Add(x1);
                        x2_list.Add(x2);
                        y1_list.Add(y1);
                        y2_list.Add(y2);
                    }
                    bitmap.Save(Path.Combine(ImageOutputFolder, Path.ChangeExtension(Path.GetFileName(image_path), "_processed" + Path.GetExtension(image_path))));
                }
            }
            return new Dataset
            {
                LabelsIndices = Converter.IntArrayToByte(label_list.ToArray()),
                X1 = Converter.FloatArrayToByte(x1_list.ToArray()),
                Y1 = Converter.FloatArrayToByte(y1_list.ToArray()),
                X2 = Converter.FloatArrayToByte(x2_list.ToArray()),
                Y2 = Converter.FloatArrayToByte(y2_list.ToArray())
            };
        }
    }
}