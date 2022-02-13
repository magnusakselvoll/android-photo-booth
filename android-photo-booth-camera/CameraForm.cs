using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MagnusAkselvoll.AndroidPhotoBooth.Camera.Logging;
using SharpDX.DirectInput;

namespace MagnusAkselvoll.AndroidPhotoBooth.Camera
{
    public partial class CameraForm : Form
    {
        private readonly SemaphoreSlim _interactiveCameraActionsSemaphore = new SemaphoreSlim(1, 1);
        private AdbController _adbController;
        private bool _deviceDetected;

        private bool _downloadLoopRunning;
        private bool _focusLoopRunning;

        private CancellationTokenSource _inactivityLockTokenSource;

        private JoystickObserver _joystickObserver;
        private JoystickOffset _joystickOffset;
        private DateTime _lastCameraAction;
        private int _lastKnownCounter;

        public CameraForm()
        {
            InitializeComponent();

            Logger.MessageLogged += OnMessageLogged;
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);

            var started = await TryFullAutoStart();

            Logger.Log(LogMessageLevel.Information,
                started
                    ? "Auto start complete"
                    : "Auto start incomplete - check settings and start services manually.");
        }

        private void Reset()
        {
            StopJoystick();
            TryStopDownloadLoop();
            TryStopFocusLoop();

            _adbController = null;
            _deviceDetected = false;
        }

        private async Task<bool> TryFullAutoStart()
        {
            if (IsDisposed) return false;

            var controller = await TryGetController(true, true);

            if (controller == null) return false;

            var joystickStarted = TryStartJoystick(true);

            var downloadStarted = TryStartDownloadLoop(true);

            return joystickStarted && downloadStarted;
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

            if (message.Level < minimumLevel) return;

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
            return
                $"[{message.TimestampLocal:s}] {message.Level} - {message.Message}{(message.Duration.HasValue ? $" [{(long)message.Duration.Value.TotalMilliseconds} ms]" : string.Empty)}{Environment.NewLine}";
        }

        private async Task<AdbController> TryGetController(bool tryDetectDevice = true, bool silent = false)
        {
            if (_adbController != null) return _adbController;

            var adbController = new AdbController(Properties.Settings.Default.AdbPath);

            if (!adbController.Validate(out var message))
            {
                if (!silent) ShowBadAdbSettingsDialog(message);

                return null;
            }

            if (tryDetectDevice)
            {
                var detected = await TryDetectDevice(adbController);

                if (!detected) return null;
            }

            return _adbController = adbController;
        }

        private async Task<bool> TryDetectDevice(AdbController controller, bool forceDetection = false)
        {
            if (_deviceDetected && !forceDetection) return true;

            var (connected, device, errorMessage) = await controller.TryConnectToDeviceAsync();

            _deviceTextBox.Text = connected ? device.ToString() : errorMessage;

            return _deviceDetected = connected;
        }


