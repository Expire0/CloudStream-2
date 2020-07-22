﻿using CloudStreamForms.Core;
using CloudStreamForms.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamEffects;
using static CloudStreamForms.Core.CloudStreamCore;
using static CloudStreamForms.MainPage;

namespace CloudStreamForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Home : ContentPage
    {
        const int POSTER_HIGHT = 96;
        const int POSTER_WIDTH = 67;
        public MainEpisodeView epView;
        public List<IMDbTopList> iMDbTopList = new List<IMDbTopList>();
        readonly List<string> genres = new List<string>() { "", "action", "adventure", "animation", "biography", "comedy", "crime", "drama", "family", "fantasy", "film-noir", "history", "horror", "music", "musical", "mystery", "romance", "sci-fi", "sport", "thriller", "war", "western" };
        readonly List<string> genresNames = new List<string>() { "Any", "Action", "Adventure", "Animation", "Biography", "Comedy", "Crime", "Drama", "Family", "Fantasy", "Film-Noir", "History", "Horror", "Music", "Musical", "Mystery", "Romance", "Sci-Fi", "Sport", "Thriller", "War", "Western" };

        readonly List<string> recomendationTypes = new List<string> { "Related", "Top 100", "Popular" };

        LabelList MovieTypePicker;
        LabelList ImdbTypePicker;

        public bool IsRecommended { get { return ImdbTypePicker.SelectedIndex == 0; } }
        public bool IsTop100 { get { return ImdbTypePicker.SelectedIndex == 1; } }
        public bool IsPopular { get { return ImdbTypePicker.SelectedIndex == 1; } }

        public int currentImageCount = 0;
        public void LoadMoreImages(bool setHeight = true)
        {
            if (!Settings.Top100Enabled) return;
            Device.BeginInvokeOnMainThread(() => {
                int count = 10;//PosterAtScreenHight * PosterAtScreenWith * 3
                for (int i = 0; i < count; i++) {
                    if (currentImageCount >= iMDbTopList.Count) {
                        if (!Fething && !IsRecommended) {
                            GetFetch(currentImageCount + 1);
                        }
                        return;
                        //Feth more data
                    }
                    else {
                        try {
                            IMDbTopList x = iMDbTopList[currentImageCount];
                            bool add = true;
                            int selGen = MovieTypePicker.SelectedIndex - 1;
                            if (selGen != -1 && IsRecommended) {

                                if (!iMDbTopList[currentImageCount].contansGenres.Contains(selGen)) {
                                    add = false;
                                }
                            }
                            if (add) {

                                string img = ConvertIMDbImagesToHD(iMDbTopList[currentImageCount].img, IsRecommended ? 76 : 180, IsRecommended ? 113 : 268);

                                AddEpisode(new EpisodeResult() { ForceDescript = true, Description = x.descript, Title = (x.place > 0 ? (x.place + ". ") : "") + x.name + " | ★ " + x.rating.Replace(",", "."), Id = x.place, PosterUrl = img, extraInfo = "Id=" + x.id + "|||Name=" + x.name + "|||" }, false);
                            }

                        }
                        catch (Exception) {

                        }

                        // ItemGrid.Children.Add(cachedImages[currentImageCount]);
                        //SetChashedImagePos(ItemGrid.Children.Count - 1);
                    }
                    currentImageCount++;

                }
                if (setHeight) {
                    SetHeight();
                }
            });
        }


        public void GetFetchRecomended()
        {
            if (!Settings.Top100Enabled) return;

            if (!Fething) {
                Fething = true;
                TempThread tempThred = mainCore.CreateThread(21);
                mainCore.StartThread("GetFetchRecomended", () => {
                    try {
                        var f = FetchRecomended(bookmarkPosters.Select(t => t.id).ToList());
                        if (!mainCore.GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                        iMDbTopList.AddRange(f);
                        LoadMoreImages();
                    }
                    finally {
                        Fething = false;
                        mainCore.JoinThred(tempThred);
                    }
                });
            }
        }

        bool _fething = false;
        public bool Fething {
            set {
                if ((value && epView.MyEpisodeResultCollection.Count == 0) || !value) {
                    Device.BeginInvokeOnMainThread(() => { LoadingIndicator.IsVisible = value; LoadingIndicator.IsEnabled = value; /*LoadingIndicator.IsRunning = value;*/ });
                }
                _fething = value;
            }
            get { return _fething; }
        }

        public void GetFetch(int start = 1)
        {
            if (!Settings.Top100Enabled) return;

            Fething = true;
            TempThread tempThred = mainCore.CreateThread(21);
            mainCore.StartThread("FethTop100", () => {
                try {
                    var f = FetchTop100(new List<string>() { genres[MovieTypePicker.SelectedIndex] }, start, top100: IsTop100);
                    if (!mainCore.GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                    iMDbTopList.AddRange(f);
                    /*  Device.BeginInvokeOnMainThread(() => {

                          for (int i = 0; i < iMDbTopList.Count; i++) {

                              string img = ConvertIMDbImagesToHD(iMDbTopList[i].img, 67, 98, 4);
                              IMDbTopList x = iMDbTopList[i];

                              AddEpisode(new EpisodeResult() { Description = x.descript, Title = x.name + " | ★ " + x.rating.Replace(",", "."), Id = x.place, PosterUrl = img, extraInfo = "Id="+x.id+"|||Name="+x.name+"|||" }, false);
                          }

                    //  LoadMoreImages(false);
                    // LoadMoreImages();

                });*/
                    //if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    LoadMoreImages();
                }
                finally {
                    Fething = false;
                    mainCore.JoinThred(tempThred);
                }
            });
        }

        private void episodeView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            episodeView.SelectedItem = null;
            //EpisodeResult episodeResult = ((EpisodeResult)((ListView)sender).BindingContext);
            //PlayEpisode(episodeResult);

        }
        private void ImageButton_Clicked(object sender, EventArgs e)
        {
            EpisodeResult episodeResult = ((EpisodeResult)((ImageButton)sender).BindingContext);
            PushPage(episodeResult);
            //  PlayEpisode(episodeResult);
        }

        public void PushPage(EpisodeResult episodeResult)
        {
            PushPageFromUrlAndName(FindShortend(episodeResult.extraInfo, "Id"), FindShortend(episodeResult.extraInfo, "Name"));

        }

        private void ViewCell_Tapped(object sender, EventArgs e)
        {
            EpisodeResult episodeResult = (EpisodeResult)(((ViewCell)sender).BindingContext);
            PushPage(episodeResult);

            // EpsodeShow(episodeResult);
            //EpisodeResult episodeResult = ((EpisodeResult)((ImageButton)sender).BindingContext);
            //App.PlayVLCWithSingleUrl(episodeResult.mirrosUrls[0], episodeResult.Title);
            //episodeView.SelectedItem = null;
        }

        List<FFImageLoading.Forms.CachedImage> play_btts = new List<FFImageLoading.Forms.CachedImage>();
        private void Image_PropertyChanging(object sender, PropertyChangingEventArgs e)
        {

            FFImageLoading.Forms.CachedImage image = ((FFImageLoading.Forms.CachedImage)sender);

            if (play_btts.Where(t => t.Id == image.Id).Count() == 0) {
                play_btts.Add(image);
                //image.Source = ImageSource.FromResource("CloudStreamForms.Resource.playBtt.png", Assembly.GetExecutingAssembly());
                /*
                if (Device.RuntimePlatform == Device.Android) {
                    image.Scale = 0.5f;
                }
                else {
                    image.Scale = 0.3f;
                }*/
            }

        }
        public Command TapCommand { set; get; } = new Command((o) => { print("Hello:"); });
        public Home()
        {

            InitializeComponent();
            if (Settings.IS_TEST_BUILD) {
                return;
            }
            try {
                epView = new MainEpisodeView();
                BindingContext = epView;

                BackgroundColor = Settings.BlackRBGColor;

                MovieTypePicker = new LabelList(MovieTypePickerBtt, genresNames);
                ImdbTypePicker = new LabelList(ImdbTypePickerBtt, recomendationTypes);

                // MovieTypePicker.ItemsSource = genresNames;
                // ImdbTypePicker.ItemsSource = recomendationTypes;
                //   UpdateBookmarks();

                MovieTypePicker.SelectedIndex = 0;
                ImdbTypePicker.SelectedIndex = -1;



                ImdbTypePicker.SelectedIndexChanged += (o, e) => {
                    ClearEpisodes();
                    mainCore.PurgeThreads(21);
                    Fething = false;
                    if (IsRecommended) {
                        GetFetchRecomended();
                    }
                    else {
                        GetFetch();
                    }
                    //    ImdbTypePickerBtt.Text = ImdbTypePicker.Items[ImdbTypePicker.SelectedIndex];

                };

                // MovieTypePickerBtt.Text = MovieTypePicker.Items[MovieTypePicker.SelectedIndex];

                MovieTypePicker.SelectedIndexChanged += (o, e) => {
                    ClearEpisodes(!IsRecommended);
                    if (IsRecommended) {
                        CoreHelpers.Shuffle(iMDbTopList);
                        LoadMoreImages();
                    }
                    else {
                        mainCore.PurgeThreads(21);
                        Fething = false;
                        GetFetch();
                    }
                    //    MovieTypePickerBtt.Text = MovieTypePicker.Items[MovieTypePicker.SelectedIndex];
                    //GetFetchRecomended
                    //  print(MovieTypePicker.SelectedIndex + "<<Selected");
                };

                episodeView.Scrolled += (o, e) => {
                    double maxY = episodeView.HeightRequest - episodeView.Height;
                    //print(maxY);
                    if (e.ScrollY >= maxY - 200) {
                        LoadMoreImages();
                    }
                };


                if (Device.RuntimePlatform == Device.UWP) {
                    // BlueSeperator.IsVisible = false;
                    // BlueSeperator.IsEnabled = false;
                    OffBar.IsVisible = false;
                    OffBar.IsEnabled = false;
                }
                else {
                    OffBar.Source = App.GetImageSource("gradient.png"); OffBar.HeightRequest = 3; OffBar.HorizontalOptions = LayoutOptions.Fill; OffBar.ScaleX = 100; OffBar.Opacity = 0.3; OffBar.TranslationY = 9;
                }

                episodeView.VerticalScrollBarVisibility = Settings.ScrollBarVisibility;


            }
            catch (Exception _ex) {
                error(_ex);
            }
            /*
            ImageScroller.Scrolled += (o, e) => {
                double maxY = ImageScroller.ContentSize.Height - ImageScroller.Height;
                if (e.ScrollY >= maxY - 200) {
                    LoadMoreImages();
                }

            };*/

            // MovieTypePicker.IsEnabled = false;
            //MovieTypePicker.IsVisible = false;
        }

        public static string FindShortend(string d, string key)
        {
            return FindHTML(d, key + "=", "|||");
        }


        async Task AddEpisodeAsync(EpisodeResult episodeResult, bool setHeight = true, int delay = 10, bool setH = false)
        {
            AddEpisode(episodeResult, setHeight, setH);
            await Task.Delay(delay);
        }

        void AddEpisode(EpisodeResult episodeResult, bool setHeight = true, bool setH = false, bool addtoGrid = false)
        {
            epView.MyEpisodeResultCollection.Add(episodeResult);


            /*
           
            // Device.BeginInvokeOnMainThread(() => {
            var ff = new FFImageLoading.Forms.CachedImage {
                Source = episodeResult.PosterUrl,
                HeightRequest = POSTER_HIGHT,
                WidthRequest = POSTER_WIDTH,
                BackgroundColor = Color.Transparent,
                VerticalOptions = LayoutOptions.Start,
                Transformations = {
                                new FFImageLoading.Transformations.RoundedTransformation(10,1,1.5,10,"#303F9F")
                            },
                InputTransparent = true,
            };

            cachedImages.Add(ff);*/
            if (addtoGrid) {
                /*
                ItemGrid.Children.Add(ff);
                if (setH) {
                    SetChashedImagePos(ItemGrid.Children.Count - 1);
                }*/
            }

            if (setHeight) {
                SetHeight();
            }

            //}); 


        }

        void ClearEpisodes(bool clearData = true)
        {
            //ItemGrid.Children.Clear();
            epView.MyEpisodeResultCollection.Clear();
            cachedImages.Clear();
            currentImageCount = 0;
            SetHeight();
            if (clearData) {

                iMDbTopList.Clear();
            }
        }

        public int PosterAtScreenWith { get { return (int)(currentWidth / (double)POSTER_WIDTH); } }
        public int PosterAtScreenHight { get { return (int)(currentWidth / (double)POSTER_HIGHT); } }
        List<FFImageLoading.Forms.CachedImage> cachedImages = new List<FFImageLoading.Forms.CachedImage>();

        void SetHeight()
        {
            /*
            ItemGrid.RowSpacing = POSTER_HIGHT / 2;

            for (int i = 0; i < cachedImages.Count; i++) {
                SetChashedImagePos(i);
            }*/

            Device.BeginInvokeOnMainThread(() => { episodeView.HeightRequest = epView.MyEpisodeResultCollection.Count * episodeView.RowHeight + 200; });

        }

        void SetChashedImagePos(int pos)
        {
            int x = pos % PosterAtScreenWith;
            int y = (int)(pos / PosterAtScreenWith);
            Grid.SetColumn(cachedImages[pos], x);
            Grid.SetRow(cachedImages[pos], y);
        }

        async void SaveData()
        {
            print("SAVING DATA");
            try {
                await Application.Current.SavePropertiesAsync();
            }
            catch (Exception _ex) {
                error(_ex);
            }
            print("SAVING DATA DONE!!");

        }

        bool hasAppered = false;

        protected override void OnDisappearing()
        {
            //  OnIconEnd(0);
            base.OnDisappearing();
        }

        protected override void OnAppearing()
        {
            if (Settings.IS_TEST_BUILD) {
                base.OnAppearing();
                return;
            }

            try {
                // OnIconStart(0);
                base.OnAppearing();
                SaveData();
                if (!hasAppered) {
                    App.UpdateStatusBar();
                    App.UpdateBackground();

                    /*
                    Application.Current.MainPage.SizeChanged += (o, e) => {
                        SetHeight();
                    };*/
                }
                UpdateBookmarks();
                Top100Stack.IsEnabled = Settings.Top100Enabled;
                Top100Stack.IsVisible = Settings.Top100Enabled;
                BackgroundColor = Settings.BlackRBGColor;
                //Color.FromHex(Settings.MainBackgroundColor);
                hasAppered = true;
            }
            catch (Exception _ex) {
                error(_ex);
            }

        }

        //            await PopupNavigation.Instance.PushAsync(new SelectPopup(new List<string>() { "Season 1", "Season 2", "Season 3", "Season 3", "Season 3", }, 1));


        List<BookmarkPoster> bookmarkPosters = new List<BookmarkPoster>();
        void UpdateBookmarks()
        {
            try {
                int height = 150;
                Bookmarks.HeightRequest = height;
                List<string> keys = App.GetKeys<string>(App.BOOKMARK_DATA);
                List<string> data = new List<string>();
                bookmarkPosters = new List<BookmarkPoster>();
                Bookmarks.Children.Clear();
                for (int i = 0; i < keys.Count; i++) {
                    string __key = App.ConvertToObject<string>(keys[i], "");
                    if (__key == "") {
                        continue;
                    }
                    string name = FindHTML(__key, "Name=", "|||");
                    print("BOOKMARK:" + name);
                    string posterUrl = FindHTML(__key, "PosterUrl=", "|||");
                    posterUrl = ConvertIMDbImagesToHD(posterUrl, 182, 268);
                    print("POSTERURL:::" + posterUrl);

                    string id = FindHTML(__key, "Id=", "|||");
                    if (name != "" && posterUrl != "" && id != "") {
                        if (CheckIfURLIsValid(posterUrl)) {
                            Grid stackLayout = new Grid();


                            //  Button imageButton = new Button() { HeightRequest = 150, WidthRequest = 90, BackgroundColor = Color.Transparent, VerticalOptions = LayoutOptions.Start };
                            var ff = new FFImageLoading.Forms.CachedImage {
                                Source = posterUrl,
                                HeightRequest = height,
                                WidthRequest = 87,
                                BackgroundColor = Color.Transparent,
                                VerticalOptions = LayoutOptions.Start,
                                Transformations = {
                                new FFImageLoading.Transformations.RoundedTransformation(1,1,1.5,0,"#303F9F")
                            },
                                InputTransparent = true,
                            };

                            //Source = p.posterUrl

                            stackLayout.Children.Add(ff);
                            // stackLayout.Children.Add(imageButton);
                            bookmarkPosters.Add(new BookmarkPoster() { id = id, name = name, posterUrl = posterUrl });
                            Grid.SetColumn(stackLayout, Bookmarks.Children.Count);
                            Bookmarks.Children.Add(stackLayout);

                            // --- RECOMMENDATIONS CLICKED -----
                            stackLayout.SetValue(XamEffects.TouchEffect.ColorProperty, Color.White);
                            Commands.SetTap(stackLayout, new Command((o) => {
                                int z = (int)o;
                                PushPageFromUrlAndName(bookmarkPosters[z].id, bookmarkPosters[z].name);
                                //do something
                            }));
                            Commands.SetTapParameter(stackLayout, i);
                            /*
                            imageButton.Clicked += (o, _e) => {
                                for (int z = 0; z < bookmarkPosters.Count; z++) {
                                    if (((Button)o).Id == bookmarkPosters[z].button.Id) {
                                        PushPageFromUrlAndName(bookmarkPosters[z].id, bookmarkPosters[z].name);
                                    }
                                }
                            };*/
                        }
                    }
                    // data.Add(App.GetKey("BookmarkData"))
                }

                MScroll.HeightRequest = keys.Count > 0 ? 130 : 0;
                if (ImdbTypePicker.SelectedIndex == -1) {

                    ImdbTypePicker.SelectedIndex = bookmarkPosters.Count > 0 ? 0 : 2; // SET TO POPULAR BY DEAFULT
                }
            }
            catch (Exception _ex) {

                error(_ex);
            }

        }
        public double currentWidth { get { return Application.Current.MainPage.Width; } }



        private void Grid_BindingContextChanged(object sender, EventArgs e)
        {
            var s = ((Grid)sender);
            Commands.SetTap(s, new Command((o) => {
                var episodeResult = ((EpisodeResult)o);
                PushPageFromUrlAndName(FindShortend(episodeResult.extraInfo, "Id"), FindShortend(episodeResult.extraInfo, "Name"));
                PushPage(episodeResult);

                //do something
            }));
            Commands.SetTapParameter(s, (EpisodeResult)s.BindingContext);
            //s.BindingContext = this;
        }
    }
    public class MainEpisode100View
    {
        private ObservableCollection<EpisodeResult> _MyEpisodeResultCollection;
        public ObservableCollection<EpisodeResult> MyEpisodeResultCollection { set { Added?.Invoke(null, null); _MyEpisodeResultCollection = value; } get { return _MyEpisodeResultCollection; } }

        public event EventHandler Added;

        public MainEpisode100View()
        {
            MyEpisodeResultCollection = new ObservableCollection<EpisodeResult>();
        }
    }

}