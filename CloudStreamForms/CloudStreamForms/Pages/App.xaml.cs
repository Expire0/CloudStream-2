﻿using CloudStreamForms.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using static CloudStreamForms.Core.CloudStreamCore;

//[assembly: ExportFont("Times-New-Roman.ttf", Alias = "Times New Roman")]
namespace CloudStreamForms
{
	public partial class App : Application
	{
		public const string baseM3u8Name = @"mirrorlist.m3u8";
		public const string baseSubtitleName = @"mirrorlist.srt";
		public const string hasDownloadedFolder = "dloaded";

		public const string VIEW_TIME_POS = "ViewHistoryTimePos";
		public const string VIEW_TIME_DUR = "ViewHistoryTimeDur";
		public const string NEXT_AIRING = "NextAiringEpisode";
		public const string BOOKMARK_DATA = "BookmarkData";
		public const string VIEW_HISTORY = "ViewHistory";
		public const string DATA_FILENAME = "CloudStreamData.txt";

		public static DownloadState GetDstate(int epId)
		{
			bool isDownloaded = App.GetKey(hasDownloadedFolder, epId.ToString(), false);
			if (!isDownloaded) {
				return DownloadState.NotDownloaded;
			}
			else {
				try {
					return App.GetDownloadInfo(epId).state.state;
				}
				catch (Exception) {
					return DownloadState.NotDownloaded;
				}
			}
		}

		public static EventHandler<DownloadProgressInfo> OnDStateChanged;

		public static int GetInternalId(string imdbId)
		{
			MD5 md5Hasher = MD5.Create();
			var hashed = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(imdbId));
			return BitConverter.ToInt32(hashed, 0);
		}

		/*
		public static void GetDownloadProgress(string imdbId, out long bytes, out long totalBytes)
		{

		}*/

		public static string GetFont(string f, bool extend = true)
		{
			if (f == "App default") {
				return "";
			}
			return f.Replace(" ", "-") + ".ttf" + (extend ? "#" + f : "");
		}

		public struct BluetoothDeviceID
		{
			public string name;
			public string id;
		}

		public static EventHandler OnAppNotInForground;
		public static EventHandler OnAppKilled;
		public static EventHandler OnAppReopen;
		public static EventHandler OnAppResume;

		public interface IPlatformDep
		{
			void ToggleRealFullScreen(bool fullscreen);
			void ShowToast(string message, double duration);
			string DownloadFile(string file, string fileName, bool mainPath, string extraPath);
			string DownloadFile(string path, string data);
			string ReadFile(string fileName, bool mainPath, string extraPath);
			string ReadFile(string path);
			bool DeleteFile(string path);
			void DownloadUpdate(string update, string version);
			string GetDownloadPath(string path, string extraFolder);
			StorageInfo GetStorageInformation(string path = "");
			int ConvertDPtoPx(int dp);
			string GetExternalStoragePath();
			void HideStatusBar();
			void ShowStatusBar();
			void UpdateStatusBar();
			void UpdateBackground(int color);
			void UpdateBackground();
			void UpdateIcon(int icon);
			void LandscapeOrientation();
			void NormalOrientation();
			void ToggleFullscreen(bool fullscreen);
			void SetBrightness(double opacity);
			double GetBrightness();
			void Test();
			EventHandler<bool> OnAudioFocusChanged { set; get; }
			bool GainAudioFocus();
			void ReleaseAudioFocus();
			bool GetPlayerInstalled(VideoPlayer player);
			//  void PlayExternalApp(VideoPlayer player);
			string DownloadHandleIntent(int id, List<BasicMirrorInfo> mirrors, string fileName, string titleName, bool mainPath, string extraPath, bool showNotification = true, bool showNotificationWhenDone = true, bool openWhenDone = false, string poster = "", string beforeTxt = "");
			DownloadProgressInfo GetDownloadProgressInfo(int id, string fileUrl);
			void UpdateDownload(int id, int state);
			/*  BluetoothDeviceID[] GetBluetoothDevices();
              void SearchBluetoothDevices();*/
			void RequestVlc(List<string> urls, List<string> names, string episodeName, string episodeId, long startId = FROM_PROGRESS, string subtitleFull = "", VideoPlayer preferedPlayer = VideoPlayer.VLC, bool generateM3u8 = true);
			public int GetArchitecture();
			public bool ResumeDownload(int id);
			public void PictureInPicture();
			public void AddShortcut(string name, string imdbId, string url);
			//	public void GetDownloadProgress(string imdbId, out long bytes, out long totalBytes);
		}

