using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Linq;
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
        private string coordinates;
        public string Name { get { return name; } }
        public string Coordinates { get { return coordinates; } }

        public ListBoxLabel(string label_name, int x1, int x2, int y1, int y2)
        {
            name = label_name;
            coordinates = "X1=" + x1.ToString() +
                       ",  Y1=" + y1.ToString() +
                       ",  X2=" + x2.ToString() +
                       ",  Y2=" + y2.ToString();
        }
    }

    public class ViewModel : INotifyPropertyChanged
    {
        private string folder_path;
        private string img;
        //private bool changed;
        //private bool cancellable;
        //private bool selected;

        // Абсолютный путь к каталогу с изображениями
        public string FolderPath
        {
            get { return folder_path; }
            set { folder_path = value; OnPropertyChanged("FolderPath"); }
        }

        // Абсолютный путь к изображению
        public string Image
        {
            get { return img; }
            set { img = value; OnPropertyChanged("Image"); }
        }

        /* Возможность запуска вычислений (отключается, когда вычисления для двух выбранных изображений уже выполнены)
        public bool ImagesChanged
        {
            get { return changed; }
            set { changed = value; OnPropertyChanged("ImagesChanged"); }
        }

        // Возможность отмены вычислений (отключается, когда нет активных вычислений)
        public bool Cancellable
        {
            get { return cancellable; }
            set { cancellable = value; OnPropertyChanged("Cancellable"); }
        }

        // Подтверждение выбора изображения для удаления из хранилища (иначе нажать кнопку удаления будет невозможно)
        public bool ImageSelected
        {
            get { return selected; }
            set { selected = value; OnPropertyChanged("ImageSelected"); }
        }*/

        public ObservableCollection<Img> FolderImages { get; set; }
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

            FolderImages = new ObservableCollection<Img>();
            DatabaseImages = new ObservableCollection<Img>();
            using (var db = new DatabaseContext())
            {
                var images = db.Imgs;
                foreach (var image in images)
                    DatabaseImages.Add(image);
            }
            Labels = new ObservableCollection<ListBoxLabel>();

            FolderImages.CollectionChanged += FolderImages_CollectionChanged;
            DatabaseImages.CollectionChanged += DatabaseImages_CollectionChanged;
            Labels.CollectionChanged += Labels_CollectionChanged;

            // ImagesChanged = true;
            // Cancellable = false;
            // ImageSelected = false;
        }
    }

    public partial class MainWindow : Window
    {
        public ViewModel ViewModel;
        public YOLOv4 Yolo;

        public MainWindow()
        {
            this.ViewModel = new ViewModel();
            this.Yolo = new YOLOv4(download_model: false);
            InitializeComponent();
            this.DataContext = ViewModel;

            FolderImagesListBox.SelectionChanged += FolderImagesListBox_SelectionChanged;
            DatabaseImagesListBox.SelectionChanged += DatabaseImagesListBox_SelectionChanged;
        }

        private void FolderImagesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // ViewModel.ImagesChanged = true;
            int i = FolderImagesListBox.SelectedIndex;
            if (i != -1)
                ViewModel.Image = ViewModel.FolderImages[i].Path;
        }

        private void DatabaseImagesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // ViewModel.ImagesChanged = true;
            int i = DatabaseImagesListBox.SelectedIndex;
            if (i != -1)
                ViewModel.Image = ViewModel.DatabaseImages[i].Path;
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
            return new Img { Name = Path.GetFileName(image_path), Path = image_path, Data = image_byte_ar };
        }

        // Добавление изображения в базу данных
        private int AddImageToDatabase(Img img)
        {
            int img_id = -1;
            if (img != null)
                using (var db = new DatabaseContext())
                {
                    db.Add(img);
                    db.SaveChanges();

                    img_id = img.ID;
                }
            return img_id;
        }

        // Добавление датасета в базу данных
        private int AddDatasetToDatabase(Dataset dataset)
        {
            int dataset_id = -1;
            if (dataset != null)
                using (var db = new DatabaseContext())
                {
                    db.Add(dataset);
                    db.SaveChanges();

                    dataset_id = dataset.ID;
                }
            return dataset_id;
        }

        // Добавление ключей к изображению и датасету в базу данных
        private int AddAnalysedImageToDatabase(int img_id, int dataset_id)
        {
            int id = -1;
            if (img_id != -1 && dataset_id != -1)
            {
                var analysed_img = new AnalysedImage { ImgID = img_id, DatasetID = dataset_id };
                using (var db = new DatabaseContext())
                {
                    db.Add(analysed_img);
                    db.SaveChanges();

                    id = analysed_img.ID;
                }
            }
            return id;
        }

        // Считывание элемента из базы данных
        private Img GetFromDatabase(int id)
        {
            Img img = null;
            using (var db = new DatabaseContext())
            {
                var q = db.Imgs.Where(x => x.ID == id);
                if (q.Any())
                    img = q.First();
            }
            return img;
        }

        // Поиск и открытие каталога с изображениями и их анализ
        private void BrowseFolders(object sender, RoutedEventArgs e)
        {
            var folder = new FolderBrowserDialog();
            if (folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ViewModel.FolderPath = folder.SelectedPath;
                ViewModel.FolderImages.Clear();

                string[] image_paths = Directory.GetFiles(ViewModel.FolderPath);
                foreach (string image_path in image_paths)
                {
                    var file_ext = Path.GetExtension(image_path);
                    if (file_ext == ".jpg" || file_ext == ".png")
                        ViewModel.FolderImages.Add(MakeImage(image_path));
                }

                // Анализ изображений нейросетью
                progressbar.Value = 0;
                if (ViewModel.FolderImages.Count != 0)
                {
                    int advance = 100 / ViewModel.FolderImages.Count;
                    foreach (var image in ViewModel.FolderImages)
                    {
                        var dataset = Yolo.MakeDataset(image.Path);

                        int img_id = AddImageToDatabase(image);
                        int dataset_id = AddDatasetToDatabase(dataset);
                        int id = AddAnalysedImageToDatabase(img_id, dataset_id);

                        progressbar.Value += advance;
                    }
                }
                progressbar.Value = 100;
            }
        }
    }
}
