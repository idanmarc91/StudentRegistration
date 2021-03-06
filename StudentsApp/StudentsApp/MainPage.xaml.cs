﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace StudentsApp
{

	public partial class MainPage : ContentPage
	{
        AzureService azureService;

        public MainPage()
        {
            InitializeComponent();

            azureService = AzureService.DefaultManager;

            // OnPlatform<T> doesn't currently support the "Windows" target platform, so we have this check here.
            //if (azureservice.isofflineenabled )
            //{
            //    var syncbutton = new button
            //    {
            //        text = "sync items",
            //        heightrequest = 30
            //    };
            //    syncbutton.clicked += onsyncitems;

            //    buttonspanel.children.add(syncbutton);
            //}
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Set syncItems to true in order to synchronize the data on startup when running in offline mode
            await RefreshItems(true, syncItems: true);
        }

        // Data methods
        async Task AddStudent(Student item)
        {
            await azureService.SaveTaskAsync(item);
            students_list.ItemsSource = await azureService.GetStudentsAsync();
        }

        //async Task CompleteItem(TodoItem item)
        //{
        //    item.Done = true;
        //    await manager.SaveTaskAsync(item);
        //    todoList.ItemsSource = await manager.GetTodoItemsAsync();
        //}

        public async void OnAdd(object sender, EventArgs e)
        {
            var student = new Student { studentName = entry.Text };
            await AddStudent(student);

            entry.Text = string.Empty;
            entry.Unfocus();
        }

        //// Event handlers
        //public async void OnSelected(object sender, SelectedItemChangedEventArgs e)
        //{
        //    var student = e.SelectedItem as TodoItem;
        //    if (Device.OS != TargetPlatform.iOS && todo != null)
        //    {
        //        // Not iOS - the swipe-to-delete is discoverable there
        //        if (Device.OS == TargetPlatform.Android)
        //        {
        //            await DisplayAlert(todo.Name, "Press-and-hold to complete task " + todo.Name, "Got it!");
        //        }
        //        else
        //        {
        //            // Windows, not all platforms support the Context Actions yet
        //            if (await DisplayAlert("Mark completed?", "Do you wish to complete " + todo.Name + "?", "Complete", "Cancel"))
        //            {
        //                await CompleteItem(todo);
        //            }
        //        }
        //    }

        //    // prevents background getting highlighted
        //    todoList.SelectedItem = null;
        //}

        //// http://developer.xamarin.com/guides/cross-platform/xamarin-forms/working-with/listview/#context
        //public async void OnComplete(object sender, EventArgs e)
        //{
        //    var mi = ((MenuItem)sender);
        //    var todo = mi.CommandParameter as TodoItem;
        //    await CompleteItem(todo);
        //}

        // http://developer.xamarin.com/guides/cross-platform/xamarin-forms/working-with/listview/#pulltorefresh
        public async void OnRefresh(object sender, EventArgs e)
        {
            var list = (ListView)sender;
            Exception error = null;
            try
            {
                await RefreshItems(false, true);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                list.EndRefresh();
            }

            if (error != null)
            {
                await DisplayAlert("Refresh Error", "Couldn't refresh data (" + error.Message + ")", "OK");
            }
        }

        public async void OnSyncItems(object sender, EventArgs e)
        {
            await RefreshItems(true, true);
        }

        private async Task RefreshItems(bool showActivityIndicator, bool syncItems)
        {
            using (var scope = new ActivityIndicatorScope(syncIndicator, showActivityIndicator))
            {
                students_list.ItemsSource = await azureService.GetStudentsAsync(syncItems);
            }
        }

        private class ActivityIndicatorScope : IDisposable
        {
            private bool showIndicator;
            private ActivityIndicator indicator;
            private Task indicatorDelay;

            public ActivityIndicatorScope(ActivityIndicator indicator, bool showIndicator)
            {
                this.indicator = indicator;
                this.showIndicator = showIndicator;

                if (showIndicator)
                {
                    indicatorDelay = Task.Delay(2000);
                    SetIndicatorActivity(true);
                }
                else
                {
                    indicatorDelay = Task.FromResult(0);
                }
            }

            private void SetIndicatorActivity(bool isActive)
            {
                this.indicator.IsVisible = isActive;
                this.indicator.IsRunning = isActive;
            }

            public void Dispose()
            {
                if (showIndicator)
                {
                    indicatorDelay.ContinueWith(t => SetIndicatorActivity(false), TaskScheduler.FromCurrentSynchronizationContext());
                }
            }
        }
    }
    
}
