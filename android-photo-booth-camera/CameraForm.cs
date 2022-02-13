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
        private readonly SemaphoreSlim _downloadSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _interactiveCameraActionsSemaphore = new SemaphoreSlim(1, 1);
        private AdbController _adbController;
        private bool _deviceDetected;
        private CancellationTokenSource _downloadCancellationTokenSource;

        private CancellationTokenSource _inactivityLockTokenSource;

        private JoystickObserver _joystickObserver;
        private JoystickOffset _joystickOffset;
        private DateTime _lastCameraAction;
        private DateTime _lastDownloadInitiated = DateTime.MinValue;
        private int _lastKnownCounter;

        public event EventHandler<int> OnCountdownChanged; 
        public event EventHandler OnCountdownTerminated; 

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

        private async Task ResetAsync()
        {
            StopJoystick();

            await CancelDownloadTasksAsync();

            _adbController = null;
            _deviceDetected = false;
        }

        private async Task CancelDownloadTasksAsync()
        {
            if (_downloadCancellationTokenSource != null)
            {
                //Trying to reasonably ensure that no tasks are currently downloading
                var gotSemaphore = await _downloadSemaphore.WaitAsync(TimeSpan.FromSeconds(30));
                
                _downloadCancellationTokenSource.Cancel();
                _downloadCancellationTokenSource = null;

                if (gotSemaphore)
                {
                    _downloadSemaphore.Release();
                }
            }
        }

        private async Task<bool> TryFullAutoStart()
        {
            if (IsDisposed) return false;

            var controller = await TryGetController(true, true);

            if (controller == null) return false;

            return TryStartJoystick(true);
        }

        protected override async void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            await ResetAsync();

            Logger.MessageLogged -= OnMessageLogged;
        }

        private void OnMessageLogged(object sender, LogMessage message)
        {
            const int maxLines = 200;
            const int reducedNumberOfLines = 150;

            // ReSharper disable once RedundantAssignment
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
            MessageBox.Show(message, @"Incorrect adb settings", MessageBoxButtons.OK);
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
                    MessageBox.Show(@"Unable to activate device screen", @"Device not interactive",
                        MessageBoxButtons.OK);
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
                    MessageBox.Show(@"Unable to unlock device. Is the pin code correct?", @"Device locked",
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

        private async void OnSettingsButtonClick(object sender, EventArgs e)
        {
            var settingsForm = new CameraSettingsForm();
            var result = settingsForm.ShowDialog(this);

            if (result == DialogResult.OK)
            {
                await ResetAsync();
            }
        }

        private async Task EnsureDownloadAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            try
            {
                var initiateDownload = DateTime.UtcNow + delay;

                if (delay > TimeSpan.Zero) await Task.Delay(delay, cancellationToken);

                await _downloadSemaphore.WaitAsync(cancellationToken);

                try
                {
                    if (_lastDownloadInitiated > initiateDownload)
                        //Someone else has downloaded while waiting for semaphore
                        return;

                    _lastDownloadInitiated = DateTime.UtcNow;

                    var controller = await TryGetController();

                    if (controller == null) return;

                    _lastKnownCounter = await controller.DownloadFilesAsync(_lastKnownCounter);
                }
                finally
                {
                    _downloadSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
            }
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
                countdown.OnCountdownComplete += OnCountdownZero;
                countdown.Start();

                var controller = await TryGetController();

                if (controller == null) return;

                if (_lastCameraAction + Properties.Settings.Default.CameraOpenTimeout < DateTime.UtcNow
                    || !await controller.IsInteractiveAndUnlocked())
                {
                    await OpenCameraSafely();

                    await Task.Delay(1000);
                }


                var timeToWait = countdown.TimeRemaining -
                                 TimeSpan.FromMilliseconds(Properties.Settings.Default.AdjustmentCountdownMS);

                if (timeToWait > TimeSpan.Zero)
                {
                    Logger.Log(LogMessageLevel.Debug,
                        $"Waiting {(int)timeToWait.TotalMilliseconds}ms for countdown to finish ({Properties.Settings.Default.AdjustmentCountdownMS}ms adjustment)");
                    await Task.Delay(timeToWait);
                }


                await controller.TakeSinglePhotoAsync();

                UpdateLastCameraAction();

                if (_downloadCancellationTokenSource == null)
                    _downloadCancellationTokenSource = new CancellationTokenSource();


#pragma warning disable CS4014
                //Not waiting for tasks to complete. Relying on cancellation tokens.
                //Subsequent tasks added in case device is slow.
                EnsureDownloadAsync(TimeSpan.FromMilliseconds(1500), _downloadCancellationTokenSource.Token);
                EnsureDownloadAsync(TimeSpan.FromMilliseconds(2000), _downloadCancellationTokenSource.Token);
                EnsureDownloadAsync(TimeSpan.FromMilliseconds(4000), _downloadCancellationTokenSource.Token);
                EnsureDownloadAsync(TimeSpan.FromSeconds(10), _downloadCancellationTokenSource.Token);
                EnsureDownloadAsync(TimeSpan.FromSeconds(30), _downloadCancellationTokenSource.Token);
#pragma warning restore CS4014

                _takeSinglePhotoButton.Enabled = true;

                Logger.Log(LogMessageLevel.Information, "Photo taken", sw.Elapsed);
            }
            finally
            {
                ReleaseInteractiveSemaphore();
            }
        }

        private void OnCountdownZero(object sender, EventArgs e)
        {
            Invoke(new Action(() =>
            {
                Logger.Log(LogMessageLevel.Debug, "Countdown complete");
                OnCountdownTerminated?.Invoke(this, EventArgs.Empty);
            }));
        }

        private void OnCountdownTick(object sender, int secondsRemaining)
        {
            Invoke(new Action(() =>
            {
                Logger.Log(LogMessageLevel.Information, $"Countdown: {secondsRemaining}");
                OnCountdownChanged?.Invoke(this, secondsRemaining);
            }));
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
                    MessageBox.Show(@"Ensure that joystick is connected and enter settings", @"No joystick configured",
                        MessageBoxButtons.OK);

                return false;
            }

            if (string.IsNullOrEmpty(Properties.Settings.Default.JoystickButton))
            {
                if (!silent)
                    MessageBox.Show(@"Ensure that joystick is connected and enter settings and detect the button",
                        @"No joystick button configured",
                        MessageBoxButtons.OK);
                return false;
            }

            if (!Enum.TryParse(Properties.Settings.Default.JoystickButton, out _joystickOffset))
            {
                if (!silent)
                    MessageBox.Show(@"Ensure that joystick is connected and enter settings and detect the button",
                        @"Incorrect joystick button configured",
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