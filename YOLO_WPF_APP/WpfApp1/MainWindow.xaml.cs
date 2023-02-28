using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using DatabaseClassLib;
using YOLOClassLib;

namespace WpfApp1
{
    public class ListBoxLabel
    {
        private string name;
        private string coords;
        private string path1;
        private string path2;
        private int x1;
        private int y1;
        private int x2;
        private int y2;
        public string Name { get { return name; } }
        public string Coordinates { get { return coords; } }
        public string OriginalImagePath { get { return path1; } }
        public string TrimmedImagePath { get { return path2; } }
        public int X1 { get { return x1; } }
        public int Y1 { get { return y1; } }    
        public int X2 { get { return x2; } }
        public int Y2 { get { return y2; } }

        public ListBoxLabel(string label_name, string orig_path, string trim_path, int x1, int x2, int y1, int y2)
        {
            name = label_name;
            path1 = orig_path;
            path2 = trim_path;
            this.x1 = x1;
            this.x2 = x2;
            this.y1 = y1;
            this.y2 = y2;
            coords = "X1=" + x1.ToString() +
                  ",  Y1=" + y1.ToString() +
                  ",  X2=" + x2.ToString() +
                  ",  Y2=" + y2.ToString();
        }
    }

    public class ViewModel : INotifyPropertyChanged
    {
        private string folder_path;
        private string label;
        private bool cancelable;
        private bool not_empty;

        // Абсолютный путь к каталогу с изображениями
        public string FolderPath
        {
            get { return folder_path; }
            set
            {
                folder_path = value;
                OnPropertyChanged("FolderPath");
            }
        }

        // выбранный класс найденных на изображениях объектов
        public string Label
        {
            get { return label; }
            set
            {
                label = value;
                OnPropertyChanged("Label");
            }
        }

        // Для отмены анализа изображений
        public bool Cancelable
        {
            get { return cancelable; }
            set
            {
                cancelable = value;
                OnPropertyChanged("Cancelable");
            }
        }

        // Индикатор того, что база данных не пуста
        public bool NotEmpty
        {
            get { return not_empty; }
            set
            {
                not_empty = value;
                OnPropertyChanged("NotEmpty");
            }
        }

        //public ObservableCollection<Img> FolderImages { get; set; }
        public ObservableCollection<string> LabelNames { get; set; }
        public ObservableCollection<Img> DatabaseImages { get; set; }
        public ObservableCollection<ListBoxLabel> Labels { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
        void FolderImages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        { OnPropertyChanged("FolderImages"); }
        void DatabaseImages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        { OnPropertyChanged("DatabaseImages"); }
        void Labels_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        { OnPropertyChanged("Labels"); }

        public ViewModel()
        {
            FolderPath = "*каталог с изображениями здесь*";
            Cancelable = false;

            Labels = new ObservableCollection<ListBoxLabel>();
            LabelNames = new ObservableCollection<string>();
            DatabaseImages = new ObservableCollection<Img>();
            using (var db = new DatabaseContext())
            {
                var images = db.Imgs;
                foreach (var image in images)
                    DatabaseImages.Add(image);
            }
            if (DatabaseImages.Count > 0) NotEmpty = true;
            else NotEmpty = false;

            LabelNames.CollectionChanged += FolderImages_CollectionChanged;
            DatabaseImages.CollectionChanged += DatabaseImages_CollectionChanged;
            Labels.CollectionChanged += Labels_CollectionChanged;
        }
    }

    public partial class MainWindow : Window
    {
        private ViewModel ViewModel;
        private YOLOv4 Yolo;
        private SemaphoreSlim database_sem;
        private List<Img> ImagesToAnalyse;
        private List<string> CancellationKeys;

        private void LabelNamesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int i = LabelNamesListBox.SelectedIndex;
            if (i != -1)
                ViewModel.Label = ViewModel.LabelNames[i];
            UpdateLabelsListBox();
        }