        private async void OnDetectDeviceButtonClickAsync(object sender, EventArgs e)
        {
            var controller = await TryGetController(false);

            if (controller == null) return;

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
            var controller = await TryGetController();

            if (controller == null) return;

            if (!await controller.IsInteractiveAsync())
            {
                await controller.EnableInteractiveAsync();

                var retries = 0;
                var isInteractive = false;

                while (!isInteractive)
                {
                    await Task.Delay(200);

                    isInteractive = await controller.IsInteractiveAsync();

                    if (++retries >= 5) break;
                }

                if (!isInteractive)
                {
                    MessageBox.Show("Unable to activate device screen", "Device not interactive", MessageBoxButtons.OK);
                    return;
                }
            }

            if (await controller.IsLockedAsync())
            {
                await controller.UnlockAsync(Properties.Settings.Default.PinCode);

                var retries = 0;
                var isLocked = true;

                while (isLocked)
                {
                    await Task.Delay(200);

                    isLocked = await controller.IsLockedAsync();

                    if (++retries >= 10) break;
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

        private void UpdateLastCameraAction()
        {
            _inactivityLockTokenSource?.Cancel();

            _inactivityLockTokenSource = new CancellationTokenSource();

            Task.Run(new Action(async () => { await LockAfterInactivityAsync(_inactivityLockTokenSource.Token); }),
                _inactivityLockTokenSource.Token);

            _lastCameraAction = DateTime.UtcNow;
        }

        private async Task LockAfterInactivityAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(Properties.Settings.Default.InactivityLockTimeout, cancellationToken);

                Invoke(new Action(async () => { await EnsureLockedAsync(); }));
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task EnsureLockedAsync()
        {
            await WaitInteractiveSemaphoreAsync();

            try
            {
                var controller = await TryGetController();

                if (controller == null) return;

                if (!await controller.IsInteractiveAsync()) return;

                await controller.LockAsync();

                await Task.Delay(500);
            }
            finally
            {
                ReleaseInteractiveSemaphore();
            }
        }

        private void OnFocusButtonClick(object sender, EventArgs e)
        {
            if (TryStopFocusLoop()) return;

            if (Properties.Settings.Default.FocusKeepaliveInterval < TimeSpan.FromSeconds(1))
                MessageBox.Show("At least one second focus keepalive interval must be set", "Too short interval",
                    MessageBoxButtons.OK);

            var totalInterval = Properties.Settings.Default.FocusKeepaliveInterval.TotalMilliseconds;
            var intervalStep = (int)Math.Round(totalInterval /
                                               ((double)(_focusProgressBar.Maximum - _focusProgressBar.Minimum) /
                                                _focusProgressBar.Step));

            _focusTimer.Interval = intervalStep;
            _focusTimer.Start();

            _focusLoopRunning = true;
        }

        private bool TryStopFocusLoop()
        {
            if (!_focusLoopRunning) return false;

            _focusTimer.Stop();
            _focusProgressBar.Value = _focusProgressBar.Minimum;
            _focusLoopRunning = false;

            return true;
        }

        private void OnSettingsButtonClick(object sender, EventArgs e)
        {
            var settingsForm = new CameraSettingsForm();
            var result = settingsForm.ShowDialog(this);

            if (result == DialogResult.OK) Reset();
        }

        private async void OnFocusTimerTickAsync(object sender, EventArgs e)
        {
            if (_focusProgressBar.Value >= _focusProgressBar.Maximum)
            {
                await WaitInteractiveSemaphoreAsync();

                try
                {
                    var controller = await TryGetController();

                    if (controller == null) return;

                    await controller.FocusCameraAsync();

                    UpdateLastCameraAction();

                    _focusProgressBar.Value = _focusProgressBar.Minimum;

                    return;
                }
                finally
                {
                    ReleaseInteractiveSemaphore();
                }
            }

            _focusProgressBar.PerformStep();
        }

        private void OnDownloadButtonClick(object sender, EventArgs e)
        {
            if (TryStopDownloadLoop()) return;

            TryStartDownloadLoop();
        }

        private bool TryStartDownloadLoop(bool silent = false)
        {
            if (_downloadLoopRunning) return false;

            if (Properties.Settings.Default.DownloadImagesInterval < TimeSpan.FromSeconds(1))
            {
                if (!silent)
                    MessageBox.Show("At least one second focus keepalive interval must be set", "Too short interval",
                        MessageBoxButtons.OK);

                return false;
            }

            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.WorkingFolder))
            {
                if (!silent)
                    MessageBox.Show("The working folder for downloads must be set", "Missing working folder",
                        MessageBoxButtons.OK);

                return false;
            }

            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.PublishFolder))
            {
                if (!silent)
                    MessageBox.Show("The publish folder for downloads must be set", "Missing publish folder",
                        MessageBoxButtons.OK);

                return false;
            }