		static public EventHandler<int> onExtendedButtonPressed;

		public static void AddShortcut(string name, string imdbId, string url)
		{
			PlatformDep.AddShortcut(name, imdbId, url);
		}

		public static VideoPlayerStatus currentVideoStatus = new VideoPlayerStatus() { isInVideoplayer = false, isLoaded = false, isPaused = false, hasNextEpisode = false, shouldSkip = false };

		private static bool _IsPictureInPicture = false;
		public static bool IsPictureInPicture { set { _IsPictureInPicture = value; OnPictureInPictureModeChanged?.Invoke(null, value); } get { return _IsPictureInPicture; } }

		public static bool FullPictureInPictureSupport = false;

		[Serializable]
		public struct VideoPlayerStatus
		{
			public bool isInVideoplayer;
			public bool isPaused;
			public bool isLoaded;
			public bool hasNextEpisode;
			public bool shouldSkip;
		}

		[Serializable]
		public struct BasicMirrorInfo
		{
			public string name;
			public string mirror;
			public string referer;
		}

		public enum DownloadState { Downloading, Downloaded, NotDownloaded, Paused }
		public enum DownloadType { Normal = 0, YouTube = 1 }
		public enum PlayerEventType { Stop = -1, Pause = 0, Play = 1, NextMirror = 2, PrevMirror = 3, SeekForward = 4, SeekBack = 5, SkipCurrentChapter = 6, NextEpisode = 7, PlayPauseToggle = 8 }

		public static EventHandler OnSomeDownloadFinished;
		public static EventHandler OnSomeDownloadFailed;
		public static EventHandler<bool> OnPictureInPictureModeChanged;
		public static EventHandler<PlayerEventType> OnRemovePlayAction;
		public static EventHandler OnVideoStatusChanged;

		[System.Serializable]
		public class DownloadInfo
		{
			public DownloadProgressInfo state;
			public DownloadEpisodeInfo info;
		}

		public class DownloadProgressInfo
		{
			public DownloadState state;

			public long bytesDownloaded;
			public long totalBytes;
			public double ProcentageDownloaded { get { return ((bytesDownloaded * 100.0) / totalBytes); } }
		}

		[System.Serializable]
		public class DownloadEpisodeInfo
		{
			/// <summary>
			/// Youtube is url, else the IMDB id
			/// </summary> 
			public string source;

			public string name;
			public string description;
			public int id;
			public int episode;
			public int season;
			public string episodeIMDBId;

			public string hdPosterUrl;

			public string fileUrl;
			public int downloadHeader;
			public DownloadType dtype;
		}

		[System.Serializable]
		public class DownloadHeader
		{
			public string name;
			public string ogName;
			//public string altName;
			public string id;
			public int RealId { get { if (id.StartsWith("tt")) { return int.Parse(id.Replace("tt", "")); } else { return ConvertStringToInt(id); } } }
			public string year;
			public string OgYear => year.Substring(0, 4);
			public string rating;
			public string runtime;
			public string posterUrl;
			public string description;
			//public int seasons;
			public string hdPosterUrl;
			public CloudStreamCore.MovieType movieType;

			//public CloudStreamCore.Title title; 
		}

		public static bool CanPlayExternalPlayer()
		{
			return ((VideoPlayer)Settings.PreferedVideoPlayer) != VideoPlayer.None;
		}

		public enum VideoPlayer
		{
			None = -1,
			VLC = 0,
			//MPV = 1,
			MXPlayer = 2,
			MXPlayerPro = 3,
			Auto = -2,
		}

		public static List<TEnum> GetEnumList<TEnum>() where TEnum : Enum
	=> ((TEnum[])Enum.GetValues(typeof(TEnum))).ToList();

		public static bool GetVideoPlayerInstalled(VideoPlayer player)
		{
			return PlatformDep.GetPlayerInstalled(player);
		}

		public static string GetVideoPlayerName(VideoPlayer player)
		{
			return player switch {
				VideoPlayer.None => "No Videoplayer",
				VideoPlayer.VLC => "VLC",
				// VideoPlayer.MPV => "MPV",
				VideoPlayer.MXPlayer => "MX Player",
				VideoPlayer.MXPlayerPro => "MX Player Pro",
				VideoPlayer.Auto => "Auto",
				_ => "",
			};
		}

