using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.FaceAnalysis;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CropImage
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private WriteableBitmap bitmapImageEx;
        private WriteableBitmap bitmapImageExMod;
        private WriteableBitmap bitmapImageExModDraw;
        private Point[] pointArray = new Point[4];
        private List<string> labels = new List<string> { "TopLeft: ", "TopRight: ", "BottomLeft: ", "BottomRight: " }; 
        private int Index = 1;
        private double Ratio = 1;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void ImageControl_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Point lClickedPoint = e.GetPosition(ImageControl);

            double TouchRatioX = bitmapImageExMod.PixelWidth / ImageControl.ActualWidth;
            double TouchRatioY = bitmapImageExMod.PixelHeight / ImageControl.ActualHeight;

            pointArray[Index - 1].X = Math.Round(lClickedPoint.X * TouchRatioX);
            pointArray[Index - 1].Y = Math.Round(lClickedPoint.Y * TouchRatioY);

            UpdateLabels();

            MoveRB();
        }

        private void UpdateLabels()
        {
            foreach (RadioButton xRB in StackPanelControl.Children.OfType<RadioButton>())
            {
                int xIndex = int.Parse(xRB.Tag.ToString())-1;
                xRB.Content = labels[xIndex] + pointArray[xIndex].ToString();
            }
            DrawSelection();
        }

        private void MoveRB()
        {
            foreach (RadioButton xRB in StackPanelControl.Children.OfType<RadioButton>())
            {
                if ((int.Parse(xRB.Tag.ToString()) == Index + 1) || (xRB.Tag.ToString() == "1") && (Index == 4))
                {
                    xRB.IsChecked = true;
                    break;
                }
            }
        }

        private void R_Checked(object sender, RoutedEventArgs e)
        {
            Index = Int32.Parse((sender as RadioButton).Tag.ToString());
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                using (Windows.Storage.Streams.IRandomAccessStream fileStream =
                    await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.SetSource(fileStream);

                    bitmapImageEx = await new WriteableBitmap(1, 1).FromStream(fileStream);

                    WidthControl.Text = bitmapImageEx.PixelWidth.ToString();

                    Resize();

                    R1.IsChecked = true;
                }
            }



        }

        private void Resize()
        {
            int lWidth = 0;
            try
            {
                Int32.TryParse(WidthControl.Text, out lWidth);
            }
            finally
            {
                if (lWidth < 0)
                {
                    lWidth = 0;
                }
                if (lWidth > bitmapImageEx.PixelWidth)
                {
                    lWidth = bitmapImageEx.PixelWidth;
                }
                WidthControl.Text = lWidth.ToString();
            }

            Ratio = (double)lWidth / (double)bitmapImageEx.PixelWidth;
            bitmapImageExMod = bitmapImageEx.Resize(lWidth, (int)Math.Round(Ratio * (double)bitmapImageEx.PixelHeight), WriteableBitmapExtensions.Interpolation.Bilinear);
            ImageControl.Source = bitmapImageExMod;

            pointArray[0].X = 0;
            pointArray[0].Y = 0;

            pointArray[1].X = bitmapImageEx.PixelWidth;
            pointArray[1].Y = 0;

            pointArray[2].X = 0;
            pointArray[2].Y = bitmapImageEx.PixelHeight;

            pointArray[3].X = bitmapImageEx.PixelWidth;
            pointArray[3].Y = bitmapImageEx.PixelHeight;

            HeightControl.Text = bitmapImageExMod.PixelHeight.ToString();

            UpdateLabels();
        }

        private void WidthControl_TextChanged(object sender, TextChangedEventArgs e)
        {
            Resize();
        }

        private void DrawSelection()
        {
            double TouchRatioX = bitmapImageExMod.PixelWidth / ImageControl.ActualWidth;
            double TouchRatioY = bitmapImageExMod.PixelHeight / ImageControl.ActualHeight;

            bitmapImageExModDraw = new WriteableBitmap((int)Math.Round(ImageControl.ActualWidth), (int)Math.Round(ImageControl.ActualHeight));
            bitmapImageExModDraw.FillRectangle(0, 0, bitmapImageExModDraw.PixelWidth, bitmapImageExModDraw.PixelHeight, Colors.Gray);
            bitmapImageExModDraw.FillQuad((int)Math.Round(pointArray[0].X / TouchRatioX), (int)Math.Round(pointArray[0].Y / TouchRatioY),
                (int)Math.Round(pointArray[1].X / TouchRatioX), (int)Math.Round(pointArray[1].Y / TouchRatioY),
                (int)Math.Round(pointArray[3].X / TouchRatioX), (int)Math.Round(pointArray[3].Y / TouchRatioY),
                (int)Math.Round(pointArray[2].X / TouchRatioX), (int)Math.Round(pointArray[2].Y / TouchRatioY),
                Colors.Transparent);
            foreach(Point xPoint in pointArray)
            {
                bitmapImageExModDraw.FillEllipseCentered((int)(Math.Round(xPoint.X / TouchRatioX)), (int)(Math.Round(xPoint.Y / TouchRatioY)), 20, 20, Colors.Black);
                bitmapImageExModDraw.FillEllipseCentered((int)(Math.Round(xPoint.X / TouchRatioX)), (int)(Math.Round(xPoint.Y / TouchRatioY)), 16, 16, Colors.Yellow);
            }
            ImageControlCrop.Source = bitmapImageExModDraw;

        }

        public class ImageItem
        {
            public string Path { get; set; }
            public Point TopLeft { get; set; }
            public Point TopRight { get; set; }
            public Point BottomLeft { get; set; }
            public Point BottomRight { get; set; }
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                ProgressRingControl.IsActive = true;

                CloudStorageAccount LCloudStorageAccount = CloudStorageAccount.Parse(ConnectionControl.Text);
                CloudBlobClient LBlobClient = LCloudStorageAccount.CreateCloudBlobClient();
                CloudBlobContainer LBlobContainer = LBlobClient.GetContainerReference(ContainerControl.Text);
                await LBlobContainer.CreateIfNotExistsAsync();
                CloudBlockBlob LBlockBlob = LBlobContainer.GetBlockBlobReference(DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss-fff") + ".jpg");
                var lstream = new InMemoryRandomAccessStream();
                await bitmapImageExMod.ToStreamAsJpeg(lstream);
                await LBlockBlob.UploadFromStreamAsync(lstream.AsStream());

                CloudQueueClient LQueueClient = LCloudStorageAccount.CreateCloudQueueClient();
                CloudQueue LQueue = LQueueClient.GetQueueReference(QueueControl.Text);
                await LQueue.CreateIfNotExistsAsync();

                ImageItem LImageItem = new ImageItem { Path = LBlockBlob.Uri.ToString(), TopLeft = pointArray[0], TopRight = pointArray[1], BottomLeft = pointArray[2], BottomRight = pointArray[3] };

                string jsonString = JsonConvert.SerializeObject(LImageItem);
                CloudQueueMessage LQueueMessage = new CloudQueueMessage(jsonString);
                await LQueue.AddMessageAsync(LQueueMessage);
            }
            finally
            {
                ProgressRingControl.IsActive = false;
            }
        }
    }
}
