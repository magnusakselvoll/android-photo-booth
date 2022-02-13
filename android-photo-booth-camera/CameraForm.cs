using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MagnusAkselvoll.AndroidPhotoBooth.Camera.Logging;
using SharpDX.DirectInput;

namespace MagnusAkselvoll.AndroidPhotoBooth.Camera
{
    public partial class CameraForm : Form
    {
        private bool _downloadLoopRunning;
        private bool _focusLoopRunning;
        private int _lastKnownCounter;
        private DateTime _lastCameraAction;
        private AdbController _adbController;
        private bool _deviceDetected;

        public CameraForm()
        {
            InitializeComponent();

            Logger.MessageLogged += OnMessageLogged;
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);

            bool started = await TryAutoStart();
        }

        private void Reset()
        {
            StopJoystick();
            TryStopDownloadLoop();
            TryStopFocusLoop();

            _adbController = null;
            _deviceDetected = false;
        }

        private async Task<bool> TryAutoStart()
        {
            if (IsDisposed)
            {
                return false;
            }

            AdbController controller = await TryGetController(true, true);

            if (controller == null)
            {
                return false;
            }

            return true;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            Reset();

            Logger.MessageLogged -= OnMessageLogged;
        }

        private void OnMessageLogged(object sender, LogMessage message)
        {
            const int maxLines = 200;
            const int reducedNumberOfLines = 150;

            var minimumLevel = LogMessageLevel.Information;

#if DEBUG
            minimumLevel = LogMessageLevel.Debug;
#endif

            if (message.Level < minimumLevel)
            {
                return;
            }

            if (_logTextBox.Lines.Length >= maxLines)
            {
                var destinationArray = new string[reducedNumberOfLines];
                Array.Copy(_logTextBox.Lines, _logTextBox.Lines.Length - reducedNumberOfLines,
                    destinationArray, 0, destinationArray.Length);

                _logTextBox.Lines = destinationArray;

            }

            _logTextBox.AppendText(GetLogMessageLine(message));
        }

