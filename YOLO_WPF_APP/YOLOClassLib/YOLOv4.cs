using System.Net;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.ML;
using static Microsoft.ML.Transforms.Image.ImageResizingEstimator;
using YOLOv4MLNet.DataStructures;
using DatabaseClassLib;

namespace YOLOClassLib
{
    public class YOLOv4
    {
        private string ModelPath = "yolov4.onnx";
        private string ImageOutputFolder = "Output";
        private PredictionEngine<YoloV4BitmapData, YoloV4Prediction> predictionEngine;
        static readonly string[] ClassesNames = new string[] { "person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat", "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "sofa", "pottedplant", "bed", "diningtable", "toilet", "tvmonitor", "laptop", "mouse", "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush" };
        private Dictionary<string, CancellationTokenSource> cancellationTokens;
        private readonly object locker;

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
            cancellationTokens = new Dictionary<string, CancellationTokenSource>();
            locker = new object();

            if (!File.Exists(ModelPath) && download_model)
                DownloadModel();
        }

        public string IndexToLabel(int index)
        { return ClassesNames[index]; }

        public void MakePredictionEngine()
        {
            var mlContext = new MLContext();

            // Define scoring pipeline
            var pipeline = mlContext.Transforms.ResizeImages(inputColumnName: "bitmap", outputColumnName: "input_1:0", imageWidth: 416, imageHeight: 416, resizing: ResizingKind.IsoPad)
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
                    modelFile: ModelPath, recursionLimit: 100));

            // Fit on empty list to obtain input data schema
            var model = pipeline.Fit(mlContext.Data.LoadFromEnumerable(new List<YoloV4BitmapData>()));

            // Create prediction engine
            predictionEngine = mlContext.Model.CreatePredictionEngine<YoloV4BitmapData, YoloV4Prediction>(model);
        }

        // Заглушка
        public async Task<(Img img, Dataset dataset)> PostCringe(Img img, string key)
        {
            Directory.CreateDirectory(ImageOutputFolder);

            var ct_source = new CancellationTokenSource();
            lock (locker)
            { cancellationTokens[key] = ct_source; }

            var dataset = await Task<Dataset>.Run( () =>
            {
                Thread.Sleep(1000);
                if (ct_source.Token.IsCancellationRequested)
                    throw new TaskCanceledException();

                return new Dataset
                {
                    Hashcode = img.Hashcode,
                    LabelsIndices = Utilities.IntArrayToByte(new int[] { 1, 2, 3, 4 }),
                    X1 = Utilities.IntArrayToByte(new int[] { 1, 5, 9, 13 }),
                    Y1 = Utilities.IntArrayToByte(new int[] { 2, 6, 10, 14 }),
                    X2 = Utilities.IntArrayToByte(new int[] { 3, 7, 11, 15 }),
                    Y2 = Utilities.IntArrayToByte(new int[] { 4, 8, 12, 16 })
                };
            }, ct_source.Token);
            return (img, dataset);
        }

        // Асинхронный анализ изображения
        public async Task<(Img img, Dataset dataset)> MakeDataset(Img img, string key)
        {
            Directory.Delete(ImageOutputFolder, true);
            Directory.CreateDirectory(ImageOutputFolder);

            var ct_source = new CancellationTokenSource();
            lock (locker)
            { cancellationTokens[key] = ct_source; }

            var dataset = await Task<Dataset>.Run(() =>
            {
                var label_list = new List<int>();
                var x1_list = new List<int>();
                var x2_list = new List<int>();
                var y1_list = new List<int>();
                var y2_list = new List<int>();
                using (var bitmap = new Bitmap(System.Drawing.Image.FromFile(img.Path)))
                {
                    // predict
                    var predict = predictionEngine.Predict(new YoloV4BitmapData() { Image = bitmap });
                    var results = predict.GetResults(ClassesNames, 0.3f, 0.7f);
                    foreach (var res in results)
                    {
                        var x1 = (int)res.BBox[0];
                        var y1 = (int)res.BBox[1];
                        var x2 = (int)res.BBox[2];
                        var y2 = (int)res.BBox[3];

                        label_list.Add(LabelToIndex(res.Label));
                        x1_list.Add(x1);
                        x2_list.Add(x2);
                        y1_list.Add(y1);
                        y2_list.Add(y2);
                    }
                }

                Thread.Sleep(1500);
                if (ct_source.Token.IsCancellationRequested)
                    throw new TaskCanceledException();

                return new Dataset
                {
                    Hashcode = img.Hashcode,
                    LabelsIndices = Utilities.IntArrayToByte(label_list.ToArray()),
                    X1 = Utilities.IntArrayToByte(x1_list.ToArray()),
                    Y1 = Utilities.IntArrayToByte(y1_list.ToArray()),
                    X2 = Utilities.IntArrayToByte(x2_list.ToArray()),
                    Y2 = Utilities.IntArrayToByte(y2_list.ToArray())
                };
            }, ct_source.Token);
            return (img, dataset);
        }

        // Обрезать по ограничивающему прямоугольнику
        public string Trim(string img_path, string label, int x1, int y1, int x2, int y2)
        {
            string trimmed_img_path;
            using (var bitmap = new Bitmap(System.Drawing.Image.FromFile(img_path)))
            {
                var rectangle = new Rectangle(x1, y1, x2 - x1, y2 - y1);
                var mybitmap = bitmap.Clone(rectangle, bitmap.PixelFormat);

                trimmed_img_path = Path.ChangeExtension(Path.GetFileName(img_path), label + x1.ToString() + x2.ToString() + y1.ToString() + y2.ToString() + Path.GetExtension(img_path));
                trimmed_img_path = Path.Combine(ImageOutputFolder, trimmed_img_path);
                try
                {
                    if(Directory.Exists(trimmed_img_path)) { mybitmap.Save(trimmed_img_path); }
                }
                catch (Exception e)
                {
                    return @"C:\Users\Иван\Downloads\error.png";
                }
            }
            return Path.GetFullPath(trimmed_img_path);
        }

        // Обвести объект на изображении по ограничивающему прямоугольнику
        public string Draw(string img_path, string label, int x1, int y1, int x2, int y2)
        {
            string drawing_path;
            using (var bitmap = new Bitmap(System.Drawing.Image.FromFile(img_path)))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    // draw predictions
                    g.DrawRectangle(Pens.Red, x1, y1, x2 - x1, y2 - y1);
                    using (var brushes = new SolidBrush(Color.FromArgb(50, Color.Red)))
                    { g.FillRectangle(brushes, x1, y1, x2 - x1, y2 - y1); }

                    g.DrawString(label, new Font("Arial", 12), Brushes.Blue, new PointF(x1, y1));
                    g.Dispose();
                }
                drawing_path = Path.ChangeExtension(Path.GetFileName(img_path), "processed" + Path.GetExtension(img_path));
                drawing_path = Path.Combine(ImageOutputFolder, drawing_path);
                drawing_path = Path.GetFullPath(drawing_path);
                try
                {
                    if (Directory.Exists(drawing_path))
                    { bitmap.Save(drawing_path);}
                }
                catch(Exception e)
                {
                    return @"C:\Users\Иван\Downloads\error1.jpg";
                }
            }
            return drawing_path;
        }

        // Отмена анализа изображения
        public bool Cancel(string key)
        {
            bool contains_key = false;

            lock (locker)
            {
                if (cancellationTokens.ContainsKey(key))
                {
                    cancellationTokens[key].Cancel();
                    cancellationTokens.Remove(key);
                    contains_key = true;
                }
            }
            return contains_key;
        }
    }
}