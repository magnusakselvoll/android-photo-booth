﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using MagnusAkselvoll.AndroidPhotoBooth.App.Properties;

namespace MagnusAkselvoll.AndroidPhotoBooth.App
{
    internal enum InterruptReason
    {
        None,
        GoPrevious,
        GoNext,
        PausePlay
    }
    internal sealed class SlideshowControl
    {
        public EventHandler<ImageChosenEventArgs> ImageChosen;
        public EventHandler<UnhandledExceptionEventArgs> UnhandledExceptionThrown;
        public Settings Settings { get; }
        private CancellationTokenSource InternalCancellationTokenSource { get; set; }
        private InterruptReason InterruptType { get; set; }

        private readonly HashSet<string> _extensions;
        private readonly Random _random = new Random();
        private DirectoryInfo _directory;
        private List<FileInfo> _files;
        private readonly Queue<FileInfo> _newFiles = new Queue<FileInfo>();


        public SlideshowControl(Settings settings)
        {
            Settings = settings;

            _extensions = GetExtensionsHashset();
        }

        private void Interrupt(InterruptReason reason)
        {
            if (InternalCancellationTokenSource == null)
            {
                return;
            }

            lock (this)
            {
                InterruptType = reason;
            }

            InternalCancellationTokenSource.Cancel();
        }

        private InterruptReason HandleInterrupt()
        {
            lock (this)
            {
                var reason = InterruptType;
                InterruptType = InterruptReason.None;
                return reason;
            }
        }

        public void GoNext()
        {
            Interrupt(InterruptReason.GoNext);
        }

        public void GoPrevious()
        {
            Interrupt(InterruptReason.GoPrevious);
        }

        public void PausePlay()
        {
            Interrupt(InterruptReason.PausePlay);
        }