		public static string GetPathFromType(MovieType t)
		{
			return t switch {
				MovieType.Movie => "Movies",
				MovieType.TVSeries => "TVSeries",
				MovieType.Anime => "Anime",
				MovieType.AnimeMovie => "Movies",
				MovieType.YouTube => "YouTube",
				_ => "Error",
			};
		}

		public static string ReadFile(string path)
		{
			return PlatformDep.ReadFile(path);
		}

		public static string ReadFile(string fileName, bool mainPath, string extraPath)
		{
			return PlatformDep.ReadFile(fileName, mainPath, extraPath);
		}

		public static string GetPathFromType(DownloadHeader header)
		{
			return GetPathFromType(header.movieType);
		}

		/// <summary>
		/// 0 = download, 1 = Pause, 2 = remove
		/// </summary>
		public static void UpdateDownload(int id, int state)
		{
			PlatformDep.UpdateDownload(id, state);
		}

		/*
        public static BluetoothDeviceID[] GetBluetoothDevices()
        {
            return platformDep.GetBluetoothDevices();
        }*/

		private static int _AudioDelay = 0;
		public static int AudioDelay { set { _AudioDelay = value; SetAudioDelay(value); } get { return _AudioDelay; } }
		//  public static string AudioDeviceId = "";
		/*  public static BluetoothDeviceID current;
          public void UpdateDevice()
          {
              var devices = GetBluetoothDevices();
              current = devices.FirstOrDefault();
          }*/

		readonly static string outputId = "none";

		public static int GetDelayAudio()
		{
			return App.GetKey("audiodelay", outputId, 0);
		}

		public static void SetAudioDelay(int delay)
		{
			App.SetKey("audiodelay", outputId, delay);
		}

		public const int FROM_START = -1;
		public const int FROM_PROGRESS = -2;

		public static async Task RequestVlc(string url, string name, string episodeName = null, string episodeId = "", int startId = FROM_PROGRESS, string subtitleFull = "", int episode = -1, int season = -1, string descript = "", bool? overrideSelectVideo = null, string headerId = "")
		{
			await RequestVlc(new List<string>() { url }, new List<string>() { name }, episodeName ?? "", episodeId, startId, subtitleFull, episode, season, descript, overrideSelectVideo, headerId);
		}

		public static EventHandler ForceUpdateVideo;

		public static bool isRequestingVLC = false;

		/// <summary>
		/// More advanced VLC launch, note subtitles seams to not work on android; can open in 
		/// </summary>
		/// <param name="urls">File or url</param>
		/// <param name="names">Name of eatch url</param>
		/// <param name="episodeName">Main name, name of the episode</param>
		/// <param name="episodeId">id for key of lenght seen</param>
		/// <param name="startId">FROM_START, FROM_PROGRESS or time in ms</param>
		/// <param name="subtitleFull">Leave emty for no subtitles, full subtitle text as seen in a regular .srt</param>
		public static async Task RequestVlc(List<string> urls, List<string> names, string episodeName, string episodeId, long startId = FROM_PROGRESS, string subtitleFull = "", int episode = -1, int season = -1, string descript = "", bool? overrideSelectVideo = null, string headerId = "", bool isFromIMDB = false, bool generateM3u8 = true)
		{
			if (isRequestingVLC) return;
			isRequestingVLC = true;
			bool useVideo = overrideSelectVideo ?? Settings.UseVideoPlayer;
			//bool subtitlesEnabled = subtitleFull != "";
			if (useVideo) {
				Page p = new VideoPage(new VideoPage.PlayVideo() {
					descript = descript,
					name = episodeName,
					isSingleMirror = urls.Count == 1,
					episode = episode,
					season = season,
					MirrorNames = names,
					MirrorUrls = urls,
					//Subtitles = subtitlesEnabled ? new List<string>() { subtitleFull } : new List<string>(),
					//SubtitlesNames = subtitlesEnabled ? new List<string>() { "English" } : new List<string>(),
					startPos = startId,
					episodeId = episodeId,
					headerId = headerId,
					isFromIMDB = isFromIMDB,
				}); ;//new List<string>() { "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4" }, new List<string>() { "Black" }, new List<string>() { });// { mainPoster = mainPoster };
				await ((MainPage)CloudStreamCore.mainPage).Navigation.PushModalAsync(p, true);
			}
			else {
				if ((VideoPlayer)Settings.PreferedVideoPlayer == VideoPlayer.None) {
					App.ShowToast("No videoplayer installed");
					return;
				};

				PlatformDep.RequestVlc(urls, names, episodeName, episodeId, startId, subtitleFull, (VideoPlayer)Settings.PreferedVideoPlayer, generateM3u8);
			}
			isRequestingVLC = false;
		}

