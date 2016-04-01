using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Drawing;
using System.IO;

namespace DataPicture
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();           
        }

		
        static void GetRectangleWidthAndHeightFromArea(long area, out int width, out int height)
        {
            var squareSide = (int)Math.Ceiling(Math.Sqrt(area));


            width = height = squareSide;

            while ((width - 1) * height >= area)
                width--;

            var rectArea = width * height;
        }

        static int GetPixelsCountFromBytesCount(long bytesCount)
        {
            var pixelsCount = (int)Math.Ceiling(bytesCount / 3.0);

            return pixelsCount;
        }


        static Bitmap GetImageFromBytes(byte[] bytes)
        {
            var informationalPixelsCount = GetPixelsCountFromBytesCount(bytes.LongLength + 8); // 8 bytes = long = header = real information size (bytes)

            int width;
            int height;

            GetRectangleWidthAndHeightFromArea(informationalPixelsCount, out width, out height);

            // test section
            var totalArea = width * height;
            var overdue = totalArea - informationalPixelsCount;
            // test section

            

            var informationalPayloadLength = bytes.LongLength;

            Bitmap bmp = new Bitmap(width, height);

            var currentByte = 0;

            #region DrawingPayload
            
            for (int y = 0; y < bmp.Height; y++)
                for (int x = 0; x < bmp.Width; x++)
                {
                    if(currentByte == informationalPayloadLength)
                    {
                        bmp.SetPixel(x, y, GetRandomPixel());

                        continue;
                    }



                    var redValue = bytes[currentByte];
                    currentByte++;




                    var greenValue = 0;
                    if (currentByte != informationalPayloadLength)
                    {
                        greenValue = bytes[currentByte];
                        currentByte++;
                    }
                    else
                        greenValue = rnd.Next(256);





                    var blueValue = 0;
                    if (currentByte != informationalPayloadLength)
                    {
                        blueValue = bytes[currentByte];
                        currentByte++;
                    }
                    else
                        blueValue = rnd.Next(256);

                    var color = System.Drawing.Color.FromArgb(redValue, greenValue, blueValue);

                    bmp.SetPixel(x, y, color);
                }
            #endregion


            #region EmbeddingPayloadSizeInfoAtTheEndOfTheImage
            var payloadSizeBytes = BitConverter.GetBytes(informationalPayloadLength); // less significant bytes are first

            var yBound = height - 1;

            for (int indexOfByteOfPayloadSize = 0;
                indexOfByteOfPayloadSize < payloadSizeBytes.Length;
                indexOfByteOfPayloadSize++)
            {
                var x = width - (indexOfByteOfPayloadSize / 3) - 1;

                //var t = bmp.GetPixel(yBound, x);

                var oldColor = bmp.GetPixel(x, yBound);

                switch (indexOfByteOfPayloadSize % 3)
                {
                    case 0: bmp.SetPixel(x, yBound, System.Drawing.Color.FromArgb(oldColor.R, oldColor.G, payloadSizeBytes[indexOfByteOfPayloadSize])); break; // blue
                    case 1: bmp.SetPixel(x, yBound, System.Drawing.Color.FromArgb(oldColor.R, payloadSizeBytes[indexOfByteOfPayloadSize], oldColor.B)); break; // green
                    case 2: bmp.SetPixel(x, yBound, System.Drawing.Color.FromArgb(payloadSizeBytes[indexOfByteOfPayloadSize], oldColor.G, oldColor.B)); break; // red
                }
            }
            #endregion


            return bmp;
        }

        static Random rnd = new Random();
        static System.Drawing.Color GetRandomPixel()
        {
            return System.Drawing.Color.FromArgb(rnd.Next(int.MaxValue));
        }




        static byte[] GetBytesFromImage(Bitmap bmp)
        {
            #region GettingPayloadSizeInfoFromTheEndOfTheImage
            var payloadSizeBytes = new byte[8]; // less significant bytes are first // = 95124 for test case

            var yBound = bmp.Height - 1;

            for (int indexOfByteOfPayloadSize = 0;
                indexOfByteOfPayloadSize < payloadSizeBytes.Length;
                indexOfByteOfPayloadSize++)
            {
                var x = bmp.Width - (indexOfByteOfPayloadSize / 3) - 1;

                //var t = bmp.GetPixel(yBound, x);

                var color = bmp.GetPixel(x, yBound);

                switch (indexOfByteOfPayloadSize % 3)
                {
                    case 0: payloadSizeBytes[indexOfByteOfPayloadSize] = color.B; break; // blue
                    case 1: payloadSizeBytes[indexOfByteOfPayloadSize] = color.G; break; // green
                    case 2: payloadSizeBytes[indexOfByteOfPayloadSize] = color.R; break; // red
                }
            }

            var payloadSize = BitConverter.ToInt64(payloadSizeBytes, 0);
            #endregion



            #region GettingPayload

            var payloadBytes = new byte[payloadSize];

            var currentByte = 0;

            for (int y = 0; y < bmp.Height; y++)
                for (int x = 0; x < bmp.Width; x++)
                {
                    var color = bmp.GetPixel(x, y);

                    for(int rgb = 0; rgb < 3; rgb++)
                    {
                        switch (currentByte % 3)
                        {
                            case 0: payloadBytes[currentByte] = color.R; break; // red
                            case 1: payloadBytes[currentByte] = color.G; break; // green
                            case 2: payloadBytes[currentByte] = color.B; break; // blue
                        }

                        currentByte++;
                        if (currentByte == payloadSize)
                            goto done;
                    }
                }
            #endregion
            done:


            return payloadBytes;
        }

        private void Stego_radioButton_Checked(object sender, RoutedEventArgs e)
        {
            if(Button_Go != null)
                Button_Go.Content = "Зашифровать";
        }

        private void UnStego_radioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (Button_Go != null)
                Button_Go.Content = "Расшифровать";
        }

        static string fileToStegoPath = "";
        static string fileToUnStegoPath = "";


        private void ChooseFileToUnstego_button_Click(object sender, RoutedEventArgs e)
        {
            var file_opening_dialog = new Microsoft.Win32.OpenFileDialog
            {
                CheckFileExists = true
            };

            if (file_opening_dialog.ShowDialog(this) != true) return;

            if(!file_opening_dialog.SafeFileName.ToLower().EndsWith(".bmp"))
            {
                MessageBox.Show("Не правильный файл! Укажите .bmp файл.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }


            fileToUnStegoPath = file_opening_dialog.FileName;
            var fileName = file_opening_dialog.SafeFileName;


            UnStegoFileame_label.Content = fileName.Replace("_", "__");
        }

        private void ChooseFileToStego_button_Click(object sender, RoutedEventArgs e)
        {
            var file_opening_dialog = new Microsoft.Win32.OpenFileDialog
            {
                CheckFileExists = true
            };

            if (file_opening_dialog.ShowDialog(this) != true) return;


            fileToStegoPath = file_opening_dialog.FileName;
            var fileName = file_opening_dialog.SafeFileName;


            StegoFileame_label.Content = fileName.Replace("_", "__");
        }

        private void Button_Go_Click(object sender, RoutedEventArgs e)
        {
            if((bool)Stego_radioButton.IsChecked)
            {
                if(fileToStegoPath == "")
                {
                    MessageBox.Show("Сначала выберите файл для шифрования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                    return;
                }

                var filenameStego = System.IO.Path.GetFileNameWithoutExtension(fileToStegoPath) + "_stego.bmp";

                var image = GetImageFromBytes(File.ReadAllBytes(fileToStegoPath));
                image.Save(filenameStego, System.Drawing.Imaging.ImageFormat.Bmp);

                

                MessageBox.Show($"Сохранено в \"{filenameStego}\"", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);

                return;
            }




            if (fileToUnStegoPath == "")
            {
                MessageBox.Show("Сначала выберите файл для дешифрования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            var filenameUnStego = System.IO.Path.GetFileNameWithoutExtension(fileToUnStegoPath) + ".unknown";

            var bytes = GetBytesFromImage((Bitmap)Bitmap.FromFile(fileToUnStegoPath));
            File.WriteAllBytes(filenameUnStego, bytes);


            MessageBox.Show($"Сохранено в \"{filenameUnStego}\"", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