        private string GetLogMessageLine(LogMessage message)
        {
            return $"[{message.TimestampLocal:s}] {message.Level} - {message.Message}{(message.Duration.HasValue ? $" [{(long) message.Duration.Value.TotalMilliseconds} ms" : String.Empty)}]{Environment.NewLine}";
        }

        private async Task<AdbController> TryGetController(bool tryDetectDevice = true, bool silent = false)
        {
            if (_adbController != null)
            {
                return _adbController;
            }

            var adbController = new AdbController(Settings.Default.AdbPath);

            if (!adbController.Validate(out string message))
            {
                if (!silent)
                {
                    ShowBadAdbSettingsDialog(message);
                }

                return null;
            }

            if (tryDetectDevice)
            {
                bool detected = await TryDetectDevice(adbController);

                if (!detected)
                {
                    return null;
                }
            }

            return _adbController = adbController;
        }

        private async Task<bool> TryDetectDevice(AdbController controller, bool forceDetection = false)
        {
            if (_deviceDetected && !forceDetection)
            {
                return true;
            }

            var (connected, device, errorMessage) = await controller.TryConnectToDeviceAsync();

            _deviceTextBox.Text = connected ? device.ToString() : errorMessage;

            return _deviceDetected = connected;
        }


        private async void OnDetectDeviceButtonClickAsync(object sender, EventArgs e)
        {
            AdbController controller = await TryGetController(false);

            if (controller == null)
            {
                return;
            }

            await TryDetectDevice(controller, true);
        }

        private void ShowBadAdbSettingsDialog(string message)
        {
            MessageBox.Show(message, "Incorrect adb settings", MessageBoxButtons.OK);
        }


        private async void OnOpenCameraButtonClickAsync(object sender, EventArgs e)
        {
            await OpenCameraSafely();
        }

        private async Task OpenCameraSafely()
        {
            AdbController controller = await TryGetController();

            if (controller == null)
            {
                return;
            }

            if (!await controller.IsInteractiveAsync())
            {
                await controller.EnableInteractiveAsync();

                int retries = 0;
                bool isInteractive = false;

                while (!isInteractive)
                {
                    await Task.Delay(200);

                    isInteractive = await controller.IsInteractiveAsync();

                    if (++retries >= 5)
                    {
                        break;
                    }
                }

                if (!isInteractive)
                {
                    MessageBox.Show("Unable to activate device screen", "Device not interactive", MessageBoxButtons.OK);
                    return;
                }
            }

            if (await controller.IsLockedAsync())
            {
                await controller.UnlockAsync(Settings.Default.PinCode);

                int retries = 0;
                bool isLocked = true;

                while (isLocked)
                {
                    await Task.Delay(200);

                    isLocked = await controller.IsLockedAsync();

                    if (++retries >= 10)
                    {
                        break;
                    }
                }

                if (isLocked)
                {
                    MessageBox.Show("Unable to unlock device. Is the pin code correct?", "Device locked",
                        MessageBoxButtons.OK);
                    return;
                }
            }

            await controller.OpenCameraAsync();

            UpdateLastCameraAction();
        }

        private CancellationTokenSource _inactivityLockTokenSource = null;

        private void UpdateLastCameraAction()
        {
            _inactivityLockTokenSource?.Cancel();

            _inactivityLockTokenSource = new CancellationTokenSource();

            Task.Run(new Action((async () =>
                {
                    await LockAfterInactivityAsync(_inactivityLockTokenSource.Token);
                })),
                _inactivityLockTokenSource.Token);

            _lastCameraAction = DateTime.UtcNow;
        }

        private async Task LockAfterInactivityAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(Settings.Default.InactivityLockTimeout, cancellationToken);

                Invoke(new Action(async () => { await LockIfNotInteractiveAsync(); }));
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task<bool> LockIfNotInteractiveAsync()
        {
            AdbController controller = await TryGetController();

            if (controller == null)
            {
                return false;
            }

            if (!await controller.IsInteractiveAsync())
            {
                return true;
            }

            await controller.LockAsync();
            return false;
        }

        private void OnFocusButtonClick(object sender, EventArgs e)
        {
            if (TryStopFocusLoop())
            {
                return;
            }

            if (Settings.Default.FocusKeepaliveInterval < TimeSpan.FromSeconds(1))
                MessageBox.Show("At least one second focus keepalive interval must be set", "Too short interval",
                    MessageBoxButtons.OK);

            var totalInterval = Settings.Default.FocusKeepaliveInterval.TotalMilliseconds;
            var intervalStep = (int) Math.Round(totalInterval /
                                                ((double) (_focusProgressBar.Maximum - _focusProgressBar.Minimum) /
                                                 _focusProgressBar.Step));

            _focusTimer.Interval = intervalStep;
            _focusTimer.Start();

            _focusLoopRunning = true;
        }

        private bool TryStopFocusLoop()
        {
            if (_focusLoopRunning)
            {
                _focusTimer.Stop();
                _focusProgressBar.Value = _focusProgressBar.Minimum;
                _focusLoopRunning = false;
                return true;
            }

            return false;
        }

        private void OnSettingsButtonClick(object sender, EventArgs e)
        {
            var settingsForm = new CameraSettingsForm();
            DialogResult result = settingsForm.ShowDialog(this);

            if (result == DialogResult.OK)
            {
                Reset();
            }
        }

        private async void OnFocusTimerTickAsync(object sender, EventArgs e)
        {
            if (_focusProgressBar.Value >= _focusProgressBar.Maximum)
            {
                AdbController controller = await TryGetController();

                if (controller == null)
                {
                    return;
                }

                await controller.FocusCameraAsync();

                UpdateLastCameraAction();

                _focusProgressBar.Value = _focusProgressBar.Minimum;

                return;
            }

            _focusProgressBar.PerformStep();
        }

        private void OnDownloadButtonClick(object sender, EventArgs e)
        {
            if (TryStopDownloadLoop())
            {
                return;
            }

            if (Settings.Default.DownloadImagesInterval < TimeSpan.FromSeconds(1))
            {
                MessageBox.Show("At least one second focus keepalive interval must be set", "Too short interval",
                    MessageBoxButtons.OK);
                return;
            }

            var totalInterval = Settings.Default.DownloadImagesInterval.TotalMilliseconds;
            var intervalStep = (int) Math.Round(totalInterval /
                                                ((double) (_downloadProgressBar.Maximum -
                                                           _downloadProgressBar.Minimum) / _downloadProgressBar.Step));

            _downloadTimer.Interval = intervalStep;
            _downloadTimer.Start();

            _downloadLoopRunning = true;
        }

        private bool TryStopDownloadLoop()
        {
            if (_downloadLoopRunning)
            {
                _downloadTimer.Stop();
                _downloadProgressBar.Value = _downloadProgressBar.Minimum;
                _downloadLoopRunning = false;
                return true;
            }

            return false;
        }

        private async void OnDownloadTimerTickAsync(object sender, EventArgs e)
        {
            if (_downloadProgressBar.Value >= _downloadProgressBar.Maximum)
            {
                try
                {
                    _downloadTimer.Stop();

                    AdbController controller = await TryGetController();

                    if (controller == null)
                    {
                        return;
                    }
                    
                    _lastKnownCounter = await controller.DownloadFilesAsync(_lastKnownCounter);
                }
                finally
                {
                    _downloadProgressBar.Value = _downloadProgressBar.Minimum;
                    _downloadTimer.Start();
                }

                return;
            }

            _downloadProgressBar.PerformStep();
        }

        private async void OnTakeSinglePhotoButtonClickedAsync(object sender, EventArgs e)
        {
            AdbController controller = await TryGetController();

            if (controller == null)
            {
                return;
            }

            if (_lastCameraAction + Settings.Default.CameraOpenTimeout < DateTime.UtcNow
                || !await controller.IsInteractiveAndUnlocked())
            {
                await OpenCameraSafely();

                await Task.Delay(1000);
            }

            await controller.TakeSinglePhotoAsync();

            UpdateLastCameraAction();
        }

        private async Task TakeSinglePhoto()
        {
            AdbController controller = await TryGetController();

            if (controller == null)
            {
                return;
            }

            if (_lastCameraAction + Settings.Default.CameraOpenTimeout < DateTime.UtcNow
                || !await controller.IsInteractiveAndUnlocked())
            {
                await OpenCameraSafely();

                await Task.Delay(1000);
            }

            await controller.TakeSinglePhotoAsync();

            UpdateLastCameraAction();
        }

        private JoystickObserver _joystickObserver = null;
        private JoystickOffset _joystickOffset;

        private void OnStartJoystickButtonClicked(object sender, EventArgs e)
        {
            JoystickInfo joystickInfo = JoystickInfo.ConfiguredJoystick;

            if (joystickInfo == null)
            {
                MessageBox.Show("Ensure that joystick is connected and enter settings", "No joystick configured",
                    MessageBoxButtons.OK);
                return;
            }

            if (String.IsNullOrEmpty(Settings.Default.JoystickButton))
            {
                MessageBox.Show("Ensure that joystick is connected and enter settings and detect the button", "No joystick button configured",
                    MessageBoxButtons.OK);
                return;
            }

            if (!Enum.TryParse(Settings.Default.JoystickButton, out _joystickOffset))
            {
                MessageBox.Show("Ensure that joystick is connected and enter settings and detect the button", "Incorrect joystick button configured",
                    MessageBoxButtons.OK);
                return;
            }

            _startJoystickButton.Enabled = false;

            _joystickObserver = new JoystickObserver(joystickInfo);
            _joystickObserver.OnJoystickUpdate += OnJoystickUpdated;

            _joystickObserver.Start();

            _stopJoystickButton.Enabled = true;
        }

        private void OnJoystickUpdated(object sender, JoystickUpdate update)
        {
            if (update.Offset != _joystickOffset)
            {
                return;
            }

            if (update.Value == 0)
            {
                return; //Button released
            }

            //Invoking click on main thread
            Invoke(new Action(() => { _takeSinglePhotoButton.PerformClick(); }));
        }

        private void OnStopJoystickButtonClicked(object sender, EventArgs e)
        {
            StopJoystick();
        }

        private void StopJoystick()
        {
            _stopJoystickButton.Enabled = false;

            if (_joystickObserver != null)
            {
                _joystickObserver.OnJoystickUpdate -= OnJoystickUpdated;
                _joystickObserver.Dispose();

                _joystickObserver = null;
            }

            _startJoystickButton.Enabled = true;
        }
    }
}