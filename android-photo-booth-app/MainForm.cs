using System;
using System.Windows.Forms;
using MagnusAkselvoll.AndroidPhotoBooth.Camera;

namespace MagnusAkselvoll.AndroidPhotoBooth.App
{
    public partial class MainForm : Form
    {
        private CameraForm _cameraForm;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadSettings();
        }

        private void OnBrowseButtonClick(object sender, EventArgs e)
        {
            _folderBrowserDialog.SelectedPath = _pictureFolder.Text;
            var result = _folderBrowserDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                _pictureFolder.Text = _folderBrowserDialog.SelectedPath;
            }
        }

        private void OnStartButtonClick(object sender, EventArgs e)
        {
            SaveSettings();

            StartSlideShow();
        }

        private void StartSlideShow()
        {
            var form = new PictureForm(Settings.Default);
            form.ShowDialog(this);
        }

        private void LoadSettings()
        {
            var settings = Settings.Default;

            _pictureFolder.Text = settings.PictureFolder;
            _minimumDisplayTime.Value = settings.MinimumDisplaySeconds;
            _maximumDisplayTime.Value = settings.MaximumDisplaySeconds;
            _showFilenames.Checked = settings.ShowFileNames;
        }


        private void SaveSettings()
        {
            var settings = Settings.Default;

            settings.PictureFolder = _pictureFolder.Text;
            settings.MinimumDisplaySeconds = (int) _minimumDisplayTime.Value;
            settings.MaximumDisplaySeconds = (int) _maximumDisplayTime.Value;
            settings.ShowFileNames = _showFilenames.Checked;

            settings.Save();
        }

        private void OnCameraButtonClick(object sender, EventArgs e)
        {
            _openCameraButton.Enabled = false;

            _cameraForm = new CameraForm();
            _cameraForm.FormClosed += OnCameraFormClosed;

            _cameraForm.Show();
        }

        private void OnCameraFormClosed(object sender, FormClosedEventArgs e)
        {
            _cameraForm?.Dispose();
            _cameraForm = null;

            _openCameraButton.Enabled = true;
        }
    }
}
