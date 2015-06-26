using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace HCBITSMonitor
{

    /// <summary>
    /// Enables a WPF application to run as a "single instance".  If a user attempts to start
    /// multiple instances, the first instance will be notified and subsequent instances
    /// will be immediately shutdown.
    /// </summary>
    internal class SingleInstanceApplication
    {

        /// <summary>
        /// Called when the first instance is started. When this event fires,
        /// the instance is clear to begin resource allocation and application startup.
        /// </summary>
        public event EventHandler ApplicationStarts;

        /// <summary>
        /// Called on the first instance when subsequent instances attempt to start.
        /// Use this to bring a window forward, etc.
        /// </summary>
        public event EventHandler AnotherInstanceAttemptsToStart;        
                        
        private static Mutex mutex;
        
        private Guid id;

        /// <summary>
        /// Instantiate an instance before you begin any work in the application. 
        /// Provide a unique application identifier.
        /// </summary>        
        public SingleInstanceApplication(Guid id) 
        {
            this.id = id;                         
        }
        

        /// <summary>
        /// After registering at least ApplicationStarts, call this method to ensure this instance is the single instance.
        /// If this instance is the first (single) instance, the ApplicationStarts event will be triggered.
        /// If this instance is not the first (single) instance, the application will be terminated immediately.
        /// </summary>
        public void RunSingleInstance()
        {
            // get information from the mutex if we're the first/single instance.
            bool owned = false;
            mutex = new Mutex(false, "Global\\{" + id.ToString() + "}", out owned);
            if (owned) // we "own" the mutex, meaning we're the first instance
            {                
                // inform the application to begin startup
                if (ApplicationStarts != null)
                    ApplicationStarts(this, new EventArgs());

                // prepare a named pipe to receive data from subsequent start attempts
                ListenNamedPipe();

            }
            else // In this case, we're a subsequent instance.
            {
                // Connect to the named pipe, if able, to signal the original instance that subsequent startup has been attempted
                // This could be extended to send actual data to the original instance
                try
                {
                    var np = new System.IO.Pipes.NamedPipeClientStream(".", id.ToString(), PipeDirection.Out);
                    np.Connect(0);
                    np.Dispose();
                }
                catch { }

                // Regardless, shut down immediately
                Application.Current.Shutdown();
            }
        }

        private void ListenNamedPipe()
        {
            var np = new System.IO.Pipes.NamedPipeServerStream(id.ToString(), System.IO.Pipes.PipeDirection.In, 1, System.IO.Pipes.PipeTransmissionMode.Byte, System.IO.Pipes.PipeOptions.Asynchronous);

            np.BeginWaitForConnection(AsyncCallback, np);                
        }

        private void AsyncCallback(IAsyncResult iar)
        {            
            // Accept the connection and then immediately close it (we don't care about it)
            // This could be extended to receive data from the other instance and
            // dispatch it in the AnotherInstanceAttemptsToStart event
            NamedPipeServerStream pipeServer = (NamedPipeServerStream)iar.AsyncState;            
            pipeServer.EndWaitForConnection(iar);
            pipeServer.Close();

            // once the pipe is used, a new pipe must be created
            ListenNamedPipe();            

            // inform the application
            if (AnotherInstanceAttemptsToStart != null)
                AnotherInstanceAttemptsToStart(this, new EventArgs());
        }

        /// <summary>
        /// Helper method for WPF methods.  Provide a WPF window and this event handler, when called,
        /// will bring that window forward to the normal state.
        /// </summary>        
        public static EventHandler BringToFrontWhenCalled(Window w)
        {
            return new EventHandler((obj, e) =>
            {
                if (w != null)
                {
                    // We won't be on the dispatcher thread, so invoke across
                    w.Dispatcher.BeginInvoke(new Action(() => 
                    {
                        // expand if minimized
                        if (w.WindowState == WindowState.Minimized)
                        {
                            w.WindowState = WindowState.Normal;
                        }
                        

                        bool top = w.Topmost;
                        // make our form jump to the top of everything
                        w.Topmost = true;
                        // set it back to whatever it was
                        w.Topmost = top;
                    }));
                    
                }
            });
        }

       
        
    }
}
