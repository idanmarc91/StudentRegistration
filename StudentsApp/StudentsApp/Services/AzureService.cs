using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices;
using Microsoft.WindowsAzure.MobileServices.SQLiteStore;
using Microsoft.WindowsAzure.MobileServices.Sync;
using Plugin.Connectivity;
using StudentsApp;
using Xamarin.Forms;


[assembly: Dependency(typeof(AzureService))]
namespace StudentsApp
{
	public class AzureService 
	{
        static AzureService defaultInstance = new AzureService();
        MobileServiceClient client;

#if OFFLINE_SYNC_ENABLED
            IMobileServiceSyncTable<TodoItem> todoTable;
#else
            IMobileServiceTable<Student> studentTable;
#endif
        const string offlineDbPath = @"localstore.db";

        //constractor function
        private AzureService()
        {
            this.client = new MobileServiceClient(Constants.ApplicationURL);

#if OFFLINE_SYNC_ENABLED
            var store = new MobileServiceSQLiteStore(offlineDbPath);
            store.DefineTable<TodoItem>();

            //Initializes the SyncContext using the default IMobileServiceSyncHandler.
            this.client.SyncContext.InitializeAsync(store);

            this.todoTable = client.GetSyncTable<TodoItem>();
#else
            this.studentTable = client.GetTable<Student>();
#endif
        }

        public static AzureService DefaultManager
        {
            get
            {
                return defaultInstance;
            }
            private set
            {
                defaultInstance = value;
            }
        }

        public MobileServiceClient CurrentClient
        {
            get { return client; }
        }

        public bool IsOfflineEnabled
        {
            get { return studentTable is Microsoft.WindowsAzure.MobileServices.Sync.IMobileServiceSyncTable<Student>; }
        }

        public async Task<ObservableCollection<Student>> GetStudentsAsync(bool syncItems = false)
        {
            try
            {
            #if OFFLINE_SYNC_ENABLED
                if (syncItems)
                {
                    await this.SyncAsync();
                }
            #endif
                IEnumerable<Student> items = await studentTable
                    .ToEnumerableAsync();

                return new ObservableCollection<Student>(items);
            }
            catch (MobileServiceInvalidOperationException msioe)
            {
                Debug.WriteLine(@"Invalid sync operation: {0}", msioe.Message);
            }
            catch (Exception e)
            {
                Debug.WriteLine(@"Sync error: {0}", e.Message);
            }
            return null;
        }

        public async Task SaveTaskAsync(Student item)
        {
            if (item.Id == null)
            {
                await studentTable.InsertAsync(item);
            }
            else
            {
                await studentTable.UpdateAsync(item);
            }
        }

#if OFFLINE_SYNC_ENABLED
        public async Task SyncAsync()
        {
            ReadOnlyCollection<MobileServiceTableOperationError> syncErrors = null;

            try
            {
                await this.client.SyncContext.PushAsync();

                await this.todoTable.PullAsync(
                    //The first parameter is a query name that is used internally by the client SDK to implement incremental sync.
                    //Use a different query name for each unique query in your program
                    "allTodoItems",
                    this.todoTable.CreateQuery());
            }
            catch (MobileServicePushFailedException exc)
            {
                if (exc.PushResult != null)
                {
                    syncErrors = exc.PushResult.Errors;
                }
            }

            // Simple error/conflict handling. A real application would handle the various errors like network conditions,
            // server conflicts and others via the IMobileServiceSyncHandler.
            if (syncErrors != null)
            {
                foreach (var error in syncErrors)
                {
                    if (error.OperationKind == MobileServiceTableOperationKind.Update && error.Result != null)
                    {
                        //Update failed, reverting to server's copy.
                        await error.CancelAndUpdateItemAsync(error.Result);
                    }
                    else
                    {
                        // Discard local change.
                        await error.CancelAndDiscardItemAsync();
                    }

                    Debug.WriteLine(@"Error executing sync operation. Item: {0} ({1}). Operation discarded.", error.TableName, error.Item["id"]);
                }
            }
        }
#endif



        /// <summary>
        /// ////////////////////////////////////////////
        /// </summary>
        //        MobileServiceClient client = null;

        //        IMobileServiceSyncTable<Student> m_studentTable;

        //		public async Task Initialize()
        //        {
        //            if (client?.SyncContext?.IsInitialized ?? false)
        //                return;

        //            //setup online portal
        //            var appUrl = "https://studentapp10.azurewebsites.net";
        //            client = new MobileServiceClient(appUrl);

        //            var fileName = "students.db";
        //            //create local database
        //            var store = new MobileServiceSQLiteStore(fileName);
        //            //define table. define as many as we want..
        //            store.DefineTable<Student>();

        //            //initialize the sync context
        //            await client.SyncContext.InitializeAsync(store);
        //            //get table
        //            m_studentTable = client.GetSyncTable<Student>();

        //        }

        //        public async Task SyncStudent()
        //        {
        //            await Initialize();
        //            try
        //            {
        //                //check if we are connected. if not dont do anything
        //                if (!CrossConnectivity.Current.IsConnected)
        //                    return;

        //                //if we are online
        //                await client.SyncContext.PushAsync();
        //                //query to the backend code
        //                await m_studentTable.PullAsync("students", m_studentTable.CreateQuery());

        //            }
        //            catch (Exception ex)
        //            {
        //                Debug.WriteLine(ex);
        //            }
        //        }

        //        public async Task <IEnumerable<Student>> GetStudents()
        //        {
        //            await Initialize();

        //            await SyncStudent();

        //            var data = await m_studentTable.ToEnumerableAsync();

        //            return data;
        //        }

        //        public async Task<Student> AddStudent(string student_name)
        //        {
        //            await Initialize();

        //            var student = new Student
        //            {
        //                studentName = student_name
        //            };

        //            await m_studentTable.InsertAsync(student);

        //            await SyncStudent();

        //            return student;
        //        }
    }
}