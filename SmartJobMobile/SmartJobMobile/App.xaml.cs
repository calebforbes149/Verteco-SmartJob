using SmartJobMobile.Services;

namespace SmartJobMobile
{
    public partial class App : Application
    {
        private readonly SessionService
            _sessionService;

        public App(
            SessionService sessionService)
        {
            InitializeComponent();

            _sessionService =
                sessionService;
        }

        protected override Window CreateWindow(
            IActivationState? activationState)
        {
            return new Window(
                new AppShell(
                    _sessionService));
        }
    }
}