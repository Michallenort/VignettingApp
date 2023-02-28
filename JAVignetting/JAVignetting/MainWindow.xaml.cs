using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using Microsoft.Win32;
using Rectangle = System.Drawing.Rectangle;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.Windows.Ink;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;
using System.IO;
using Path = System.IO.Path;
using VignetteCreator;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace JAVignetting
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Bitmap originalImage;
        private Bitmap vignettedImage;

        Stopwatch stopWatch;

        private string fileName;

        [DllImport(@"C:\Users\micha\OneDrive\Pulpit\JAVignetting\x64\Debug\VignettteAsm.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern float Vignette(byte[] pixelArray, int index, int length, int centerX, int centerY, int radious, int gradientArea);

        public MainWindow()
        {
            InitializeComponent();
            runBtn.IsEnabled = false;
            saveBtn.IsEnabled = false;
            threadsNumber.Value = Environment.ProcessorCount;

            originalImage = new Bitmap(1, 1);
            vignettedImage = new Bitmap(1, 1);
            stopWatch = new Stopwatch();
            fileName = "";
        }

        private void loadBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image files (*.jpg,*.jpeg,*.png)|*.jpg;*.jpeg;*.png";

            if (dialog.ShowDialog() == true)
            {
                fileName = dialog.FileName;
                originalImage = new Bitmap(dialog.FileName);
                imgBefore.Source = new BitmapImage(new Uri(dialog.FileName));
                runBtn.IsEnabled = true;
            }

            saveBtn.IsEnabled = false;
        }

        private void runBtn_Click(object sender, RoutedEventArgs e)
        {
            vignettedImage = (Bitmap) originalImage.Clone(new Rectangle(0, 0, originalImage.Width, originalImage.Height), PixelFormat.Format32bppArgb);
            BitmapData vignettedBitmapData = vignettedImage.LockBits(new Rectangle(0, 0, originalImage.Width, originalImage.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            int uStride = vignettedBitmapData.Stride;
            int uHeight = vignettedImage.Height;
            byte[] pixelArray = new byte[uStride * uHeight];

            Marshal.Copy(vignettedBitmapData.Scan0, pixelArray, 0, vignettedBitmapData.Stride * vignettedImage.Height);

            List<int> rowIndexArray = new List<int>();
            for (int i = 0; i < (uStride * uHeight); i += uStride)
            {
                rowIndexArray.Add(i);
            }

            int centerX = vignettedImage.Width * (int)xPositionValue.Value / 100;
            int centerY = vignettedImage.Height * (int)yPositionValue.Value / 100;
            int gradientArea = (int) (0.2 * Math.Min(vignettedImage.Width, vignettedImage.Height));
            int radious = (int) (radiousValue.Value / 100.0 * Math.Min(vignettedImage.Height, vignettedImage.Width));

            ParallelOptions howManyThreads = new ParallelOptions { MaxDegreeOfParallelism = (int)threadsNumber.Value };
            stopWatch.Restart();

            if (csharpButton.IsChecked == true)
            {
                stopWatch.Start();

                Parallel.ForEach(rowIndexArray, howManyThreads, index =>
                {
                    Vignetter.Vignette(ref pixelArray, index, uStride, centerX, centerY, radious, gradientArea);
                });

                stopWatch.Stop();
            }
            else if (asmButton.IsChecked == true)
            {
                stopWatch.Start();

                Parallel.ForEach(rowIndexArray, howManyThreads, index =>
                {
                    Vignette(pixelArray, index, uStride, centerX, centerY, radious, gradientArea);
                });

                stopWatch.Stop();
            }

            Marshal.Copy(pixelArray, 0, vignettedBitmapData.Scan0, uStride * uHeight);

            vignettedImage.UnlockBits(vignettedBitmapData);

            using (MemoryStream memory = new MemoryStream())
            {
                vignettedImage.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitMapImage = new BitmapImage();
                bitMapImage.BeginInit();
                bitMapImage.StreamSource = memory;
                bitMapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitMapImage.EndInit();

                imgAfter.Source = bitMapImage;
                saveBtn.IsEnabled = true;
            }

            time.Text = "Time: " + stopWatch.Elapsed.TotalMilliseconds.ToString() + " ms";
        }

        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG Images (.png)|*.png|JPG Images (.jpg)|*.jpg|BMP Images (.bmp)|*.bmp";

            bool? result = saveFileDialog.ShowDialog();


            try
            {
                if (result == true)
                {
                    string fileToSave = saveFileDialog.FileName;
                    string extension = Path.GetExtension(fileToSave);

                    if (fileToSave == fileName)
                    {
                        fileToSave = GetNewFileName(fileToSave);
                    }
                    Mouse.OverrideCursor = Cursors.Wait;
                    Image image = Image.FromHbitmap(vignettedImage.GetHbitmap());
                    image.Save(fileToSave);
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private string GetNewFileName(string fileForSaving)
        {
            string fileOnly = Path.GetFileNameWithoutExtension(fileForSaving);
            string extension = Path.GetExtension(fileForSaving);
            string newFileName = fileOnly + "_";
            newFileName += extension;
            return newFileName;
        }
    }
}