        private void LabelsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int i = LabelsListBox.SelectedIndex;
            if (i != -1)
            {
                string original_img_path;
                using (var db = new DatabaseContext())
                { original_img_path = ViewModel.Labels[i].OriginalImagePath; }
                
                var drawing_path = Yolo.Draw(
                    original_img_path,
                    ViewModel.Labels[i].Name,
                    ViewModel.Labels[i].X1,
                    ViewModel.Labels[i].Y1,
                    ViewModel.Labels[i].X2,
                    ViewModel.Labels[i].Y2
                );
                OpenImage(drawing_path);
            }
        }

        private void UpdateLabelsListBox()
        {
            ViewModel.Labels.Clear();
            using (var db = new DatabaseContext())
            {
                foreach(var dataset in db.Datasets)
                {
                    var indices = Utilities.ByteToIntArray(dataset.LabelsIndices);
                    for (int k = 0; k < indices.Length; k++)
                        if (Yolo.IndexToLabel(indices[k]) == ViewModel.Label)
                        {
                            int x1 = Utilities.ByteToIntArray(dataset.X1)[k];
                            int y1 = Utilities.ByteToIntArray(dataset.Y1)[k];
                            int x2 = Utilities.ByteToIntArray(dataset.X2)[k];
                            int y2 = Utilities.ByteToIntArray(dataset.Y2)[k];

                            var original_img_path = db.Imgs.Where(x => x.Hashcode == dataset.Hashcode).First().Path;
                            var trimmed_img_path = Yolo.Trim(original_img_path, ViewModel.Label, x1, y1, x2, y2);
                            ViewModel.Labels.Add(new ListBoxLabel(ViewModel.Label, original_img_path, trimmed_img_path, x1, x2, y1, y2));
                        }
                }
            }
        }

        private void OpenImage(string path)
        {
            Form form = new Form();
            form.Text = "Image Viewer";
            PictureBox pictureBox = new PictureBox();
            pictureBox.Image = System.Drawing.Image.FromFile(path);
            pictureBox.Dock = DockStyle.Fill;
            form.Controls.Add(pictureBox);
            //System.Windows.Forms.Application.Run(form);
            form.ShowDialog();
        }

        private Img MakeImage(string image_path)
        {
            // Конвертация изображения в массив байт
            var img = System.Drawing.Image.FromFile(image_path);
            byte[] image_byte_ar;
            using (var ms = new MemoryStream())
            {
                img.Save(ms, img.RawFormat);
                image_byte_ar = ms.ToArray();
            }
            return new Img
            {
                Name = Path.GetFileName(image_path),
                Path = image_path,
                Data = image_byte_ar,
                Hashcode = Utilities.GetHashcode(image_path)
            };
        }

        private void MakeLabelNames()
        {
            ViewModel.LabelNames.Clear();
            using (var db = new DatabaseContext())
            {
                foreach (var dataset in db.Datasets)
                {
                    var indices_array = Utilities.ByteToIntArray(dataset.LabelsIndices);
                    foreach (var index in indices_array)
                    {
                        var label = Yolo.IndexToLabel(index);
                        bool is_unique_label = true;
                        for (int k = 0; k < ViewModel.LabelNames.Count; k++)
                            if (ViewModel.LabelNames[k] == label)
                                is_unique_label = false;
                        if (is_unique_label)
                            ViewModel.LabelNames.Add(label);
                    }
                }
            }
        }

        public async void AddToDatabase(Img img, Dataset dataset)
        {
            if (img != null && dataset != null)
            {
                await database_sem.WaitAsync();
                using (var db = new DatabaseContext())
                {
                    var query = db.Imgs.Where(x => x.Hashcode == img.Hashcode);
                    if(!query.Any())
                    {
                        // Добавление изображения в базу данных
                        db.Add(img);
                        int img_id = img.ID;

                        // Добавление датасета в базу данных
                        db.Add(dataset);
                        int dataset_id = dataset.ID;
                        db.SaveChanges();

                        if (db.Imgs.Any() && db.Datasets.Any())
                            ViewModel.NotEmpty = true;

                        ViewModel.DatabaseImages.Clear();
                        query = db.Imgs;
                        foreach (var new_img in query)
                            ViewModel.DatabaseImages.Add(new_img);
                    }
                }
                database_sem.Release();
            }
        }

