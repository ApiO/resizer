using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO;
using Ookii.Dialogs.Wpf;

namespace Resizer
{
    public partial class MainWindow : Window
    {
        private static int num_file_done;
        private static int num_files;
        private ResizerCore resizer;
        private VistaFolderBrowserDialog dialog;
        private Thread thread;


        private void resizer_done(string file)
        {
            num_file_done++;
            Dispatcher.Invoke(new Action(() => { 
                lbl_progress.Content = num_file_done.ToString() + "/" + num_files.ToString();
            }));
        }

        private void resizer_finished()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                bt_resize.IsEnabled = true;
                bt_stop.IsEnabled = false;
            }));
            MessageBox.Show("Resize done", "Mazeltof", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        public MainWindow()
        {
            InitializeComponent();

            dialog = new VistaFolderBrowserDialog();

            num_file_done = 0;

            resizer = new ResizerCore();
            resizer.file_done += resizer_done;
            resizer.finished  += resizer_finished; 

            slider.ValueChanged += slider_ValueChanged;
        }

        private void bt_src_Click(object sender, RoutedEventArgs e)
        {
            dialog.Description = "Please select a folder.";
            dialog.UseDescriptionForTitle = true;
            if ((bool)dialog.ShowDialog(this))
            {
                tb_src.Text = dialog.SelectedPath;
            }
        }

        private void bt_dest_Click(object sender, RoutedEventArgs e)
        {
            dialog.Description = "Please select a folder.";
            dialog.UseDescriptionForTitle = true;
            if ((bool)dialog.ShowDialog(this))
            {
                tb_dest.Text = dialog.SelectedPath;
            }
        }

        private void bt_resize_Click(object sender, RoutedEventArgs e)
        {
            //Src checks
            if (string.IsNullOrWhiteSpace(tb_src.Text))
            {
                MessageBox.Show("Source must not be empty", "Wrong path", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!Directory.Exists(tb_src.Text))
            {
                MessageBox.Show("Source directory does not exist", "Wrong path", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            //Dest checks
            if (string.IsNullOrWhiteSpace(tb_dest.Text))
            {
                MessageBox.Show("Destination must not be empty", "Wrong path", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!Directory.Exists(tb_dest.Text))
            {
                MessageBox.Show("Destination directory does not exist", "Wrong path", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // path comparaisons
            if (cb_recursive.IsChecked == true)
            {
                if (tb_dest.Text == tb_src.Text || tb_dest.Text.Contains(tb_src.Text))
                {
                    MessageBox.Show("The destination path must not be include in source path", "Wrong path", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                if (tb_dest.Text == tb_src.Text)
                {
                    MessageBox.Show("The source path must be different from destination path", "Wrong path", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }


            ResizerContext data = new ResizerContext(Directory.GetFiles(tb_src.Text, "*.JPG"), tb_dest.Text, tb_dest.Text, (int)slider.Value);

            num_file_done = 0;
            num_files = data.files.Length;

            set_ui_state(false);

            thread = new Thread(() => resizer.run(data));
            thread.Start();

        }

        void set_ui_state(bool value)
        {
            bt_resize.IsEnabled = value;
            slider.IsEnabled = value;
            bt_stop.IsEnabled = !value;
            pgrogress_bar.Visibility = value ? System.Windows.Visibility.Hidden : System.Windows.Visibility.Visible;
        }
        
        private void bt_stop_Click(object sender, RoutedEventArgs e)
        {
            set_ui_state(true);
            
            lbl_progress.Content = string.Empty;

            thread.Abort();
        }

        private void bt_close_Click(object sender, RoutedEventArgs e)
        {
            if(thread != null) thread.Abort();
            this.Close();
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lbl_slider.Content = ((int)e.NewValue).ToString() + " %";
        }

    }
}