            var totalInterval = Properties.Settings.Default.DownloadImagesInterval.TotalMilliseconds;
            var intervalStep = (int)Math.Round(totalInterval /
                                               ((double)(_downloadProgressBar.Maximum -
                                                         _downloadProgressBar.Minimum) / _downloadProgressBar.Step));

            _downloadTimer.Interval = intervalStep;
            _downloadTimer.Start();

            _downloadLoopRunning = true;

            return true;
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

                    var controller = await TryGetController();

                    if (controller == null) return;

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
            await WaitInteractiveSemaphoreAsync();

            try
            {
                var sw = Stopwatch.StartNew();
                _takeSinglePhotoButton.Enabled = false;

                var countdown = new Countdown(Properties.Settings.Default.Countdown);
                countdown.OnCountdownTick += OnCountdownTick;
                countdown.Start();

                var controller = await TryGetController();

                if (controller == null) return;

                if (_lastCameraAction + Properties.Settings.Default.CameraOpenTimeout < DateTime.UtcNow
                    || !await controller.IsInteractiveAndUnlocked())
                {
                    await OpenCameraSafely();

                    await Task.Delay(1000);
                }

                
                TimeSpan timeToWait = countdown.TimeRemaining - TimeSpan.FromMilliseconds(Properties.Settings.Default.AdjustmentCountdownMS);

                if (timeToWait > TimeSpan.Zero)
                {

                    Logger.Log(LogMessageLevel.Debug, $"Waiting {(int) timeToWait.TotalMilliseconds}ms for countdown to finish ({Properties.Settings.Default.AdjustmentCountdownMS}ms adjustment)");
                    await Task.Delay(timeToWait);
                }


                await controller.TakeSinglePhotoAsync();

                UpdateLastCameraAction();

                _takeSinglePhotoButton.Enabled = true;

                Logger.Log(LogMessageLevel.Information, "Photo taken", sw.Elapsed);
            }
            finally
            {
                ReleaseInteractiveSemaphore();
            }
        }

        private void OnCountdownTick(object sender, int secondsRemaining)
        {
            Invoke(new Action(() => { Logger.Log(LogMessageLevel.Information, $"Countdown: {secondsRemaining}"); }));
        }

        private void ReleaseInteractiveSemaphore()
        {
            _interactiveCameraActionsSemaphore.Release();
        }

        private async Task WaitInteractiveSemaphoreAsync()
        {
            await _interactiveCameraActionsSemaphore.WaitAsync();
        }

        private void OnStartJoystickButtonClicked(object sender, EventArgs e)
        {
            TryStartJoystick();
        }

        private bool TryStartJoystick(bool silent = false)
        {
            var joystickInfo = JoystickInfo.ConfiguredJoystick;

            if (joystickInfo == null)
            {
                if (!silent)
                    MessageBox.Show("Ensure that joystick is connected and enter settings", "No joystick configured",
                        MessageBoxButtons.OK);

                return false;
            }

            if (string.IsNullOrEmpty(Properties.Settings.Default.JoystickButton))
            {
                if (!silent)
                    MessageBox.Show("Ensure that joystick is connected and enter settings and detect the button",
                        "No joystick button configured",
                        MessageBoxButtons.OK);
                return false;
            }

            if (!Enum.TryParse(Properties.Settings.Default.JoystickButton, out _joystickOffset))
            {
                if (!silent)
                    MessageBox.Show("Ensure that joystick is connected and enter settings and detect the button",
                        "Incorrect joystick button configured",
                        MessageBoxButtons.OK);

                return false;
            }

            _startJoystickButton.Enabled = false;

            _joystickObserver = new JoystickObserver(joystickInfo);
            _joystickObserver.OnJoystickUpdate += OnJoystickUpdated;

            _joystickObserver.Start();

            _stopJoystickButton.Enabled = true;

            return true;
        }

        private void OnJoystickUpdated(object sender, JoystickUpdate update)
        {
            if (update.Offset != _joystickOffset) return;

            if (update.Value == 0) return; //Button released

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