        // Поиск и открытие каталога с изображениями и их анализ
        private async void BrowseFolders(object sender, RoutedEventArgs e)
        {
            var folder = new FolderBrowserDialog();
            if (folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ViewModel.FolderPath = folder.SelectedPath;
                ImagesToAnalyse.Clear();

                string[] image_paths = Directory.GetFiles(ViewModel.FolderPath);
                foreach (string image_path in image_paths)
                {
                    var file_ext = Path.GetExtension(image_path);
                    if (file_ext == ".jpg" || file_ext == ".png")
                        ImagesToAnalyse.Add(MakeImage(image_path));
                }
                Analyse();
            }
        }

        // Анализ изображений нейросетью
        private async void Analyse()
        {
            progressbar.Value = 0;
            ViewModel.Cancelable = true;

            if (ImagesToAnalyse.Count != 0)
            {
                //Yolo.MakePredictionEngine();
                var ActiveTasks = new List<Task<(Img img, Dataset dataset)>>();
                int advance = 100 / ImagesToAnalyse.Count;

                foreach (var image in ImagesToAnalyse)
                {
                    var key = Guid.NewGuid().ToString();
                    CancellationKeys.Add(key);

                    var img = MakeImage(image.Path);
                    //var dataset_task = Yolo.MakeDataset(img, key);
                    var dataset_task = Yolo.PostCringe(img, key);
                    ActiveTasks.Add(dataset_task);

                }
                while (ActiveTasks.Count > 0)
                {
                    try
                    {
                        var finished_task = await Task.WhenAny(ActiveTasks);
                        AddToDatabase(finished_task.Result.img, finished_task.Result.dataset);
                        ActiveTasks.Remove(finished_task);
                        progressbar.Value += advance;
                    }
                    catch (AggregateException ae)
                    {
                        foreach (Exception e in ae.InnerExceptions)
                        {
                            if (e is TaskCanceledException)
                            {
                                TaskCanceledException ex = (TaskCanceledException)e;
                                System.Windows.MessageBox.Show("Процесс отменен!");
                                ActiveTasks.Clear();
                            }
                            else
                            ActiveTasks.Clear();
                            System.Windows.MessageBox.Show(e.Message);
                        }
                    }
                }
                ViewModel.Cancelable = false;
            }
            progressbar.Value = 100;
            MakeLabelNames();
        }

        private async void Cancel_Analysis(object sender, RoutedEventArgs e)
        {
            foreach(var key in CancellationKeys)
                Yolo.Cancel(key); 
        }

        private async void Delete_All_Images(object sender, RoutedEventArgs e)
        {
            await database_sem.WaitAsync();
            using (var db = new DatabaseContext())
            {
                while (db.Imgs.Any())
                {
                    db.Remove(db.Imgs.First());
                    db.Remove(db.Datasets.First());
                    db.SaveChanges();
                }
            }
            database_sem.Release();
            ViewModel.DatabaseImages.Clear();
            ViewModel.NotEmpty = false;
        }

        public MainWindow()
        {
            ViewModel = new ViewModel();
            Yolo = new YOLOv4(download_model: true);
            database_sem = new SemaphoreSlim(1);
            ImagesToAnalyse = new List<Img>();
            CancellationKeys = new List<string>();

            InitializeComponent();
            DataContext = ViewModel;

            LabelsListBox.SelectionChanged += LabelsListBox_SelectionChanged;
            LabelNamesListBox.SelectionChanged += LabelNamesListBox_SelectionChanged;
        }
    }
}

