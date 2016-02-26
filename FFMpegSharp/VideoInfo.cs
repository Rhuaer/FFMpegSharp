﻿using FFMpegSharp.Enums;
using FFMpegSharp.FFMPEG;
using FFMpegSharp.FFMPEG.Enums;
using FFMpegSharp.FFMPEG.Extend;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace FFMpegSharp
{
    public partial class VideoInfo
    {
        private FFMpeg ffmpeg = null;
        private FFMpeg FFmpeg
        {
            get
            {
                if (ffmpeg != null && ffmpeg.IsWorking)
                    throw new InvalidOperationException("Another operation is in progress, please wait for this to finish before launching another operation. To do multiple operations, create another VideoInfo object targeting that file.");

                return ffmpeg ?? (ffmpeg = new FFMpeg());
            }
        }
        private FileInfo file;

        /// <summary>
        /// Returns the percentage of the current conversion progress.
        /// </summary>
        public ConversionHandler OnConversionProgress;

        /// <summary>
        /// Duration of the video file.
        /// </summary>
        public TimeSpan Duration { get; internal set; }
        /// <summary>
        /// Audio format of the video file.
        /// </summary>
        public string AudioFormat { get; internal set; }
        /// <summary>
        /// Video format of the video file.
        /// </summary>
        public string VideoFormat { get; internal set; }
        /// <summary>
        /// Aspect ratio.
        /// </summary>
        public string Ratio { get; internal set; }
        /// <summary>
        /// Video frame rate.
        /// </summary>
        public double FrameRate { get; internal set; }
        /// <summary>
        /// Height of the video file.
        /// </summary>
        public int Height { get; internal set; }
        /// <summary>
        /// Width of the video file.
        /// </summary>
        public int Width { get; internal set; }
        /// <summary>
        /// Video file size in MegaBytes (MB).
        /// </summary>
        public double Size { get; internal set; }

        /// <summary>
        /// Create a video information object from a file information object.
        /// </summary>
        /// <param name="fileInfo">Video file information.</param>
        /// <returns></returns>
        public static VideoInfo FromFileInfo(FileInfo fileInfo)
        {
            return FromPath(fileInfo.FullName);
        }

        /// <summary>
        /// Create a video information object from a target path.
        /// </summary>
        /// <param name="path">Path to video.</param>
        /// <returns></returns>
        public static VideoInfo FromPath(string path)
        {
            return new VideoInfo(path);
        }

        /// <summary>
        /// Create a video information object from a file information object.
        /// </summary>
        /// <param name="fileInfo">Video file information.</param>
        public VideoInfo(FileInfo fileInfo)
        {
            fileInfo.Refresh();

            if(!fileInfo.Exists)
                throw new ArgumentException(string.Format("Input file {0} does not exist!", fileInfo.FullName));

            file = fileInfo;

            new FFProbe().ParseVideoInfo(this);
        }

        /// <summary>
        /// Create a video information object from a target path.
        /// </summary>
        /// <param name="path">Path to video.</param>
        public VideoInfo(string path) : this(new FileInfo(path)) { }

        /// <summary>
        /// Pretty prints the video information.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Video Path : " + FullName + Environment.NewLine +
                   "Video Root : " + Directory.FullName + Environment.NewLine +
                   "Video Name: " + Name + Environment.NewLine +
                   "Video Extension : " + Extension + Environment.NewLine +
                   "Video Duration : " + Duration + Environment.NewLine +
                   "Audio Format : " + AudioFormat + Environment.NewLine +
                   "Video Format : " + VideoFormat + Environment.NewLine +
                   "Aspect Ratio : " + Ratio + Environment.NewLine +
                   "Framerate : " + FrameRate + "fps" + Environment.NewLine +
                   "Resolution : " + Width + "x" + Height + Environment.NewLine +
                   "Size : " + Size + " MB";
        }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        public string Name { get { return file.Name; } }

        /// <summary>
        /// Gets the full path of the file.
        /// </summary>
        public string FullName { get { return file.FullName; } }

        /// <summary>
        /// Gets the file extension.
        /// </summary>
        public string Extension { get { return file.Extension; } }

        /// <summary>
        /// Gets a flag indicating if the file is read-only.
        /// </summary>
        public bool IsReadOnly { get { return file.IsReadOnly; } }

        /// <summary>
        /// Gets a flag indicating if the file exists (no cache, per call verification).
        /// </summary>
        public bool Exists { get { return File.Exists(FullName); } }

        /// <summary>
        /// Gets the creation date.
        /// </summary>
        public DateTime CreationTime { get { return file.CreationTime; } }

        /// <summary>
        /// Gets the parent directory information.
        /// </summary>
        public DirectoryInfo Directory { get { return file.Directory; } }

        /// <summary>
        /// Open a file stream.
        /// </summary>
        /// <param name="mode">Opens a file in a specified mode.</param>
        /// <returns>File stream of the video file.</returns>
        public FileStream FileOpen(FileMode mode)
        {
            return file.Open(mode);
        }

        /// <summary>
        /// Move file to a specific directory.
        /// </summary>
        /// <param name="destination"></param>
        public void MoveTo(DirectoryInfo destination)
        {
            string newLocation = string.Format("{0}\\{1}{2}", destination.FullName, Name, Extension);
            file.MoveTo(newLocation);
            file = new FileInfo(newLocation);
        }

        /// <summary>
        /// Delete the file.
        /// </summary>
        public void Delete()
        {
            file.Delete();
        }

        /// <summary>
        /// Convert file to a specified format.
        /// </summary>
        /// <param name="type">Output format.</param>
        /// <param name="output">Output location.</param>
        /// <param name="speed">MP4 encoding speed (applies only to mp4 format). Faster results in lower quality.</param>
        /// <param name="size">Aspect ratio of the output video file.</param>
        /// <param name="audio">Audio quality of the output video file.</param>
        /// <param name="multithread">Tell FFMpeg to use multithread in the conversion process.</param>
        /// <param name="tryToPurge">Flag original file purging after conversion is done (Will not result in exception if file is readonly or missing.).</param>
        /// <returns>Video information object with the new video file.</returns>
        public VideoInfo ConvertTo(VideoType type, FileInfo output, Speed speed = Speed.SuperFast, VideoSize size = VideoSize.Original, AudioQuality audio = AudioQuality.Normal, bool multithread = false, bool tryToPurge = false)
        {
            bool success = false;
            FFmpeg.OnProgress += OnConversionProgress;
            switch (type)
            {
                case VideoType.MP4: success = FFmpeg.ToMP4(this, output, speed, size, audio, multithread); break;
                case VideoType.OGV: success = FFmpeg.ToOGV(this, output, size, audio, multithread); break;
                case VideoType.WebM: success = FFmpeg.ToWebM(this, output, size, audio); break;
                case VideoType.TS: success = FFmpeg.ToTS(this, output); break;
                default: throw new ArgumentException("Video type is not supported yet!");
            }

            if (!success)
                throw new OperationCanceledException("The conversion process could not be completed.");

            if (tryToPurge)
            {
                try
                {
                    if (this.Exists)
                        this.Delete();
                }
                catch { }
            }

            FFmpeg.OnProgress -= OnConversionProgress;

            return FromFileInfo(output);
        }

        /// <summary>
        /// Remove audio channel from video file.
        /// </summary>
        /// <param name="output">Location of the output video file.</param>
        /// <returns>Flag indicating if process ended succesfully.</returns>
        public bool Mute(FileInfo output)
        {
            return FFmpeg.Mute(this, output);
        }

        /// <summary>
        /// Extract audio channel from video file.
        /// </summary>
        /// <param name="output">Location of the output video file.</param>
        /// <returns>Flag indicating if process ended succesfully.</returns>
        public bool ExtractAudio(FileInfo output)
        {
            return FFmpeg.ExtractAudio(this, output);
        }

        /// <summary>
        /// Replace the audio of the video file.
        /// </summary>
        /// <param name="audio"></param>
        /// <param name="output"></param>
        /// <returns>Flag indicating if process ended succesfully.</returns>
        public bool ReplaceAudio(FileInfo audio, FileInfo output)
        {
            return FFmpeg.ReplaceAudio(this, audio, output);
        }

        /// <summary>
        /// Take a snapshot in memory.
        /// </summary>
        /// <param name="size">Size of the snapshot (resolution).</param>
        /// <param name="captureTime">Seek the video part that needs to get captured.</param>
        /// <returns>Bitmap of the snapshot.</returns>
        public Bitmap Snapshot(Size? size = null, TimeSpan? captureTime = null)
        {
            FileInfo output = new FileInfo(string.Format("{0}.png", Environment.TickCount));

            var success = FFmpeg.Snapshot(this, output, size, captureTime);

            if (!success)
                throw new OperationCanceledException("Could not take snapshot!");

            output.Refresh();

            Bitmap result;

            using (Bitmap bmp = (Bitmap)Image.FromFile(output.FullName))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Png);

                    result = new Bitmap(ms);
                }
            }

            if(output.Exists)
            {
                output.Delete();
            }

            return result;
        }

        /// <summary>
        /// Take a snapshot with output.
        /// </summary>
        /// <param name="output">Output file.</param>
        /// <param name="size">Size of the snapshot (resolution).</param>
        /// <param name="captureTime">Seek the video part that needs to get captured.</param>
        /// <returns>Bitmap of the snapshot.</returns>
        public Bitmap Snapshot(FileInfo output, Size? size = null, TimeSpan? captureTime = null)
        {
            var success = FFmpeg.Snapshot(this, output, size, captureTime);

            if (!success)
                throw new OperationCanceledException("Could not take snapshot!");

            Bitmap result;

            using (var bmp = (Bitmap)Bitmap.FromFile(output.FullName))
            {
                result = (Bitmap)bmp.Clone();
            }

            return result;
        }

        /// <summary>
        /// Join the video file with other video files.
        /// </summary>
        /// <param name="output">Output location of the resulting video file.</param>
        /// <param name="purgeSources">>Flag original file purging after conversion is done.</param>
        /// <param name="videos">Videos that need to be joined to the video.</param>
        /// <returns>Video information object with the new video file.</returns
        public VideoInfo JoinWith(FileInfo output, bool purgeSources = false, params VideoInfo[] videos)
        {
            var queuedVideos = videos.ToList();

            queuedVideos.Insert(0, this);

            var success = FFmpeg.Join(output, queuedVideos.ToArray());

            if (!success)
                throw new OperationCanceledException("Could not join the videos.");

            if(purgeSources)
            {
                foreach(var video in videos)
                {
                    video.Delete();
                }
            }

            return new VideoInfo(output);
        }

        /// <summary>
        /// Tell FFMpeg to stop the current process.
        /// </summary>
        public void CancelOperation()
        {
            FFmpeg.Stop();
        }

        /// <summary>
        /// See if ffmpeg process associated to this video is idle (not alive).
        /// </summary>
        public bool OperationIdle
        {
            get
            {
                return !FFmpeg.IsWorking;
            }
        }
    }
}
