﻿using BrailleIO.Interface;
using BrailleIO.Structs;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace BrailleIO
{
    /// <summary>
    /// Abstract implementation for basic functions a real Hardware modeling device Adapter has to implement
    /// </summary>
    public abstract class AbstractBrailleIOAdapterBase : IBrailleIOAdapter2, IDisposable
    {
        // display options

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="AbstractBrailleIOAdapterBase"/> is synchronize.
        /// </summary>
        /// <value><c>true</c> if synchronize; otherwise, <c>false</c>.</value>
        public bool Synch
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a flag indicating whether the pins should bee locked.
        /// This means no more synchronization should be possible. 
        /// The last pin state stays as set until the lock is released and Synchronize(bool[,] matrix) is called.
        /// </summary>
        /// <value><c>true</c> if [lock pins]; otherwise, <c>false</c>.</value>
        public bool LockPins
        {
            get;
            set;
        }

        BrailleIODevice _device;
        /// <summary>
        /// Gets or sets the device.
        /// The device gives access to specific properties of the modeled hardware device.
        /// </summary>
        /// <value>The device.</value>
        public BrailleIODevice Device
        {
            get { if (_device == null) _device = createDevice(); return _device; }
            protected set { _device = value; }
        }

        /// <summary>
        /// Creates an dummy device width dimensions of a Metec BrailleDis device.
        /// </summary>
        /// <returns></returns>
        protected virtual BrailleIODevice createDevice()
        {
            return new BrailleIODevice(120, 60, "UNKNOWN", true, true, 10, this.GetType().ToString());
        }

        private float _dpiX = 10;
        /// <summary>
        /// Gets or sets the horizontal resolution of the pin matrix for this device.
        /// </summary>
        /// <value>The horizontal device resolution in dpi.</value>
        public virtual float DpiX
        {
            get { return _dpiX; }
        }

        private float _dpiY = 10;
        /// <summary>
        /// Gets or sets the vertical resolution of the pin matrix for this device.
        /// </summary>
        /// <value>The vertical device resolution in dpi.</value>
        public virtual float DpiY
        {
            get { return _dpiY; }
        }

        private bool _connected = false;
        /// <summary>
        /// Gets a value indicating whether this <see cref="AbstractBrailleIOAdapterBase"/> is connected or not.
        /// </summary>
        /// <value><c>true</c> if connected; otherwise, <c>false</c>.</value>
        public bool Connected
        {
            get
            {
                if (!_connected) _connected = this.Connect();
                return _connected;
            }
            protected set { _connected = value; }
        }

        /// <summary>
        /// The adapter manager this adapter is registered in.
        /// </summary>
        protected IBrailleIOAdapterManager manager = null;

        #region ButtonStates

        /// <summary>
        /// Gets all currently pressed device buttons.
        /// </summary>
        /// <value>
        /// The currently pressed device buttons.
        /// </value>
        public BrailleIO_DeviceButton PressedDeviceButtons { get; protected set; }

        /// <summary>
        /// Gets all currently pressed braille keyboard buttons.
        /// </summary>
        /// <value>
        /// The currently pressed braille keyboard buttons.
        /// </value>
        public BrailleIO_BrailleKeyboardButton PressedBrailleKeyboardButtons { get; protected set; }

        /// <summary>
        /// Gets all currently pressed additional buttons.
        /// </summary>
        /// <value>
        /// The currently pressed additional buttons.
        /// </value>
        public BrailleIO_AdditionalButton[] PressedAdditionalButtons { get; protected set; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractBrailleIOAdapterBase"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        public AbstractBrailleIOAdapterBase(IBrailleIOAdapterManager manager)
        {
            this.manager = manager;
            PressedDeviceButtons = BrailleIO_DeviceButton.None;
            PressedBrailleKeyboardButtons = BrailleIO_BrailleKeyboardButton.None;
            PressedAdditionalButtons = null;
        }

        /// <summary>
        /// Connects this instance.
        /// </summary>
        /// <returns><c>true</c> if connected successfully; otherwise, <c>false</c>.</returns>
        public virtual bool Connect()
        {
            if (manager.ActiveAdapter == null)
            {
                manager.ActiveAdapter = this;
                _connected = true;
            }
            return _connected;
        }

        /// <summary>
        /// Disconnects this instance.
        /// </summary>
        /// <returns><c>true</c> if disconnected successfully; otherwise, <c>false</c>.</returns>
        public virtual bool Disconnect()
        {
            manager.ActiveAdapter = null;
            fireClosed();
            _connected = false;
            return true;
        }

        #region on before event handlers
        internal virtual void OnBrailleIO_KeyPressed_EventHandler(BrailleIO_KeyPressed_EventArgs e) { keyPressed(this, e); }
        internal virtual void OnBrailleIO_KeyStateChanged_EventHandler(BrailleIO_KeyStateChanged_EventArgs e) { keyStateChanged(this, e); }
        internal virtual void OnBrailleIO_TouchValuesChanged_EventHandler(BrailleIO_TouchValuesChanged_EventArgs e) { touchValuesChanged(this, e); }
        internal virtual void OnBrailleIO_PinStateChanged_EventHandler(BrailleIO_PinStateChanged_EventArgs e) { pinStateChanged(this, e); }
        internal virtual void OnBrailleIO_ErrorOccured_EventHandler(BrailleIO_ErrorOccured_EventArgs e) { errorOccurred(this, e); }
        #endregion

        /// <summary>
        /// Occurs when a key was pressed.
        /// </summary>
        public event EventHandler<BrailleIO_KeyPressed_EventArgs> keyPressed;
        /// <summary>
        /// Occurs when the state of a key has changed. This can be a pressed or a released
        /// </summary>
        public event EventHandler<BrailleIO_KeyStateChanged_EventArgs> keyStateChanged;
        /// <summary>
        /// Occurs when the device was successfully initialized.
        /// </summary>
        public event EventHandler<BrailleIO_Initialized_EventArgs> initialized;
        /// <summary>
        /// Occurs when the device was closed.
        /// </summary>
        public event EventHandler<BrailleIO_Initialized_EventArgs> closed;
        /// <summary>
        /// Occurs when some properties of the input changes.
        /// </summary>
        public event EventHandler<BrailleIO_InputChanged_EventArgs> inputChanged;
        /// <summary>
        /// Occurs when some touch values had changed.
        /// </summary>
        public event EventHandler<BrailleIO_TouchValuesChanged_EventArgs> touchValuesChanged;
        /// <summary>
        /// Occurs when  some pin states had changed.
        /// </summary>
        public event EventHandler<BrailleIO_PinStateChanged_EventArgs> pinStateChanged;
        /// <summary>
        /// Occurs when an error has occurred.
        /// </summary>
        public event EventHandler<BrailleIO_ErrorOccured_EventArgs> errorOccurred;
        /// <summary>
        /// Occurs when a key combination was released.
        /// </summary>
        public event EventHandler<BrailleIO_KeyCombinationReleased_EventArgs> keyCombinationReleased;

        /// <summary>
        /// Fires an initialized event.
        /// </summary>
        /// <param name="e">The <see cref="BrailleIO.Interface.BrailleIO_Initialized_EventArgs"/> instance containing the event data.</param>
        protected virtual void fireInitialized(BrailleIO_Initialized_EventArgs e = null)
        {
            if (initialized != null)
            {
                try
                {
                    if (e == null) e = new BrailleIO_Initialized_EventArgs(this.Device);
                    initialized(this, new BrailleIO_Initialized_EventArgs(this.Device));
                }
                catch { }
            }
        }

        /// <summary>
        /// Fires an initialized event.
        /// </summary>
        /// <param name="e">The <see cref="BrailleIO.Interface.BrailleIO_Initialized_EventArgs"/> instance containing the event data.</param>
        protected virtual void fireClosed(BrailleIO_Initialized_EventArgs e = null)
        {
            if (closed != null)
            {
                if (e == null) e = new BrailleIO_Initialized_EventArgs(this.Device);
                {
                    try { closed(this, e); }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Fires a key state changed event.
        /// </summary>
        /// <param name="keyCode">The key code.</param>
        /// <param name="raw">The raw.</param>
        /// <param name="keyboardCode">optional combined Braille keyboard button states.</param>
        /// <param name="additionalKeyCode">list of optional combined additional button states.</param>
        protected virtual void fireKeyStateChanged(
            BrailleIO_DeviceButtonStates keyCode,
            ref OrderedDictionary raw,
            BrailleIO_BrailleKeyboardButtonStates keyboardCode = BrailleIO_BrailleKeyboardButtonStates.None,
            BrailleIO_AdditionalButtonStates[] additionalKeyCode = null)
        {
            BrailleIO_DeviceButton pressed = BrailleIO_DeviceButton.None;
            BrailleIO_DeviceButton released = BrailleIO_DeviceButton.None;

            BrailleIO_BrailleKeyboardButton pressedKbB = BrailleIO_BrailleKeyboardButton.None;
            BrailleIO_BrailleKeyboardButton releasedKbB = BrailleIO_BrailleKeyboardButton.None;

            Dictionary<int, BrailleIO_AdditionalButton> pressedAdBs = null;
            Dictionary<int, BrailleIO_AdditionalButton> releasedAdBs = null;
            try
            {
                // general buttons
                updatePressedDeviceButtons(keyCode, out pressed, out released);

                // braille keyboard buttons
                updatePressedKeyboardButtons(keyboardCode, out pressedKbB, out releasedKbB);

                // additional buttons
                updatePressedAdditionalButtons(additionalKeyCode, out pressedAdBs, out releasedAdBs);
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("ERROR in updating the current button states");
            }
            finally
            {
                // TODO: what to do to get it valid again?!
            }

            if (keyStateChanged != null)
            {
                try
                {
                    keyStateChanged(this, new BrailleIO_KeyStateChanged_EventArgs(keyCode, ref raw, keyboardCode, additionalKeyCode));
                }
                catch { }

            }

            checkForKeyCombination(
                pressed, released,
                pressedKbB, releasedKbB,
                pressedAdBs, releasedAdBs);
        }

        #region Button Helper Functions

        private object _syncLock = new Object();

        private void updatePressedAdditionalButtons(
            BrailleIO_AdditionalButtonStates[] additionalKeyCode,
            out Dictionary<int, BrailleIO_AdditionalButton> pressedAdBs,
            out Dictionary<int, BrailleIO_AdditionalButton> releasedAdBs
            )
        {
            pressedAdBs = new Dictionary<int, BrailleIO_AdditionalButton>();
            releasedAdBs = new Dictionary<int, BrailleIO_AdditionalButton>();

            if (additionalKeyCode != null && additionalKeyCode.Length > 0)
            {
                lock (_syncLock)
                {
                    if (PressedAdditionalButtons == null) { PressedAdditionalButtons = new BrailleIO_AdditionalButton[additionalKeyCode.Length]; }
                    else if (additionalKeyCode.Length > PressedAdditionalButtons.Length)
                    {
                        var _tmp = PressedAdditionalButtons;
                        PressedAdditionalButtons = new BrailleIO_AdditionalButton[additionalKeyCode.Length];
                        for (int i = 0; i < _tmp.Length; i++) { PressedAdditionalButtons[i] = _tmp[i]; }
                        _tmp = null;
                    }

                    for (int i = 0; i < additionalKeyCode.Length; i++)
                    {
                        var pressedAdB = Utils.GetAllDownAdditionalButtons(additionalKeyCode[i]);
                        PressedAdditionalButtons[i] = PressedAdditionalButtons[i] | pressedAdB;
                        pressedAdBs.Add(i, pressedAdB);
                        var releasedAdB = Utils.GetAllUpAdditionalButtons(additionalKeyCode[i]);
                        PressedAdditionalButtons[i] = PressedAdditionalButtons[i] ^ releasedAdB;
                        releasedAdBs.Add(i, releasedAdB);
                    }
                }
            }
        }

        private void updatePressedKeyboardButtons(
            BrailleIO_BrailleKeyboardButtonStates keyboardCode,
            out BrailleIO_BrailleKeyboardButton pressedKbB,
            out BrailleIO_BrailleKeyboardButton releasedKbB)
        {
            pressedKbB = BrailleIO_BrailleKeyboardButton.None;
            releasedKbB = BrailleIO_BrailleKeyboardButton.None;

            if (keyboardCode != BrailleIO_BrailleKeyboardButtonStates.None)
            {
                lock (_syncLock)
                {
                    pressedKbB = Utils.GetAllDownBrailleKeyboardButtons(keyboardCode);
                    PressedBrailleKeyboardButtons = PressedBrailleKeyboardButtons | pressedKbB;
                    releasedKbB = Utils.GetAllUpBrailleKeyboardButtons(keyboardCode);
                    PressedBrailleKeyboardButtons = PressedBrailleKeyboardButtons ^ releasedKbB;
                }
            }
        }

        private void updatePressedDeviceButtons(BrailleIO_DeviceButtonStates keyCode,
            out BrailleIO_DeviceButton pressedDB,
            out BrailleIO_DeviceButton releasedDB)
        {
            pressedDB = BrailleIO_DeviceButton.None;
            releasedDB = BrailleIO_DeviceButton.None;

            if (keyCode != BrailleIO_DeviceButtonStates.None)
            {
                lock (_syncLock)
                {
                    pressedDB = Utils.GetAllDownDeviceButtons(keyCode);
                    PressedDeviceButtons = PressedDeviceButtons | pressedDB;
                    releasedDB = Utils.GetAllUpDeviceButtons(keyCode);
                    PressedDeviceButtons = PressedDeviceButtons ^ releasedDB;
                }
            }
        }

        #region Key Combination Interpreter

        private double _keyCombinationTimerInterval = 500;
        /// <summary>
        /// The time interval for collecting key released events to combine them as one 
        /// key combination in milliseconds.
        /// After this timespan the released keys are sent as a key combination.
        /// </summary>
        public double KeyCombinationTimeout
        {
            get { return _keyCombinationTimerInterval; }
            set { _keyCombinationTimerInterval = value; }
        }
        readonly Object _kcLock = new Object();
        Object _kc = null;
        /// <summary>
        /// A placeholder for a global collection of button states.
        /// </summary>
        /// <value>
        /// The key combination collector.
        /// </value>
        protected Object Kc
        {
            get
            {
                lock (_kcLock)
                {
                    return _kc;
                }
            }
            set
            {
                lock (_kcLock)
                {
                    _kc = value;
                }
            }
        }

        System.Timers.Timer _keyCombinationTimer = null;
        /// <summary>
        /// Gets or sets the key combination timer collection.
        /// </summary>
        /// <value>
        /// The key combination timer.
        /// </value>
        protected System.Timers.Timer keyCombinationTimer
        {
            get
            {
                if (_keyCombinationTimer == null)
                {
                    _keyCombinationTimer = new System.Timers.Timer(KeyCombinationTimeout);
                }
                return _keyCombinationTimer;
            }
            set { _keyCombinationTimer = value; }
        }

        /// <summary>
        /// Checks if a key combination was released.
        /// </summary>
        /// <param name="pressed">The pressed general buttons.</param>
        /// <param name="released">The released general buttons.</param>
        /// <param name="pressedKbB">The pressed Braille keyboard buttons.</param>
        /// <param name="releasedKbB">The released Braille keyboard buttons.</param>
        /// <param name="pressedAdBs">The pressed additional buttons.</param>
        /// <param name="releasedAdBs">The released additional buttons.</param>
        private void checkForKeyCombination(
            BrailleIO_DeviceButton pressed,
            BrailleIO_DeviceButton released,
            BrailleIO_BrailleKeyboardButton pressedKbB,
            BrailleIO_BrailleKeyboardButton releasedKbB,
            Dictionary<int, BrailleIO_AdditionalButton> pressedAdBs,
            Dictionary<int, BrailleIO_AdditionalButton> releasedAdBs)
        {

            //System.Diagnostics.Debug.WriteLine("---- pressed: " + pressed);
            //System.Diagnostics.Debug.WriteLine("---- released: " + released);
            //System.Diagnostics.Debug.WriteLine("---- pressedKbB: " + pressedKbB);
            //System.Diagnostics.Debug.WriteLine("---- releasedKbB: " + releasedKbB);
            //System.Diagnostics.Debug.WriteLine("---- pressedAdBs: " + String.Join(", ", pressedAdBs.Values));
            //System.Diagnostics.Debug.WriteLine("---- releasedAdBs: " + String.Join(", ", releasedAdBs.Values));

            if (keyCombinationTimer != null) // if timer is running for collecting released events
            {
                keyCombinationTimer.Stop(); // stop further expiring

                if (!(Kc is KeyCombinationItem))
                {
                    Kc = new KeyCombinationItem(
                        BrailleIO_DeviceButton.None,
                        BrailleIO_DeviceButton.None,
                        BrailleIO_BrailleKeyboardButton.None,
                        BrailleIO_BrailleKeyboardButton.None,
                        null, null);
                }

                KeyCombinationItem kc = (KeyCombinationItem)Kc;

                // general keys
                kc.PressedGeneralKeys = (this.PressedDeviceButtons & ~BrailleIO_DeviceButton.Unknown);
                kc.ReleasedGeneralKeys |= released;
                kc.ReleasedGeneralKeys = kc.ReleasedGeneralKeys & ~kc.PressedGeneralKeys & ~BrailleIO_DeviceButton.Unknown;
                                
                // keyboard
                kc.PressedKeyboardKeys = this.PressedBrailleKeyboardButtons;
                kc.ReleasedKeyboardKeys |= releasedKbB;
                kc.ReleasedKeyboardKeys = kc.ReleasedKeyboardKeys & ~kc.PressedKeyboardKeys;

                // additional
                kc.PressedAdditionalKeys = this.PressedAdditionalButtons;
                kc.ReleasedAdditionalKeys = Utils.CombineAdditionalButtonCollections(kc.ReleasedAdditionalKeys, pressedAdBs);
                
                Kc = kc; // store globally
                keyCombinationTimer.Start();

                if (!kc.AreButtonsPressed()) //if no more buttons pressed
                {
                    t_Elapsed(keyCombinationTimer, null);
                }
            }
        }

        void t_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Timers.Timer t = sender as System.Timers.Timer;
            if (t != null)
            {
                t.Stop();

                if (t == keyCombinationTimer) // reset the timer
                {
                    keyCombinationTimer.Elapsed -= t_Elapsed;
                    keyCombinationTimer = null;
                }

                //try get the keys
                if (Kc is KeyCombinationItem)
                {
                    KeyCombinationItem kc = (KeyCombinationItem)Kc;

                    Kc = null; // reset the stored keys during handling.
                    t.Elapsed -= t_Elapsed;
                    t = null;

                    if (kc.AreButtonsReleased())
                    {
                        fireKeyCombinationReleased(kc);
                    }
                    else
                    {

                    }
                }
            }
        }

        #endregion

        #endregion

        /// <summary>
        /// Fires an input changed event.
        /// </summary>
        /// <param name="touches">The touches.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="raw">The raw.</param>
        protected virtual void fireInputChanged(bool[,] touches, int timestamp, ref OrderedDictionary raw)
        {
            if (inputChanged != null)
                try
                {
                    inputChanged(this, new BrailleIO_InputChanged_EventArgs(touches, timestamp, ref raw));
                }
                catch { }
        }

        /// <summary>
        /// Fires the key combination released event.
        /// </summary>
        /// <param name="keyCombination">The key combination container item.</param>
        protected virtual void fireKeyCombinationReleased(
            KeyCombinationItem keyCombination
            )
        {
            if (keyCombinationReleased != null)
            {
                try
                {
                    // FIXME: only for debugging
                    System.Diagnostics.Debug.WriteLine("Key combination released event: {0}", keyCombination);

                    keyCombinationReleased(this, new BrailleIO_KeyCombinationReleased_EventArgs(keyCombination));
                }
                catch { }
            }
        }

        /// <summary>
        /// Fires a touch values changed event.
        /// </summary>
        /// <param name="touches">The touches.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="raw">The raw.</param>
        protected virtual void fireTouchValuesChanged(double[,] touches, int timestamp, ref OrderedDictionary raw)
        { fireTouchValuesChanged(touches, timestamp, ref raw, null); }
        /// <summary>
        /// Fires a touch values changed event.
        /// </summary>
        /// <param name="touches">The touches.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="raw">The raw.</param>
        /// <param name="detailedTouch">An optional list of detailed touch information.</param>
        protected virtual void fireTouchValuesChanged(double[,] touches, int timestamp, ref OrderedDictionary raw, List<Touch> detailedTouch = null)
        {
            if (touchValuesChanged != null)
                try
                {
                    touchValuesChanged(this, new BrailleIO_TouchValuesChanged_EventArgs(touches, timestamp, ref raw, detailedTouch));
                }
                catch { }
        }

        /// <summary>
        /// Fires an error occurred event.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="raw">The raw.</param>
        public virtual void fireErrorOccured(ErrorCode errorCode, ref OrderedDictionary raw)
        {
            if (errorOccurred != null)
                try
                {
                    errorOccurred(this, new BrailleIO_ErrorOccured_EventArgs(errorCode, ref raw));
                }
                catch { }
        }

        /// <summary>
        /// Synchronizes the specified matrix. 
        /// That means the Adapter try to sent the given Matrix to the real hardware 
        /// device as an output.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        public virtual void Synchronize(bool[,] matrix)
        {
            if (!LockPins) manager.ActiveAdapter.Synchronize(matrix);
        }

        #region Touch

        /// <summary>
        /// Recalibrate the Touch detection Adapter with the specified threshold.
        /// //FIXME: BAD-HACK function to find bad detectors. Make this real working
        /// </summary>
        /// <param name="threshold">The threshold.</param>
        /// <returns><c>true</c> if the devices touch abilities could be recalibrated</returns>
        public virtual bool Recalibrate(double threshold)
        {
            if (Device != null)
            {
                touchValuesChanged += new EventHandler<BrailleIO_TouchValuesChanged_EventArgs>(AbstractBrailleIOAdapterBase_touchValuesChanged);
                bool[,] empty = new bool[Device.DeviceSizeY, Device.DeviceSizeX];
                bool[,] full = GetFullSetMatrix();
                //clear the correction matrix 
                touchCorrectionMatrix = new double[Device.DeviceSizeY, Device.DeviceSizeX];

                System.Threading.Thread.Sleep(1000);
                LockPins = false;
                Synchronize(empty);
                LockPins = true;
                System.Threading.Thread.Sleep(1000);
                LockPins = false;
                Synchronize(full);
                LockPins = true;
                System.Threading.Thread.Sleep(1000);
                LockPins = false;
                Synchronize(empty);
                LockPins = true;
                System.Threading.Thread.Sleep(1000);
                LockPins = false;
                Synchronize(full);
                LockPins = true;
                System.Threading.Thread.Sleep(1000);
                LockPins = false;
                Synchronize(empty);
                LockPins = true;
                System.Threading.Thread.Sleep(1000);
                LockPins = false;
                Synchronize(full);
                LockPins = true;
                System.Threading.Thread.Sleep(1000);
                LockPins = false;
                Synchronize(empty);
                LockPins = true;
                System.Threading.Thread.Sleep(1000);
                LockPins = false;
                Synchronize(full);
                LockPins = true;
                System.Threading.Thread.Sleep(1000);
                LockPins = false;
                Synchronize(empty);
                LockPins = true;
                System.Threading.Thread.Sleep(3000);

                touchValuesChanged -= new EventHandler<BrailleIO_TouchValuesChanged_EventArgs>(AbstractBrailleIOAdapterBase_touchValuesChanged);
                touchCorrectionMatrix = tempTouchCorrectionMatrix;

                LockPins = false;
            }

            return true;
        }

        #region Touch matrix calibration Methods -- ONLY A TRY

        void AbstractBrailleIOAdapterBase_touchValuesChanged(object sender, BrailleIO_TouchValuesChanged_EventArgs e)
        {
            addToFlickeringTouchMatrix(e.touches);
        }

        double[,] _touchCorrectionMatrix;
        private static readonly object tcmLock = new object();
        /// <summary>
        /// Gets or sets the touch correction matrix.
        /// This matrix will be subtracted from the incoming touch matrix to correct the 
        /// sensory data.
        /// </summary>
        /// <value>
        /// The touch correction matrix.
        /// </value>
        protected double[,] touchCorrectionMatrix
        {
            get { return _touchCorrectionMatrix; }
            set { lock (tcmLock) { _touchCorrectionMatrix = value; } }
        }

        double[,] _tmpTouchCorrectionMatrix;
        static readonly object ttcmLock = new object();
        double[,] tempTouchCorrectionMatrix
        {
            get { return _tmpTouchCorrectionMatrix; }
            set { lock (ttcmLock) { _tmpTouchCorrectionMatrix = value; } }
        }

        private double[,] addToFlickeringTouchMatrix(double[,] tMatrix)
        {
            double[,] ftm = tempTouchCorrectionMatrix;

            if (ftm != null && tMatrix != null)
            {
                int rows = tMatrix.GetLength(0);
                int cols = tMatrix.GetLength(1);

                Parallel.For(0, rows, i =>
                {
                    // Parallel.For(0, cols, j =>
                    for (int j = 0; j < cols; j++)
                    {
                        try
                        {
                            // Use a temporary to improve parallel performance. 
                            double temp = 0;
                            temp += Math.Max(tMatrix[i, j], ftm[i, j]);
                            ftm[i, j] = temp;
                        }
                        catch (System.Exception) { }

                    }
                    //); // Parallel.For cols

                }); // Parallel.For rows
            }
            else
            {
                ftm = tMatrix;
            }
            tempTouchCorrectionMatrix = ftm;
            return ftm;
        }

        #endregion

        private bool[,] _fullMatrix;
        /// <summary>
        /// returns a matrix full of raised pins.
        /// </summary>
        /// <returns>a matrix full of raised pins in the dimension of the device.</returns>
        public bool[,] GetFullSetMatrix()
        {
            if (_fullMatrix == null || (Device != null && (_fullMatrix.GetLength(0) != Device.DeviceSizeY || _fullMatrix.GetLength(1) != Device.DeviceSizeX)))
            {
                int rows = Device.DeviceSizeY;
                int cols = Device.DeviceSizeX;
                _fullMatrix = new bool[rows, cols];
                Parallel.For(0, rows, i =>
                {
                    //Parallel.For(0, cols, j => { try { _fullMatrix[i, j] = true; } catch { } });
                    for (int j = 0; j < cols; j++) { try { _fullMatrix[i, j] = true; } catch { } }
                });
            }
            return _fullMatrix;
        }

        #endregion

        #region Util

        static Dictionary<int, BrailleIO_AdditionalButton> combineAdditionalButtonLists(Dictionary<int, BrailleIO_AdditionalButton> dict, BrailleIO_AdditionalButton[] arr)
        {
            Dictionary<int, BrailleIO_AdditionalButton> result = dict;
            if (dict != null)
            {
                if (arr != null && arr.Length > 0)
                {
                    for (int i = 0; i < arr.Length; i++)
                    {
                        if (dict.ContainsKey(i) && dict[i] != BrailleIO_AdditionalButton.None)
                        {
                            result[i] = dict[i] | arr[i];
                        }
                        else
                        {
                            if (arr[i] != BrailleIO_AdditionalButton.None)
                                result.Add(i, arr[i]);
                        }
                    }
                }
            }

            return result;
        }
        static Dictionary<int, BrailleIO_AdditionalButton> combineAdditionalButtonLists(
                    Dictionary<int, BrailleIO_AdditionalButton> dict,
                    Dictionary<int, BrailleIO_AdditionalButton> dict2)
        {
            Dictionary<int, BrailleIO_AdditionalButton> result = dict;
            if (dict != null)
            {
                if (dict2 != null && dict2.Count > 0)
                {
                    // for (int i = 0; i < arr.Length; i++)
                    foreach (var kvPair in dict2)
                    {
                        var i = kvPair.Key;
                        var v = kvPair.Value;

                        if (dict.ContainsKey(i))
                        {
                            result[i] = dict[i] | v;
                        }
                        else
                        {
                            if (v != BrailleIO_AdditionalButton.None)
                                result.Add(i, v);
                        }
                    }
                }
            }
            return result;
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            try
            {
                Disconnect();

            }
            catch { }
        }

        #endregion
    }

}