		public static bool ResumeDownload(int id)
		{
			return PlatformDep.ResumeDownload(id);
		}

		public static string CensorFilename(string name, bool toLower = false)
		{
			name = Regex.Replace(name, @"[^A-Za-z0-9\.\-\: ]", string.Empty);//Regex.Replace(name, @"[^A-Za-z0-9\.]+", String.Empty);
			name.Replace(" ", "");
			if (toLower) {
				name = name.ToLower();
			}
			return name;
		}

		public static char[] invalidChars = { '|', '\\', '?', '*', '<', '\"', ':', '>', '/' };

		public static string GetPath(MovieType movieType, int season, int episode, string episodeName, string titleName, string ending)
		{
			static string SanitizePath(string input) // FIX EXTRA FOLDER BUG
			{
				for (int i = 0; i < invalidChars.Length; i++) {
					input = input.Replace(invalidChars[i], ' ');
				}
				return input;
			}

			string ConvertToRealPath(string input)
			{
				return input
					.Replace("{type}", GetPathFromType(movieType))
					.Replace("{tname}", SanitizePath(titleName))
					.Replace("{name}", SanitizePath(episodeName))
					.Replace("{ep}", episode.ToString())
					.Replace("{se}", season.ToString());
			}

			return ConvertToRealPath(Settings.VideoDownloadLocation) + ConvertToRealPath(movieType.IsMovie() ? Settings.VideoDownloadMovie : Settings.VideoDownloadTvSeries) + ending;
		}

		public static string DownloadHandleIntent(MovieType movieType, int season, int episode, string episodeName, string titleName, int id, List<BasicMirrorInfo> mirrors, bool showNotification = true, bool showNotificationWhenDone = true, bool openWhenDone = false, string poster = "", string beforeTxt = "")//, int mirror, string title, string path, string poster, string fileName, string beforeTxt, bool openWhenDone, bool showNotificaion, bool showDoneNotificaion, bool showDoneAsToast, bool resumeIntent)
		{
			string path = GetPath(movieType, season, episode, episodeName, titleName, ".mp4");
			return PlatformDep.DownloadHandleIntent(id, mirrors, episodeName, (movieType == MovieType.Movie || movieType == MovieType.AnimeMovie) ? titleName : $"{titleName} • {episodeName}", false, path, showNotification, showNotificationWhenDone, openWhenDone, poster, beforeTxt);
		}

		public static string RequestDownload(int id, string name, string description, int episode, int season, List<BasicMirrorInfo> mirrors, string downloadTitle, string poster, CloudStreamCore.Title title, string episodeIMDBId)
		{
			App.SetKey(hasDownloadedFolder, id.ToString(), true);

			DownloadHeader header = ConvertTitleToHeader(title);
			bool isMovie = header.movieType == MovieType.AnimeMovie || header.movieType == MovieType.Movie;


			print("HEADERID::: " + header.RealId);
			App.SetKey(nameof(DownloadHeader), "id" + header.RealId, header);

			App.SetKey("DownloadIds", id.ToString(), id);
			string fileUrl = DownloadHandleIntent(title.movieType, season, episode, name, title.name, id, mirrors, true, true, false, poster, isMovie ? "{name}\n" : ($"S{season}:E{episode} - " + "{name}\n"));//PlatformDep.DownloadHandleIntent(id, mirrors, downloadTitle, name, true, extraPath, true, true, false, poster, isMovie ? "{name}\n" : ($"S{season}:E{episode} - " + "{name}\n"));
			App.SetKey(nameof(DownloadEpisodeInfo), "id" + id, new DownloadEpisodeInfo() { dtype = DownloadType.Normal, source = header.id, description = description, downloadHeader = header.RealId, episode = episode, season = season, fileUrl = fileUrl, id = id, name = name, hdPosterUrl = poster, episodeIMDBId = episodeIMDBId });

			return fileUrl;
			// (isMovie) ? $"{mirrorName}\n" : $"S{currentSeason}:E{episodeResult.Episode} - {mirrorName}\n
			//  string dpath = App.DownloadAdvanced(GetCorrectId(episodeResult), mirrorUrl, episodeResult.Title + ".mp4", isMovie ? currentMovie.title.name : $"{currentMovie.title.name} · {episodeResult.OgTitle}", true, "/" + GetPathFromType(), true, true, false, episodeResult.PosterUrl, (isMovie) ? $"{mirrorName}\n" : $"S{currentSeason}:E{episodeResult.Episode} - {mirrorName}\n");
			//   string dpath = App.DownloadAdvanced(GetCorrectId(episodeResult), mirrorUrl, episodeResult.Title + ".mp4", isMovie ? currentMovie.title.name : $"{currentMovie.title.name} · {episodeResult.OgTitle}", true, "/" + GetPathFromType(), true, true, false, episodeResult.PosterUrl, (isMovie) ? $"{mirrorName}\n" : $"S{currentSeason}:E{episodeResult.Episode} - {mirrorName}\n");
		}

