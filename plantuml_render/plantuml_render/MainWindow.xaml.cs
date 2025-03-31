using Microsoft.Win32;
using PlantUml.Net;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace plantuml_render;

public partial class MainWindow : Window
{
    private Point _lastMousePosition;
    private bool _isDragging = false;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void RenderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string plantCode = PlantUmlCodeTextBox.Text;

            if (string.IsNullOrWhiteSpace(plantCode))
            {
                Debug.WriteLine("PlantUML code is empty or whitespace.");
                MessageBox.Show("Please enter valid PlantUML code.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StatusText.Text = "Status: Rendering...";
            Debug.WriteLine("Starting PlantUML rendering...");
            Debug.WriteLine($"PlantUML Code:\n{plantCode}");

            var renderer = new RendererFactory().CreateRenderer();
            byte[] imageBytes = await renderer.RenderAsync(plantCode, OutputFormat.Png);

            if (imageBytes == null || imageBytes.Length == 0)
            {
                Debug.WriteLine("Rendered image bytes are empty.");
                MessageBox.Show("Failed to render PlantUML code.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (FileStream fileStream = new FileStream("output.png", FileMode.Create))
            {
                fileStream.Write(imageBytes, 0, imageBytes.Length);
            }

            Debug.WriteLine("Successfully rendered PlantUML code.");

            Application.Current.Dispatcher.Invoke(() =>
            {
                using (var stream = new MemoryStream(imageBytes))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    UmlImage.Source = bitmap;
                }
            });

            Debug.WriteLine("Image loaded into Image control.");
            StatusText.Text = "Status: Ready";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error rendering PlantUML code: {ex}");
            MessageBox.Show($"Error rendering PlantUML code: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Status: Error";
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (UmlImage.Source == null)
        {
            Debug.WriteLine("No image to save.");
            MessageBox.Show("No image to save.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SaveFileDialog saveFileDialog = new SaveFileDialog
        {
            Filter = "PNG Image|*.png",
            Title = "Save an Image File",
            RestoreDirectory = true
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                using (FileStream fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create((BitmapSource)UmlImage.Source));
                    encoder.Save(fileStream);
                }

                Debug.WriteLine($"Image saved to {saveFileDialog.FileName}");
                MessageBox.Show($"Image saved to {saveFileDialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving image: {ex}");
                MessageBox.Show($"Error saving image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void UmlImage_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        double zoomFactor = 0.1;
        if (e.Delta > 0)
        {
            ImageScaleTransform.ScaleX += zoomFactor;
            ImageScaleTransform.ScaleY += zoomFactor;
        }
        else
        {
            ImageScaleTransform.ScaleX -= zoomFactor;
            ImageScaleTransform.ScaleY -= zoomFactor;
        }
        e.Handled = true;
    }

    private void UmlImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            _isDragging = true;
            _lastMousePosition = e.GetPosition(ImageCanvas);
            e.Handled = true;
        }
    }

    private void UmlImage_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            Point currentPosition = e.GetPosition(ImageCanvas);
            double deltaX = currentPosition.X - _lastMousePosition.X;
            double deltaY = currentPosition.Y - _lastMousePosition.Y;

            ImageTranslateTransform.X += deltaX;
            ImageTranslateTransform.Y += deltaY;

            _lastMousePosition = currentPosition;
            e.Handled = true;
        }
    }

    private void UmlImage_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            _isDragging = false;
            e.Handled = true;
        }
    }

    private void UmlImage_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ImageScaleTransform.ScaleX = 1;
        ImageScaleTransform.ScaleY = 1;
        ImageTranslateTransform.X = 0;
        ImageTranslateTransform.Y = 0;
        e.Handled = true;
    }
}