namespace WpfPhotoAdministrator
{
    using Microsoft.Win32;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<FileTransform> files = new List<FileTransform>();
        private List<string> arrHeaders = new List<string>();
        private string folder;
        private const string DateTakenAtttribute = "Date taken";
        private string[] defaultAttributes =  { "Name" };

        private class FileTransform { 
            public string OriginalPath { get; set; }
            public string NewFileName{ get; set; }
            public DateTime NewModifiedTime { get; set; }
            public string NewFilePath
            {
                get
                {
                    return System.IO.Path.GetDirectoryName(OriginalPath) +"\\"+ NewFileName;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog filedialog = new OpenFileDialog();

            // Show dialog and take result into account
            bool? result = filedialog.ShowDialog();
            if (!result.GetValueOrDefault())
                return;
            folder = System.IO.Path.GetDirectoryName(filedialog.FileName);

            OpenFolder(result);
        }


        private void listBoxFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Shell32.Shell shell = new Shell32.Shell();
            Shell32.Folder objFolder;

            objFolder = shell.NameSpace(folder);
            listBoxPreview.Items.Clear();
            var item = objFolder.Items().Item(listBoxFiles.SelectedIndex);
            for (int i = 0; i < arrHeaders.Count; i++)
            {
//                listBoxPreview.Items.Add(String.Format("{0}\t{1}: {2}", i, arrHeaders[i], objFolder.GetDetailsOf(item, i)));
            }

        }

        private void buttonProcess_Click(object sender, RoutedEventArgs e)
        {
            prepareFiles();
        }

        private void buttonApply_Click(object sender, RoutedEventArgs e)
        {
            foreach (var fileTransform in files)
            {
                if (System.IO.Directory.Exists(fileTransform.NewFilePath))
                {
                    continue;
                }
                    if (fileTransform.NewFilePath != fileTransform.OriginalPath)
                {
                    int i = 1;
                    var newFileName = System.IO.Path.GetFileNameWithoutExtension(fileTransform.NewFileName);
                    var fileExt = System.IO.Path.GetExtension(fileTransform.NewFileName);

                    while (System.IO.File.Exists(fileTransform.NewFilePath))
                    {
                        fileTransform.NewFileName = $"{newFileName} ({i++}){fileExt}";
                    }
                    System.IO.File.Move(fileTransform.OriginalPath, fileTransform.NewFilePath);
                }

                System.IO.File.SetLastWriteTime(fileTransform.NewFilePath, fileTransform.NewModifiedTime);
            }
            MessageBox.Show("Processing Complete");
            OpenFolder(true);
        }

        private void OpenFolder(bool? result)
        {
            files.Clear();
            listBoxFiles.Items.Clear();

            arrHeaders.Clear();
            checklistBoxProperties.Items.Clear();
            checklistBoxProperties.SelectedItems.Clear();

            labelDirectoryPath.Text = folder;
            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                Shell32.Shell shell = new Shell32.Shell();
                Shell32.Folder objFolder;

                objFolder = shell.NameSpace(folder);

                for (int i = 0; i < short.MaxValue; i++)
                {
                    string header = objFolder.GetDetailsOf(null, i);
                    if (String.IsNullOrEmpty(header))
                        break;
                    arrHeaders.Add(header);
                    checklistBoxProperties.Items.Add(header);
                }

                foreach (string attribute in defaultAttributes)
                {
                    var itemIndex = checklistBoxProperties.Items.IndexOf(attribute);
                    if (itemIndex > -1)
                    {
                        checklistBoxProperties.SelectedItems.Add(checklistBoxProperties.Items[itemIndex]);
                    }
                }

                var items = objFolder.Items();
                var itemCount = items.Count;
                foreach (Shell32.FolderItem2 item in items)
                {
                    var filename = System.IO.Path.GetFileName(item.Path);
                    listBoxFiles.Items.Add(filename);
                    files.Add(new FileTransform { OriginalPath = item.Path });
                }
            }
        }
        
        private void checkBox_Checked(object sender, RoutedEventArgs e)
        {
            textBoxReplace.IsEnabled = checkBox.IsChecked.GetValueOrDefault(false);
            textBoxReplaceWith.IsEnabled = checkBox.IsChecked.GetValueOrDefault(false);
        }

        private void prepareFiles()
        {
            if (!files.Any())
                return;

            Shell32.Shell shell = new Shell32.Shell();
            Shell32.Folder objFolder;

            objFolder = shell.NameSpace(folder);
            listBoxPreview.Items.Clear();
            var fileProperties = new List<string>();
            for (int i = 0; i < objFolder.Items().Count; i++)
            {
                var file = objFolder.Items().Item(i);
                fileProperties.Clear();
                var fileTransform = files[i];
                foreach (var selectedItem in checklistBoxProperties.SelectedItems)
                {
                    int propertyIndex = arrHeaders.IndexOf(selectedItem.ToString());
                    string fileProperty = objFolder.GetDetailsOf(file, propertyIndex);

                    if (selectedItem.ToString() == "Name" && checkBox.IsChecked.GetValueOrDefault() && !String.IsNullOrEmpty(textBoxReplace.Text))
                    {
                        try
                        {
                            fileProperty = Regex.Replace(fileProperty, textBoxReplace.Text, textBoxReplaceWith.Text);
                        }
                        catch
                        {
                            
                        }
                    }
                    if (string.IsNullOrEmpty(fileProperty))
                    {
                        fileProperty = "Unknown";
                    }

                    fileProperties.Add(fileProperty.Trim());
                }

                string extension = System.IO.Path.GetExtension(fileTransform.OriginalPath);
                fileTransform.NewFileName = string.Join(" - ", fileProperties) + extension;
                listBoxPreview.Items.Add(fileTransform.NewFileName);

                int dateTakenIndex = arrHeaders.IndexOf(DateTakenAtttribute);
                var rawDateTaken = objFolder.GetDetailsOf(file, dateTakenIndex);

                if (!string.IsNullOrEmpty(rawDateTaken))
                {
                    var dateTaken = Regex.Replace(rawDateTaken, @"[^\w\. /:\\]", "");
                    fileTransform.NewModifiedTime = DateTime.Parse(dateTaken);
                }
                else
                {
                    fileTransform.NewModifiedTime = System.IO.File.GetLastWriteTime(fileTransform.OriginalPath);
                }
            }

        }

        private void toolBar_Loaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            if (overflowGrid != null)
            {
                overflowGrid.Visibility = Visibility.Collapsed;
            }
            var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;
            if (mainPanelBorder != null)
            {
                mainPanelBorder.Margin = new Thickness();
            }

        }

        private void ButtonPreview_Click(object sender, RoutedEventArgs e)
        {
            prepareFiles();
        }
    }
}
