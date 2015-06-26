# Enforcing Single Application Instances in WPF with Named Pipes

See <a href="http://www.kolls.net/blog/?p=171">blog post</a>.

## Usage: 
```cs
    public partial class App : Application
    {
        
        protected override void OnStartup(StartupEventArgs e)
        {
            // hook on error before app really starts
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            base.OnStartup(e);

            // Set a unique application ID
            Guid id = Guid.Parse("DC2A927C-AC89-4512-BB29-7AB0A18DE105");

            // Instantiate an SIA
            SingleInstanceApplication sia = new SingleInstanceApplication(id);            
            // Handle the ApplicationStarts event
            // When this event fires, initialize the application
            sia.ApplicationStarts += (sender, earg) =>
            {
                var mw = new MainWindow();
                mw.Show();
                // If another instance attempts to start, bring our window to the front
                sia.AnotherInstanceAttemptsToStart += SingleInstanceApplication.BringToFrontWhenCalled(mw);
            };
            // Optionally handle AnotherInstanceAttemptsToStart, for example, to log other attempts
            sia.AnotherInstanceAttemptsToStart += (sender, earg) =>
            {
                Logger.LogInfo("### Captured another instance trying to start");

            };
            // Run the application and single instance protection
            sia.RunSingleInstance();            

        }
```        