		public static int ConvertStringToInt(string inp)
		{
			MD5 md5Hasher = MD5.Create();
			var hashed = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(inp));
			return Math.Abs(BitConverter.ToInt32(hashed, 0));
		}
		public static DownloadHeader ConvertTitleToHeader(CloudStreamCore.Title title)
		{
			return new DownloadHeader() { description = title.description, hdPosterUrl = title.hdPosterUrl, id = title.id, name = title.name, ogName = title.ogName, posterUrl = title.posterUrl, rating = title.rating, runtime = title.runtime, year = title.year, movieType = title.movieType };
		}

		public static DownloadInfo GetDownloadInfo(int id, bool hasState = true)
		{
			var info = GetDownloadEpisodeInfo(id);
			if (info == null) return null;
			//  Stopwatch stop = new Stopwatch();
			//stop.Start();
			var i = new DownloadInfo() { info = info, state = hasState ? PlatformDep.GetDownloadProgressInfo(id, info.fileUrl) : null };
			//   stop.Stop(); print("DLENNNNN:::" + stop.ElapsedMilliseconds);
			return i;
		}

		public static DownloadEpisodeInfo GetDownloadEpisodeInfo(int id)
		{
			return App.GetKey<DownloadEpisodeInfo>(nameof(DownloadEpisodeInfo), "id" + id, null);
		}

		public static DownloadHeader GetDownloadHeaderInfo(int id)
		{
			print("HEADERIDFRONM::: " + id);
			return App.GetKey<DownloadHeader>(nameof(DownloadHeader), "id" + id, null);
		}

		public class StorageInfo
		{
			public long TotalSpace = 0;
			public long AvailableSpace = 0;
			public long FreeSpace = 0;
			public long UsedSpace { get { return TotalSpace - AvailableSpace; } }
			/// <summary>
			/// From 0-1
			/// </summary>
			public double UsedProcentage { get { return ConvertBytesToGB(UsedSpace, 4) / ConvertBytesToGB(TotalSpace, 4); } }
		}

		public static void Test()
		{
			PlatformDep.Test();
		}

		private static IPlatformDep _platformDep;
		public static IPlatformDep PlatformDep {
			set {
				_platformDep = value;
				_platformDep.OnAudioFocusChanged += (o, e) => { OnAudioFocusChanged?.Invoke(o, e); };
			}
			get { return _platformDep; }
		}

		public static EventHandler<bool> OnAudioFocusChanged;

		public static bool GainAudioFocus()
		{
			return PlatformDep.GainAudioFocus();
		}

		public static void ReleaseAudioFocus()
		{
			PlatformDep.ReleaseAudioFocus();
		}

		public static void UpdateStatusBar()
		{
			PlatformDep.UpdateStatusBar();
		}

		public static void ToggleFullscreen(bool fullscreen)
		{
			PlatformDep.ToggleFullscreen(fullscreen);
		}

		public static void ToggleRealFullScreen(bool fullscreen)
		{
			PlatformDep.ToggleRealFullScreen(fullscreen);
		}

		public static double GetBrightness()
		{
			return PlatformDep.GetBrightness();
		}

		public static void SetBrightness(double brightness)
		{
			PlatformDep.SetBrightness(brightness);
		}

		public static bool isOnMainPage = true;

		public static void UpdateBackground(int color = -1)
		{
			if (color == -1) {
				color = Math.Max(0, Settings.BlackColor - 5); // Settings.BlackColor;//
			}
			CloudStreamForms.MainPage.mainPage.BarBackgroundColor = new Color(color / 255.0, color / 255.0, color / 255.0, 1);
			if (isOnMainPage || !Settings.IsTransparentNonMain) {
				PlatformDep.UpdateBackground(isOnMainPage ? color : Settings.BlackColor);
			}
			else {
				UpdateToTransparentBg();
			}
		}

		public static void UpdateIcon(int icon)
		{
			PlatformDep.UpdateIcon(icon);
		}

		public static void UpdateToTransparentBg()
		{
			PlatformDep.UpdateBackground();
		}

		public static void HideStatusBar()
		{
			PlatformDep.HideStatusBar();
		}

		public static void ShowStatusBar()
		{
			PlatformDep.ShowStatusBar();
		}

		public static void LandscapeOrientation()
		{
			PlatformDep.LandscapeOrientation();
		}

		public static void NormalOrientation()
		{
			PlatformDep.NormalOrientation();
		}

		public static App instance;
		public App()
		{
			instance = this;
			Settings.CurrentFont = Settings.CurrentGlobalFonts[Settings.CurrentGlobalFont];
			InitializeComponent();

			try {
				MainPage = new MainPage();
			}
			catch (Exception) {
			}
		}


		public static int ConvertDPtoPx(int dp)
		{
			return PlatformDep.ConvertDPtoPx(dp);
		}

		public static StorageInfo GetStorage()
		{
			return PlatformDep.GetStorageInformation();
		}

		public static double ConvertBytesToGB(long bytes, int digits = 2)
		{
			return ConvertBytesToAny(bytes, digits, 3);
		}

		public static double ConvertBytesToAny(long bytes, int digits = 2, int steps = 3)
		{
			int div = GetSizeOfJumpOnSystem();
			return Math.Round((bytes / Math.Pow(div, steps)), digits);
		}

		public static int GetSizeOfJumpOnSystem()
		{
			return Device.RuntimePlatform == Device.UWP ? 1024 : 1000;
		}

		public static bool DeleteFile(string path)
		{
			return PlatformDep.DeleteFile(path);
		}

		public static void ShowToast(string message, double duration = 2.5)
		{
			PlatformDep.ShowToast(message, duration);
		}

		public static string GetBuildNumber()
		{
			try {
				var v = Assembly.GetExecutingAssembly().GetName().Version;
				return v.Major + "." + v.Minor + "." + v.Build;
			}
			catch (Exception _ex) {
				error(_ex);
				return "";
			}
		}

		public enum AndroidVersionArchitecture
		{
			Universal = 0,
			arm64_v8a = 1,
			armeabi_v7a = 2,
			x86 = 3,
			x86_64 = 4,
		}

		static string GetVersionBuildName(AndroidVersionArchitecture version)
		{
			if (version == AndroidVersionArchitecture.Universal) {
				return "";
			}
			return version.ToString().Replace("_", "-") + "-";
		}

		public static string GetVersionPublicName(AndroidVersionArchitecture version)
		{
			return version.ToString().Replace("_", "-");
		}

		public static void DownloadNewGithubUpdate(string update, AndroidVersionArchitecture version)
		{
			PlatformDep.DownloadUpdate(update, GetVersionBuildName(version));
		}

		public static string GetDownloadPath(string path, string extraFolder)
		{
			return PlatformDep.GetDownloadPath(path, extraFolder);
		}

		public static void SaveData()
		{
			print("SAVING DATA");
			try {
				System.Threading.Thread t = new System.Threading.Thread(() => { // idk await cant be used inside lock, and I dont want it blocking mainthread
					lock (keyMutexLock) {
						Application.Current.SavePropertiesAsync().Wait();
						print("SAVING DATA DONE!!");
					}
				});
				t.Start();
			}
			catch (Exception _ex) {
				error(_ex);
			}
		}

		static string GetKeyPath(string folder, string name = "")
		{
			string _s = ":" + folder + "-";
			if (name != "") {
				_s += name + ":";
			}
			return _s;
		}

		public static void SetKey(string path, object value)
		{
			try {
				string _set = ConvertToString(value);
				lock (keyMutexLock) {
					if (MyApp.Properties.ContainsKey(path)) {
						CloudStreamCore.print("CONTAINS KEY" + path);
						MyApp.Properties[path] = _set;
					}
					else {
						CloudStreamCore.print("ADD KEY" + path);
						MyApp.Properties.Add(path, _set);
					}
				}
			}
			catch (Exception _ex) {
				print("ERROR SETTING KEYU" + _ex);
			}

		}

		public static void SetKey(string folder, string name, object value)
		{
			if (Settings.CachedPauseHis && (folder == VIEW_HISTORY || folder == VIEW_TIME_DUR || folder == VIEW_TIME_POS)) return;

			try {
				string path = GetKeyPath(folder, name);
				SetKey(path, value);
			}
			catch (Exception _ex) {
				print("EX SET KEY:" + _ex);
			}

		}

		static long GetLongRegex(string id)
		{
			try {
				return long.Parse(Regex.Replace(id, @"[^0-9]", ""));
			}
			catch (Exception) {
				return 0;
			}
		}

		public static void SetViewPos(string id, long res)
		{
			SetKey(VIEW_TIME_POS, GetLongRegex(id).ToString(), res);
		}

		public static void SetViewDur(string id, long res)
		{
			SetKey(VIEW_TIME_DUR, GetLongRegex(id).ToString(), res);
		}

		public static long GetViewPos(string id)
		{
			if (!id.IsClean()) return -1;
			long _parse = GetLongRegex(id);
			return GetKey(VIEW_TIME_POS, _parse.ToString(), -1L);
		}

		public static long GetViewPos(long _parse)
		{
			return GetKey(VIEW_TIME_POS, _parse.ToString(), -1L);
		}

		public static long GetViewDur(string id)
		{
			if (!id.IsClean()) return -1;
			long _parse = GetLongRegex(id);
			return GetViewDur(_parse);
		}

		public static long GetViewDur(long _parse)
		{
			return GetKey(VIEW_TIME_DUR, _parse.ToString(), -1L);
		}

		static readonly object keyMutexLock = new object();

		public static T GetKey<T>(string folder, string name, T defVal)
		{
			try {
				string path = GetKeyPath(folder, name);
				//1print("GEKEY::: " + folder + "|" + name + "|" + defVal + "|" + path);
				return GetKey<T>(path, defVal);
			}
			catch (Exception) {
				return defVal;
			}
		}

		public static void RemoveFolder(string folder)
		{
			string[] keys = App.GetKeysPath(folder);
			for (int i = 0; i < keys.Length; i++) {
				RemoveKey(keys[i]);
			}
		}

		public static string GetRawKey(string path, string defVal = "")
		{
			try {
				lock (keyMutexLock) {
					return MyApp.Properties[path] as string;
				}
			}
			catch (Exception) {
				return defVal;
			}
		}

		public static void SetRawKey(string path, string data)
		{
			try {
				lock (keyMutexLock) {
					MyApp.Properties[path] = data;
				}
			}
			catch (Exception) {
			}
		}

		public static void ClearEveryKey()
		{
			lock (keyMutexLock) {
				MyApp.Properties.Clear();
			}
		}

		public static string[] GetAllKeys()
		{
			lock (keyMutexLock) {
				return MyApp.Properties.Keys.ToArray();
			}
		}

		public static T GetKey<T>(string path, T defVal)
		{
			try {
				lock (keyMutexLock) {
					if (MyApp.Properties.ContainsKey(path)) {
						// CloudStreamCore.print("GETKEY::" + myApp.Properties[path]);
						// CloudStreamCore.print("GETKEY::" + typeof(T).ToString() + "||" + ConvertToObject<T>(myApp.Properties[path] as string, defVal));
						return (T)ConvertToObject<T>(MyApp.Properties[path] as string, defVal);
					}
					else {
						return defVal;
					}
				}
			}
			catch (Exception) {
				return defVal;
			}

		}

		public static T[] GetKeys<T>(string folder)
		{
			try {
				string[] keyNames = GetKeysPath(folder);
				int len = keyNames.Length;
				T[] allKeys = new T[len];

				lock (keyMutexLock) {
					for (int i = 0; i < len; i++) {
						string p = (string)MyApp.Properties[keyNames[i]];
						allKeys[i] = ConvertToObject<T>(p, default);
					}
				}

				return allKeys;
			}
			catch (Exception _ex) {
				error(_ex);
				return new T[0];
			}
		}

		public static int GetKeyCount(string folder)
		{
			return GetKeysPath(folder).Length;
		}

		public static string[] GetKeysPath(string folder)
		{
			lock (keyMutexLock) {
				string[] copy = new string[MyApp.Properties.Keys.Count];
				try {
					MyApp.Properties.Keys.CopyTo(copy, 0);
					string[] keyNames = copy.Where(t => t != null).Where(t => t.StartsWith(GetKeyPath(folder))).ToArray();
					return keyNames;
				}
				catch (Exception _ex) {
					print("MAN EX GET KEY PARKKK " + _ex);
					for (int i = 0; i < copy.Length; i++) {
						print("MAX COPY::" + copy[i]);
					}
					App.ShowToast("Error");
					return new string[0];
				}
			}
		}

		public static bool KeyExists(string folder, string name)
		{
			string path = GetKeyPath(folder, name);
			return KeyExists(path);
		}

		public static bool KeyExists(string path)
		{
			lock (keyMutexLock) {
				return (MyApp.Properties.ContainsKey(path));
			}
		}

		public static void RemoveKey(string folder, string name)
		{
			string path = GetKeyPath(folder, name);
			RemoveKey(path);
		}

		public static void RemoveKey(string path)
		{
			try {
				lock (keyMutexLock) {
					if (MyApp.Properties.ContainsKey(path)) {
						MyApp.Properties.Remove(path);
					}
				}
			}
			catch (Exception _ex) {
				error(_ex);
			}
		}

		static Application MyApp { get { return Current; } }

		public static T ConvertToObject<T>(string str, T defValue)
		{
			try {
				return FromByteArray<T>(Convert.FromBase64String(str));
			}
			catch (Exception) {
				return defValue;
			}
		}

		public static T FromByteArray<T>(byte[] rawValue)
		{
			BinaryFormatter bf = new BinaryFormatter();
			using MemoryStream ms = new MemoryStream(rawValue);
			return (T)bf.Deserialize(ms);
		}

		public static string ConvertToString(object o)
		{
			return Convert.ToBase64String(ToByteArray(o));
		}

		public static byte[] ToByteArray(object obj)
		{
			if (obj == null)
				return null;
			BinaryFormatter bf = new BinaryFormatter();
			using MemoryStream ms = new MemoryStream();
			bf.Serialize(ms, obj);
			return ms.ToArray();
		}

		public static ImageSource GetImageSource(string inp)
		{
			return ImageSource.FromResource("CloudStreamForms.Resource." + inp, Assembly.GetExecutingAssembly());
		}

		public static string DownloadFile(string file, string fileName, bool mainPath = true, string extraPath = "")
		{
			return PlatformDep.DownloadFile(file, fileName, mainPath, extraPath);
		}

		public static string DownloadFile(string path, string data)
		{
			return PlatformDep.DownloadFile(path, data);
		}

		public static string ConvertPathAndNameToM3U8(List<string> path, List<string> name, bool isSubtitleEnabled = false, string beforePath = "", string overrideSubtitles = null)
		{
			string _s = "#EXTM3U";
			if (isSubtitleEnabled) {
				_s += "\n";
				_s += "\n";
				//  _s += "#EXT-X-MEDIA:TYPE=SUBTITLES,GROUP-ID=\"subs\",NAME=\"English\",DEFAULT=YES,AUTOSELECT=YES,FORCED=NO,LANGUAGE=\"en\",CHARACTERISTICS=\"public.accessibility.transcribes-spoken-dialog, public.accessibility.describes-music-and-sound\",URI=" + beforePath + baseSubtitleName + "\"";
				_s += "#EXTVLCOPT:sub-file=" + (overrideSubtitles ?? (beforePath + baseSubtitleName));
				_s += "\n";
			}
			for (int i = 0; i < path.Count; i++) {
				_s += "\n#EXTINF:" + ", " + name[i].Replace("-", "").Replace("  ", " ") + "\n" + path[i]; //+ (isSubtitleEnabled ? ",SUBTITLES=\"subs\"" : "");
			}
			return _s;
		}

		public static void OpenSpecifiedBrowser(string url)
		{
			print("SPECTrying to open: " + url);
			if (CheckIfURLIsValid(url)) {
				try {
					Browser.OpenAsync(new Uri(url));
				}
				catch (Exception _ex) {
					error("SPECBROWSER LOADED ERROR, SHOULD NEVER HAPPEND!!" + _ex);
				}
			}
		}

		public static async Task OpenBrowser(string url)
		{
			if (CheckIfURLIsValid(url)) {
				try {
					await Launcher.OpenAsync(new Uri(url));
				}
				catch (Exception _ex) {
					error(_ex);
				}
			}
		}

		protected override void OnStart()
		{
			// Handle when your app starts
		}

		protected override void OnSleep()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
		}
	}
}