        public void Start(CancellationToken externalCancellationToken)
        {
            CancellationToken cancellationToken = ResetCancellationToken(externalCancellationToken);

            uint previousExecutionState = 0;

            try
            {
                previousExecutionState = NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS |
                                                      NativeMethods.ES_DISPLAY_REQUIRED |
                                                      NativeMethods.ES_SYSTEM_REQUIRED);

                var history = new HistoryQueue<FileInfo>(1000);
                int? behindInHistory = null;
                bool userNavigated = false;

                _directory = new DirectoryInfo(Settings.PictureFolder);
                RefreshFiles();

                var watcher = new FileSystemWatcher(_directory.FullName)
                {
                    IncludeSubdirectories = Settings.Recursive,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
                };

                watcher.Deleted += FilesDeleted;
                watcher.Renamed += FilesRenamed;
                watcher.Created += FilesCreated;

                watcher.EnableRaisingEvents = true;

                while (true)
                {

                    FileInfo fileInfo;
                    bool newFile = false;

                    if (behindInHistory.HasValue)
                    {
                        if (behindInHistory.Value >= history.Count)
                        {
                            behindInHistory = history.Count - 1;
                        }

                        if (!userNavigated)
                        {
                            if (--behindInHistory < 0)
                            {
                                behindInHistory = 0;
                            }
                        }

                        fileInfo = history[history.Count - 1 - behindInHistory.Value];

                        if (behindInHistory.Value == 0)
                        {
                            behindInHistory = null;
                        }
                    }

                    else
                    {
                        newFile = _newFiles.Count > 0;
                        fileInfo = newFile ? _newFiles.Dequeue() : GetRandomFile(_files);
                        history.Enqueue(fileInfo);
                    }

                    Image image;
                    try
                    {
                        image = ReadImage(fileInfo);
                    }
                    catch (Exception e)
                    {
                        using (EventLog eventLog = new EventLog("Application"))
                        {
                            eventLog.Source = "Application";
                            eventLog.WriteEntry(
                                $"flashair -slideshow: Unable to parse file '{fileInfo.FullName}' as image. Exception: {e}",
                                EventLogEntryType.Warning);
                        }

                        image = null; //Continuing to next image
                    }

                    if (image == null)
                    {
                        RefreshFiles(true);
                        continue;
                    }

                    DateTime imageDisplayed = DateTime.Now;
                    FireImageChosen(image, fileInfo.Name);

                    if (newFile && _newFiles.Count == 0) //If last new file, refresh list of files
                    {
                        RefreshFiles();
                    }

                    DateTime maxDisplayUntil = imageDisplayed.AddSeconds(Settings.MaximumDisplaySeconds);
                    DateTime minDisplayUntil = imageDisplayed.AddSeconds(Settings.MinimumDisplaySeconds);
                    bool interrupted = false;
                    bool paused = false;
                    while (!interrupted && (maxDisplayUntil > DateTime.Now || paused))
                    {
                        if (_newFiles.Count > 0 && minDisplayUntil < DateTime.Now)
                        {
                            break;
                        }

                        int millisecondsToSleep = Math.Min(Settings.ClockIntervalMilliseconds,
                            (int) Math.Ceiling((maxDisplayUntil - DateTime.Now).TotalMilliseconds));

                        bool cancelled =
                            cancellationToken.WaitHandle.WaitOne(millisecondsToSleep);

                        userNavigated = false;

                        if (cancelled)
                        {
                            InterruptReason internalInterrupt = HandleInterrupt();
                            switch (internalInterrupt)
                            {
                                case InterruptReason.None: //The cancellation is external
                                    return;
                                case InterruptReason.GoPrevious:
                                    if (!behindInHistory.HasValue)
                                    {
                                        behindInHistory = 0;
                                    }

                                    behindInHistory++;
                                    userNavigated = true;
                                    interrupted = true;
                                    break;
                                case InterruptReason.GoNext:
                                    if (behindInHistory.HasValue)
                                    {
                                        if (--behindInHistory < 0)
                                        {
                                            behindInHistory = 0;
                                        }

                                        userNavigated = true;
                                    }

                                    interrupted = true;
                                    break;
                                case InterruptReason.PausePlay:
                                    paused = !paused;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                            cancellationToken = ResetCancellationToken(externalCancellationToken);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                UnhandledExceptionThrown?.Invoke(this, new UnhandledExceptionEventArgs(e, true));

                throw;
            }
            finally
            {
                NativeMethods.SetThreadExecutionState(previousExecutionState);
            }
        }

        private CancellationToken ResetCancellationToken(CancellationToken externalCancellationToken)
        {
            InternalCancellationTokenSource = new CancellationTokenSource();
            var linkedCancellationSource =
                CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken,
                    InternalCancellationTokenSource.Token);

            var cancellationToken = linkedCancellationSource.Token;
            return cancellationToken;
        }

        private void FilesCreated(object sender, FileSystemEventArgs e)
        {
            string extension = Path.GetExtension(e.Name);

            if (!_extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            _newFiles.Enqueue(new FileInfo(e.FullPath));
        }

        private void FilesRenamed(object sender, RenamedEventArgs e)
        {
            RefreshFiles();
        }

        private void FilesDeleted(object sender, FileSystemEventArgs e)
        {
            RefreshFiles();
        }

        DateTime _lastRefresh = DateTime.MinValue;

        private void RefreshFiles(bool force = false)
        {
            if (force || _lastRefresh.AddMinutes(1) < DateTime.Now)
            {
                _lastRefresh = DateTime.Now;
                _files = GetFiles(_directory);
            }
        }

        private static Image ReadImage(FileInfo fileInfo)
        {
            
            if (!fileInfo.Exists)
            {
                return null;
            }

            Image clonedImage;

            using (var image = new Bitmap(fileInfo.FullName))
            {
                foreach (var prop in image.PropertyItems)
                {
                    if (prop.Id == 0x0112)
                    {
                        int orientationValue = prop.Value[0];
                        RotateFlipType rotateFlipType = GetOrientationToFlipType(orientationValue);
                        image.RotateFlip(rotateFlipType);
                        break;
                    }
                }

                clonedImage = new Bitmap(image);
            }

            return clonedImage;
        }

        private static RotateFlipType GetOrientationToFlipType(int orientationValue)
        {
            RotateFlipType rotateFlipType;

            switch (orientationValue)
            {
                case 1:
                    rotateFlipType = RotateFlipType.RotateNoneFlipNone;
                    break;
                case 2:
                    rotateFlipType = RotateFlipType.RotateNoneFlipX;
                    break;
                case 3:
                    rotateFlipType = RotateFlipType.Rotate180FlipNone;
                    break;
                case 4:
                    rotateFlipType = RotateFlipType.Rotate180FlipX;
                    break;
                case 5:
                    rotateFlipType = RotateFlipType.Rotate90FlipX;
                    break;
                case 6:
                    rotateFlipType = RotateFlipType.Rotate90FlipNone;
                    break;
                case 7:
                    rotateFlipType = RotateFlipType.Rotate270FlipX;
                    break;
                case 8:
                    rotateFlipType = RotateFlipType.Rotate270FlipNone;
                    break;
                default:
                    rotateFlipType = RotateFlipType.RotateNoneFlipNone;
                    break;
            }

            return rotateFlipType;
        }

        private FileInfo GetRandomFile(IList<FileInfo> files)
        {
            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }
            if (files.Count == 0)
            {
                throw new ArgumentException(@"At least one file required", nameof(files));
            }

            return files[_random.Next(files.Count - 1)];
        }

        private List<FileInfo> GetFiles(DirectoryInfo directory)
        {
            FileInfo[] allFiles = directory.GetFiles("*", Settings.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            List<FileInfo> files = allFiles.Where(x => _extensions.Contains(x.Extension, StringComparer.OrdinalIgnoreCase))
                .ToList();
            return files;
        }

        private HashSet<string> GetExtensionsHashset()
        {
            var hashSet = new HashSet<string>();

            foreach (string extension in Settings.FilenameExtensions)
            {
                hashSet.Add(extension);
            }

            return hashSet;
        }

        private void FireImageChosen(Image image, string fileName)
        {
            ImageChosen?.Invoke(this, new ImageChosenEventArgs(image, fileName));
        }
    }
}